using ISOAuditAgent.Contracts;
using ISOAuditAgent.DocumentAnalysis.Agent;
using ISOAuditAgent.DocumentAnalysis.Agent.Models;
using ISOAuditAgent.Infrastructure.Entities;
using ISOAuditAgent.Infrastructure.Repositories;

namespace ISOAuditAgent.Infrastructure.DocumentAnalysis;

public sealed class ContextoAuditoriaService : IContextoAuditoriaService
{
    private readonly IProyectoRepository _proyectoRepo;
    private readonly IEtapaRepository _etapaRepo;
    private readonly IArtefactoEsperadoRepository _artefactoRepo;
    private readonly IConfiguracionSistemaRepository _configRepo;

    public ContextoAuditoriaService(
        IProyectoRepository proyectoRepo,
        IEtapaRepository etapaRepo,
        IArtefactoEsperadoRepository artefactoRepo,
        IConfiguracionSistemaRepository configRepo)
    {
        _proyectoRepo = proyectoRepo ?? throw new ArgumentNullException(nameof(proyectoRepo));
        _etapaRepo = etapaRepo ?? throw new ArgumentNullException(nameof(etapaRepo));
        _artefactoRepo = artefactoRepo ?? throw new ArgumentNullException(nameof(artefactoRepo));
        _configRepo = configRepo ?? throw new ArgumentNullException(nameof(configRepo));
    }

    public async Task<ProyectoContexto> GetContextoAuditoriaAsync(
        int proyectoId,
        int etapaIdActual,
        CancellationToken cancellationToken = default)
    {
        var proyecto = await _proyectoRepo
            .GetByIdAsync(proyectoId, cancellationToken)
            .ConfigureAwait(false);

        if (proyecto is null)
        {
            throw new InvalidOperationException($"No existe proyecto activo con Id={proyectoId}.");
        }

        var etapaActual = await _etapaRepo
            .GetByIdAsync(etapaIdActual, cancellationToken)
            .ConfigureAwait(false);

        if (etapaActual is null)
        {
            throw new InvalidOperationException($"No existe etapa con Id={etapaIdActual}.");
        }

        if (etapaActual.ProcedimientoId != proyecto.ProcedimientoId)
        {
            throw new InvalidOperationException(
                "La etapa indicada no pertenece al procedimiento del proyecto.");
        }

        var artefactos = await _artefactoRepo
            .ListarPorProcedimientoAsync(proyecto.ProcedimientoId, cancellationToken)
            .ConfigureAwait(false);

        var driveFolderTemplates = await _configRepo
            .GetValorAsync(ConfiguracionSistemaClaves.DriveCarpetaTemplatesId, cancellationToken)
            .ConfigureAwait(false);

        var views = new List<ArtefactoEsperadoView>();
        foreach (var a in artefactos)
        {
            if (a.Etapa is null)
            {
                throw new InvalidOperationException(
                    $"ArtefactoEsperado {a.Id} sin Etapa cargada.");
            }

            var exig = a.Etapa.Orden <= etapaActual.Orden
                ? ExigibilidadArtefacto.Exigible
                : ExigibilidadArtefacto.PendienteEtapaFutura;

            var oblig = proyecto.TipoProyecto switch
            {
                TipoProyecto.A => a.MandatorioTipoA
                    ? ObligatoriedadArtefacto.Mandatorio
                    : ObligatoriedadArtefacto.EvaluarYJustificar,
                TipoProyecto.B => a.MandatorioTipoB
                    ? ObligatoriedadArtefacto.Mandatorio
                    : ObligatoriedadArtefacto.EvaluarYJustificar,
                _ => ObligatoriedadArtefacto.EvaluarYJustificar
            };

            views.Add(new ArtefactoEsperadoView(
                a.Id,
                a.Codigo,
                a.Nombre,
                a.EtapaId,
                a.Etapa.Orden,
                a.Etapa.Nombre,
                exig,
                oblig,
                a.TemplateDriveFilename));
        }

        return new ProyectoContexto(
            proyecto.Id,
            proyecto.ProcedimientoId,
            proyecto.Procedimiento?.Codigo ?? string.Empty,
            etapaActual.Id,
            etapaActual.Orden,
            etapaActual.Nombre,
            proyecto.TipoProyecto,
            proyecto.DriveFolderId,
            driveFolderTemplates,
            views);
    }
}
