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

    [KernelFunction("search_by_name")]
    [Description("Search for Orders by Buyer Name")]
    public string SearchByName([Description("Buyer")] string name)
    {
        var orders = new[]
        {
            new { Buyer = "Alice Smith", OrderId = 1, Product = "Laptop", Amount = 1200 },
            new { Buyer = "Alice Smith", OrderId = 2, Product = "Mouse", Amount = 25 },
            new { Buyer = "Bob Johnson", OrderId = 3, Product = "Monitor", Amount = 300 },
            new { Buyer = "Charlie Brown", OrderId = 4, Product = "Keyboard", Amount = 75 }
        };

        var results = orders
            .Where(o => o.Buyer.Contains(name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Serialize to JSON for LLM consumption
        var json = System.Text.Json.JsonSerializer.Serialize(orders);

        return json;
    }

    [KernelFunction("search_by_id")]
    [Description("Search for Orders by Order ID")]  
    public string SearchById([Description("Order ID")] int orderId)
    {
        var orders = new[]
        {
            new { Buyer = "Alice Smith", OrderId = 1, Product = "Laptop", Amount = 1200 },
            new { Buyer = "Alice Smith", OrderId = 2, Product = "Mouse", Amount = 25 },
            new { Buyer = "Bob Johnson", OrderId = 3, Product = "Monitor", Amount = 300 },
            new { Buyer = "Charlie Brown", OrderId = 4, Product = "Keyboard", Amount = 75 }
        };

        var result = orders.FirstOrDefault(o => o.OrderId == orderId);

        // Serialize to JSON for LLM consumption
        var json = System.Text.Json.JsonSerializer.Serialize(result);

        return json;
    }
}
