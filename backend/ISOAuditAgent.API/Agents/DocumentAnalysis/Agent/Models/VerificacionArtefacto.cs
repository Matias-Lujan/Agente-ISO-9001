using ISOAuditAgent.Contracts;

namespace ISOAuditAgent.DocumentAnalysis.Agent.Models;

public sealed record VerificacionArtefacto(
    bool Encontrado,
    string? NombreArchivo,
    string? HashContenido,
    IReadOnlyList<SeccionDetectada> Secciones,
    string? TemplateFileIdResuelto,
    string? ErrorMensaje);
