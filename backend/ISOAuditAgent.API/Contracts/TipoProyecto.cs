namespace ISOAuditAgent.Contracts;

/// <summary>
/// Tipo de proyecto de BDT, determinado por las horas estimadas:
/// <list type="bullet">
///   <item><description><see cref="A"/>: proyectos con &gt; 1200 horas estimadas.</description></item>
///   <item><description><see cref="B"/>: proyectos con ≤ 1200 horas estimadas.</description></item>
/// </list>
/// El tipo define la <c>Obligatoriedad</c> de cada artefacto esperado
/// (mandatorio vs evaluar-y-justificar) combinando los flags
/// <c>mandatorio_tipo_a</c> / <c>mandatorio_tipo_b</c> del procedimiento.
/// </summary>
public enum TipoProyecto
{
    A,
    B
}
