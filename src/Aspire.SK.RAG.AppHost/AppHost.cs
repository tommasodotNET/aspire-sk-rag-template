var builder = DistributedApplication.CreateBuilder(args);

// var search = builder.ExecutionContext.IsPublishMode
//     ? builder.AddAzureSearch("search")
//     : builder.AddConnectionString("search");

var azureOpenAI = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureOpenAI("azureOpenAI").AddDeployment(
        name: "gpt-4o",
        modelName: "gpt-4o",
        modelVersion: "2024-11-20")
    : builder.AddConnectionString("azureOpenAI");

var apiService = builder.AddProject<Projects.Aspire_SK_RAG_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(azureOpenAI)
    .WaitFor(azureOpenAI);

// apiService
//     .WithReference(search)
//     .WaitFor(search);

var frontend = builder.AddNpmApp("frontend", "../frontend", "dev")
    .WithNpmPackageInstallation()
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithHttpEndpoint(env: "PORT");

builder.Build().Run();
