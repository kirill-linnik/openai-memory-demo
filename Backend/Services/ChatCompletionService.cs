﻿using Backend.Models;
using Backend.Models.Files;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using System.Text.Json;

namespace Backend.Services;

public sealed class ChatCompletionService(Kernel kernel, AzureSearchEmbedService azureSearchEmbedService)
{
    public async Task<ResponseChoice> ProcessRequestAsync(ChatRequest chatRequest, CancellationToken cancellationToken)
    {
        var chat = kernel.GetRequiredService<IChatCompletionService>();
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var embedding = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        var history = chatRequest.History;
        var question = history.LastOrDefault(m => m.IsUser)?.Content is { } userQuestion
            ? userQuestion
            : throw new InvalidOperationException("Use question is null");

        var languageValues = Enum.GetValues(typeof(SupportedLanguages)).Cast<SupportedLanguages>();
        var queryLanguages = languageValues.Select(x =>
        {
            var language = x.ToString();
            return @$"""query.{language}"": // the search query translated to {language}. ";
        });
        var languages = string.Join(", ", languageValues);

        // step 1
        // use llm to get query if retrieval mode is not vector
        var getQueryChat = new ChatHistory(@$"You are a helpful AI assistant, generate search query for followup question.
Make your respond simple and precise. Query examples:

Northwind Health Plus AND standard plan.
standard plan AND dental AND employee benefit.

Remember this search query. Later you will need to translate it to other languages. You can translate everything, except AND.

You answer needs to be a json object with the following format.
{{
    {string.Join("\n\t", queryLanguages)}
    ""language"": // the language of the user message. e.g. English, Estonian, Russian etc.
}}
");

        getQueryChat.AddUserMessage(question);
        var result = await chat.GetChatMessageContentAsync(getQueryChat, cancellationToken: cancellationToken);
        var queryJson = result.Content ?? throw new InvalidOperationException("Failed to get search query");
        var queryObject = JsonSerializer.Deserialize<JsonElement>(queryJson);
        var queryParts = languageValues.Select(x =>
        {
            var languageQuery = queryObject.GetProperty($"query.{x}").GetString();
            if (languageQuery is null)
            {
                throw new InvalidOperationException($"Failed to get query in {x}");
            }
            if (languageQuery.EndsWith("."))
            {
                languageQuery = languageQuery.Substring(0, languageQuery.Length - 1);
            }
            return "(" + languageQuery + ")";
        });
        var query = string.Join(" OR ", queryParts);
        var userLanguage = queryObject.GetProperty("language").GetString() ?? throw new InvalidOperationException("Failed to get language");

        // step 2
        // use query to search related docs
        var embeddings = (await embedding.GenerateEmbeddingAsync(question, cancellationToken: cancellationToken)).ToArray();

        var documentContentList = await azureSearchEmbedService.QueryDocumentsAsync(query, embeddings, cancellationToken);

        string documentContents = string.Empty;
        if (!documentContentList.Any())
        {
            documentContents = "no source available.";
        }
        else
        {
            documentContents = string.Join("\r", documentContentList.Select(x => $"{x.Title}:{x.Content}"));
        }

        // step 3
        // put together related docs and conversation history to generate answer
        var answerChat = new ChatHistory(
            "You are a system assistant who helps the company employees with their questions. Be brief in your answers");

        // add chat history
        foreach (var message in history)
        {
            if (message.IsUser)
            {
                answerChat.AddUserMessage(message.Content);
            }
            else
            {
                answerChat.AddAssistantMessage(message.Content);
            }
        }
        var prompt = @$" ## Source ##
{documentContents}
## End ##

Your reply should be only in {userLanguage} language. Translate to {userLanguage} if needed.

You answer needs to be a json object with the following format.
{{
    ""answer"": // the answer to the question, add a source reference to the end of each sentence. e.g. Apple is a fruit [reference1.pdf][reference2.pdf]. Escape all special characters. If no source available, put the answer as I don't know.
    ""thoughts"": // brief thoughts on how you came up with the answer, e.g. what sources you used, what you thought about, etc. Escape all special characters. 
}}";
        answerChat.AddUserMessage(prompt);

        var promptExecutingSetting = new OpenAIPromptExecutionSettings
        {
            MaxTokens = 1024,
            Temperature = 0.7,
            StopSequences = [],
        };

        // get answer
        var answer = await chat.GetChatMessageContentAsync(
                       answerChat,
                       promptExecutingSetting,
                       cancellationToken: cancellationToken);

        var answerJson = answer.Content ?? throw new InvalidOperationException("Failed to get search query");
        var answerObject = JsonSerializer.Deserialize<JsonElement>(answerJson);
        var ans = answerObject.GetProperty("answer").GetString() ?? throw new InvalidOperationException("Failed to get answer");
        var thoughts = answerObject.GetProperty("thoughts").GetString() ?? throw new InvalidOperationException("Failed to get thoughts");

        var responseMessage = new ResponseMessage("assistant", ans);
        var responseContext = new ResponseContext(
            DataPointsContent: documentContentList.Select(x => new SupportingContentRecord(x.Title, x.Content)).ToList(),
            Thoughts: thoughts);

        var choice = new ResponseChoice(
            Message: responseMessage,
            Context: responseContext);

        return choice;
    }
}
