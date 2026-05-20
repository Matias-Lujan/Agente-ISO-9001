using System.Text;
using ISOAuditAgent.DocumentAnalysis.Parsing;

namespace ISOAuditAgent.DocumentAnalysis.Tests.Parsing;

public sealed class ContentHasherTests
{
    private const string EmptySha256Hex =
        "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

    [Fact]
    public void ComputeSha256OfBytes_de_buffer_vacio_es_constante_conocida()
    {
        var hash = ContentHasher.ComputeSha256OfBytes(ReadOnlySpan<byte>.Empty);
        Assert.Equal(EmptySha256Hex, hash);
    }

    [Fact]
    public void ComputeSha256OfBytes_devuelve_hex_64_caracteres_lowercase()
    {
        var bytes = Encoding.UTF8.GetBytes("hola mundo");
        var hash = ContentHasher.ComputeSha256OfBytes(bytes);

        Assert.Equal(64, hash.Length);
        Assert.Matches("^[0-9a-f]{64}$", hash);
    }

    [Fact]
    public void ComputeSha256OfBytes_es_deterministico()
    {
        var bytes = Encoding.UTF8.GetBytes("mismo input");
        Assert.Equal(
            ContentHasher.ComputeSha256OfBytes(bytes),
            ContentHasher.ComputeSha256OfBytes(bytes));
    }

    [Fact]
    public void ComputeSha256OfBytes_distinto_para_inputs_diferentes()
    {
        Assert.NotEqual(
            ContentHasher.ComputeSha256OfBytes(Encoding.UTF8.GetBytes("a")),
            ContentHasher.ComputeSha256OfBytes(Encoding.UTF8.GetBytes("b")));
    }

    [Fact]
    public async Task ComputeSha256OfStreamAsync_coincide_con_ComputeSha256OfBytes()
    {
        var bytes = Encoding.UTF8.GetBytes("contenido del archivo de prueba");
        using var ms = new MemoryStream(bytes, writable: false);

        var hashStream = await ContentHasher.ComputeSha256OfStreamAsync(ms);
        var hashBytes = ContentHasher.ComputeSha256OfBytes(bytes);

        Assert.Equal(hashBytes, hashStream);
    }

    [Fact]
    public async Task ComputeSha256OfStreamAsync_deja_stream_en_position_0_si_es_seekable()
    {
        var bytes = Encoding.UTF8.GetBytes("hola");
        using var ms = new MemoryStream(bytes);

        await ContentHasher.ComputeSha256OfStreamAsync(ms);

        Assert.Equal(0, ms.Position);
    }

    [Fact]
    public void ComputeSha256OfText_codifica_en_UTF8()
    {
        var hashText = ContentHasher.ComputeSha256OfText("auditoría ISO 9001");
        var hashBytes = ContentHasher.ComputeSha256OfBytes(
            Encoding.UTF8.GetBytes("auditoría ISO 9001"));

        Assert.Equal(hashBytes, hashText);
    }

    [Fact]
    public void ComputeSha256OfText_de_texto_vacio_es_constante_conocida()
    {
        Assert.Equal(EmptySha256Hex, ContentHasher.ComputeSha256OfText(string.Empty));
    }
}
