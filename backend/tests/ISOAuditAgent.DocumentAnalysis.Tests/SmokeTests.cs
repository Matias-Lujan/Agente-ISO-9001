using ISOAuditAgent.Contracts;
using ISOAuditAgent.DocumentAnalysis;

namespace ISOAuditAgent.DocumentAnalysis.Tests;

/// <summary>
/// Tests de humo: validan que la solución compila, que el assembly del
/// módulo se carga y que los DTOs del contrato vigente son construibles.
/// </summary>
public class SmokeTests
{
    [Fact]
    public void Assembly_de_DocumentAnalysis_se_carga()
    {
        var assembly = typeof(DocumentAnalysisRequest).Assembly;

        Assert.NotNull(assembly);
        Assert.Equal("ISOAuditAgent.DocumentAnalysis", assembly.GetName().Name);
    }

    [Fact]
    public void DocumentAnalysisRequest_se_puede_construir_con_EtapaId()
    {
        var request = new DocumentAnalysisRequest(
            AuditoriaId: 1,
            ProyectoId: 42,
            EtapaId: 3,
            SolicitudEnUtc: DateTimeOffset.UtcNow);

        Assert.Equal(1, request.AuditoriaId);
        Assert.Equal(42, request.ProyectoId);
        Assert.Equal(3, request.EtapaId);
    }

    [Fact]
    public void IniciarAuditoriaWorkflowInput_se_puede_construir()
    {
        var input = new IniciarAuditoriaWorkflowInput(
            AuditoriaId: 1,
            ProyectoId: 42,
            EtapaId: 3);

        Assert.Equal(1, input.AuditoriaId);
        Assert.Equal(42, input.ProyectoId);
        Assert.Equal(3, input.EtapaId);
    }

    [Fact]
    public void DocumentosExtraidos_se_puede_construir_con_lista_vacia()
    {
        var dto = new DocumentosExtraidos(
            AuditoriaId: 1,
            ProyectoId: 42,
            EtapaId: 3,
            Artefactos: Array.Empty<ArtefactoExtraido>());

        Assert.Empty(dto.Artefactos);
    }

    [Fact]
    public void ArtefactoExtraido_se_puede_construir_para_pendiente_etapa_futura()
    {
        var artefacto = new ArtefactoExtraido(
            ArtefactoEsperadoId: 10,
            CodigoArtefacto: "FR 32",
            NombreArtefacto: "Casos de Prueba",
            EtapaArtefactoId: 5,
            Exigibilidad: ExigibilidadArtefacto.PendienteEtapaFutura,
            Obligatoriedad: ObligatoriedadArtefacto.Mandatorio,
            EstadoTailoring: EstadoTailoring.Aplica,
            JustificacionNoAplica: null,
            EstadoDisponibilidad: EstadoDisponibilidad.NoBuscado,
            UrlReferencia: null,
            PathTemplateAbsoluto: null,
            DocumentoEncontrado: null,
            SeccionesDetectadas: Array.Empty<SeccionDetectada>());

        Assert.Equal(ExigibilidadArtefacto.PendienteEtapaFutura, artefacto.Exigibilidad);
        Assert.Equal(EstadoDisponibilidad.NoBuscado, artefacto.EstadoDisponibilidad);
        Assert.Null(artefacto.DocumentoEncontrado);
    }

    [Fact]
    public void DocumentoEncontrado_se_puede_construir_para_Drive()
    {
        var doc = new DocumentoEncontrado(
            NombreArchivo: "ERS-Proyecto-30052.docx",
            Fuente: FuenteDocumento.Drive,
            HashContenido: new string('0', 64));

        Assert.Equal(FuenteDocumento.Drive, doc.Fuente);
        Assert.Equal(64, doc.HashContenido.Length);
    }
}
