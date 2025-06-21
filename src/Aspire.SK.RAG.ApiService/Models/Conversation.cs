using System;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Aspire.SK.RAG.ApiService.Models;

public class Conversation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public Dictionary<string, object?> Properties { get; set; } = [];
    public ChatHistory Messages { get; set; } = [];
    public bool IsSummary { get; set; } = false;
}