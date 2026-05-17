using ISOAuditAgent.Contracts;

namespace ISOAuditAgent.DocumentAnalysis.Agent.Models;

public sealed record ProyectoContexto(
    int ProyectoId,
    int ProcedimientoId,
    string ProcedimientoCodigo,
    int EtapaActualId,
    int EtapaActualOrden,
    string EtapaActualNombre,
    TipoProyecto TipoProyecto,
    string? DriveFolderIdProyecto,
    string? DriveFolderIdTemplates,
    IReadOnlyList<ArtefactoEsperadoView> ArtefactosEsperados);
