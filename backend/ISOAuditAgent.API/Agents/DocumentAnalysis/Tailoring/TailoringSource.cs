// ============================================================================
//  TailoringSource — Implementación de ITailoringSource (D3.4)
// ----------------------------------------------------------------------------
//  Cáscara fina sobre TailoringReader. Adapta la firma de la interface
//  (ValueTask<IReadOnlyList<FilaTailoring>>) y mantiene el contrato vigente
//  intacto.
//
//  Esta es la pieza que consume DocumentAnalysisNode vía DI.
// ============================================================================

namespace ISOAuditAgent.API.Agents.DocumentAnalysis.Tailoring;

public sealed class TailoringSource : ITailoringSource
{
    private readonly TailoringReader _reader;

    public TailoringSource(TailoringReader reader)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    public async ValueTask<TailoringExtraido> ObtenerAsync(
        string driveFolderId, CancellationToken ct)
    {
        return await _reader.LeerAsync(driveFolderId, ct).ConfigureAwait(false);
    }
}