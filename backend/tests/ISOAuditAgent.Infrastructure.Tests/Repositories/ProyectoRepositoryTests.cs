using ISOAuditAgent.Contracts;
using ISOAuditAgent.Infrastructure.Entities;
using ISOAuditAgent.Infrastructure.Repositories;

namespace ISOAuditAgent.Infrastructure.Tests.Repositories;

public sealed class ProyectoRepositoryTests
{
    [Fact]
    public async Task GetByIdAsync_devuelve_proyecto_seedeado_con_procedimiento_cargado()
    {
        using var db = InMemoryDbContextFactory.CreateWithSeed();
        var repo = new ProyectoRepository(db);

        var proyecto = await repo.GetByIdAsync(1);

        Assert.NotNull(proyecto);
        Assert.Equal(1, proyecto!.Id);
        Assert.Equal(TipoProyecto.A, proyecto.TipoProyecto);
        Assert.NotNull(proyecto.Procedimiento);
        Assert.Equal("PR 11-13", proyecto.Procedimiento!.Codigo);
    }

    [Fact]
    public async Task GetByIdAsync_devuelve_null_si_no_existe()
    {
        using var db = InMemoryDbContextFactory.CreateWithSeed();
        var repo = new ProyectoRepository(db);

        var proyecto = await repo.GetByIdAsync(999);

        Assert.Null(proyecto);
    }

    [Fact]
    public async Task GetByIdAsync_no_devuelve_proyectos_inactivos()
    {
        using var db = InMemoryDbContextFactory.CreateWithSeed();
        db.Proyectos.Add(new Proyecto
        {
            Id = 1001,
            Nombre = "Proyecto inactivo",
            TipoProyecto = TipoProyecto.B,
            HorasEstimadas = 100m,
            ProcedimientoId = 1,
            DriveFolderId = "abc",
            Activo = false
        });
        await db.SaveChangesAsync();
        var repo = new ProyectoRepository(db);

        var proyecto = await repo.GetByIdAsync(1001);

        Assert.Null(proyecto);
    }
}
