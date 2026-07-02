namespace ISOAuditAgent.API.Agents.Shared;

/// <summary>
/// El servicio de IA respondió, pero con un formato que no se pudo interpretar
/// (JSON malformado, propiedades faltantes, prosa en lugar de datos). La lanzan
/// los nodos al parsear la respuesta del LLM. El clasificador de errores la
/// mapea a <c>CategoriaErrorAuditoria.RespuestaIAInvalida</c>.
/// </summary>
public sealed class RespuestaLlmInvalidaException : Exception
{
    public RespuestaLlmInvalidaException(string mensaje) : base(mensaje) { }

    public RespuestaLlmInvalidaException(string mensaje, Exception inner)
        : base(mensaje, inner) { }
}
