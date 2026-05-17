using System.ComponentModel;
using ISOAuditAgent.DocumentAnalysis.Mcp;
using ISOAuditAgent.DocumentAnalysis.Parsing;
using ISOAuditAgent.DocumentAnalysis.Tailoring;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ISOAuditAgent.DocumentAnalysis.Agent.Tools;

public sealed class TailoringTool
{
    private readonly ITailoringReader _reader;

    public TailoringTool(ITailoringReader reader)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    [Description(
        "Lee el workbook FR 29 (tailoring) del proyecto desde Drive y devuelve cada fila interpretable.")]
    public Task<IReadOnlyList<EntradaTailoring>> GetTailoringAsync(
        [Description("Id del proyecto")] int proyectoId,
        CancellationToken cancellationToken = default) =>
        _reader.ReadAsync(proyectoId, cancellationToken);
}
