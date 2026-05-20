using ISOAuditAgent.DocumentAnalysis.Drive;
using ModelContextProtocol;

namespace ISOAuditAgent.API.Mcp.Drive;

/// <summary>
/// Mapea excepciones de la cadena <c>Tool → ListingService → DriveClient</c>
/// a <see cref="McpException"/> con un mensaje útil para el cliente MCP.
/// </summary>
/// <remarks>
/// El SDK MCP propaga el mensaje de <see cref="McpException"/> tal cual al
/// cliente (vía JSON-RPC con <c>isError: true</c>). El resto de las
/// excepciones se enmascaran como mensaje genérico para evitar filtrar
/// detalles internos. Por eso aquí solo exponemos información pública:
/// nombre del tipo, mensaje, y el inner exception inmediato (suele ser la
/// <c>GoogleApiException</c> con la causa real).
/// </remarks>
internal static class DriveMcpErrors
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
            ProyectoDriveMappingNotFoundException notFound =>
                $"[{toolName}] No hay mapeo Drive configurado para ProyectoId={notFound.ProyectoId}. " +
                "Verifique 'DocumentAnalysis:Drive:Mappings' en appsettings.",

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
