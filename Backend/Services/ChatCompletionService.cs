using System.Threading;
using Azure.Core;
using Backend.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;

namespace Backend.Services;

public class ChatCompletionService
{
    private readonly Kernel _kernel;

    public ChatCompletionService()
    {
        var kernelBuilder = Kernel.CreateBuilder();
        var deployedModelName = Environment.GetEnvironmentVariable("AZURE_OPENAI_CHATGPT_DEPLOYMENT");
        ArgumentNullException.ThrowIfNullOrWhiteSpace(deployedModelName);
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        ArgumentNullException.ThrowIfNullOrWhiteSpace(endpoint);
        var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
        ArgumentNullException.ThrowIfNullOrWhiteSpace(apiKey);
        kernelBuilder = kernelBuilder.AddAzureOpenAIChatCompletion(deployedModelName, endpoint, apiKey);

        _kernel = kernelBuilder.Build();
    }

    public async Task<ResponseMessage> ProcessRequestAsync(ChatRequest chatRequest)
    {
        var chat = _kernel.GetRequiredService<IChatCompletionService>();
        var getQueryChat = new ChatHistory(@"You are a helpful AI assistant, try to help with any questions you can.
");

        for (var i = 0; i < chatRequest.History.Length; i++)
        {
            var message = chatRequest.History[i];
            if (message.Role == "user")
            {
                getQueryChat.AddUserMessage(message.Content);
            }
            else
            {
                getQueryChat.AddAssistantMessage(message.Content);
            }
        }
        var result = await chat.GetChatMessageContentAsync(getQueryChat);

        var query = result.Content ?? throw new InvalidOperationException("Failed to get search query");

        return new ResponseMessage("assistant", query);
    }
}
