using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.SK.RAG.ApiService.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json.Linq;

namespace Aspire.SK.RAG.ApiService.Services;

public interface IConversationRepository
{
    Task<Conversation> LoadAsync(string? conversationId = null, CancellationToken cancellationToken = default);
    Task DeleteConversationAsync(string conversationId, CancellationToken cancellationToken = default);
    Task SaveAsync(Conversation conversation, CancellationToken cancellationToken = default);
}

public class CosmosConversationRepository : IConversationRepository
{
    private readonly Container _cosmosContainer;
    private readonly ILogger<CosmosConversationRepository> _logger;

    public CosmosConversationRepository([FromKeyedServices("conversations")] Container cosmosContainer, ILogger<CosmosConversationRepository> logger)
    {
        _cosmosContainer = cosmosContainer ?? throw new ArgumentNullException(nameof(cosmosContainer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task DeleteConversationAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(conversationId))
        {
            throw new ArgumentException("Conversation ID cannot be null or empty.", nameof(conversationId));
        }

        List<JObject> docs = await ExecuteQueryAsync(conversationId, cancellationToken).ConfigureAwait(false);

        if (docs.Count == 0)
        {
            _logger.LogInformation("No documents found for conversationId {conversationId}", conversationId);
            return;
        }

        foreach (var doc in docs)
        {
            if (doc.TryGetValue("id", out var idNode) && idNode is JToken idValue)
            {
                var id = idValue.ToString();
                try
                {
                    await _cosmosContainer.DeleteItemAsync<JsonObject>(id, new PartitionKey(conversationId), cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Document with id {id} not found for deletion.", id);
                }
            }
        }
    }

    public async Task<Conversation> LoadAsync(string? conversationId = null, CancellationToken cancellationToken = default)
    {
        Conversation output = new() { Id = conversationId ?? string.Empty };

        if (string.IsNullOrEmpty(conversationId))
        {
            return output;
        }

        List<JObject> queryResults = await ExecuteQueryAsync(conversationId, cancellationToken).ConfigureAwait(false);

        if (queryResults.Count == 0)
        {
            _logger.LogInformation("No documents found for conversationId {conversationId}", conversationId);
            return output;
        }

        List<ConversationMessage> messages = [];
        foreach (var item in queryResults)
        {
            if (item != null && Enum.TryParse(item?[BaseConversationItem.ItemTypeKey]?.ToString(), out ConversationItemType type))
            {
                if (type == ConversationItemType.Header)
                {
                    ConversationHeader? header = JsonSerializer.Deserialize<ConversationHeader>(item.ToString()!);
                    output.Properties = header?.Properties ?? [];
                }
                else if (type == ConversationItemType.Message)
                {
                    var m = JsonSerializer.Deserialize<ConversationMessage>(item.ToString());
                    if (m != null)
                    {
                        messages.Add(m);
                    }
                }
            }
        }

        _logger.LogInformation("Loaded conversation {conversationId} with {messageCount} messages",
            conversationId, output.Messages.Count);

        messages = messages.OrderBy(m => m.MessageIndex).ToList();

        output.Id = conversationId;

        if (messages.Count > 0)
        {
            foreach (var message in messages)
            {
                var content = JsonSerializer.Deserialize<ChatMessageContent>(message.ChatMessageContent);
                output.Messages.Add(content);
            }
        }
        return output;
    }

    public async Task SaveAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        if (string.IsNullOrEmpty(conversation.Id))
        {
            throw new ArgumentException("Conversation ID cannot be null or empty.", nameof(conversation));
        }

        _logger.LogInformation("Saving conversation {conversationId} with {messageCount} messages",
            conversation.Id, conversation.Messages.Count);

        PartitionKey partitionKey = new(conversation.Id);
        TransactionalBatch batch = _cosmosContainer.CreateTransactionalBatch(partitionKey);

        var headerDoc = new
        {
            id = conversation.Id,
            conversationId = conversation.Id,
            properties = conversation.Properties,
            itemType = ConversationItemType.Header.ToString(),
        };
        batch.UpsertItem(item: headerDoc);

        for (int i = 0; i < conversation.Messages.Count; i++)
        {
            var message = conversation.Messages[i];

            var messageDocument = new
            {
                id = $"{conversation.Id}-message-{i:D6}",
                conversationId = conversation.Id,
                messageIndex = i,
                chatMessageContent = JsonSerializer.Serialize(message),
                timestamp = DateTime.UtcNow.ToString("o"),
                isSummary = i == 0 && conversation.IsSummary,
                itemType = ConversationItemType.Message.ToString()
            };

            batch.UpsertItem(item: messageDocument);
        }

        var batchResponse = await batch.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        if (!batchResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Error in SaveAsync, batchResponse: {batchResponse}", batchResponse);
        }
    }

    private async Task<List<JObject>> ExecuteQueryAsync(string conversationId, CancellationToken cancellationToken)
    {
        QueryDefinition query = new("SELECT * FROM c");
        FeedIterator<JObject> results = _cosmosContainer.GetItemQueryIterator<JObject>(
            query,
            requestOptions: new QueryRequestOptions { PartitionKey = new(conversationId) }
        );

        List<JObject> queryResults = [];
        while (results.HasMoreResults)
        {
            FeedResponse<JObject> response = await results.ReadNextAsync(cancellationToken).ConfigureAwait(false);
            queryResults.AddRange(response);
        }
        return queryResults;
    }
}
