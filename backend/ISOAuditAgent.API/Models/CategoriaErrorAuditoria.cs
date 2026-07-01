namespace ISOAuditAgent.API.Models;

/// <summary>
/// Familia del error ocurrido durante una auditoría. Traduce una excepción
/// técnica a una categoría entendible para el auditor, que se muestra en la UI
/// y se guarda junto al registro de error.
/// </summary>
public enum CategoriaErrorAuditoria
{
    /// <summary>Datos o configuración del proyecto incompletos: sin carpeta de
    /// Drive, etapa que no corresponde, clave de configuración faltante, etc.</summary>
    ConfiguracionProyecto,

    /// <summary>Fallo comunicándose con una integración externa (Drive, Trello,
    /// Clockify): la fuente no respondió o devolvió un error.</summary>
    IntegracionExterna,

    /// <summary>La integración respondió, pero no se encontró un documento que la
    /// auditoría necesitaba (ej. la planilla de tailoring FR-29 no está en la
    /// carpeta de Drive, o su nombre no permite identificarla).</summary>
    DocumentoNoEncontrado,

    /// <summary>El servicio de IA no respondió tras los reintentos (timeout,
    /// error de red, servicio caído).</summary>
    ServicioIA,

    /// <summary>El servicio de IA respondió, pero con un formato inválido que no
    /// se pudo interpretar (JSON malformado, campos faltantes).</summary>
    RespuestaIAInvalida,

    /// <summary>Cualquier otro fallo no clasificado. Revisar el detalle técnico.</summary>
    ErrorInterno
}
