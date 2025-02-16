using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Backend.Models.Files;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace Backend.Services;

public sealed class AzureBlobStorageService
{
    private readonly BlobServiceClient _blobService;
    private readonly BlobContainerClient _container;
    private readonly BlobContainerClient _corpusContainer;
    private readonly AzureSearchEmbedService _embeddingService;
    public AzureBlobStorageService(BlobServiceClient blobService, AzureSearchEmbedService embeddingService)
    {
        _blobService = blobService;
        _container = _blobService.GetBlobContainerClient(Environment.GetEnvironmentVariable("AZURE_STORAGE_CONTAINER_NAME"));
        _container.CreateIfNotExists();
        _corpusContainer = _blobService.GetBlobContainerClient("corpus");
        _corpusContainer.CreateIfNotExists();
        _embeddingService = embeddingService;
    }

    internal async Task<UploadDocumentsResponse> UploadDocumentsAsync(IEnumerable<IFormFile> files, CancellationToken cancellationToken)
    {
        try
        {
            List<string> uploadedFiles = [];
            foreach (var file in files)
            {
                var fileName = file.FileName;

                await using var stream = file.OpenReadStream();

                // if file is an image (end with .png, .jpg, .jpeg, .gif), upload it to blob storage
                if (Path.GetExtension(fileName).ToLower() is ".png" or ".jpg" or ".jpeg" or ".gif")
                {
                    var blobName = BlobNameFromFilePage(fileName);
                    var blobClient = _container.GetBlobClient(blobName);
                    if (await blobClient.ExistsAsync(cancellationToken))
                    {
                        continue;
                    }

                    var url = blobClient.Uri.AbsoluteUri;
                    await using var fileStream = file.OpenReadStream();
                    await blobClient.UploadAsync(fileStream, new BlobHttpHeaders
                    {
                        ContentType = "image"
                    }, cancellationToken: cancellationToken);
                    uploadedFiles.Add(blobName);
                }
                else if (Path.GetExtension(fileName).ToLower() is ".pdf")
                {
                    using var documents = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
                    for (int i = 0; i < documents.PageCount; i++)
                    {
                        var documentName = BlobNameFromFilePage(fileName, i);
                        var blobClient = _container.GetBlobClient(documentName);
                        if (await blobClient.ExistsAsync(cancellationToken))
                        {
                            continue;
                        }

                        var tempFileName = Path.GetTempFileName();

                        try
                        {
                            using var document = new PdfDocument();
                            document.AddPage(documents.Pages[i]);
                            document.Save(tempFileName);

                            await using var tempStream = File.OpenRead(tempFileName);
                            await blobClient.UploadAsync(tempStream, new BlobHttpHeaders
                            {
                                ContentType = "application/pdf"
                            }, cancellationToken: cancellationToken);

                            uploadedFiles.Add(documentName);
                            var result = await _embeddingService.EmbedPDFBlobAsync(_corpusContainer, tempStream, fileName, documentName);
                            await UpdateBlobMetadataAsync(blobClient, result);
                        }
                        finally
                        {
                            File.Delete(tempFileName);
                        }
                    }
                }
                else
                {
                    var blobHttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = GetContentType(fileName)
                    };
                    var blobName = BlobNameFromFilePage(fileName);
                    var blobClient = _container.GetBlobClient(blobName);
                    await blobClient.UploadAsync(stream, blobHttpHeaders);
                    var result = await _embeddingService.EmbedPDFBlobAsync(_corpusContainer, stream, fileName, blobName);
                    await UpdateBlobMetadataAsync(blobClient, result);
                    uploadedFiles.Add(fileName);
                }
            }

            if (uploadedFiles.Count is 0)
            {
                return UploadDocumentsResponse.FromError("""
                    No files were uploaded. Either the files already exist or the files are not PDFs or images.
                    """);
            }

            return new UploadDocumentsResponse([.. uploadedFiles]);
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
        {
            return UploadDocumentsResponse.FromError(ex.ToString());
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }

    private static async Task UpdateBlobMetadataAsync(BlobClient blobClient, bool result)
    {
        var status = result switch
        {
            true => DocumentProcessingStatus.Succeeded,
            _ => DocumentProcessingStatus.Failed
        };
        await blobClient.SetMetadataAsync(new Dictionary<string, string>
        {
            { nameof(DocumentProcessingStatus), status.ToString() }
        });
    }

    internal async Task<IEnumerable<DocumentResponse>> GetDocumentsAsync(CancellationToken cancellationToken)
    {
        var documents = new List<DocumentResponse>();
        await foreach (var blob in _container.GetBlobsAsync(cancellationToken: cancellationToken))
        {
            if (blob is not null and { Deleted: false })
            {
                var blobClient = _container.GetBlobClient(blob.Name);
                var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);

                var metadata = properties.Value.Metadata;

                var documentProcessingStatus = GetMetadataEnumOrDefault<DocumentProcessingStatus>(
                    metadata, nameof(DocumentProcessingStatus), DocumentProcessingStatus.NotProcessed);

                documents.Add(
                    new DocumentResponse(
                        blob.Name,
                        properties.Value.ContentType,
                        properties.Value.ContentLength,
                        properties.Value.LastModified,
                        documentProcessingStatus
                    ));
            }
        }
        return documents;
    }

    private static TEnum GetMetadataEnumOrDefault<TEnum>(
                    IDictionary<string, string> metadata,
                    string key,
                    TEnum @default) where TEnum : struct => metadata.TryGetValue(key, out var value)
                        && Enum.TryParse<TEnum>(value, out var status)
                            ? status
                            : @default;

    private static string BlobNameFromFilePage(string filename, int page = 0) =>
        Path.GetExtension(filename).ToLower() is ".pdf"
            ? $"{Path.GetFileNameWithoutExtension(filename)}-{page}.pdf"
            : Path.GetFileName(filename);

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",

            _ => "application/octet-stream"
        };
    }
}
