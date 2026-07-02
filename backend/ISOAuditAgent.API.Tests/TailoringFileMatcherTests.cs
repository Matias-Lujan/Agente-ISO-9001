using ISOAuditAgent.API.Agents.DocumentAnalysis.Tailoring;

namespace ISOAuditAgent.API.Tests;

// Tests de TailoringFileMatcher.Score: puntúa candidatos a archivo de tailoring
// según el código/nombre del artefacto (que vienen del marco, NO hardcodeados).
// código en el nombre → +10; palabra significativa del nombre → +5.
public class TailoringFileMatcherTests
{
    private const string Codigo = "FR 29";
    private const string Nombre = "Tailoring del Proyecto";

    [Fact]
    public void Score_CodigoYPalabra_Suma15()
    {
        Assert.Equal(15, TailoringFileMatcher.Score(
            "FR 29-05 Tailoring de proyecto (30.052).xlsx", Codigo, Nombre));
    }

    [Fact]
    public void Score_SoloCodigo_Suma10()
    {
        // Tiene el código pero no la palabra "tailoring".
        Assert.Equal(10, TailoringFileMatcher.Score(
            "FR 29-05 archivo (30.052).xlsx", Codigo, Nombre));
    }

    [Fact]
    public void Score_SoloPalabra_Suma5()
    {
        Assert.Equal(5, TailoringFileMatcher.Score(
            "Tailoring general del equipo.xlsx", Codigo, Nombre));
    }

    [Fact]
    public void Score_NiCodigoNiPalabra_Cero()
    {
        Assert.Equal(0, TailoringFileMatcher.Score(
            "planilla del proyecto (30.052).xlsx", Codigo, Nombre));
    }

    [Fact]
    public void Score_CodigoTolerante_ConSeparadores()
    {
        Assert.Equal(10, TailoringFileMatcher.Score("FR29 planilla.xlsx", Codigo, Nombre));
        Assert.Equal(10, TailoringFileMatcher.Score("FR-29 planilla.xlsx", Codigo, Nombre));
    }

    // Se EXIGE el código: aunque el nombre diga "Tailoring", un código distinto (o
    // ausente) en el nombre del archivo hace que NO sea candidato al tailoring.
    [Fact]
    public void CoincideCodigo_ExigeElCodigoEnElNombre()
    {
        Assert.True(TailoringFileMatcher.CoincideCodigo("FR 29-05 Tailoring (30.052).xlsx", Codigo));
        Assert.True(TailoringFileMatcher.CoincideCodigo("FR-29 planilla.xlsx", Codigo));

        // Código distinto, aunque tenga "Tailoring" → no coincide.
        Assert.False(TailoringFileMatcher.CoincideCodigo("FR 99-05 Tailoring (30.052).xlsx", Codigo));
        // Sin código en el nombre → no coincide.
        Assert.False(TailoringFileMatcher.CoincideCodigo("Tailoring del proyecto.xlsx", Codigo));
    }
}
