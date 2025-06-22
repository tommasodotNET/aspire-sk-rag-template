using System;
using Aspire.SK.RAG.ApiService.Models;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Aspire.SK.RAG.ApiService.Services;

public static class ThreadUtilityExtensions
{
    public static async Task<AgentThread> GetThread(
        this Agent agent,
        string? sessionId,
        IConversationRepository? conversationRepository,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(agent);

        logger.LogInformation("Retrieving thread with sessionId: {SessionId}", sessionId);

        AgentThread? thread;
        if (sessionId == null)
        {   
            logger.LogInformation("SessionId is null, creating a new thread.");
            logger.LogInformation("Creating a new ChatHistoryAgentThread with an empty history.");

            thread = new ChatHistoryAgentThread(new ChatHistory(), Guid.NewGuid().ToString());
        }
        else
        {
            logger.LogInformation("Retrieving ChatHistoryAgentThread for sessionId: {SessionId}", sessionId);

            if (conversationRepository == null)
            {
                throw new ArgumentNullException(nameof(conversationRepository), "Conversation repository is required for non-Azure agents.");
            }

            var c = await conversationRepository.LoadAsync(sessionId);

            var history = new ChatHistory(c.Messages);
            thread = new ChatHistoryAgentThread(history, sessionId);
        }

        logger.LogInformation("Thread retrieved: {ThreadId}", thread.Id);

        return thread;

    }

    public static async Task SaveThread(
        this Agent agent,
        AgentThread? thread,
        IConversationRepository? conversationRepository,
        IChatHistoryReducer? reducer,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);

        logger.LogInformation("Saving ChatHistoryAgentThread with ID: {ThreadId}", thread.Id);

        if (conversationRepository == null)
        {
            throw new ArgumentNullException(nameof(conversationRepository), "Conversation repository is required for non-Azure agents.");
        }

        var chatHistoryThread = thread as ChatHistoryAgentThread;
        var historyToBeSaved = new ChatHistory();
        if (reducer != null)
        {
            logger.LogInformation("Ecaluate reducing conversation for thread ID: {ThreadId}", chatHistoryThread.Id);
            //se è stato passato un reducer, lo utilizzo per eventualmente ridurre la conversazione prima di salvarla
            var reducedConversation = await reducer.ReduceAsync(chatHistoryThread.ChatHistory, cancellationToken);
            if (reducedConversation is not null)
            {
                logger.LogInformation("Reduced conversation for thread ID: {ThreadId}", chatHistoryThread.Id);
                await conversationRepository.DeleteConversationAsync(chatHistoryThread.Id, cancellationToken);

                historyToBeSaved.AddRange(reducedConversation);
            }
            else
            {
                logger.LogInformation("No reduction applied for thread ID: {ThreadId}", chatHistoryThread.Id);
                historyToBeSaved.AddRange(chatHistoryThread.ChatHistory);
            }
        }

        logger.LogInformation("Saving conversation for thread ID: {ThreadId}", chatHistoryThread.Id);

        //salvo il thread
        var conversation = new Conversation
        {
            Id = chatHistoryThread.Id,
            Messages = historyToBeSaved
        };

        await conversationRepository.SaveAsync(conversation);
    }
}
