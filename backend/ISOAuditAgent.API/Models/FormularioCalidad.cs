namespace ISOAuditAgent.API.Models;

/// <summary>
/// Vigencia del formulario (FR) vigente según el Departamento de Calidad.
/// Es la "referencia" contra la que se valida la vigencia detectada en los
/// documentos del proyecto: si la vigencia del documento difiere de la que está
/// registrada acá como vigente, el formulario usado está desactualizado y se
/// genera un hallazgo OBS.
///
/// Fuente reemplazable: hoy es una tabla MySQL, pero el acceso vive detrás de
/// <c>IReferenciaCalidad</c> para poder cambiar la fuente (un repositorio real
/// del Depto. de Calidad en Drive/API) sin tocar el resto del workflow.
/// </summary>
public class FormularioCalidad
{
    public int Id { get; set; }

    /// <summary>Código del formulario, ej. "FR 30", "FR 48". Único.</summary>
    public string CodigoFormulario { get; set; } = null!;

    /// <summary>Nombre del formulario (informativo), ej. "Sign-Off".</summary>
    public string? Nombre { get; set; }

    /// <summary>Vigencia del formulario vigente según Calidad.</summary>
    public DateOnly VigenciaVigente { get; set; }
}
