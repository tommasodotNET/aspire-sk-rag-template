using System.ComponentModel;
using Azure.Search.Documents.Indexes;
using Microsoft.SemanticKernel;

namespace Aspire.SK.RAG.ApiService.Plugins;

[Description("Set of plugins to search your documents.")]
public class RAGPlugin
{
    // private SearchIndexClient _searchIndexClient;

    // public RAGPlugin(SearchIndexClient searchIndexClient)
    // {
    //     _searchIndexClient = searchIndexClient;
    // }

    [KernelFunction("search")]
    [Description("Search for relevant documents in the RAG index.")]
    public string Search([Description("The user question")]string query)
    {
        // Simulated customer and order data
        var customers = new[]
        {
            new
            {
                CustomerId = 1,
                Name = "Alice Smith",
                Orders = new[]
                {
                    new { OrderId = 101, Product = "Laptop", Amount = 1200 },
                    new { OrderId = 102, Product = "Mouse", Amount = 25 }
                }
            },
            new
            {
                CustomerId = 2,
                Name = "Bob Johnson",
                Orders = new[]
                {
                    new { OrderId = 103, Product = "Monitor", Amount = 300 }
                }
            }
        };

        // Serialize to JSON for LLM consumption
        var json = System.Text.Json.JsonSerializer.Serialize(customers);

        return json;
    }
}
