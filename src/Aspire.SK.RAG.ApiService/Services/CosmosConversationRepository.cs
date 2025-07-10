using System.Runtime.CompilerServices;
using System.Text.Json;
using Aspire.SK.RAG.ApiService.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.SemanticKernel;

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

    public CosmosConversationRepository([FromKeyedServices("conversations")] Container cosmosContainer,
        ILogger<CosmosConversationRepository> logger)
    {
        _cosmosContainer = cosmosContainer ?? throw new ArgumentNullException(nameof(cosmosContainer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    //TODO: sostituire con il metodo DeleteAllItemsByPartitionKeyStreamAsync appena disponibile in Cosmos SDK non beta 
    public async Task DeleteConversationAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(conversationId))
        {
            throw new ArgumentException("Conversation ID cannot be null or empty.", nameof(conversationId));
        }

        _logger.LogDebug("Deleting conversation with ID: {conversationId}", conversationId);

        var partitionKey = new PartitionKey(conversationId);
        var query = new QueryDefinition("SELECT c.id, c.conversationId FROM c");

        using var iterator = _cosmosContainer.GetItemQueryIterator<ConversationMessage>(
            query,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = partitionKey,
                MaxItemCount = 100 // Process in batches
            });

        var deletedCount = 0;
  
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);

            foreach (var msg in response)
            {
                try
                {
                    await _cosmosContainer.DeleteItemAsync<object>(
                        msg.Id,
                        partitionKey,
                        cancellationToken: cancellationToken).ConfigureAwait(false);

                    deletedCount++;
                }
                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Document with id {id} not found for deletion in conversation {conversationId}", msg.Id, conversationId);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Error deleting document {id} from conversation {conversationId}", msg.Id, conversationId);
                }
            }
        }

        if (deletedCount == 0)
        {
            _logger.LogDebug("No documents found for conversationId {conversationId}", conversationId);
        }
        else
        {
            _logger.LogDebug("Deleted {deletedCount} documents for conversationId {conversationId}", deletedCount, conversationId);
        }

    }

    public async Task<long> GetLastMessageTsAsync(string? conversationId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(conversationId))
        {
            return 0;
        }

        _logger.LogDebug("Retrieving last message timestamp for conversationId {conversationId}", conversationId);

        try
        {
            // Use direct iterator instead of ExecuteQueryAsync for better performance
            var query = new QueryDefinition("SELECT VALUE c._ts FROM c ORDER BY c._ts DESC OFFSET 0 LIMIT 1");

            using var iterator = _cosmosContainer.GetItemQueryIterator<long>(
                query,
                requestOptions: new QueryRequestOptions
                {
                    PartitionKey = new(conversationId),
                    MaxItemCount = 1 // Optimize for single result
                });

            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
                return response.FirstOrDefault();
            }

            _logger.LogDebug("No documents found for conversationId {conversationId}", conversationId);
            return 0;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogDebug("Conversation {conversationId} not found", conversationId);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving last message timestamp for conversationId {conversationId}", conversationId);
            throw;
        }
    }

    public async Task<Conversation> LoadAsync(string? conversationId = null, CancellationToken cancellationToken = default)
    {
        Conversation output = new() { Id = conversationId ?? string.Empty };

        if (string.IsNullOrEmpty(conversationId))
        {
            return output;
        }

        //la query gia ci torna i messaggi ordinati per messageIndex ed evitiamo di farlo in memoria
        var query = new QueryDefinition("SELECT * FROM c ORDER BY c.messageIndex ASC");

        // Stream processing for better memory efficiency
        await foreach (var message in StreamMessagesAsync(query, conversationId, cancellationToken))
        {
            try
            {
                var content = JsonSerializer.Deserialize<ChatMessageContent>(message.ChatMessageContent);
                if (content != null)
                {
                    output.Messages.Add(content);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize message content for conversationId {conversationId}", conversationId);
            }
        }

        _logger.LogInformation("Loaded conversation {conversationId} with {messageCount} messages", conversationId, output.Messages.Count);

        return output;
    }

    //TODO: passare ad approccio con TransactionBatch per migliorare le performance appena disponibile nell'emulatore
    public async Task SaveAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        if (string.IsNullOrEmpty(conversation.Id))
        {
            throw new ArgumentException("Conversation ID cannot be null or empty.", nameof(conversation));
        }

        _logger.LogDebug("Saving conversation {conversationId} with {messageCount} messages", conversation.Id, conversation.Messages.Count);

        var requestOptions = new ItemRequestOptions
        {
            EnableContentResponseOnWrite = false, // We don't need the response body on write
        };

        var partitionKey = new PartitionKey(conversation.Id);

        // Save each message as a separate document
        for (int i = 0; i < conversation.Messages.Count; i++)
        {
            var message = conversation.Messages[i];

            if (message==null)
            {
                continue;
            }

            var messageDocument = new ConversationMessage()
            {
                Id = $"{conversation.Id}-message-{i:D6}",
                ConversationId = conversation.Id,
                MessageIndex = i,
                IsSummary = i==0 && message.Metadata!=null && message.Metadata.ContainsKey("__summary__"),
                ChatMessageContent = JsonSerializer.Serialize(message),
                Timestamp = DateTime.UtcNow.ToString("o"),
                Ttl = 86400
            };

            await _cosmosContainer.UpsertItemAsync(
                messageDocument,
                partitionKey,
                requestOptions: requestOptions,
                cancellationToken: cancellationToken);
        }
    }

    private async IAsyncEnumerable<ConversationMessage> StreamMessagesAsync(QueryDefinition query, string conversationId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var results = _cosmosContainer.GetItemQueryIterator<ConversationMessage>(
            query,
            requestOptions: new QueryRequestOptions { PartitionKey = new(conversationId) }
        );

        while (results.HasMoreResults)
        {
            var response = await results.ReadNextAsync(cancellationToken).ConfigureAwait(false);
            foreach (var item in response)
            {
                yield return item;
            }
        }
    }

}
