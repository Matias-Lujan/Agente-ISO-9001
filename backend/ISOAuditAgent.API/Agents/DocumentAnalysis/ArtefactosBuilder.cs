// ============================================================================
//  ArtefactosBuilder — DocumentAnalysis (Parte 4.C)
// ----------------------------------------------------------------------------
//  Lógica que combina:
//    contexto (contrato 2)
//  + lo que dijo el LLM sobre tailoring (LlmOutput)
//  + verificación física vía IArtefactoFisicoChecker
//  → IReadOnlyList<ArtefactoExtraido> del contrato 3.
//
//  ORIGEN: Agent/DocumentAnalysisAgent.BuildArtefactosAsync del diff, adaptado a:
//   - contratos del monorepo (nombres EtapaArtefactoId, NombreArtefacto,
//     CodigoArtefacto, EstadoAplicacionTailoring).
//   - IArtefactoFisicoChecker en lugar de VerificarArtefactoTool.
//   - PathTemplateAbsoluto ya viene resuelto en el contrato 2 (no se calcula
//     dentro del checker como hacía el diff). El builder lo propaga tal cual.
//
//  ÁRBOL DE DECISIÓN POR ARTEFACTO:
//
//    Exigibilidad == PendienteEtapaFutura → NoBuscado (no se invoca al checker).
//
//    estadoTailoring == NoAplica  → NoBuscado (el cliente decidió excluirlo;
//                                                consultas_cliente.md consulta 5).
//
//    estadoTailoring == SinDeclararEnTailoring → NoBuscado.
//      (Generará hallazgo NC-07 en ComplianceValidation vía determinístico.)
//
//    estadoTailoring == Aplica && Exigibilidad == Exigible → invocar al checker:
//        ver.Encontrado && ver.HashContenido != null → Encontrado
//        en otro caso                                → Faltante
//
//  REGLA DE NEGOCIO FORZADA EN CÓDIGO:
//   La justificacionNoAplica solo se propaga al contrato 3 si
//   EstadoAplicacionTailoring == NoAplica. En cualquier otro estado se fuerza
//   a null. Esto refuerza la invariante que después valida InvariantsValidator.
//
//  FALLA RUIDOSO:
//   Si el LLM no devolvió fila para algún ArtefactoEsperadoId del contexto
//   (invariante "1 a 1" del contrato del LLM), se lanza InvalidOperationException.
// ============================================================================

using ISOAuditAgent.API.Agents.Contracts;
using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Agents.DocumentAnalysis;

internal static class ArtefactosBuilder
{
    public static async ValueTask<IReadOnlyList<ArtefactoExtraido>> ConstruirAsync(
        ContextoAuditoria contexto,
        LlmOutput llm,
        IArtefactoFisicoChecker checker,
        CancellationToken ct)
    {
        // Index por ArtefactoEsperadoId para lookup O(1). El LLM puede haber
        // devuelto las filas en cualquier orden — no podemos asumir paridad
        // posicional.
        var porId = llm.Artefactos.ToDictionary(x => x.ArtefactoEsperadoId);

        // Validación temprana: TODOS los artefactos del contexto tienen su
        // fila en el LLM. Si falta alguno, fallar acá antes de ir a Drive.
        foreach (var view in contexto.ArtefactosEsperados)
        {
            if (!porId.ContainsKey(view.ArtefactoEsperadoId))
            {
                throw new InvalidOperationException(
                    $"El LLM no devolvió fila para ArtefactoEsperadoId={view.ArtefactoEsperadoId}. " +
                    "Invariante 1-a-1 del contrato violada.");
            }
        }

        // Validación temprana: el LLM no agregó IDs inventados.
        var idsContexto = contexto.ArtefactosEsperados
            .Select(v => v.ArtefactoEsperadoId)
            .ToHashSet();
        foreach (var artefactoLlm in llm.Artefactos)
        {
            if (!idsContexto.Contains(artefactoLlm.ArtefactoEsperadoId))
            {
                throw new InvalidOperationException(
                    $"El LLM devolvió ArtefactoEsperadoId={artefactoLlm.ArtefactoEsperadoId} " +
                    "que no estaba en el contexto. Invariante 1-a-1 del contrato violada.");
            }
        }

        // DriveFolderId requerido para invocar al checker. Si en algún
        // artefacto vamos a necesitar buscar archivos físicos, debe estar.
        // En MVP la implementación del checker usa Drive, así que validamos
        // arriba de forma defensiva. Cuando se agreguen Trello/Clockify como
        // fuentes, esta validación se relaja por artefacto (el checker
        // decide qué integración usar).
        var driveFolderId = contexto.Integraciones.DriveFolderId;

        var resultado = new List<ArtefactoExtraido>(contexto.ArtefactosEsperados.Count);

        foreach (var view in contexto.ArtefactosEsperados)
        {
            var fila = porId[view.ArtefactoEsperadoId];

            var estadoTailoring = ParseEstadoTailoring(fila.EstadoTailoring);

            // Regla forzada en código: justificacionNoAplica solo en NoAplica.
            string? justificacion = estadoTailoring == EstadoAplicacionTailoring.NoAplica
                ? (string.IsNullOrWhiteSpace(fila.JustificacionNoAplica)
                    ? null
                    : fila.JustificacionNoAplica.Trim())
                : null;

            string? urlRef = string.IsNullOrWhiteSpace(fila.UrlReferenciaTailoring)
                ? null
                : fila.UrlReferenciaTailoring.Trim();

            // Resolver disponibilidad y datos físicos.
            EstadoDisponibilidad disponibilidad;
            DocumentoEncontrado? doc;
            IReadOnlyList<SeccionDetectada> secciones;

            var debeBuscarFisicamente =
                view.Exigibilidad == ExigibilidadArtefacto.Exigible
                && estadoTailoring == EstadoAplicacionTailoring.Aplica;

            if (!debeBuscarFisicamente)
            {
                disponibilidad = EstadoDisponibilidad.NoBuscado;
                doc = null;
                secciones = Array.Empty<SeccionDetectada>();
            }
            else
            {
                // Defensa MVP: si el proyecto no tiene Drive, no podemos buscar.
                // Cuando lleguen otras fuentes esta condición se mueve adentro
                // del checker (que sabe qué integración necesita).
                if (string.IsNullOrWhiteSpace(driveFolderId))
                {
                    throw new InvalidOperationException(
                        $"Artefacto {view.ArtefactoEsperadoId} requiere verificación física " +
                        "pero el proyecto no tiene DriveFolderId en Integraciones. " +
                        "Un proyecto que aplique artefactos debe tener carpeta de Drive (MVP).");
                }

                var ver = await checker.VerificarAsync(
                    contexto.Integraciones,
                    view.ArtefactoEsperadoId,
                    view.CodigoArtefacto,
                    view.NombreArtefacto,
                    urlRef,
                    view.PathTemplateAbsoluto,
                    ct).ConfigureAwait(false);

                if (ver.Encontrado)
                {
                    // Inconsistencias del checker: fallar ruidoso, no enmascarar.
                    // Encontrado=true sin hash o sin nombre es bug de la
                    // implementación del checker, no un faltante.
                    if (string.IsNullOrEmpty(ver.HashContenido))
                    {
                        throw new InvalidOperationException(
                            $"IArtefactoFisicoChecker devolvió Encontrado=true sin " +
                            $"HashContenido para ArtefactoEsperadoId={view.ArtefactoEsperadoId}. " +
                            "Inconsistencia de la implementación del checker.");
                    }

                    if (string.IsNullOrWhiteSpace(ver.NombreArchivo))
                    {
                        throw new InvalidOperationException(
                            $"IArtefactoFisicoChecker devolvió Encontrado=true sin " +
                            $"NombreArchivo para ArtefactoEsperadoId={view.ArtefactoEsperadoId}. " +
                            "Inconsistencia de la implementación del checker.");
                    }

                    disponibilidad = EstadoDisponibilidad.Encontrado;
                    doc = new DocumentoEncontrado(
                        ver.NombreArchivo,
                        ver.Fuente,
                        ver.HashContenido);
                    secciones = ver.Secciones;
                }
                else
                {
                    disponibilidad = EstadoDisponibilidad.Faltante;
                    doc = null;
                    secciones = Array.Empty<SeccionDetectada>();
                }
            }

            resultado.Add(new ArtefactoExtraido(
                ArtefactoEsperadoId: view.ArtefactoEsperadoId,
                CodigoArtefacto: view.CodigoArtefacto,
                NombreArtefacto: view.NombreArtefacto,
                EtapaArtefactoId: view.EtapaArtefactoId,
                Exigibilidad: view.Exigibilidad,
                Obligatoriedad: view.Obligatoriedad,
                EstadoAplicacionTailoring: estadoTailoring,
                JustificacionNoAplica: justificacion,
                EstadoDisponibilidad: disponibilidad,
                UrlReferencia: urlRef,
                PathTemplateAbsoluto: view.PathTemplateAbsoluto,
                DocumentoEncontrado: doc,
                SeccionesDetectadas: secciones));
        }

        return resultado;
    }

    /// <summary>
    /// Convierte el string que devuelve el LLM al enum interno
    /// EstadoAplicacionTailoring. Case-insensitive, falla ruidoso ante valores
    /// no reconocidos (el system prompt es explícito sobre los 3 valores).
    /// </summary>
    private static EstadoAplicacionTailoring ParseEstadoTailoring(string s)
    {
        var t = s?.Trim() ?? string.Empty;

        if (string.Equals(t, "Aplica", StringComparison.OrdinalIgnoreCase))
            return EstadoAplicacionTailoring.Aplica;
        if (string.Equals(t, "NoAplica", StringComparison.OrdinalIgnoreCase))
            return EstadoAplicacionTailoring.NoAplica;
        if (string.Equals(t, "SinDeclararEnTailoring", StringComparison.OrdinalIgnoreCase))
            return EstadoAplicacionTailoring.SinDeclararEnTailoring;

        throw new InvalidOperationException(
            $"estadoTailoring no reconocido en la respuesta del LLM: '{s}'. " +
            "Valores válidos: Aplica, NoAplica, SinDeclararEnTailoring.");
    }
}
