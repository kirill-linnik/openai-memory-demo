using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Storage.Blobs;
using Backend.Services;
using Microsoft.SemanticKernel;
using System.ClientModel;

namespace Backend.Extensions;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        services.AddSingleton<ChatCompletionService>();
        services.AddSingleton<AzureSearchEmbedService>();
        services.AddSingleton<AzureBlobStorageService>();
        services.AddSingleton<UserService>();
        services.AddSingleton<UserRequestService>();

        return services;
    }

    internal static IServiceCollection AddAzureServices(this IServiceCollection services)
    {
        var azureOpenAiServiceEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        ArgumentNullException.ThrowIfNullOrEmpty(azureOpenAiServiceEndpoint);
        var azureOpenAiServiceApiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
        ArgumentNullException.ThrowIfNullOrEmpty(azureOpenAiServiceApiKey);

        var azureSearchServiceEndpoint = Environment.GetEnvironmentVariable("AZURE_SEARCH_SERVICE_ENDPOINT");
        ArgumentNullException.ThrowIfNullOrEmpty(azureSearchServiceEndpoint);
        var azureSearchServiceApiKey = Environment.GetEnvironmentVariable("AZURE_SEARCH_SERVICE_API_KEY");
        ArgumentNullException.ThrowIfNullOrEmpty(azureSearchServiceApiKey);

        var openAiClient = new AzureOpenAIClient(new Uri(azureOpenAiServiceEndpoint), new ApiKeyCredential(azureOpenAiServiceApiKey));


        services.AddSingleton<BlobServiceClient>(sp =>
        {
            var blobConnectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");
            ArgumentNullException.ThrowIfNullOrWhiteSpace(blobConnectionString);
            return new BlobServiceClient(blobConnectionString);
        });

        services.AddSingleton<SearchClient>(_ =>
        {
            var azureSearchIndexName = Environment.GetEnvironmentVariable("AZURE_SEARCH_INDEX_NAME");
            ArgumentNullException.ThrowIfNullOrEmpty(azureSearchIndexName);
            return new SearchClient(new Uri(azureSearchServiceEndpoint), azureSearchIndexName, new AzureKeyCredential(azureSearchServiceApiKey));
        });

        services.AddSingleton<SearchIndexClient>(_ =>
        {
            return new SearchIndexClient(new Uri(azureSearchServiceEndpoint), new AzureKeyCredential(azureSearchServiceApiKey));
        });

        services.AddSingleton<DocumentAnalysisClient>(sp =>
        {

            var formRecognizerServiceEndpoint = Environment.GetEnvironmentVariable("AZURE_FORMRECOGNIZER_SERVICE_ENDPOINT");
            ArgumentNullException.ThrowIfNullOrWhiteSpace(formRecognizerServiceEndpoint);
            var formRecognizerServiceApiKey = Environment.GetEnvironmentVariable("AZURE_FORMRECOGNIZER_SERVICE_API_KEY");
            ArgumentNullException.ThrowIfNullOrWhiteSpace(formRecognizerServiceApiKey);

            var documentAnalysisClient = new DocumentAnalysisClient(
                new Uri(formRecognizerServiceEndpoint), new AzureKeyCredential(formRecognizerServiceApiKey));
            return documentAnalysisClient;
        });

        services.AddSingleton<Kernel>(sp =>
        {
            var kernelBuilder = Kernel.CreateBuilder();
            var deployedModelName = Environment.GetEnvironmentVariable("AZURE_OPENAI_CHATGPT_DEPLOYMENT");
            ArgumentNullException.ThrowIfNullOrWhiteSpace(deployedModelName);
            kernelBuilder = kernelBuilder.AddAzureOpenAIChatCompletion(deployedModelName, azureOpenAiServiceEndpoint, azureOpenAiServiceApiKey);

            var embeddingModelName = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT");
            ArgumentNullException.ThrowIfNullOrWhiteSpace(embeddingModelName);
#pragma warning disable SKEXP0010
            kernelBuilder = kernelBuilder.AddAzureOpenAITextEmbeddingGeneration(embeddingModelName, azureOpenAiServiceEndpoint, azureOpenAiServiceApiKey);

            return kernelBuilder.Build();
        });

        return services;
    }
}
