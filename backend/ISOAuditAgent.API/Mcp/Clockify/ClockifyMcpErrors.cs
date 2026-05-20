using ModelContextProtocol;

namespace ISOAuditAgent.API.Mcp.Clockify;

/// <summary>
/// Mapea excepciones de la cadena <c>ClockifyMcpTools → ClockifyApiClient</c>
/// a <see cref="McpException"/> con mensajes útiles para el cliente MCP.
/// </summary>
internal static class ClockifyMcpErrors
{
    public static McpException AsToolError(this Exception ex, string toolName)
    {
        var summary = BuildSummary(ex, toolName);
        return new McpException(summary, ex);
    }

    private static string BuildSummary(Exception ex, string toolName)
    {
        var head = ex switch
        {
            InvalidOperationException invalid =>
                $"[{toolName}] {invalid.Message}",

            OperationCanceledException =>
                $"[{toolName}] Operación cancelada por el cliente.",

            _ => $"[{toolName}] {ex.GetType().Name}: {ex.Message}"
        };

        if (ex.InnerException is { } inner)
        {
            head += $" — causa: {inner.GetType().Name}: {inner.Message}";
        }

        return head;
    }
}
