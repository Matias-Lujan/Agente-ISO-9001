using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ISOAuditAgent.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "configuracion_sistema",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    clave = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    valor = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    descripcion = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuracion_sistema", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "procedimiento",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    codigo = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    nombre = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    descripcion = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_procedimiento", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "etapa",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    procedimiento_id = table.Column<int>(type: "int", nullable: false),
                    nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    orden = table.Column<int>(type: "int", nullable: false),
                    descripcion = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_etapa", x => x.id);
                    table.ForeignKey(
                        name: "FK_etapa_procedimiento_procedimiento_id",
                        column: x => x.procedimiento_id,
                        principalTable: "procedimiento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "proyecto",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nombre = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    descripcion = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fecha_inicio = table.Column<DateOnly>(type: "date", nullable: true),
                    fecha_fin = table.Column<DateOnly>(type: "date", nullable: true),
                    tipo_proyecto = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    horas_estimadas = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    procedimiento_id = table.Column<int>(type: "int", nullable: false),
                    trello_board_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    clockify_project_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    drive_folder_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    activo = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proyecto", x => x.id);
                    table.ForeignKey(
                        name: "FK_proyecto_procedimiento_procedimiento_id",
                        column: x => x.procedimiento_id,
                        principalTable: "procedimiento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "artefacto_esperado",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    etapa_id = table.Column<int>(type: "int", nullable: false),
                    codigo = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    nombre = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    descripcion = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    mandatorio_tipo_a = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    mandatorio_tipo_b = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    template_drive_filename = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_artefacto_esperado", x => x.id);
                    table.ForeignKey(
                        name: "FK_artefacto_esperado_etapa_etapa_id",
                        column: x => x.etapa_id,
                        principalTable: "etapa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "configuracion_sistema",
                columns: new[] { "id", "clave", "descripcion", "valor" },
                values: new object[] { 1, "drive_carpeta_templates_id", "FolderId de Drive donde viven los templates de los FRs. Se compone con ArtefactoEsperado.TemplateDriveFilename para resolver cada template específico. Reemplazar por el FolderId real cuando exista la carpeta en el Drive de BDT.", "REEMPLAZAR_POR_FOLDERID_REAL" });

            migrationBuilder.InsertData(
                table: "procedimiento",
                columns: new[] { "id", "codigo", "descripcion", "nombre" },
                values: new object[] { 1, "PR 11-13", "Procedimiento maestro de BDT para proyectos de desarrollo de software (llave en mano y customizaciones). Define las etapas del ciclo de vida y los artefactos esperados en cada una.", "Diseño y Análisis, Desarrollo e Implementación de Software" });

            migrationBuilder.InsertData(
                table: "etapa",
                columns: new[] { "id", "descripcion", "nombre", "orden", "procedimiento_id" },
                values: new object[,]
                {
                    { 1, "Definición inicial del proyecto, alcance y tailoring.", "Planificación", 1, 1 },
                    { 2, "Relevamiento, ERS y diseño técnico.", "Análisis y Diseño", 2, 1 },
                    { 3, "Construcción del producto de software.", "Desarrollo", 3, 1 },
                    { 4, "Pruebas funcionales y casos de prueba.", "Testing", 4, 1 },
                    { 5, "Despliegue, sign-off y entrega al cliente.", "Implementación", 5, 1 },
                    { 6, "Seguimiento post-implementación y control de costos.", "Seguimiento", 6, 1 }
                });

            migrationBuilder.InsertData(
                table: "proyecto",
                columns: new[] { "id", "activo", "clockify_project_id", "descripcion", "drive_folder_id", "fecha_fin", "fecha_inicio", "horas_estimadas", "nombre", "procedimiento_id", "tipo_proyecto", "trello_board_id" },
                values: new object[] { 1, true, null, "Proyecto de prueba usado para validación end-to-end del agente. Reemplaza el mapeo que vivía en appsettings.json:DocumentAnalysis:Drive:Mappings.", "15Y7zY72QGzVlN2CqDDRFaUwVZNngLT5n", null, new DateOnly(2026, 1, 1), 1500m, "Proyecto de prueba (30.052)", 1, "A", null });

            migrationBuilder.InsertData(
                table: "artefacto_esperado",
                columns: new[] { "id", "codigo", "descripcion", "etapa_id", "mandatorio_tipo_a", "mandatorio_tipo_b", "nombre", "template_drive_filename" },
                values: new object[,]
                {
                    { 1, "FR 29", "Excel que define qué del procedimiento aplica a este proyecto. Piedra fundacional de la auditoría.", 1, true, true, "Tailoring del Proyecto", "FR-29.xlsx" },
                    { 2, "FR 30", "Documento que captura los requerimientos funcionales y no funcionales del proyecto.", 2, true, false, "Especificación de Requerimiento de Software (ERS)", "FR-30.docx" },
                    { 3, "FR 32", "Matriz de casos de prueba funcionales sobre el producto.", 4, true, false, "Casos de Prueba", "FR-32.xlsx" },
                    { 4, "FR 48", "Conformidad escrita del cliente sobre la entrega del producto.", 5, true, true, "Sign-Off", "FR-48.docx" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_artefacto_esperado_etapa_id_codigo",
                table: "artefacto_esperado",
                columns: new[] { "etapa_id", "codigo" });

            migrationBuilder.CreateIndex(
                name: "IX_configuracion_sistema_clave",
                table: "configuracion_sistema",
                column: "clave",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_etapa_procedimiento_id_orden",
                table: "etapa",
                columns: new[] { "procedimiento_id", "orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_procedimiento_codigo",
                table: "procedimiento",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_proyecto_procedimiento_id",
                table: "proyecto",
                column: "procedimiento_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "artefacto_esperado");

            migrationBuilder.DropTable(
                name: "configuracion_sistema");

            migrationBuilder.DropTable(
                name: "proyecto");

            migrationBuilder.DropTable(
                name: "etapa");

            migrationBuilder.DropTable(
                name: "procedimiento");
        }
    }
}
