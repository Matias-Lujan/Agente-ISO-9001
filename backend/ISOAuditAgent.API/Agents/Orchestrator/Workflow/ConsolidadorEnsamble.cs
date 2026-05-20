// ============================================================================
//  ConsolidadorEnsamble — Lógica de ensamble del AuditoriaResultado
// ----------------------------------------------------------------------------
//  Código determinista puro. SIN LLM. Lo invoca ConsolidadorResultadoNode [6].
//
//  Transforma:
//    DocumentosExtraidos   (input original, contexto)
//    HallazgosClasificados (output de FindingsClassification)
//  en:
//    AuditoriaResultado    (contrato 5) — 3 listas paralelas unidas por
//                          ArtefactoEsperadoId.
//
//  REGLA DE RESULTADO (matriz modelo_bd.md §3 + criterio de degradación):
//
//    Exigibilidad = PendienteEtapaFutura          -> PendienteEtapaFutura
//    aplica = NoAplica + justificación             -> NoAplica
//    aplica = NoAplica sin justificación           -> NoConforme
//    aplica = SinDeclararEnTailoring               -> NoConforme
//    aplica = Si + EstadoDisponibilidad = Faltante -> NoConforme
//    aplica = Si + presente + algún hallazgo NC    -> NoConforme
//    aplica = Si + presente + sin NC (cero/OBS/OM) -> Conforme
//
//  Solo un hallazgo NC degrada a NoConforme un artefacto presente. OBS y OM se
//  conservan en la lista de Hallazgos pero NO degradan el resultado (ERS §4.2:
//  OBS no implica incumplimiento directo).
//
//  EL CONSOLIDADOR NO GENERA HALLAZGOS. Los hallazgos los detectan los
//  validadores (ComplianceValidation, ConsistencyVerification) y los clasifica
//  FindingsClassification. El consolidador solo ENSAMBLA y VERIFICA COHERENCIA.
//  En particular, el hallazgo que explica un artefacto faltante / NoAplica sin
//  justificación / SinDeclararEnTailoring es responsabilidad de
//  ComplianceValidation (es Gap Analysis — su tarea central).
//
//  POLÍTICA DE ERRORES — fallar ruidoso, no esconder. El consolidador no
//  filtra datos inválidos en silencio: lanza EnsambleAuditoriaException ante
//  cualquier inconsistencia. El worker la captura y marca la auditoría
//  Fallida. Validaciones de coherencia:
//    (a) duplicados de ArtefactoEsperadoId;
//    (b) hallazgo huérfano (apunta a artefacto inexistente);
//    (c) hallazgo sobre artefacto que por contrato no admite hallazgos;
//    (d) artefacto NoConforme SIN ningún hallazgo que lo explique.
// ============================================================================

using ISOAuditAgent.API.Agents.Contracts;
using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Agents.Orchestrator;

/// <summary>
/// Error de ensamble. Indica que un DTO de entrada violó un contrato o
/// invariante. La lanza ConsolidadorEnsamble; el worker la traduce a Fallida.
/// </summary>
public sealed class EnsambleAuditoriaException : Exception
{
    public EnsambleAuditoriaException(string mensaje) : base(mensaje) { }
}

public static class ConsolidadorEnsamble
{
    /// <summary>
    /// Ensambla el AuditoriaResultado final.
    /// </summary>
    public static AuditoriaResultado Ensamblar(
        DocumentosExtraidos documentos,
        HallazgosClasificados clasificacion)
    {
        // --- Validación 0: misma auditoría -----------------------------------
        if (documentos.AuditoriaId != clasificacion.AuditoriaId)
        {
            throw new EnsambleAuditoriaException(
                $"DocumentosExtraidos es de la auditoría " +
                $"{documentos.AuditoriaId} y HallazgosClasificados de la " +
                $"{clasificacion.AuditoriaId}.");
        }

        // --- Validación (a): sin ArtefactoEsperadoId duplicados --------------
        var idsDuplicados = documentos.Artefactos
            .GroupBy(a => a.ArtefactoEsperadoId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (idsDuplicados.Count > 0)
        {
            throw new EnsambleAuditoriaException(
                $"DocumentosExtraidos trae ArtefactoEsperadoId duplicados: " +
                $"{string.Join(", ", idsDuplicados)}. DocumentAnalysis debe " +
                $"emitir cada artefacto una sola vez.");
        }

        // --- Paso 1: resolver el resultado de cada artefacto -----------------
        var hallazgosPorArtefacto = clasificacion.Hallazgos
            .GroupBy(h => h.ArtefactoEsperadoId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<HallazgoClasificado>)g.ToList());

        var artefactosEvaluados = new List<ArtefactoEvaluadoResultado>();
        var resultadoPorArtefacto = new Dictionary<int, ResultadoEvaluacion>();

        foreach (var artefacto in documentos.Artefactos)
        {
            var hallazgosDelArtefacto = hallazgosPorArtefacto
                .GetValueOrDefault(artefacto.ArtefactoEsperadoId,
                    Array.Empty<HallazgoClasificado>());

            ResultadoEvaluacion resultado = ResolverResultado(
                artefacto, hallazgosDelArtefacto);

            resultadoPorArtefacto[artefacto.ArtefactoEsperadoId] = resultado;

            artefactosEvaluados.Add(new ArtefactoEvaluadoResultado(
                ArtefactoEsperadoId: artefacto.ArtefactoEsperadoId,
                EstadoAplicacionTailoring: artefacto.EstadoAplicacionTailoring,
                JustificacionNoAplica: artefacto.JustificacionNoAplica,
                Resultado: resultado,
                Observaciones: null));
        }

        // --- Paso 2: documentos analizados -----------------------------------
        var documentosAnalizados = new List<DocumentoAnalizadoResultado>();

        foreach (var artefacto in documentos.Artefactos)
        {
            var resultado = resultadoPorArtefacto[artefacto.ArtefactoEsperadoId];

            // Invariante contrato 5: PendienteEtapaFutura y NoAplica no llevan
            // documentos asociados.
            if (DebeAdjuntarEvidencia(resultado)
                && artefacto.DocumentoEncontrado is { } doc)
            {
                documentosAnalizados.Add(new DocumentoAnalizadoResultado(
                    ArtefactoEsperadoId: artefacto.ArtefactoEsperadoId,
                    NombreArchivo: doc.NombreArchivo,
                    Fuente: doc.Fuente,
                    UrlReferencia: artefacto.UrlReferencia,
                    HashContenido: doc.HashContenido));
            }
        }

        // --- Paso 3: validar cada hallazgo -----------------------------------
        // (b) y (c): cada hallazgo debe apuntar a un artefacto existente cuyo
        // resultado admita hallazgos. Violación -> excepción, no descarte.
        foreach (var hallazgo in clasificacion.Hallazgos)
        {
            if (!resultadoPorArtefacto.TryGetValue(
                    hallazgo.ArtefactoEsperadoId, out var resultado))
            {
                // (b) Hallazgo huérfano: ID inexistente.
                throw new EnsambleAuditoriaException(
                    $"Hallazgo huérfano: apunta al ArtefactoEsperadoId " +
                    $"{hallazgo.ArtefactoEsperadoId}, que no existe en " +
                    $"DocumentosExtraidos. Revisar la salida de los agentes.");
            }

            if (!DebeAdjuntarEvidencia(resultado))
            {
                // (c) Hallazgo sobre artefacto NoAplica o PendienteEtapaFutura.
                throw new EnsambleAuditoriaException(
                    $"Hallazgo inválido sobre el ArtefactoEsperadoId " +
                    $"{hallazgo.ArtefactoEsperadoId}: su resultado es " +
                    $"{resultado}, que por contrato no admite hallazgos. " +
                    $"Los validadores no deben generar hallazgos sobre " +
                    $"artefactos no exigibles o no aplicables.");
            }
        }

        // --- Paso 4: validar coherencia inversa ------------------------------
        // (d): todo artefacto NoConforme debe tener al menos un hallazgo que lo
        // explique. El consolidador NO genera ese hallazgo — debe haberlo
        // producido un validador (típicamente ComplianceValidation, para los
        // casos de artefacto faltante / tailoring incompleto / NoAplica sin
        // justificación). Si no hay hallazgo, es una inconsistencia: o el
        // validador no hizo su trabajo, o el LLM omitió el hallazgo. Se falla
        // ruidoso en vez de mostrar un NoConforme mudo en el dashboard.
        foreach (var artefacto in artefactosEvaluados)
        {
            if (artefacto.Resultado != ResultadoEvaluacion.NoConforme)
                continue;

            bool tieneHallazgo = hallazgosPorArtefacto
                .ContainsKey(artefacto.ArtefactoEsperadoId);

            if (!tieneHallazgo)
            {
                throw new EnsambleAuditoriaException(
                    $"Artefacto {artefacto.ArtefactoEsperadoId} resuelto como " +
                    $"NoConforme pero sin ningún hallazgo que lo explique. " +
                    $"El validador correspondiente (típicamente " +
                    $"ComplianceValidation) debe generar el hallazgo del " +
                    $"desvío. Revisar el prompt del validador.");
            }
        }

        return new AuditoriaResultado(
            AuditoriaId: documentos.AuditoriaId,
            ArtefactosEvaluados: artefactosEvaluados,
            Hallazgos: clasificacion.Hallazgos,
            DocumentosAnalizados: documentosAnalizados);
    }

    // ------------------------------------------------------------------------
    //  Resolución del ResultadoEvaluacion de un artefacto
    // ------------------------------------------------------------------------

    private static ResultadoEvaluacion ResolverResultado(
        ArtefactoExtraido artefacto,
        IReadOnlyList<HallazgoClasificado> hallazgos)
    {
        // 1. Etapa futura: no se exige aún. Prioridad sobre todo lo demás.
        if (artefacto.Exigibilidad == ExigibilidadArtefacto.PendienteEtapaFutura)
            return ResultadoEvaluacion.PendienteEtapaFutura;

        // 2. Según lo que dijo el tailoring.
        switch (artefacto.EstadoAplicacionTailoring)
        {
            case EstadoAplicacionTailoring.NoAplica:
                return string.IsNullOrWhiteSpace(artefacto.JustificacionNoAplica)
                    ? ResultadoEvaluacion.NoConforme   // falta justificación
                    : ResultadoEvaluacion.NoAplica;    // justificación válida

            case EstadoAplicacionTailoring.SinDeclararEnTailoring:
                return ResultadoEvaluacion.NoConforme; // tailoring incompleto

            case EstadoAplicacionTailoring.Aplica:
                // 2.a — El artefacto aplica pero no se encontró: NoConforme
                // determinístico, sin importar los hallazgos. El hallazgo que
                // lo explica lo genera ComplianceValidation (Gap Analysis).
                if (artefacto.EstadoDisponibilidad == EstadoDisponibilidad.Faltante)
                    return ResultadoEvaluacion.NoConforme;

                // 2.b — El artefacto aplica y está presente: solo NC degrada.
                // OBS/OM no son incumplimiento -> Conforme.
                bool tieneNoConformidad = hallazgos.Any(h => h.Tipo == TipoHallazgo.NC);
                return tieneNoConformidad
                    ? ResultadoEvaluacion.NoConforme
                    : ResultadoEvaluacion.Conforme;

            default:
                throw new EnsambleAuditoriaException(
                    $"EstadoAplicacionTailoring no contemplado: {artefacto.EstadoAplicacionTailoring}.");
        }
    }

    /// <summary>
    /// True si el resultado admite hallazgos y documentos asociados.
    /// Invariante contrato 5: PendienteEtapaFutura y NoAplica no.
    /// </summary>
    private static bool DebeAdjuntarEvidencia(ResultadoEvaluacion resultado)
        => resultado is ResultadoEvaluacion.Conforme
                      or ResultadoEvaluacion.NoConforme;


}
