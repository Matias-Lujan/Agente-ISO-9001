using System.Globalization;
using System.Text;
using ISOAuditAgent.DocumentAnalysis.Parsing;

namespace ISOAuditAgent.DocumentAnalysis.Tests.Parsing;

public sealed class TextNormalizerTests
{
    [Fact]
    public void Devuelve_cadena_vacia_para_null_o_empty()
    {
        Assert.Equal(string.Empty, TextNormalizer.Normalize(null));
        Assert.Equal(string.Empty, TextNormalizer.Normalize(string.Empty));
    }

    [Fact]
    public void Elimina_BOM_inicial()
    {
        var raw = "\uFEFFhola";
        Assert.Equal("hola", TextNormalizer.Normalize(raw));
    }

    [Fact]
    public void Unifica_line_endings_a_LF()
    {
        var raw = "linea1\r\nlinea2\rlinea3";
        Assert.Equal("linea1\nlinea2\nlinea3", TextNormalizer.Normalize(raw));
    }

    [Fact]
    public void Elimina_caracteres_de_control_pero_preserva_tab_y_LF()
    {
        var raw = "a\u0001b\u0007c\td\ne";
        Assert.Equal("abc\td\ne", TextNormalizer.Normalize(raw));
    }

    [Fact]
    public void Aplica_NFC_a_caracteres_unicode_descompuestos()
    {
        var precompuesto = "á";
        var descompuesto = "a\u0301";

        Assert.Equal(precompuesto, TextNormalizer.Normalize(descompuesto));
        Assert.Equal(precompuesto, TextNormalizer.Normalize(precompuesto));

        var nfc = TextNormalizer.Normalize(descompuesto);
        Assert.True(nfc.IsNormalized(NormalizationForm.FormC));
    }

    [Fact]
    public void TrimEnd_de_lineas_y_colapsa_saltos_multiples()
    {
        var raw = "linea uno   \n\n\n\nlinea dos    \n\n\n";
        Assert.Equal("linea uno\n\nlinea dos", TextNormalizer.Normalize(raw));
    }

    [Fact]
    public void Conserva_indentacion_inicial_de_lineas()
    {
        var raw = "  linea con sangria";
        Assert.Equal("  linea con sangria", TextNormalizer.Normalize(raw));
    }

    [Fact]
    public void Idempotente()
    {
        var raw = "  texto con\r\n\u0001  acento á y caracter\tspecial   \r\n\r\n\r\n";
        var first = TextNormalizer.Normalize(raw);
        var second = TextNormalizer.Normalize(first);

        Assert.Equal(first, second);
    }
}
