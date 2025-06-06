# Description

This is a demo to showcase how to use the [Semantic Kernel Agent Framework](https://learn.microsoft.com/en-us/semantic-kernel/frameworks/agent/?pivots=programming-language-csharp) with [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview).

This sample simulates an agent that bases its answer on a knowledge base stored in Azure AI Search and uses Azure OpenAI to generate the answer. The knowledge base is populated via static data and is not retrieved from Azure AI Search. Nonetheless, it is easy to adapt the code to retrieve the data from Azure AI Search.

## How to run the example

1. Configure the OpenAI integration for .NET Aspire according to the [documentation](https://learn.microsoft.com/en-us/dotnet/aspire/azureai/azureai-openai-integration?tabs=dotnet-cli#connect-to-an-existing-azure-openai-service). You can also configure the Azure AI Search integration by following the [documentation](https://learn.microsoft.com/en-us/dotnet/aspire/azureai/azureai-search-document-integration?tabs=dotnet-cli).

This template uses [.NET Aspire Azure integrations](https://learn.microsoft.com/en-us/dotnet/aspire/azure/integrations-overview?tabs=dotnet-cli#use-existing-azure-resources).

You need to configure your Azure environment in AppHost `appsettings.json` file.

```json
"Azure": {
  "SubscriptionId": "<YOUR_SUBSCRIPTION_ID>",
  "AllowResourceGroupCreation": true,
  "ResourceGroup": "<YOUR_RESOURCE_GROUP_NAME>",
  "Location": "<YOUR_LOCATION>",
  "CredentialSource": "<YOUR_CREDENTIAL_SOURCE>" // Options: "AzureCli", "AzurePowerShell", "VisualStudio", "AzureDeveloperCli", "InteractiveBrowser"
},
"Parameters": {
  "existingOpenAIName": "<EXISTING_AZ_OAI_NAME>",
  "existingOpenAIResourceGroup": "<EXISTING_RESOURCE_GROUP_NAME>"
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
