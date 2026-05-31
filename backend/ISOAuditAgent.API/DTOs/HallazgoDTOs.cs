namespace ISOAuditAgent.API.DTOs;

public record HallazgoResponse(
    int    Id,
    int    AuditoriaId,
    int    ArtefactoEvaluadoId,
    string NombreArtefacto,
    string Tipo,
    string Descripcion,
    string Justificacion,
    string AgenteOrigen);
