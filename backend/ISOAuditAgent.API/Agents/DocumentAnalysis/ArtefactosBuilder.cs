using ISOAuditAgent.API.Agents.Contracts;
using ISOAuditAgent.API.Agents.DocumentAnalysis.Clockify;
using ISOAuditAgent.API.Agents.DocumentAnalysis.Trello;
using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Agents.DocumentAnalysis;

internal static class ArtefactosBuilder
{
    public static async ValueTask<IReadOnlyList<ArtefactoExtraido>> ConstruirAsync(
        ContextoAuditoria contexto,
        LlmOutput llm,
        IArtefactoFisicoChecker driveChecker,
        TrelloChecker trelloChecker,
        ClockifyChecker clockifyChecker,
        CancellationToken ct)
    {
        var porId = llm.Artefactos.ToDictionary(x => x.ArtefactoEsperadoId);

        foreach (var view in contexto.ArtefactosEsperados)
        {
            if (!porId.ContainsKey(view.ArtefactoEsperadoId))
                throw new InvalidOperationException(
                    $"El LLM no devolvió fila para ArtefactoEsperadoId={view.ArtefactoEsperadoId}. " +
                    "Invariante 1-a-1 del contrato violada.");
        }

        var idsContexto = contexto.ArtefactosEsperados.Select(v => v.ArtefactoEsperadoId).ToHashSet();
        foreach (var artefactoLlm in llm.Artefactos)
        {
            if (!idsContexto.Contains(artefactoLlm.ArtefactoEsperadoId))
                throw new InvalidOperationException(
                    $"El LLM devolvió ArtefactoEsperadoId={artefactoLlm.ArtefactoEsperadoId} " +
                    "que no estaba en el contexto. Invariante 1-a-1 del contrato violada.");
        }

        var driveFolderId = contexto.Integraciones.DriveFolderId;
        var resultado = new List<ArtefactoExtraido>(contexto.ArtefactosEsperados.Count);

        foreach (var view in contexto.ArtefactosEsperados)
        {
            var fila = porId[view.ArtefactoEsperadoId];
            var estadoTailoring = ParseEstadoTailoring(fila.EstadoTailoring);

            string? justificacion = estadoTailoring == EstadoAplicacionTailoring.NoAplica
                ? (string.IsNullOrWhiteSpace(fila.JustificacionNoAplica)
                    ? null
                    : fila.JustificacionNoAplica.Trim())
                : null;

            string? urlRef = string.IsNullOrWhiteSpace(fila.UrlReferenciaTailoring)
                ? null
                : fila.UrlReferenciaTailoring.Trim();

            EstadoDisponibilidad disponibilidad;
            DocumentoEncontrado? doc;
            IReadOnlyList<SeccionDetectada> secciones;
            IReadOnlyList<SeccionDetectada> seccionesTemplate;
            DateOnly? vigenciaDetectada = null;
            bool documentoParseable = false;
            string? codigoDetectado = null;
            string? proyectoDetectado = null;

            var debeBuscarFisicamente =
                view.Exigibilidad == ExigibilidadArtefacto.Exigible
                && estadoTailoring == EstadoAplicacionTailoring.Aplica;

            if (!debeBuscarFisicamente)
            {
                disponibilidad = EstadoDisponibilidad.NoBuscado;
                doc = null;
                secciones = Array.Empty<SeccionDetectada>();
                seccionesTemplate = Array.Empty<SeccionDetectada>();
            }
            else
            {
                var ver = await VerificarSegunFuenteAsync(
                    view,
                    contexto.Integraciones,
                    urlRef,
                    driveFolderId,
                    driveChecker,
                    trelloChecker,
                    clockifyChecker,
                    ct).ConfigureAwait(false);

                if (ver.ExportFallido)
                {
                    // Archivo existe en Drive pero no fue exportable (ej: >10 MB).
                    // No es Faltante (el archivo está) ni Encontrado (sin contenido).
                    // HallazgosDeterministicos genera un hallazgo específico para esto.
                    disponibilidad = EstadoDisponibilidad.ExportFallido;
                    doc = null;
                    secciones = Array.Empty<SeccionDetectada>();
                    seccionesTemplate = Array.Empty<SeccionDetectada>();
                }
                else if (ver.Encontrado)
                {
                    if (string.IsNullOrWhiteSpace(ver.NombreArchivo))
                        throw new InvalidOperationException(
                            $"Checker devolvió Encontrado=true sin NombreArchivo " +
                            $"para ArtefactoEsperadoId={view.ArtefactoEsperadoId}.");

                    // HashContenido requerido solo para Drive (archivos binarios).
                    // Trello/Clockify usan string.Empty — validado en InvariantsValidator.
                    if (ver.Fuente == FuenteDocumento.Drive
                        && string.IsNullOrEmpty(ver.HashContenido))
                    {
                        throw new InvalidOperationException(
                            $"DriveChecker devolvió Encontrado=true sin HashContenido " +
                            $"para ArtefactoEsperadoId={view.ArtefactoEsperadoId}.");
                    }

                    disponibilidad = EstadoDisponibilidad.Encontrado;
                    doc = new DocumentoEncontrado(
                        ver.NombreArchivo,
                        ver.Fuente,
                        ver.HashContenido ?? string.Empty);
                    secciones = ver.Secciones;
                    seccionesTemplate = ver.SeccionesTemplate;
                    vigenciaDetectada = ver.VigenciaDetectada;
                    documentoParseable = ver.DocumentoParseable;
                    codigoDetectado = ver.CodigoDetectado;
                    proyectoDetectado = ver.ProyectoDetectado;
                }
                else
                {
                    disponibilidad = EstadoDisponibilidad.Faltante;
                    doc = null;
                    secciones = Array.Empty<SeccionDetectada>();
                    seccionesTemplate = Array.Empty<SeccionDetectada>();
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
                NombreTemplateArchivo: view.NombreTemplateArchivo,
                DocumentoEncontrado: doc,
                SeccionesDetectadas: secciones,
                SeccionesTemplate: seccionesTemplate,
                Descripcion: view.Descripcion,
                VigenciaDetectada: vigenciaDetectada,
                VigenciaEsperada: view.VigenciaEsperada,
                DocumentoParseable: documentoParseable,
                CodigoDetectado: codigoDetectado,
                ProyectoDetectado: proyectoDetectado));
        }

        return resultado;
    }

    private static async ValueTask<VerificacionFisica> VerificarSegunFuenteAsync(
        ArtefactoEsperadoContexto view,
        IntegracionesProyecto integraciones,
        string? urlRef,
        string? driveFolderId,
        IArtefactoFisicoChecker driveChecker,
        TrelloChecker trelloChecker,
        ClockifyChecker clockifyChecker,
        CancellationToken ct)
    {
        switch (view.FuenteVerificacion)
        {
            case FuenteVerificacion.Drive:
                if (string.IsNullOrWhiteSpace(driveFolderId))
                    throw new InvalidOperationException(
                        $"Artefacto {view.ArtefactoEsperadoId} ({view.NombreArtefacto}) " +
                        "requiere FuenteVerificacion=Drive pero el proyecto no tiene DriveFolderId.");

                return await driveChecker.VerificarAsync(
                    integraciones,
                    view.ArtefactoEsperadoId,
                    view.CodigoArtefacto,
                    view.NombreArtefacto,
                    urlRef,
                    view.NombreTemplateArchivo,
                    ct).ConfigureAwait(false);

            case FuenteVerificacion.Trello:
                // Board ID se extrae de la URL del tailoring, no de proyectos.trello_board_id.
                // URL ausente o sin board ID extraíble → NoEncontrado (NC via ComplianceValidation).
                return await trelloChecker.VerificarAsync(urlRef, ct).ConfigureAwait(false);

            case FuenteVerificacion.Clockify:
                // Project ID se extrae de la URL del tailoring (requiere segmento /projects/{id}).
                // URLs de /reports/summary no tienen ID extraíble → NoEncontrado.
                return await clockifyChecker.VerificarAsync(urlRef, ct).ConfigureAwait(false);

            default:
                throw new NotSupportedException(
                    $"FuenteVerificacion '{view.FuenteVerificacion}' no soportada. " +
                    "Agregar el checker correspondiente y el case en este switch.");
        }
    }

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
