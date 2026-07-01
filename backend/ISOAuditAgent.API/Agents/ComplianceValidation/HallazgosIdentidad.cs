// ============================================================================
//  HallazgosIdentidad — ComplianceValidation
// ----------------------------------------------------------------------------
//  Valida la IDENTIDAD del documento encontrado contra lo esperado, usando la
//  metadata leída del encabezado (no el nombre del archivo, que es solo un
//  localizador flexible):
//
//   - código-match:   el código FR DENTRO del documento vs el esperado. Detecta
//                     "el archivo encontrado es otro formulario".
//   - proyecto-match: el proyecto declarado DENTRO del documento vs el auditado.
//                     Detecta "el documento es de otro proyecto".
//
//  Ambos llevan OrigenRegla.Identidad → ClasificacionResponseParser los fuerza a
//  NC (evidencia equivocada = incumplimiento, no desvío formal).
//
//  CONSERVADOR: solo dispara ante un mismatch CLARO. Si no se pudo leer el código
//  o el proyecto del documento (null), no genera hallazgo (el checker ya logueó).
//  proyecto-match usa substring en ambas direcciones, así que ante datos difusos
//  tiende a NO marcar (falso negativo) antes que a un NC falso.
//
//  FUNCIÓN PURA: golden-testeable, sin I/O ni logging.
// ============================================================================

using System.Text.RegularExpressions;
using ISOAuditAgent.API.Agents.Contracts;
using ISOAuditAgent.API.Agents.DocumentAnalysis.Tailoring;

namespace ISOAuditAgent.API.Agents.ComplianceValidation;

internal static class HallazgosIdentidad
{
    private static readonly Regex NumeroFrRegex = new(
        @"FR\s*(\d{1,3})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    public static List<HallazgoPreliminar> Generar(DocumentosExtraidos input)
    {
        var hallazgos = new List<HallazgoPreliminar>();

        foreach (var a in input.Artefactos)
        {
            // Solo sobre artefactos exigibles efectivamente encontrados.
            if (a.Exigibilidad != ExigibilidadArtefacto.Exigible) continue;
            if (a.EstadoDisponibilidad != EstadoDisponibilidad.Encontrado) continue;

            var codigoHallazgo = CodigoMatch(a);
            if (codigoHallazgo is not null) hallazgos.Add(codigoHallazgo);

            var proyectoHallazgo = ProyectoMatch(a, input.NombreProyecto);
            if (proyectoHallazgo is not null) hallazgos.Add(proyectoHallazgo);
        }

        return hallazgos;
    }

    // --- código-match -------------------------------------------------------

    private static HallazgoPreliminar? CodigoMatch(ArtefactoExtraido a)
    {
        var esperado = NumeroFr(a.CodigoArtefacto);     // FR esperado (sin versión)
        var detectado = NumeroFr(a.CodigoDetectado);    // FR leído del documento

        // No comparable: el artefacto no es un FR formal, o no se detectó código.
        if (esperado is null || detectado is null) return null;

        if (esperado == detectado) return null; // coincide → conforme

        return new HallazgoPreliminar(
            ArtefactoEsperadoId: a.ArtefactoEsperadoId,
            Descripcion:
                $"El documento de '{a.NombreArtefacto}' corresponde a otro formulario " +
                $"(código {a.CodigoDetectado}, se esperaba {a.CodigoArtefacto}).",
            Justificacion:
                $"El documento encontrado para el artefacto '{a.NombreArtefacto}' declara " +
                $"internamente el código {a.CodigoDetectado}, pero el esperado es {a.CodigoArtefacto}. " +
                $"El archivo localizado corresponde a otro formulario, por lo que la evidencia del " +
                $"artefacto esperado no está realmente presente.",
            OrigenRegla: OrigenRegla.Identidad);
    }

    // --- proyecto-match -----------------------------------------------------

    private static HallazgoPreliminar? ProyectoMatch(ArtefactoExtraido a, string nombreProyectoAuditado)
    {
        if (string.IsNullOrWhiteSpace(nombreProyectoAuditado)) return null;
        if (string.IsNullOrWhiteSpace(a.ProyectoDetectado)) return null; // no comparable

        var auditado = Normalizar(nombreProyectoAuditado);
        var enDoc = Normalizar(a.ProyectoDetectado);
        if (auditado.Length == 0 || enDoc.Length == 0) return null;

        // Coincide si uno contiene al otro ("App Productores" ⊂ "30052 - App
        // Productores"). Solo se marca cuando claramente NO hay solapamiento.
        if (enDoc.Contains(auditado, StringComparison.Ordinal)
            || auditado.Contains(enDoc, StringComparison.Ordinal))
            return null;

        return new HallazgoPreliminar(
            ArtefactoEsperadoId: a.ArtefactoEsperadoId,
            Descripcion:
                $"El documento de '{a.NombreArtefacto}' parece ser de otro proyecto.",
            Justificacion:
                $"El documento encontrado para el artefacto '{a.NombreArtefacto}' declara el proyecto " +
                $"'{a.ProyectoDetectado!.Trim()}', que no coincide con el proyecto auditado " +
                $"'{nombreProyectoAuditado}'. La evidencia parece corresponder a otro proyecto.",
            OrigenRegla: OrigenRegla.Identidad);
    }

    // --- helpers ------------------------------------------------------------

    /// <summary>"FR 48-01" → "48"; "FR 30" → "30"; null/sin match → null.</summary>
    private static string? NumeroFr(string? codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo)) return null;
        var m = NumeroFrRegex.Match(codigo);
        return m.Success ? m.Groups[1].Value : null;
    }

    private static string Normalizar(string s) =>
        TailoringColumnMapper.NormalizeHeaderKey(s);
}
