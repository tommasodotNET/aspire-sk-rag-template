var builder = DistributedApplication.CreateBuilder(args);

// If you want to use Azure Search, uncomment the following lines
// var existingSearchName = builder.AddParameter("existingSearchName");
// var existingSearchResourceGroup = builder.AddParameter("existingSearchResourceGroup");
// var search = builder.AddAzureSearch("search")
//     .AsExisting(existingSearchName, existingSearchResourceGroup);

var existingOpenAIName = builder.AddParameter("existingOpenAIName");
var existingOpenAIResourceGroup = builder.AddParameter("existingOpenAIResourceGroup");

var azureOpenAI = builder.AddAzureOpenAI("azureOpenAI");
        
// If you want to use an existing Azure OpenAI resource, uncomment the following line
azureOpenAI.AsExisting(existingOpenAIName, existingOpenAIResourceGroup);

#pragma warning disable ASPIRECOSMOSDB001
var cosmos = builder.AddAzureCosmosDB("cosmos-db")
    .RunAsPreviewEmulator(
        emulator =>
        {
            emulator.WithDataExplorer();
            emulator.WithLifetime(ContainerLifetime.Persistent);
        });
var db = cosmos.AddCosmosDatabase("db");
var conversations = db.AddContainer("conversations", "/conversationId");

var apiService = builder.AddProject<Projects.Aspire_SK_RAG_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(azureOpenAI)
    .WithReference(conversations)
    .WaitFor(azureOpenAI)
    .WaitFor(cosmos);

// If you want to use Azure Search, uncomment the following lines
// apiService
//     .WithReference(search)
//     .WaitFor(search);

var frontend = builder.AddNpmApp("frontend", "../frontend", "dev")
    .WithNpmPackageInstallation()
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithHttpEndpoint(env: "PORT");

builder.Build().Run();
