using ISOAuditAgent.DocumentAnalysis.Configuration;
using ISOAuditAgent.DocumentAnalysis.Drive;

namespace ISOAuditAgent.DocumentAnalysis.Tests.Drive;

/// <summary>
/// Tests de <see cref="DriveExclusionPolicy"/>: matching por nombre exacto
/// y por regex pre-compilados.
/// </summary>
public class DriveExclusionPolicyTests
{
    [Theory]
    [InlineData("Archivo", true)]
    [InlineData("archivo", true)]
    [InlineData("ARCHIVO", true)]
    [InlineData("Backup", true)]
    [InlineData("Operativa", false)]
    [InlineData("", false)]
    public void Excluye_por_nombre_exacto_case_insensitive(string nombre, bool excluido)
    {
        var policy = new DriveExclusionPolicy(new DriveExclusionOptions
        {
            NombresExactos = new List<string> { "Archivo", "Backup" }
        });

        Assert.Equal(excluido, policy.IsFolderExcluded(nombre));
    }

    [Theory]
    [InlineData("archivo-2024", true)]
    [InlineData("Mi backup viejo", true)]
    [InlineData("BACKUPS", true)]
    [InlineData("Procesos", false)]
    public void Excluye_por_patron_regex_compilado(string nombre, bool excluido)
    {
        var policy = new DriveExclusionPolicy(new DriveExclusionOptions
        {
            PatronesRegex = new List<string> { ".*archivo.*", ".*backup.*" }
        });

        Assert.Equal(excluido, policy.IsFolderExcluded(nombre));
    }

    [Fact]
    public void Excluye_si_aplica_nombre_exacto_O_regex_indistintamente()
    {
        var policy = new DriveExclusionPolicy(new DriveExclusionOptions
        {
            NombresExactos = new List<string> { "Borradores" },
            PatronesRegex = new List<string> { @"^\d{4}-archivo$" }
        });

        Assert.True(policy.IsFolderExcluded("Borradores"));
        Assert.True(policy.IsFolderExcluded("2023-archivo"));
        Assert.False(policy.IsFolderExcluded("Procesos"));
    }

    [Fact]
    public void Sin_reglas_configuradas_no_excluye_nada()
    {
        var policy = new DriveExclusionPolicy(new DriveExclusionOptions());

        Assert.False(policy.IsFolderExcluded("Archivo"));
        Assert.False(policy.IsFolderExcluded("Cualquier-nombre"));
    }
}
