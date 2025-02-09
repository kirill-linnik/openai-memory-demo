using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Backend.Models.Files;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Backend.Services;

public sealed partial class AzureSearchEmbedService(
    Kernel kernel,
    SearchClient searchClient,
    SearchIndexClient searchIndexClient,
    DocumentAnalysisClient documentAnalysisClient,
    ILogger<AzureSearchEmbedService> logger)
{

    private static Regex MatchInSetRegex() => new("[^0-9a-zA-Z_-]");

    public async Task<bool> EmbedPDFBlobAsync(BlobContainerClient corpusContainerClient, Stream pdfBlobStream, string fileName, string blobName)
    {
        try
        {
            pdfBlobStream.Position = 0;
            var searchIndexName = searchClient.IndexName;
            await EnsureSearchIndexAsync(searchIndexName);
            logger.LogInformation($"Embedding blob '{blobName}'");
            var pageMap = await GetDocumentTextAsync(pdfBlobStream, blobName);

            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(blobName);

            // Create corpus from page map and upload to blob
            // Corpus name format: fileName-{page}.txt
            foreach (var page in pageMap)
            {
                var corpusName = $"{fileNameWithoutExtension}-{page.Index}.txt";
                await UploadCorpusAsync(corpusContainerClient, corpusName, page.Text);
            }

            var sections = CreateSections(pageMap, fileName, blobName);

            logger.LogInformation("""
                Indexing sections from '{BlobName}' into search index '{SearchIndexName}'
                """,
                    blobName,
                    searchIndexName);

            await IndexSectionsAsync(sections);

            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception, "Failed to embed blob '{BlobName}'", blobName);

            throw;
        }
    }

    public async Task CreateSearchIndexAsync(string searchIndexName, CancellationToken ct = default)
    {
        string vectorSearchConfigName = "my-vector-config";
        string vectorSearchProfile = "my-vector-profile";
        var index = new SearchIndex(searchIndexName)
        {
            VectorSearch = new()
            {
                Algorithms =
                {
                    new HnswAlgorithmConfiguration(vectorSearchConfigName)
                },
                Profiles =
                {
                    new VectorSearchProfile(vectorSearchProfile, vectorSearchConfigName)
                }
            },
            Fields =
            {
                new SimpleField("id", SearchFieldDataType.String) { IsKey = true },
                new SearchableField("content") { AnalyzerName = LexicalAnalyzerName.EnMicrosoft },
                new SimpleField("category", SearchFieldDataType.String) { IsFacetable = true },
                new SimpleField("sourcepage", SearchFieldDataType.String) { IsFacetable = true },
                new SimpleField("sourcefile", SearchFieldDataType.String) { IsFacetable = true },
                new SearchField("embedding", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    VectorSearchDimensions = 1536,
                    IsSearchable = true,
                    VectorSearchProfileName = vectorSearchProfile,
                }
            },
            SemanticSearch = new()
            {
                Configurations =
                {
                    new SemanticConfiguration("default", new()
                    {
                        ContentFields =
                        {
                            new SemanticField("content")
                        }
                    })
                }
            }
        };

        logger.LogInformation(
            "Creating '{searchIndexName}' search index", searchIndexName);

        await searchIndexClient.CreateIndexAsync(index);
    }

    public async Task EnsureSearchIndexAsync(string searchIndexName, CancellationToken ct = default)
    {
        var indexNames = searchIndexClient.GetIndexNamesAsync();
        await foreach (var page in indexNames.AsPages())
        {
            if (page.Values.Any(indexName => indexName == searchIndexName))
            {
                logger.LogWarning(
                    "Search index '{SearchIndexName}' already exists", searchIndexName);
                return;
            }
        }

        await CreateSearchIndexAsync(searchIndexName, ct);
    }

    public async Task<IReadOnlyList<PageDetail>> GetDocumentTextAsync(Stream blobStream, string blobName)
    {
        logger.LogInformation(
            "Extracting text from '{Blob}' using Azure AI Document Intelligence", blobName);

        using var ms = new MemoryStream();
        blobStream.CopyTo(ms);
        ms.Position = 0;
        AnalyzeDocumentOperation operation = await documentAnalysisClient.AnalyzeDocumentAsync(
        WaitUntil.Started, "prebuilt-layout", ms);

        var offset = 0;
        List<PageDetail> pageMap = [];

        var results = await operation.WaitForCompletionAsync();
        var pages = results.Value.Pages;
        for (var i = 0; i < pages.Count; i++)
        {
            IReadOnlyList<DocumentTable> tablesOnPage =
                results.Value.Tables.Where(t => t.BoundingRegions[0].PageNumber == i + 1).ToList();

            // Mark all positions of the table spans in the page
            int pageIndex = pages[i].Spans[0].Index;
            int pageLength = pages[i].Spans[0].Length;
            int[] tableChars = Enumerable.Repeat(-1, pageLength).ToArray();
            for (var tableId = 0; tableId < tablesOnPage.Count; tableId++)
            {
                foreach (DocumentSpan span in tablesOnPage[tableId].Spans)
                {
                    // Replace all table spans with "tableId" in tableChars array
                    for (var j = 0; j < span.Length; j++)
                    {
                        int index = span.Index - pageIndex + j;
                        if (index >= 0 && index < pageLength)
                        {
                            tableChars[index] = tableId;
                        }
                    }
                }
            }

            // Build page text by replacing characters in table spans with table HTML
            StringBuilder pageText = new();
            HashSet<int> addedTables = [];
            for (int j = 0; j < tableChars.Length; j++)
            {
                if (tableChars[j] == -1)
                {
                    pageText.Append(results.Value.Content[pageIndex + j]);
                }
                else if (!addedTables.Contains(tableChars[j]))
                {
                    pageText.Append(TableToHtml(tablesOnPage[tableChars[j]]));
                    addedTables.Add(tableChars[j]);
                }
            }

            pageText.Append(' ');
            pageMap.Add(new PageDetail(i, offset, pageText.ToString()));
            offset += pageText.Length;
        }
        return pageMap.AsReadOnly();
    }

    private static string TableToHtml(DocumentTable table)
    {
        var tableHtml = new StringBuilder("<table>");
        var rows = new List<DocumentTableCell>[table.RowCount];
        for (int i = 0; i < table.RowCount; i++)
        {
            rows[i] =
            [
                .. table.Cells.Where(c => c.RowIndex == i)
                                .OrderBy(c => c.ColumnIndex)
,
            ];
        }

        foreach (var rowCells in rows)
        {
            tableHtml.Append("<tr>");
            foreach (DocumentTableCell cell in rowCells)
            {
                var tag = (cell.Kind == "columnHeader" || cell.Kind == "rowHeader") ? "th" : "td";
                var cellSpans = string.Empty;
                if (cell.ColumnSpan > 1)
                {
                    cellSpans += $" colSpan='{cell.ColumnSpan}'";
                }

                if (cell.RowSpan > 1)
                {
                    cellSpans += $" rowSpan='{cell.RowSpan}'";
                }

                tableHtml.AppendFormat(
                    "<{0}{1}>{2}</{0}>", tag, cellSpans, WebUtility.HtmlEncode(cell.Content));
            }

            tableHtml.Append("</tr>");
        }

        tableHtml.Append("</table>");

        return tableHtml.ToString();
    }

    private async Task UploadCorpusAsync(BlobContainerClient corpusContainerClient, string corpusBlobName, string text)
    {
        var blobClient = corpusContainerClient.GetBlobClient(corpusBlobName);
        if (await blobClient.ExistsAsync())
        {
            return;
        }

        logger.LogInformation("Uploading corpus '{CorpusBlobName}'", corpusBlobName);

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        await blobClient.UploadAsync(stream, new BlobHttpHeaders
        {
            ContentType = "text/plain"
        });
    }

    public IEnumerable<Section> CreateSections(
        IReadOnlyList<PageDetail> pageMap,
        string fileName,
        string blobName)
    {
        const int MaxSectionLength = 1_000;
        const int SentenceSearchLimit = 100;
        const int SectionOverlap = 100;

        var sentenceEndings = new[] { '.', '!', '?' };
        var wordBreaks = new[] { ',', ';', ':', ' ', '(', ')', '[', ']', '{', '}', '\t', '\n' };
        var allText = string.Concat(pageMap.Select(p => p.Text));
        var length = allText.Length;
        var start = 0;
        var end = length;

        logger.LogInformation("Splitting '{BlobName}' into sections", blobName);

        while (start + SectionOverlap < length)
        {
            var lastWord = -1;
            end = start + MaxSectionLength;

            if (end > length)
            {
                end = length;
            }
            else
            {
                // Try to find the end of the sentence
                while (end < length && (end - start - MaxSectionLength) < SentenceSearchLimit && !sentenceEndings.Contains(allText[end]))
                {
                    if (wordBreaks.Contains(allText[end]))
                    {
                        lastWord = end;
                    }
                    end++;
                }

                if (end < length && !sentenceEndings.Contains(allText[end]) && lastWord > 0)
                {
                    end = lastWord; // Fall back to at least keeping a whole word
                }
            }

            if (end < length)
            {
                end++;
            }

            // Try to find the start of the sentence or at least a whole word boundary
            lastWord = -1;
            while (start > 0 && start > end - MaxSectionLength -
                (2 * SentenceSearchLimit) && !sentenceEndings.Contains(allText[start]))
            {
                if (wordBreaks.Contains(allText[start]))
                {
                    lastWord = start;
                }
                start--;
            }

            if (!sentenceEndings.Contains(allText[start]) && lastWord > 0)
            {
                start = lastWord;
            }
            if (start > 0)
            {
                start++;
            }

            var sectionText = allText[start..end];

            yield return new Section(
                Id: MatchInSetRegex().Replace($"{blobName}-{start}", "_").TrimStart('_'),
                Content: sectionText,
                SourcePage: blobName,
                SourceFile: fileName);

            var lastTableStart = sectionText.LastIndexOf("<table", StringComparison.Ordinal);
            if (lastTableStart > 2 * SentenceSearchLimit && lastTableStart > sectionText.LastIndexOf("</table", StringComparison.Ordinal))
            {
                // If the section ends with an unclosed table, we need to start the next section with the table.
                // If table starts inside SentenceSearchLimit, we ignore it, as that will cause an infinite loop for tables longer than MaxSectionLength
                // If last table starts inside SectionOverlap, keep overlapping
                if (logger.IsEnabled(LogLevel.Warning) is true)
                {
                    logger.LogWarning("""
                        Section ends with unclosed table, starting next section with the
                        table at page {Offset} offset {Start} table start {LastTableStart}
                        """,
                        FindPage(pageMap, start),
                        start,
                        lastTableStart);
                }

                start = Math.Min(end - SectionOverlap, start + lastTableStart);
            }
            else
            {
                start = end - SectionOverlap;
            }
        }

        if (start + SectionOverlap < end)
        {
            yield return new Section(
                Id: MatchInSetRegex().Replace($"{blobName}-{start}", "_").TrimStart('_'),
                Content: allText[start..end],
                SourcePage: BlobNameFromFilePage(blobName, FindPage(pageMap, start)),
                SourceFile: blobName);
        }
    }

    public async Task<IEnumerable<SupportingContentRecord>> QueryDocumentsAsync(
        string? query = null,
        float[]? embedding = null,
        CancellationToken cancellationToken = default)
    {
        if (query is null && embedding is null)
        {
            throw new ArgumentException("Either query or embedding must be provided");
        }

        var top = 3;

        var searchOptions = new SearchOptions
        {
            Size = top,
        };

        if (embedding != null)
        {
            var vectorQuery = new VectorizedQuery(embedding)
            {
                KNearestNeighborsCount = top,
            };
            vectorQuery.Fields.Add("embedding");
            searchOptions.VectorSearch = new();
            searchOptions.VectorSearch.Queries.Add(vectorQuery);
        }

        var searchResultResponse = await searchClient.SearchAsync<SearchDocument>(
            query, searchOptions, cancellationToken);
        if (searchResultResponse.Value is null)
        {
            throw new InvalidOperationException("fail to get search result");
        }

        SearchResults<SearchDocument> searchResult = searchResultResponse.Value;

        // Assemble sources here.
        // Example output for each SearchDocument:
        // {
        //   "@search.score": 11.65396,
        //   "id": "Northwind_Standard_Benefits_Details_pdf-60",
        //   "content": "x-ray, lab, or imaging service, you will likely be responsible for paying a copayment or coinsurance. The exact amount you will be required to pay will depend on the type of service you receive. You can use the Northwind app or website to look up the cost of a particular service before you receive it.\nIn some cases, the Northwind Standard plan may exclude certain diagnostic x-ray, lab, and imaging services. For example, the plan does not cover any services related to cosmetic treatments or procedures. Additionally, the plan does not cover any services for which no diagnosis is provided.\nIt’s important to note that the Northwind Standard plan does not cover any services related to emergency care. This includes diagnostic x-ray, lab, and imaging services that are needed to diagnose an emergency condition. If you have an emergency condition, you will need to seek care at an emergency room or urgent care facility.\nFinally, if you receive diagnostic x-ray, lab, or imaging services from an out-of-network provider, you may be required to pay the full cost of the service. To ensure that you are receiving services from an in-network provider, you can use the Northwind provider search ",
        //   "category": null,
        //   "sourcepage": "Northwind_Standard_Benefits_Details-24.pdf",
        //   "sourcefile": "Northwind_Standard_Benefits_Details.pdf"
        // }
        var sb = new List<SupportingContentRecord>();
        foreach (var doc in searchResult.GetResults())
        {
            doc.Document.TryGetValue("sourcepage", out var sourcePageValue);
            string? contentValue;
            try
            {
                doc.Document.TryGetValue("content", out var value);
                contentValue = (string)value;
            }
            catch (ArgumentNullException)
            {
                contentValue = null;
            }

            if (sourcePageValue is string sourcePage && contentValue is string content)
            {
                content = content.Replace('\r', ' ').Replace('\n', ' ');
                sb.Add(new SupportingContentRecord(sourcePage, content));
            }
        }

        return sb;
    }

    private static int FindPage(IReadOnlyList<PageDetail> pageMap, int offset)
    {
        var length = pageMap.Count;
        for (var i = 0; i < length - 1; i++)
        {
            if (offset >= pageMap[i].Offset && offset < pageMap[i + 1].Offset)
            {
                return i;
            }
        }

        return length - 1;
    }

    private static string BlobNameFromFilePage(string blobName, int page = 0) => blobName;

    private async Task IndexSectionsAsync(IEnumerable<Section> sections)
    {
        var iteration = 0;
        var batch = new IndexDocumentsBatch<SearchDocument>();
        foreach (var section in sections)
        {
#pragma warning disable SKEXP0001
            var embeddings = await kernel.GetRequiredService<ITextEmbeddingGenerationService>().GenerateEmbeddingAsync(section.Content.Replace('\r', ' '));
            var embedding = embeddings.ToArray() ?? [];
            batch.Actions.Add(new IndexDocumentsAction<SearchDocument>(
                IndexActionType.MergeOrUpload,
                new SearchDocument
                {
                    ["id"] = section.Id,
                    ["content"] = section.Content,
                    ["category"] = section.Category,
                    ["sourcepage"] = section.SourcePage,
                    ["sourcefile"] = section.SourceFile,
                    ["embedding"] = embedding,
                }));

            iteration++;
            if (iteration % 1_000 is 0)
            {
                // Every one thousand documents, batch create.
                IndexDocumentsResult result = await searchClient.IndexDocumentsAsync(batch);
                int succeeded = result.Results.Count(r => r.Succeeded);
                if (logger.IsEnabled(LogLevel.Information) is true)
                {
                    logger.LogInformation("""
                        Indexed {Count} sections, {Succeeded} succeeded
                        """,
                        batch.Actions.Count,
                        succeeded);
                }

                batch = new();
            }
        }

        if (batch is { Actions.Count: > 0 })
        {
            // Any remaining documents, batch create.
            IndexDocumentsResult result = await searchClient.IndexDocumentsAsync(batch);
            int succeeded = result.Results.Count(r => r.Succeeded);
            if (logger.IsEnabled(LogLevel.Information) is true)
            {
                var message = $"Indexed {batch.Actions.Count} sections, {succeeded} succeeded";
                logger.LogInformation(message);
            }
        }
    }

}
