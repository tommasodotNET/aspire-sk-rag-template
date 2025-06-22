using System;
using System.Text.Json.Serialization;

namespace Aspire.SK.RAG.ApiService.Models;

public abstract class BaseConversationItem
{
    public const string ItemTypeKey = "itemType";

    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("conversationId")]
    public required string ConversationId { get; set; }

    [JsonPropertyName("itemType")]
    public abstract ConversationItemType ItemType { get; }
}

public class ConversationHeader : BaseConversationItem
{
    /// <summary>
    /// Timestamp when the conversation was created
    /// </summary>
    [JsonPropertyName("properties")]
    public Dictionary<string, object?> Properties { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public override ConversationItemType ItemType => ConversationItemType.Header;

}

/// <summary>
/// Represents a conversation message document stored in Cosmos DB.
/// Each message in a conversation is stored as a separate document.
/// </summary>
public class ConversationMessage : BaseConversationItem
{
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
    /// Indicates if this message is part of a summarized conversation
    /// </summary>
    [JsonPropertyName("isSummary")]
    public bool IsSummary { get; set; } = false;

    /// <summary>
    /// Indicates if this conversation is a summary
    /// </summary>
    [JsonPropertyName("itemType")]
    public override ConversationItemType ItemType => ConversationItemType.Message;
}

public enum ConversationItemType
{
    Header,
    Message,
}