using System.Text.Json;
using Aspire.SK.RAG.ApiService.Plugins;
using Aspire.SK.RAG.ApiService.Services;
using Aspire.SK.RAG.Models;
using Azure.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

var builder = WebApplication.CreateBuilder(args);

AppContext.SetSwitch("Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnosticsSensitive", true);

builder.AddServiceDefaults();

builder.AddAzureOpenAIClient("azureOpenAI", configureSettings: settings =>
{
    settings.Credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions(){ TenantId = "16b3c013-d300-468d-ac64-7eda0820b6d3" });
});
// builder.AddAzureOpenAIClient("azureOpenAI");
// builder.AddAzureSearchClient("search");
builder.AddKeyedAzureCosmosContainer("conversations", configureClientOptions: (option) => { option.Serializer = new CosmosSystemTextJsonSerializer(); });
builder.Services.AddSingleton<IConversationRepository, CosmosConversationRepository>();

builder.Services.AddOpenApi();

builder.Services.AddSingleton<RAGPlugin>();
builder.Services.AddKernel().AddAzureOpenAIChatCompletion("gpt-4o");

builder.Services.AddSingleton(builder =>
{
    var _settings = new OpenAIPromptExecutionSettings()
    {
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
        Temperature = 0.1,
        MaxTokens = 500,
    };
    var agent = new ChatCompletionAgent
    {
        Name = "RAGAgent",
        Instructions = "You are a helpful assistant. Answer the user's questions to the best of your ability using your tools.",
        Kernel = builder.GetRequiredService<Kernel>(),
        Arguments = new(_settings)
    };
    agent.Kernel.Plugins.AddFromObject(builder.GetRequiredService<RAGPlugin>());

    return agent;
});

builder.Services.AddSingleton<IChatHistoryReducer>(provider =>
{
    var kernel = provider.GetRequiredService<Kernel>();
    var chatCompletion = kernel.Services.GetRequiredService<IChatCompletionService>();

    return new ChatHistorySummarizationReducer(chatCompletion, 1, 5);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/agent/chat/stream", async (
    [FromServices] ChatCompletionAgent agent,
    [FromServices]IConversationRepository? conversationRepository,
    [FromServices]IChatHistoryReducer? chatHistoryReducer,
    [FromServices] ILogger<Program> logger,
    HttpResponse response,
    [FromBody]AIChatRequest request) =>
{
    //retrieve the conversation thread based on the session state
    AgentThread thread = await agent.GetThread(request.SessionState, conversationRepository, logger);

    if (request.Messages.Count == 0)
    {
        logger.LogInformation("First message from user, sending greeting.");

        AIChatCompletionDelta delta = new(
            new AIChatMessageDelta() { Content = $"Hi, I'm {agent.Name}" }
        )
        {
            SessionState = thread.Id
        };

        await response.WriteAsync($"{JsonSerializer.Serialize(delta)}\r\n");
        await response.Body.FlushAsync();
        return;
    }

    var lastMessage = request.Messages.LastOrDefault();

    var agentThread = new ChatHistoryAgentThread();

    response.Headers.Append("Content-Type", "application/jsonl");
    await foreach (var delta in agent.InvokeStreamingAsync(new ChatMessageContent(AuthorRole.User, lastMessage.Content), thread))
    {
        await response.WriteAsync($"{JsonSerializer.Serialize(new AIChatCompletionDelta(new AIChatMessageDelta() { Content = delta.Message.Content }))}\r\n");
        await response.Body.FlushAsync();
    }
    
    await agent.SaveThread(thread, conversationRepository, chatHistoryReducer, logger);
})
.WithName("ChatStreamAgent");

app.MapDefaultEndpoints();

app.Run();