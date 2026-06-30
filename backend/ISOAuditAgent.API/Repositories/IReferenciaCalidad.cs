namespace ISOAuditAgent.API.Repositories;

/// <summary>
/// Fuente de la referencia del Departamento de Calidad: la vigencia vigente de
/// cada formulario (FR). El acceso vive detrás de esta interfaz para poder
/// reemplazar la fuente (hoy una tabla MySQL; mañana un repositorio real de
/// Calidad en Drive/API) sin tocar el resto del workflow.
///
/// La consume ResolutorContexto — el único nodo que toca la BD — para
/// precalcular la VigenciaEsperada de cada artefacto.
/// </summary>
public interface IReferenciaCalidad
{
    /// <summary>
    /// Carga la referencia completa de una vez y devuelve un snapshot inmutable
    /// que resuelve la vigencia esperada por código de formulario (tolerante a
    /// "FR 30" / "FR-30" / "FR30").
    /// </summary>
    Task<ReferenciaCalidadVigencias> ObtenerAsync(CancellationToken ct);
}

/// <summary>
/// Snapshot inmutable de la referencia de Calidad. Encapsula la normalización de
/// códigos para que el lookup sea consistente en un solo lugar.
/// </summary>
public sealed class ReferenciaCalidadVigencias
{
    /// <summary>Referencia vacía (no hay vigencias cargadas).</summary>
    public static readonly ReferenciaCalidadVigencias Vacia =
        new(new Dictionary<string, DateOnly>());

    private readonly IReadOnlyDictionary<string, DateOnly> _porCodigoNormalizado;

    public ReferenciaCalidadVigencias(IReadOnlyDictionary<string, DateOnly> porCodigoNormalizado)
    {
        _porCodigoNormalizado = porCodigoNormalizado;
    }

    /// <summary>
    /// Vigencia vigente para el código dado, o null si Calidad no tiene entrada
    /// para ese formulario (o el código es null/vacío).
    /// </summary>
    public DateOnly? VigenciaEsperada(string? codigoArtefacto)
    {
        if (string.IsNullOrWhiteSpace(codigoArtefacto)) return null;
        return _porCodigoNormalizado.TryGetValue(NormalizarCodigo(codigoArtefacto), out var v)
            ? v
            : null;
    }

    /// <summary>
    /// Normaliza un código de formulario para comparación tolerante:
    /// minúsculas + sin espacios, guiones, underscore ni puntos.
    /// "FR 30" / "FR-30" / "FR_30" / "FR30" / "fr.30" → "fr30".
    /// </summary>
    public static string NormalizarCodigo(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;

        var sb = new System.Text.StringBuilder(raw.Length);
        foreach (var c in raw)
        {
            if (char.IsWhiteSpace(c)) continue;
            if (c is '-' or '_' or '.') continue;
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }
}
