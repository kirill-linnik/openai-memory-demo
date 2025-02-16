using Backend.Models;
using Backend.Models.Files;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using System.Text.Json;

namespace Backend.Services;

public sealed class ChatCompletionService(
    Kernel kernel,
    AzureSearchEmbedService azureSearchEmbedService,
    UserService userService,
    UserRequestService userRequestService,
    ILogger<ChatCompletionService> logger)
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

        // step 0
        // get new information about user and case from the last message
        await UpdateUserProfileAndRequest(question, chat, user, userRequest, cancellationToken);

        // step 1
        // get search query and user language
        var (query, userLanguage) = await GetQueryAndLanguageAsync(question, chat, user, userRequest, cancellationToken);



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
        var (ans, thoughts) = await GenerateAnswerAndThoughtsAsync(question, chat, user, userRequest, query, userLanguage, documentContents, cancellationToken);


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

    private async Task UpdateUserProfileAndRequest(string question, IChatCompletionService chat, User user, UserRequest userRequest, CancellationToken cancellationToken)
    {
        var userDataChat = new ChatHistory(
@$"You are an assistant for analyzing user message. Your job is to find update for user profiler and their request.

Always summarize the user profile and user request.

For user profile, use everything that user has said about themselves. For example, if user says ""I like France"", you should update user profile to include this information: ""User likes France""

For user request, collect everything that user wants you to do and details they add about the request. For example, if user says ""I want to book a vacation"", you should update user request to include this information: ""User wants to book a vacation""

Current user profile: ""{user.Profile}""
Current user request: ""{userRequest.Content}""

User provides new information in the message.

Always reply in English. If needed, translate reply into English. 

Never answer to user question. Only update user profile and user request.

You answer needs to be a JSON object with the following format.
{{
    ""profile"": // updated user profile. Explain everything you know about user. If no new information, put information you already know.
    ""request"": // updated user request. Explain everything user wants you to do. If no new information, put information you already know.
}}

If the response is not in JSON, please retry and provide the correct JSON structure.");

        if (userRequest.LastAssistantResponse != null)
        {
            userDataChat.AddAssistantMessage(userRequest.LastAssistantResponse);
        }
        userDataChat.AddUserMessage(question);
        var userDataResult = await chat.GetChatMessageContentAsync(userDataChat, cancellationToken: cancellationToken);
        var userDataJson = userDataResult.Content ?? throw new InvalidOperationException("Failed to get user data");

        logger.LogInformation("User data: {userDataJson}", userDataJson);

        var userDataObject = JsonSerializer.Deserialize<JsonElement>(userDataJson);
        var updatedUserProfile = userDataObject.GetProperty("profile").GetString() ?? throw new InvalidOperationException("Failed to get profile");
        var updatedUserRequest = userDataObject.GetProperty("request").GetString() ?? throw new InvalidOperationException("Failed to get request");
        user.Profile = updatedUserProfile;
        userRequest.Content = updatedUserRequest;
    }

    private async Task<(string, string)> GetQueryAndLanguageAsync(string question, IChatCompletionService chat, User user, UserRequest userRequest, CancellationToken cancellationToken)
    {
        var languageValues = Enum.GetValues(typeof(SupportedLanguages)).Cast<SupportedLanguages>();
        var queryLanguages = languageValues.Select(x =>
        {
            var language = x.ToString();
            return @$"""query.{language}"": // the search query translated to {language}. ";
        });
        var languages = string.Join(", ", languageValues);

        var getQueryChat = new ChatHistory(
@$"You are a helpful AI assistant, generate search query for followup question and determine user language. 

## User Profile ##
{user.Profile}
{userRequest.Content}
## End User Profile ##

Make use of User Profile for generating search query. Make your respond simple and precise. Query examples:

Northwind Health Plus AND standard plan.
standard plan AND dental AND employee benefit.

Remember this search query. Later you will need to translate it to other languages. You can translate everything, except AND.

You answer needs to be a JSON object with the following format.
{{
    {string.Join("\n\t", queryLanguages)}
    ""language"": // the language of the user message. e.g. English, Estonian, Russian etc.
}}

Never answer to user question. Only generate search query and determine user language and reply in required JSON structure.

If the response is not in JSON, please retry and provide the correct JSON structure.");

        getQueryChat.AddUserMessage(question);
        var result = await chat.GetChatMessageContentAsync(getQueryChat, cancellationToken: cancellationToken);

        var queryJson = result.Content ?? throw new InvalidOperationException("Failed to get search query");

        logger.LogInformation("Query JSON: {queryJson}", queryJson);

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

        return (query, userLanguage);
    }

    private async Task<(string, string)> GenerateAnswerAndThoughtsAsync(string question, IChatCompletionService chat, User user, UserRequest userRequest, string query, string userLanguage, string documentContents, CancellationToken cancellationToken)
    {
        var answerChat = new ChatHistory(
@$"You are an assistant representing DevClub Tours. You help our users and potential customers with their questions. Be brief in your answers.
## Source ##
{documentContents}
## End Source ##

## User Profile ##
User name is {user.Name}
User profile is {user.Profile}
## End User Profile ##

## User Request ##
{userRequest.Content}
## End User Request ##

Use User Profile to undertand user better and User Request to help user with their request. Use Source to provide information to user.

For reference, today is: {DateTime.Now:yyyy-MM-dd}. You can only offer details that are available in the sources provided above. If user is asking about destinations you don't know, tell them you don't know.
If you don't know what to answer, ask user to provide more information about their wishes, interests, preferred destinations.

Your reply should be only in {userLanguage} language. Translate to {userLanguage} if needed.

You answer needs to be a JSON object with the following format:
{{
    ""answer"": // the answer to the question, add a source reference to the end of each sentence. e.g. Apple is a fruit [reference1.pdf][reference2.pdf]. If no source available, ask user to provide more information.
    ""thoughts"": // brief thoughts on how you came up with the answer, e.g. what sources you used, how did you use user profile and user request, what you thought about, etc. 
}}

If the response is not in JSON, please retry and provide the correct JSON structure.");


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

        logger.LogInformation("Answer: {answer}", answerJson);

        var ans = "I don't know";
        var thoughts = "I don't know";
        try
        {
            var answerObject = JsonSerializer.Deserialize<JsonElement>(answerJson);
            ans = answerObject.GetProperty("answer").GetString() ?? throw new InvalidOperationException("Failed to get answer");
            thoughts = answerObject.GetProperty("thoughts").GetString() ?? throw new InvalidOperationException("Failed to get thoughts");
        }
        catch (JsonException ex)
        {
            ans = answerJson;
            logger.LogWarning(ex, "Failed to parse answer JSON, took the whole answer instead");
        }

        return (ans, thoughts);
    }

}
