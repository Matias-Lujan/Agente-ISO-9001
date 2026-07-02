// ============================================================================
//  AuditoriaPersistenceService — Persistencia transaccional del resultado
// ----------------------------------------------------------------------------
//  Recibe el AuditoriaResultado (output del workflow) y lo persiste en una
//  ÚNICA transacción, siguiendo el "flujo de persistencia" del contrato 5:
//
//    1. Insertar ArtefactoEvaluado por cada item -> genera IDs.
//    2. Construir el mapa ArtefactoEsperadoId -> ArtefactoEvaluadoId.
//    3. Insertar Hallazgo resolviendo artefacto_evaluado_id con el mapa.
//    4. Insertar DocumentoAnalizado resolviendo artefacto_evaluado_id idem.
//    5. Marcar Auditoria.estado = Completada.
//    6. Todo en una transacción. Si algo falla, rollback.
//
//  Por qué un Service y no un repositorio: coordina 4 entidades + el cambio
//  de estado. Los repositorios son finos (insertan filas); este service abre
//  la transacción y los orquesta.
//
//  El marcado de Auditoria como Fallida NO vive acá: si este service lanza,
//  la transacción ya hizo rollback, y es el worker (fuera de transacción)
//  quien marca Fallida. Acá solo está el camino de éxito.
//
//  ENUMS: los DTOs de contrato y las entidades EF comparten los mismos enums
//  (EstadoAplicacionTailoring, ResultadoEvaluacion, TipoHallazgo, AgenteOrigen,
//  FuenteDocumento). Por eso la asignación es directa, sin mapeo.
//  // verificar: que esos enums estén definidos UNA sola vez en un namespace
//  común referenciado tanto por Models como por los contratos. Si estuvieran
//  duplicados (uno en Models, otro en Contracts), serían tipos distintos y la
//  asignación directa no compilaría.
// ============================================================================

using ISOAuditAgent.API.Agents.Contracts;
using ISOAuditAgent.API.Models;
using ISOAuditAgent.API.Repositories;

namespace ISOAuditAgent.API.Agents.Orchestrator;

/// <summary>
/// Persiste el resultado de una auditoría completada. La consume el worker
/// cuando el workflow terminó con éxito.
/// </summary>
public interface IAuditoriaPersistenceService
{
    /// <summary>
    /// Persiste el AuditoriaResultado completo en una transacción única y
    /// marca la auditoría como Completada. Si algo falla, hace rollback y
    /// propaga la excepción (el worker la captura y marca Fallida).
    /// </summary>
    Task PersistirResultadoAsync(AuditoriaResultado resultado, CancellationToken ct);
}

public sealed class AuditoriaPersistenceService : IAuditoriaPersistenceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IArtefactoEvaluadoRepository _artefactoEvaluadoRepo;
    private readonly IHallazgoRepository _hallazgoRepo;
    private readonly IDocumentoAnalizadoRepository _documentoRepo;
    private readonly IAuditoriaRepository _auditoriaRepo;

    public AuditoriaPersistenceService(
        IUnitOfWork unitOfWork,
        IArtefactoEvaluadoRepository artefactoEvaluadoRepo,
        IHallazgoRepository hallazgoRepo,
        IDocumentoAnalizadoRepository documentoRepo,
        IAuditoriaRepository auditoriaRepo)
    {
        _unitOfWork = unitOfWork;
        _artefactoEvaluadoRepo = artefactoEvaluadoRepo;
        _hallazgoRepo = hallazgoRepo;
        _documentoRepo = documentoRepo;
        _auditoriaRepo = auditoriaRepo;
    }

    public Task PersistirResultadoAsync(
        AuditoriaResultado resultado, CancellationToken ct)
    {
        // Todo el flujo de persistencia dentro de una sola transacción.
        // Si cualquier paso lanza, EjecutarEnTransaccionAsync hace rollback.
        return _unitOfWork.EjecutarEnTransaccionAsync(
            async tx => await PersistirInternoAsync(resultado, tx),
            ct);
    }

    private async Task PersistirInternoAsync(
        AuditoriaResultado resultado, CancellationToken ct)
    {
        // --- Paso 1: insertar ArtefactoEvaluado, recolectar IDs --------------
        // El mapa traduce el ArtefactoEsperadoId (llave lógica de los DTOs del
        // workflow) al ArtefactoEvaluadoId (FK real que generó la BD).
        var mapaArtefactoEvaluado = new Dictionary<int, int>();

        foreach (var ae in resultado.ArtefactosEvaluados)
        {
            var entidad = new ArtefactoEvaluado
            {
                AuditoriaId = resultado.AuditoriaId,
                ArtefactoEsperadoId = ae.ArtefactoEsperadoId,
                Aplica = ae.EstadoAplicacionTailoring,   // enum compartido
                JustificacionNoAplica = ae.JustificacionNoAplica,
                Resultado = ae.Resultado,                // enum compartido
                Observaciones = ae.Observaciones
            };

            int idGenerado = await _artefactoEvaluadoRepo.AgregarAsync(entidad, ct);

            // Invariante: un solo ArtefactoEvaluado por ArtefactoEsperadoId en
            // la auditoría. El ConsolidadorEnsamble ya valida los duplicados;
            // se reasegura acá.
            if (!mapaArtefactoEvaluado.TryAdd(ae.ArtefactoEsperadoId, idGenerado))
            {
                throw new InvalidOperationException(
                    $"ArtefactoEsperadoId {ae.ArtefactoEsperadoId} duplicado " +
                    $"en ArtefactosEvaluados al persistir.");
            }
        }

        // --- Paso 2 + 3: insertar Hallazgo resolviendo la FK ----------------
        var hallazgos = resultado.Hallazgos
            .Select(h => new Hallazgo
            {
                ArtefactoEvaluadoId = ResolverFk(
                    mapaArtefactoEvaluado, h.ArtefactoEsperadoId, "Hallazgo"),
                Tipo = h.Tipo,                  // enum compartido
                Descripcion = h.Descripcion,
                Justificacion = h.Justificacion,
                AgenteOrigen = h.AgenteOrigen   // enum compartido
            })
            .ToList();

        await _hallazgoRepo.AgregarRangoAsync(hallazgos, ct);

        // --- Paso 4: insertar DocumentoAnalizado resolviendo la FK ----------
        var documentos = resultado.DocumentosAnalizados
            .Select(d => new DocumentoAnalizado
            {
                AuditoriaId = resultado.AuditoriaId,
                ArtefactoEvaluadoId = ResolverFk(
                    mapaArtefactoEvaluado, d.ArtefactoEsperadoId, "DocumentoAnalizado"),
                NombreArchivo = d.NombreArchivo,
                Fuente = d.Fuente,              // enum compartido
                UrlReferencia = d.UrlReferencia,
                HashContenido = d.HashContenido
            })
            .ToList();

        await _documentoRepo.AgregarRangoAsync(documentos, ct);

        // --- Paso 5: marcar la auditoría como Completada --------------------
        // Dentro de la misma transacción: el éxito de la auditoría es atómico
        // con la persistencia de sus resultados.
        await _auditoriaRepo.MarcarCompletadaAsync(resultado.AuditoriaId, ct);
    }

    /// <summary>
    /// Traduce un ArtefactoEsperadoId a su ArtefactoEvaluadoId real usando el
    /// mapa. Si el ID no está, es una inconsistencia del DTO: un hallazgo o
    /// documento referencia un artefacto que no se evaluó. El contrato 5 dice
    /// que la API valida esto antes de persistir — esta es esa validación.
    /// </summary>
    private static int ResolverFk(
        IReadOnlyDictionary<int, int> mapa, int artefactoEsperadoId, string entidad)
    {
        if (!mapa.TryGetValue(artefactoEsperadoId, out int fk))
        {
            throw new InvalidOperationException(
                $"{entidad} referencia el ArtefactoEsperadoId " +
                $"{artefactoEsperadoId}, que no está entre los artefactos " +
                $"evaluados de la auditoría. Resultado del workflow inconsistente.");
        }
        return fk;
    }
}
