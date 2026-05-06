using ISOAuditAgent.Contracts;

namespace ISOAuditAgent.DocumentAnalysis.Tests;

/// <summary>
/// Tests de humo de la Fase 0: validan que la solución compila, que los
/// ensamblados se cargan y que los DTOs del contrato son construibles.
/// </summary>
/// <remarks>
/// La verificación de DI del <c>DocumentAnalysisRunner</c> vive en
/// <c>Configuration/AddDocumentAnalysisRegistrationTests</c>: como el
/// runner exige <c>IDriveClient</c> a partir de Fase 5, el provider
/// requiere un fake registrado y por eso no es un smoke test puro.
/// </remarks>
public class SmokeTests
{
    [Fact]
    public void Solucion_compila_y_assembly_de_DocumentAnalysis_se_carga()
    {
        var assembly = typeof(DocumentAnalysisRunner).Assembly;

        Assert.NotNull(assembly);
        Assert.Equal("ISOAuditAgent.DocumentAnalysis", assembly.GetName().Name);
    }

    [Fact]
    public void DocumentAnalysisRequest_se_puede_construir()
    {
        var request = new DocumentAnalysisRequest(
            AuditoriaId: 1,
            ProyectoId: 42,
            SolicitudEnUtc: DateTimeOffset.UtcNow);

        Assert.Equal(1, request.AuditoriaId);
        Assert.Equal(42, request.ProyectoId);
    }

    [Fact]
    public void Contrato_DocumentosExtraidos_se_puede_construir_con_lista_vacia()
    {
        var dto = new DocumentosExtraidos(
            AuditoriaId: 1,
            ProyectoId: 42,
            Documentos: Array.Empty<DocumentoExtraido>());

        Assert.Empty(dto.Documentos);
        Assert.Equal(FuenteDocumento.GoogleDrive, FuenteDocumento.GoogleDrive);
        Assert.Equal(FormatoContenido.PlainText, FormatoContenido.PlainText);
    }
}
