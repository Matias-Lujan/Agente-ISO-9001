// ============================================================================
//  ResolutorContexto — Nodo determinista inicial del workflow
// ----------------------------------------------------------------------------
//  Primer nodo del workflow MAF. NO usa LLM. Responsabilidad:
//
//   1. Recibe IniciarAuditoriaWorkflowInput (AuditoriaId, ProyectoId, EtapaId).
//   2. Consulta BD vía repositorios de lectura.
//   3. Valida la integridad del input contra la BD (falla temprano y barato).
//   4. Precalcula Exigibilidad, Obligatoriedad y PathTemplateAbsoluto.
//   5. Emite ContextoAuditoria para que lo consuma DocumentAnalysis.
//
//  Por qué precalcula: comparar orden de etapas, combinar tipo de proyecto
//  con flags de obligatoriedad y concatenar rutas son cálculos deterministas.
//  No corresponde que los haga un agente LLM (más caro, más lento, menos
//  confiable). Coherente con el principio del proyecto: el plumbing es código.
//
//  Manejo de errores: si la validación falla, lanza ContextoAuditoriaException.
//  El background worker la captura y marca la Auditoria como Fallida. Fallar
//  acá evita gastar llamadas a Gemini en una auditoría que no puede prosperar.
// ============================================================================

using ISOAuditAgent.API.Agents.Contracts;
using ISOAuditAgent.API.Models;
using ISOAuditAgent.API.Repositories;

namespace ISOAuditAgent.API.Agents.Orchestrator;

/// <summary>
/// Excepción de validación del contexto. La lanza ResolutorContexto cuando el
/// input no es coherente con la BD. El worker la traduce a estado Fallida.
/// </summary>
public sealed class ContextoAuditoriaException : Exception
{
    public ContextoAuditoriaException(string mensaje) : base(mensaje) { }
}

/// <summary>
/// Nodo ResolutorContexto. Se registra como executor del workflow MAF.
/// Recibe sus dependencias por DI (repositorios de lectura).
/// </summary>
public sealed class ResolutorContextoService
{
    private const string ClavePathTemplates = "path_carpeta_templates";

    private readonly IProyectoRepository _proyectoRepo;
    private readonly IProcedimientoRepository _procedimientoRepo;
    private readonly IConfiguracionRepository _configuracionRepo;
    private readonly IReferenciaCalidad _referencia;
    private readonly ILogger<ResolutorContextoService> _logger;

    public ResolutorContextoService(
        IProyectoRepository proyectoRepo,
        IProcedimientoRepository procedimientoRepo,
        IConfiguracionRepository configuracionRepo,
        IReferenciaCalidad referencia,
        ILogger<ResolutorContextoService> logger)
    {
        _proyectoRepo = proyectoRepo;
        _procedimientoRepo = procedimientoRepo;
        _configuracionRepo = configuracionRepo;
        _referencia = referencia;
        _logger = logger;
    }

    /// <summary>
    /// Resuelve el ContextoAuditoria a partir del input del workflow.
    /// </summary>
    public async Task<ContextoAuditoria> ResolverAsync(
        IniciarAuditoriaWorkflowInput input, CancellationToken ct)
    {
        // --- 1. Proyecto -----------------------------------------------------
        var proyecto = await _proyectoRepo.ObtenerPorIdOrNullAsync(input.ProyectoId, ct)
            ?? throw new ContextoAuditoriaException(
                $"El proyecto {input.ProyectoId} no existe.");

        // --- 2. Etapas del procedimiento que rige al proyecto ---------------
        var etapas = await _procedimientoRepo.ObtenerEtapasAsync(
            proyecto.ProcedimientoId, ct);

        // La etapa auditada debe pertenecer al procedimiento del proyecto.
        // Si no, el input es inconsistente: la etapa es de otro procedimiento.
        var etapaAuditada = etapas.FirstOrDefault(e => e.Id == input.EtapaId)
            ?? throw new ContextoAuditoriaException(
                $"La etapa {input.EtapaId} no pertenece al procedimiento " +
                $"{proyecto.ProcedimientoId} que rige al proyecto {input.ProyectoId}.");

        // --- 3. Carpeta de templates (ConfiguracionSistema) -----------------
        var pathCarpetaTemplates = await _configuracionRepo.ObtenerValorOrNullAsync(
            ClavePathTemplates, ct)
            ?? throw new ContextoAuditoriaException(
                $"No está configurada la clave '{ClavePathTemplates}' en " +
                $"ConfiguracionSistema. No se pueden resolver los templates.");

        // --- 4. Artefactos esperados del procedimiento ----------------------
        var artefactosEsperados = await _procedimientoRepo
            .ObtenerArtefactosEsperadosAsync(proyecto.ProcedimientoId, ct);

        // --- 4.5. Referencia de Calidad: vigencias vigentes por formulario --
        var vigencias = await _referencia.ObtenerAsync(ct);

        // --- 5. Precálculo, artefacto por artefacto -------------------------
        var artefactosContexto = artefactosEsperados
            .Select(ae => ConstruirArtefactoContexto(
                ae, etapaAuditada, proyecto.TipoProyecto, pathCarpetaTemplates, vigencias))
            .ToList();

        // --- 6. Ensamblar el ContextoAuditoria ------------------------------
        return new ContextoAuditoria(
            AuditoriaId: input.AuditoriaId,
            ProyectoId: input.ProyectoId,
            EtapaId: input.EtapaId,
            ProcedimientoCodigo: etapaAuditada.Procedimiento.Codigo,
            ProcedimientoNombre: etapaAuditada.Procedimiento.Nombre,
            Integraciones: new IntegracionesProyecto(
                DriveFolderId: proyecto.DriveFolderId,
                TrelloBoardId: proyecto.TrelloBoardId,
                ClockifyProjectId: proyecto.ClockifyProjectId,
                TemplatesFolderId: pathCarpetaTemplates),
            ArtefactosEsperados: artefactosContexto,
            NombreProyecto: proyecto.Nombre);
    }

    // ------------------------------------------------------------------------
    //  Precálculo de un artefacto
    // ------------------------------------------------------------------------

    private ArtefactoEsperadoContexto ConstruirArtefactoContexto(
        ArtefactoEsperado ae,
        Etapa etapaAuditada,
        TipoProyecto tipoProyecto,
        string pathCarpetaTemplates,
        ReferenciaCalidadVigencias vigencias)
    {
        var exigibilidad = ResolverExigibilidad(ae.Etapa, etapaAuditada);
        var vigenciaEsperada = vigencias.VigenciaEsperada(ae.Codigo);

        // Sin referencia para un FR exigible: log/warning, no se valida su vigencia
        // (regla acordada: "sin referencia → solo log, no hallazgo").
        if (exigibilidad == ExigibilidadArtefacto.Exigible
            && !string.IsNullOrWhiteSpace(ae.Codigo)
            && vigenciaEsperada is null)
        {
            _logger.LogWarning(
                "Calidad no tiene vigencia cargada para el formulario '{Codigo}' " +
                "({Nombre}); no se validará la vigencia de ese artefacto.",
                ae.Codigo, ae.Nombre);
        }

        return new ArtefactoEsperadoContexto(
            ArtefactoEsperadoId: ae.Id,
            CodigoArtefacto: ae.Codigo,
            NombreArtefacto: ae.Nombre,
            EtapaArtefactoId: ae.EtapaId,
            Exigibilidad: exigibilidad,
            Obligatoriedad: ResolverObligatoriedad(ae, tipoProyecto),
            NombreTemplateArchivo: ae.PathTemplateRelativo,
            FuenteVerificacion: ae.FuenteVerificacion,
            Descripcion: ae.Descripcion,
            VigenciaEsperada: vigenciaEsperada);
    }

    /// <summary>
    /// Un artefacto es Exigible si su etapa es anterior o igual a la etapa
    /// auditada. Si pertenece a una etapa posterior, es PendienteEtapaFutura
    /// (es esperable que aún no exista; no genera hallazgos).
    /// flujo_auditoria.md, paso 3.
    /// </summary>
    private static ExigibilidadArtefacto ResolverExigibilidad(
        Etapa etapaArtefacto, Etapa etapaAuditada)
    {
        return etapaArtefacto.Orden <= etapaAuditada.Orden
            ? ExigibilidadArtefacto.Exigible
            : ExigibilidadArtefacto.PendienteEtapaFutura;
    }

    /// <summary>
    /// La obligatoriedad depende del tipo de proyecto: para tipo A se mira el
    /// flag mandatorio_tipo_a, para tipo B el mandatorio_tipo_b. Si el flag es
    /// true el artefacto es Mandatorio; si es false, EvaluarYJustificar.
    /// lectura_dominio_bdt.md §3.5, modelo_bd.md tabla ArtefactoEsperado.
    /// </summary>
    private static ObligatoriedadArtefacto ResolverObligatoriedad(
        ArtefactoEsperado ae, TipoProyecto tipoProyecto)
    {
        bool esMandatorio = tipoProyecto == TipoProyecto.A
            ? ae.MandatorioTipoA
            : ae.MandatorioTipoB;

        return esMandatorio
            ? ObligatoriedadArtefacto.Mandatorio
            : ObligatoriedadArtefacto.EvaluarYJustificar;
    }

}
