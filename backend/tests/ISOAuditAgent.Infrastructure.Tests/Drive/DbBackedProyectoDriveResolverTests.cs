using ISOAuditAgent.Contracts;
using ISOAuditAgent.DocumentAnalysis.Drive;
using ISOAuditAgent.Infrastructure.Drive;
using ISOAuditAgent.Infrastructure.Entities;

namespace ISOAuditAgent.Infrastructure.Tests.Drive;

public sealed class DbBackedProyectoDriveResolverTests
{
    [Fact]
    public void ResolveFolderId_devuelve_el_DriveFolderId_del_proyecto_seedeado()
    {
        using var db = InMemoryDbContextFactory.CreateWithSeed();
        var resolver = new DbBackedProyectoDriveResolver(db);

        var folderId = resolver.ResolveFolderId(1);

        Assert.False(string.IsNullOrWhiteSpace(folderId));
    }

    [Fact]
    public void ResolveFolderId_lanza_si_el_proyecto_no_existe()
    {
        using var db = InMemoryDbContextFactory.CreateWithSeed();
        var resolver = new DbBackedProyectoDriveResolver(db);

        var ex = Assert.Throws<ProyectoDriveMappingNotFoundException>(
            () => resolver.ResolveFolderId(999));

        Assert.Equal(999, ex.ProyectoId);
    }

    [Fact]
    public void TryResolveFolderId_devuelve_false_si_el_proyecto_esta_inactivo()
    {
        using var db = InMemoryDbContextFactory.CreateWithSeed();
        db.Proyectos.Add(new Proyecto
        {
            Id = 2001,
            Nombre = "Inactivo",
            TipoProyecto = TipoProyecto.B,
            HorasEstimadas = 100m,
            ProcedimientoId = 1,
            DriveFolderId = "folder-xyz",
            Activo = false
        });
        db.SaveChanges();
        var resolver = new DbBackedProyectoDriveResolver(db);

        var ok = resolver.TryResolveFolderId(2001, out var folderId);

        Assert.False(ok);
        Assert.Null(folderId);
    }

    [Fact]
    public void TryResolveFolderId_devuelve_false_si_el_proyecto_no_tiene_DriveFolderId()
    {
        using var db = InMemoryDbContextFactory.CreateWithSeed();
        db.Proyectos.Add(new Proyecto
        {
            Id = 2002,
            Nombre = "Sin Drive",
            TipoProyecto = TipoProyecto.B,
            HorasEstimadas = 100m,
            ProcedimientoId = 1,
            DriveFolderId = null,
            Activo = true
        });
        db.SaveChanges();
        var resolver = new DbBackedProyectoDriveResolver(db);

        var ok = resolver.TryResolveFolderId(2002, out var folderId);

        Assert.False(ok);
        Assert.Null(folderId);
    }
}
