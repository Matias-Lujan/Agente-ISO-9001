using Microsoft.Extensions.AI;

namespace ISOAuditAgent.DocumentAnalysis.Tests.Agent;

/// <summary>
/// <see cref="IChatClient"/> mínimo para tests: devuelve JSON fijo por cola.
/// </summary>
internal sealed class FixedJsonChatClient : IChatClient
{
    private readonly Queue<string> _jsonResponses;

    public FixedJsonChatClient(params string[] jsonResponses)
    {
        _jsonResponses = new Queue<string>(jsonResponses);
    }

    public ChatClientMetadata Metadata { get; } = new ChatClientMetadata("test");

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (_jsonResponses.Count == 0)
            throw new InvalidOperationException("No hay más respuestas encoladas para FixedJsonChatClient.");

        var json = _jsonResponses.Dequeue();
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, json)));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        EmptyStreaming();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    private static async IAsyncEnumerable<ChatResponseUpdate> EmptyStreaming()
    {
        await Task.CompletedTask.ConfigureAwait(false);
        yield break;
    }

    public void Dispose()
    {
    }
}
