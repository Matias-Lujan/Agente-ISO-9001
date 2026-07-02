using System.Text;
using ISOAuditAgent.API.Agents.DocumentAnalysis.Parsing;

namespace ISOAuditAgent.API.Tests;

// Tests del helper de hashing SHA-256. Es una función pura → casos
// deterministas con vectores conocidos.
public class ContentHasherTests
{
    [Fact]
    public void Sha256Hex_BytesVacios_DevuelveHashConocido()
    {
        var resultado = ContentHasher.Sha256Hex([]);

        Assert.Equal(
            "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            resultado);
    }

    [Fact]
    public void Sha256Hex_TextoAbc_DevuelveHashConocido()
    {
        var resultado = ContentHasher.Sha256Hex(Encoding.UTF8.GetBytes("abc"));

        Assert.Equal(
            "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad",
            resultado);
    }

    [Fact]
    public void Sha256Hex_Siempre64HexEnMinusculas()
    {
        var resultado = ContentHasher.Sha256Hex(Encoding.UTF8.GetBytes("cualquier contenido"));

        // Formato que exige InvariantsValidator: ^[a-f0-9]{64}$
        Assert.Matches("^[a-f0-9]{64}$", resultado);
    }

    [Fact]
    public void Sha256Hex_Null_LanzaArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ContentHasher.Sha256Hex(null!));
    }
}
