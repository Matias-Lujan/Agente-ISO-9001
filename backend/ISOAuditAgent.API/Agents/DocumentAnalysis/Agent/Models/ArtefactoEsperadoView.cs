using ISOAuditAgent.Contracts;

namespace ISOAuditAgent.DocumentAnalysis.Agent.Models;

public sealed record ArtefactoEsperadoView(
    int ArtefactoEsperadoId,
    string? Codigo,
    string Nombre,
    int EtapaId,
    int EtapaOrden,
    string EtapaNombre,
    ExigibilidadArtefacto Exigibilidad,
    ObligatoriedadArtefacto Obligatoriedad,
    string? TemplateDriveFilename);
