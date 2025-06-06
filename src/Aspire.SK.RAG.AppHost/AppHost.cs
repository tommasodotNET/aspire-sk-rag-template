var builder = DistributedApplication.CreateBuilder(args);

// If you want to use Azure Search, uncomment the following lines
// var search = builder.ExecutionContext.IsPublishMode
//     ? builder.AddAzureSearch("search")
//     : builder.AddConnectionString("search");

var existingOpenAIName = builder.AddParameter("existingOpenAIName");
var existingOpenAIResourceGroup = builder.AddParameter("existingOpenAIResourceGroup");

var azureOpenAI = builder.AddAzureOpenAI("azureOpenAI");
        
// If you want to use an existing Azure OpenAI resource, uncomment the following line
azureOpenAI.AsExisting(existingOpenAIName, existingOpenAIResourceGroup);

var apiService = builder.AddProject<Projects.Aspire_SK_RAG_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(azureOpenAI)
    .WaitFor(azureOpenAI);

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
