using System.Text;
using ISOAuditAgent.DocumentAnalysis.Parsing;

namespace ISOAuditAgent.DocumentAnalysis.Tests.Parsing;

/// <summary>
/// Tests del diagnóstico de paquetes OpenXml. El helper se invoca
/// desde <see cref="DocxDocumentParser"/> y <see cref="XlsxDocumentParser"/>
/// vía reflexión (es <c>internal</c>); por eso el ensamblado expone
/// <c>InternalsVisibleTo</c> hacia este proyecto de tests, o bien usamos
/// los parsers como fachada. Aquí ejercitamos el comportamiento a
/// través de los parsers, cuyo mensaje final compone el helper.
/// </summary>
public sealed class OpenXmlPackageDiagnosticsTests
{
    [Fact]
    public async Task Archivo_vacio_falla_temprano_con_mensaje_de_tamano_cero()
    {
        using var empty = new MemoryStream(Array.Empty<byte>());
        var parser = new DocxDocumentParser();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => parser.ParseAsync(empty, "vacio.docx"));

        Assert.Contains("vacío", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("0 bytes", ex.Message);
    }

    [Fact]
    public async Task Archivo_demasiado_pequeno_falla_con_diagnostico_de_tamano()
    {
        // 50 bytes que comienzan con firma ZIP válida pero claramente
        // no son un OpenXml (mínimo razonable >= 200 bytes).
        var bytes = new byte[50];
        bytes[0] = 0x50; bytes[1] = 0x4B; bytes[2] = 0x03; bytes[3] = 0x04;
        using var stream = new MemoryStream(bytes);
        var parser = new DocxDocumentParser();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => parser.ParseAsync(stream, "chico.docx"));

        Assert.Contains("demasiado pequeño", ex.Message);
        Assert.Contains("50 bytes", ex.Message);
        Assert.Contains("chico.docx", ex.Message);
    }

    [Fact]
    public async Task Contenido_HTML_se_detecta_via_magic_number()
    {
        var html = "<!DOCTYPE html><html><body>Drive error 403</body></html>";
        var bytes = Encoding.UTF8.GetBytes(html.PadRight(300, ' '));
        using var stream = new MemoryStream(bytes);
        var parser = new DocxDocumentParser();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => parser.ParseAsync(stream, "respuesta.docx"));

        Assert.Contains("firma ZIP", ex.Message);
        Assert.Contains("magic detectado: HTML", ex.Message);
    }

    [Fact]
    public async Task PDF_disfrazado_de_DOCX_se_detecta_via_magic_number()
    {
        // %PDF-1.4 + padding hasta superar el umbral
        var bytes = Encoding.ASCII.GetBytes("%PDF-1.4".PadRight(300, ' '));
        using var stream = new MemoryStream(bytes);
        var parser = new DocxDocumentParser();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => parser.ParseAsync(stream, "raro.docx"));

        Assert.Contains("firma ZIP", ex.Message);
        Assert.Contains("magic detectado: PDF", ex.Message);
    }

    [Fact]
    public async Task Mensaje_incluye_primeros_bytes_en_hex_y_ascii()
    {
        var bytes = Encoding.ASCII.GetBytes("ABCDEFGH".PadRight(300, ' '));
        using var stream = new MemoryStream(bytes);
        var parser = new XlsxDocumentParser();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => parser.ParseAsync(stream, "mal.xlsx"));

        Assert.Contains("primeros bytes:", ex.Message);
        Assert.Contains("hex=[", ex.Message);
        Assert.Contains("ascii=\"", ex.Message);
        Assert.Contains("41 42 43 44", ex.Message); // ABCD en hex
    }
}
