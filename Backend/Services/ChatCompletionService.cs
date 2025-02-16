using Backend.Models;
using Backend.Models.Files;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using System.Text.Json;

namespace Backend.Services;

public sealed class ChatCompletionService(Kernel kernel, AzureSearchEmbedService azureSearchEmbedService, UserService userService, UserRequestService userRequestService)
{
    public async Task<ResponseChoice> ProcessRequestAsync(ChatRequest chatRequest, CancellationToken cancellationToken)
    {
        var chat = kernel.GetRequiredService<IChatCompletionService>();
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var embedding = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var user = userService.GetUser();
        var userRequest = userRequestService.GetUserRequest(chatRequest.RequestId);


        var question = chatRequest.Message;

        var languageValues = Enum.GetValues(typeof(SupportedLanguages)).Cast<SupportedLanguages>();
        var queryLanguages = languageValues.Select(x =>
        {
            var language = x.ToString();
            return @$"""query.{language}"": // the search query translated to {language}. ";
        });
        var languages = string.Join(", ", languageValues);

        // step 1
        // use llm to get query
        var getQueryChat = new ChatHistory(
@$"You are a helpful AI assistant, generate search query for followup question.
Make your respond simple and precise. Query examples:

Northwind Health Plus AND standard plan.
standard plan AND dental AND employee benefit.

Remember this search query. Later you will need to translate it to other languages. You can translate everything, except AND.

You answer needs to be a JSON object with the following format.
{{
    {string.Join("\n\t", queryLanguages)}
    ""language"": // the language of the user message. e.g. English, Estonian, Russian etc.
}}

If the response is not in JSON format, please retry and provide the correct format.");

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

        // step 1.5
        // get new information about user and case from the last message

        var userDataChat = new ChatHistory(
@$"You are an assistant for analyzing user message. Your job is to find update for user profiler and their request.

Always append new information to the current information. You cannot remove current information.

For user profile, use everything that user has said about themselves. For example, if user says ""I like France"", you should update user profile to include this information: ""User likes France""

For user request, collect everything that user wants you to do and details they add about the request. For example, if user says ""I want to book a vacation"", you should update user request to include this information: ""User wants to book a vacation""

Current user profile: ""{user.Profile}""
Current user request: ""{userRequest.Content}""

User provides new information in the message.

Always reply in English. If needed, translate reply into English.

You answer needs to be a JSON object with the following format.
{{
    ""profile"": // updated user profile. Explain everything you know about user. If no new information, put information you already know.
    ""request"": // updated user request. Explain everything user wants you to do. If no new information, put information you already know.
}}

If the response is not in JSON format, please retry and provide the correct format.");

        if (userRequest.LastAssistantResponse != null)
        {
            userDataChat.AddAssistantMessage(userRequest.LastAssistantResponse);
        }
        userDataChat.AddUserMessage(question);
        var userDataResult = await chat.GetChatMessageContentAsync(userDataChat, cancellationToken: cancellationToken);
        var userDataJson = userDataResult.Content ?? throw new InvalidOperationException("Failed to get user data");
        var userDataObject = JsonSerializer.Deserialize<JsonElement>(userDataJson);
        var updatedUserProfile = userDataObject.GetProperty("profile").GetString() ?? throw new InvalidOperationException("Failed to get profile");
        var updatedUserRequest = userDataObject.GetProperty("request").GetString() ?? throw new InvalidOperationException("Failed to get request");
        user.Profile = updatedUserProfile;
        userRequest.Content = updatedUserRequest;

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
@$"You are an assistant who helps users with their questions. Be brief in your answers.
## Source ##
{documentContents}
## End ##

## User Profile ##
{updatedUserProfile}
## End ##

## User Request ##
{updatedUserRequest}
## End ##

Use User Profile to undertand user better and User Request to help user with their request. Use Source to provide information to user.

For reference, today is: {DateTime.Now:yyyy-MM-dd}.

Your reply should be only in {userLanguage} language. Translate to {userLanguage} if needed.

You answer needs to be a JSON object with the following format:
{{
    ""answer"": // the answer to the question, add a source reference to the end of each sentence. e.g. Apple is a fruit [reference1.pdf][reference2.pdf]. If no source available, put the answer as I don't know.
    ""thoughts"": // brief thoughts on how you came up with the answer, e.g. what sources you used, how did you use user profile and user request, what you thought about, etc. 
}}

If the response is not in JSON format, please retry and provide the correct format.");


        if (userRequest.LastAssistantResponse != null)
        {
            answerChat.AddAssistantMessage(userRequest.LastAssistantResponse);
        }
        answerChat.AddUserMessage(question);

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

        userRequest.LastAssistantResponse = ans;

        return choice;
    }
}
