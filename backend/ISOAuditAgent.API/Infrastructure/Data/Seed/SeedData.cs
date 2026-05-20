using ISOAuditAgent.Contracts;
using ISOAuditAgent.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace ISOAuditAgent.Infrastructure.Data.Seed;

/// <summary>
/// Seed estático que viaja en la migración inicial.
/// </summary>
/// <remarks>
/// <para>
/// <b>Alcance — seed mínimo de muestra</b> (Fase B, iteración 1 del plan
/// de desarrollo): suficiente para probar el cruce
/// tailoring × artefactos esperados end-to-end. El seed completo del
/// procedimiento PR 11-13 (todas las etapas con todos los artefactos
/// y su mandatoriedad por tipo A/B) se cargará en una iteración posterior
/// cuando el equipo provea los datos crudos del cliente.
/// </para>
/// <para>
/// Datos cargados:
/// </para>
/// <list type="bullet">
///   <item><description>
///     1 procedimiento <c>PR 11-13</c> con sus 6 etapas oficiales del
///     ciclo de vida de desarrollo de software de BDT.
///   </description></item>
///   <item><description>
///     4 artefactos esperados representativos de los FRs más comunes:
///     FR 29 (Tailoring), FR 30 (ERS), FR 32 (Casos de Prueba),
///     FR 48 (Sign-Off).
///   </description></item>
///   <item><description>
///     1 proyecto de prueba (Id = 1) con su mapeo a la carpeta de Drive
///     del proyecto 30.052 que ya estaba en <c>appsettings.json</c>.
///   </description></item>
///   <item><description>
///     1 entrada de <c>configuracion_sistema</c> con la clave
///     <c>drive_carpeta_templates_id</c> (FolderId placeholder; se
///     reemplaza cuando exista la carpeta real de templates en Drive).
///   </description></item>
/// </list>
/// </remarks>
internal static class SeedData
{
    // IDs fijos para que el seed sea idempotente y referenciable entre
    // migraciones. Los rangos se asignan por entidad para evitar choques
    // cuando el equipo agregue datos manualmente.
    private const int PrIdSoftware = 1;

    public const int EtapaIdPlanificacion = 1;
    public const int EtapaIdAnalisisDiseno = 2;
    public const int EtapaIdDesarrollo = 3;
    public const int EtapaIdTesting = 4;
    public const int EtapaIdImplementacion = 5;
    public const int EtapaIdSeguimiento = 6;

    private const int ArtFr29Tailoring = 1;
    private const int ArtFr30Ers = 2;
    private const int ArtFr32CasosPrueba = 3;
    private const int ArtFr48SignOff = 4;

    private const int ProyectoIdMuestra = 1;

    private const int ConfigIdDriveTemplates = 1;

    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Procedimiento>().HasData(new Procedimiento
        {
            Id = PrIdSoftware,
            Codigo = "PR 11-13",
            Nombre = "Diseño y Análisis, Desarrollo e Implementación de Software",
            Descripcion = "Procedimiento maestro de BDT para proyectos de desarrollo de software (llave en mano y customizaciones). Define las etapas del ciclo de vida y los artefactos esperados en cada una."
        });

        modelBuilder.Entity<Etapa>().HasData(
            new Etapa { Id = EtapaIdPlanificacion,   ProcedimientoId = PrIdSoftware, Nombre = "Planificación",       Orden = 1, Descripcion = "Definición inicial del proyecto, alcance y tailoring." },
            new Etapa { Id = EtapaIdAnalisisDiseno,  ProcedimientoId = PrIdSoftware, Nombre = "Análisis y Diseño",   Orden = 2, Descripcion = "Relevamiento, ERS y diseño técnico." },
            new Etapa { Id = EtapaIdDesarrollo,      ProcedimientoId = PrIdSoftware, Nombre = "Desarrollo",          Orden = 3, Descripcion = "Construcción del producto de software." },
            new Etapa { Id = EtapaIdTesting,         ProcedimientoId = PrIdSoftware, Nombre = "Testing",             Orden = 4, Descripcion = "Pruebas funcionales y casos de prueba." },
            new Etapa { Id = EtapaIdImplementacion,  ProcedimientoId = PrIdSoftware, Nombre = "Implementación",      Orden = 5, Descripcion = "Despliegue, sign-off y entrega al cliente." },
            new Etapa { Id = EtapaIdSeguimiento,     ProcedimientoId = PrIdSoftware, Nombre = "Seguimiento",         Orden = 6, Descripcion = "Seguimiento post-implementación y control de costos." }
        );

        // Artefactos representativos. Los flags mandatorio_tipo_a/b son
        // valores de muestra que el equipo deberá ajustar contra el
        // procedimiento real cuando se cargue el seed completo.
        modelBuilder.Entity<ArtefactoEsperado>().HasData(
            new ArtefactoEsperado
            {
                Id = ArtFr29Tailoring,
                EtapaId = EtapaIdPlanificacion,
                Codigo = "FR 29",
                Nombre = "Tailoring del Proyecto",
                Descripcion = "Excel que define qué del procedimiento aplica a este proyecto. Piedra fundacional de la auditoría.",
                MandatorioTipoA = true,
                MandatorioTipoB = true,
                TemplateDriveFilename = "FR-29.xlsx"
            },
            new ArtefactoEsperado
            {
                Id = ArtFr30Ers,
                EtapaId = EtapaIdAnalisisDiseno,
                Codigo = "FR 30",
                Nombre = "Especificación de Requerimiento de Software (ERS)",
                Descripcion = "Documento que captura los requerimientos funcionales y no funcionales del proyecto.",
                MandatorioTipoA = true,
                MandatorioTipoB = false,
                TemplateDriveFilename = "FR-30.docx"
            },
            new ArtefactoEsperado
            {
                Id = ArtFr32CasosPrueba,
                EtapaId = EtapaIdTesting,
                Codigo = "FR 32",
                Nombre = "Casos de Prueba",
                Descripcion = "Matriz de casos de prueba funcionales sobre el producto.",
                MandatorioTipoA = true,
                MandatorioTipoB = false,
                TemplateDriveFilename = "FR-32.xlsx"
            },
            new ArtefactoEsperado
            {
                Id = ArtFr48SignOff,
                EtapaId = EtapaIdImplementacion,
                Codigo = "FR 48",
                Nombre = "Sign-Off",
                Descripcion = "Conformidad escrita del cliente sobre la entrega del producto.",
                MandatorioTipoA = true,
                MandatorioTipoB = true,
                TemplateDriveFilename = "FR-48.docx"
            }
        );

        modelBuilder.Entity<Proyecto>().HasData(new Proyecto
        {
            Id = ProyectoIdMuestra,
            Nombre = "Proyecto de prueba (30.052)",
            Descripcion = "Proyecto de prueba usado para validación end-to-end del agente. Reemplaza el mapeo que vivía en appsettings.json:DocumentAnalysis:Drive:Mappings.",
            FechaInicio = new DateOnly(2026, 1, 1),
            FechaFin = null,
            TipoProyecto = TipoProyecto.A,
            HorasEstimadas = 1500m,
            ProcedimientoId = PrIdSoftware,
            TrelloBoardId = null,
            ClockifyProjectId = null,
            DriveFolderId = "15Y7zY72QGzVlN2CqDDRFaUwVZNngLT5n",
            Activo = true
        });

        modelBuilder.Entity<ConfiguracionSistema>().HasData(new ConfiguracionSistema
        {
            Id = ConfigIdDriveTemplates,
            Clave = ConfiguracionSistemaClaves.DriveCarpetaTemplatesId,
            Valor = "REEMPLAZAR_POR_FOLDERID_REAL",
            Descripcion = "FolderId de Drive donde viven los templates de los FRs. Se compone con ArtefactoEsperado.TemplateDriveFilename para resolver cada template específico. Reemplazar por el FolderId real cuando exista la carpeta en el Drive de BDT."
        });
    }
}
