using System.Text.Json;
using Aspire.SK.RAG.ApiService.Plugins;
using Aspire.SK.RAG.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

var builder = WebApplication.CreateBuilder(args);

AppContext.SetSwitch("Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnosticsSensitive", true);

builder.AddServiceDefaults();

builder.AddAzureOpenAIClient("azureOpenAI");
builder.AddAzureSearchClient("search");

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/agent/chat/stream", async (ChatCompletionAgent agent, HttpResponse response, AIChatRequest request) =>
{
    var history = new ChatHistory();
    foreach(var message in request.Messages)
    {
        var role = message.Role == AIChatRole.Assistant ? AuthorRole.Assistant : AuthorRole.User;
        history.Add(new ChatMessageContent(role, message.Content));
    }

    var agentThread = new ChatHistoryAgentThread();

    response.Headers.Append("Content-Type", "application/jsonl");
    await foreach(var delta in agent.InvokeStreamingAsync(history, agentThread))
    {
        await response.WriteAsync($"{JsonSerializer.Serialize(new AIChatCompletionDelta(new AIChatMessageDelta() { Content = delta.Message.Content }))}\r\n");
        await response.Body.FlushAsync();
    }
})
.WithName("ChatStreamAgent");

app.MapDefaultEndpoints();

app.Run();