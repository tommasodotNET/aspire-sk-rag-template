using System;
using System.Text.Json.Serialization;

namespace Aspire.SK.RAG.ApiService.Models;

public class ConversationMessage
{
    /// <summary>
    /// the message identifier, which is a unique string inside the conversation.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// The conversation identifier to which this message belongs.
    /// </summary>
    [JsonPropertyName("conversationId")]
    public required string ConversationId { get; set; }

    [JsonPropertyName("ttl")]
    public int? Ttl { get; set; }

    /// <summary>
    /// Zero-based index of the message within the conversation
    /// </summary>
    [JsonPropertyName("messageIndex")]
    public int MessageIndex { get; set; }

    [JsonPropertyName("chatMessageContent")]
    public string ChatMessageContent { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the message was created/saved
    /// </summary>
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");

    /// <summary>
    /// Indicates if this message is a summarization of the conversation
    /// </summary>
    [JsonPropertyName("isSummary")]
    public bool IsSummary { get; set; } = false;
}
