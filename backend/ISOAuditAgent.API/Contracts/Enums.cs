namespace ISOAuditAgent.Contracts;

/// <summary>
/// Origen del documento o artefacto recolectado por DocumentAnalysis.
/// Alineado con <c>contratos_agentes.md</c> §3.2 (Contrato 2).
/// </summary>
public enum FuenteDocumento
{
    Drive,
    Trello,
    Clockify,
    MSProject
}

/// <summary>
/// Indica si un artefacto declarado en el procedimiento es exigible en la
/// auditoría actual o si pertenece a una etapa futura del proyecto.
/// </summary>
public enum ExigibilidadArtefacto
{
    Exigible,
    PendienteEtapaFutura
}

/// <summary>
/// Obligatoriedad del artefacto resuelta combinando <c>TipoProyecto</c>
/// (A / B) con los flags <c>mandatorio_tipo_a</c> / <c>mandatorio_tipo_b</c>
/// del <c>ArtefactoEsperado</c>. DocumentAnalysis la entrega pre-resuelta.
/// </summary>
public enum ObligatoriedadArtefacto
{
    Mandatorio,
    EvaluarYJustificar
}

/// <summary>
/// Voz del tailoring sobre el artefacto: si declara que aplica, que no
/// aplica con justificación, o si directamente no lo declara aunque el
/// procedimiento lo espere.
/// </summary>
public enum EstadoTailoring
{
    Aplica,
    NoAplica,
    SinDeclararEnTailoring
}

/// <summary>
/// Resultado de la recolección física del artefacto. Solo los artefactos
/// exigibles que aplican se buscan; el resto queda en <c>NoBuscado</c>.
/// </summary>
public enum EstadoDisponibilidad
{
    Encontrado,
    Faltante,
    NoBuscado
}
