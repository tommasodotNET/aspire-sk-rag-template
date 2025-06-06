# Description

This is a demo to showcase how to use the [Semantic Kernel Agent Framework](https://learn.microsoft.com/en-us/semantic-kernel/frameworks/agent/?pivots=programming-language-csharp) with [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview).

This sample simulates an agent that bases its answer on a knowledge base stored in Azure AI Search and uses Azure OpenAI to generate the answer. The knowledge base is populated via static data and is not retrieved from Azure AI Search. Nonetheless, it is easy to adapt the code to retrieve the data from Azure AI Search.

## How to run the example

1. Configure the OpenAI integration for .NET Aspire according to the [documentation](https://learn.microsoft.com/en-us/dotnet/aspire/azureai/azureai-openai-integration?tabs=dotnet-cli#connect-to-an-existing-azure-openai-service). You can also configure the Azure AI Search integration by following the [documentation](https://learn.microsoft.com/en-us/dotnet/aspire/azureai/azureai-search-document-integration?tabs=dotnet-cli).

Note that you can use either DefaultAzureCredential or API key authentication.

You need to add the connection string to your AppHost `appsettings.json` file. The connection string format depends on the authentication method you choose.

Using DefaultAzureCredentials:

```json
{
 "ConnectionStrings": {
    "search": "Endpoint=https://<SEARCH_NAME>.search.windows.net/",
    "azureOpenAI": "Endpoint=https://<AZ_OAI_NAME>.openai.azure.com/"
  }
}
```

Using API Keys:

```json
{
 "ConnectionStrings": {
    "search": "Endpoint=https://<SEARCH_NAME>.search.windows.net;Key=<SEARCH_KEY>;",
    "azureOpenAI": "Endpoint=https://<AZ_OAI_NAME>.openai.azure.com/;Key=<AZ_OAI_KEY>;"
  }
}
```

2. Run the sample

You can use dotnet to start the application:

```bash
cd src/Aspire.SK.RAG.AppHost
dotnet run
```

Or you can use the Aspire CLI to run the application:

```bash
aspire run
```
