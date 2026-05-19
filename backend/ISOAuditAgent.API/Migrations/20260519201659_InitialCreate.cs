using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISOAuditAgent.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "configuraciones_sistema",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    clave = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    valor = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    descripcion = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuraciones_sistema", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "procedimientos",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    codigo = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    nombre = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    descripcion = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_procedimientos", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nombre = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    email = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    password_hash = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    rol = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    activo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "etapas",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    procedimiento_id = table.Column<int>(type: "int", nullable: false),
                    nombre = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    orden = table.Column<int>(type: "int", nullable: false),
                    descripcion = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_etapas", x => x.id);
                    table.ForeignKey(
                        name: "FK_etapas_procedimientos_procedimiento_id",
                        column: x => x.procedimiento_id,
                        principalTable: "procedimientos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "proyectos",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nombre = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    descripcion = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fecha_inicio = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    fecha_fin = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    tipo_proyecto = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    horas_estimadas = table.Column<int>(type: "int", nullable: false),
                    procedimiento_id = table.Column<int>(type: "int", nullable: false),
                    trello_board_id = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    clockify_project_id = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    drive_folder_id = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    activo = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proyectos", x => x.id);
                    table.ForeignKey(
                        name: "FK_proyectos_procedimientos_procedimiento_id",
                        column: x => x.procedimiento_id,
                        principalTable: "procedimientos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "artefactos_esperados",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    etapa_id = table.Column<int>(type: "int", nullable: false),
                    codigo = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    nombre = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    descripcion = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    mandatorio_tipo_a = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    mandatorio_tipo_b = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    path_template_relativo = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_artefactos_esperados", x => x.id);
                    table.ForeignKey(
                        name: "FK_artefactos_esperados_etapas_etapa_id",
                        column: x => x.etapa_id,
                        principalTable: "etapas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "auditorias",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    proyecto_id = table.Column<int>(type: "int", nullable: false),
                    usuario_id = table.Column<int>(type: "int", nullable: false),
                    etapa_id = table.Column<int>(type: "int", nullable: false),
                    fecha_inicio_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    fecha_finalizacion_utc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    estado = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auditorias", x => x.id);
                    table.ForeignKey(
                        name: "FK_auditorias_etapas_etapa_id",
                        column: x => x.etapa_id,
                        principalTable: "etapas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_auditorias_proyectos_proyecto_id",
                        column: x => x.proyecto_id,
                        principalTable: "proyectos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_auditorias_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "proyecto_usuarios",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    proyecto_id = table.Column<int>(type: "int", nullable: false),
                    usuario_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proyecto_usuarios", x => x.id);
                    table.ForeignKey(
                        name: "FK_proyecto_usuarios_proyectos_proyecto_id",
                        column: x => x.proyecto_id,
                        principalTable: "proyectos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_proyecto_usuarios_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "artefactos_evaluados",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    auditoria_id = table.Column<int>(type: "int", nullable: false),
                    artefacto_esperado_id = table.Column<int>(type: "int", nullable: false),
                    aplica = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    justificacion_no_aplica = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    resultado = table.Column<int>(type: "int", nullable: false),
                    observaciones = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_artefactos_evaluados", x => x.id);
                    table.ForeignKey(
                        name: "FK_artefactos_evaluados_artefactos_esperados_artefacto_esperado~",
                        column: x => x.artefacto_esperado_id,
                        principalTable: "artefactos_esperados",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_artefactos_evaluados_auditorias_auditoria_id",
                        column: x => x.auditoria_id,
                        principalTable: "auditorias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "informes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    auditoria_id = table.Column<int>(type: "int", nullable: false),
                    fecha_generacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    tipo = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    contenido = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_informes", x => x.id);
                    table.ForeignKey(
                        name: "FK_informes_auditorias_auditoria_id",
                        column: x => x.auditoria_id,
                        principalTable: "auditorias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "documentos_analizados",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    auditoria_id = table.Column<int>(type: "int", nullable: false),
                    artefacto_evaluado_id = table.Column<int>(type: "int", nullable: false),
                    nombre_archivo = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fuente = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    url_referencia = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    hash_contenido = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documentos_analizados", x => x.id);
                    table.ForeignKey(
                        name: "FK_documentos_analizados_artefactos_evaluados_artefacto_evaluad~",
                        column: x => x.artefacto_evaluado_id,
                        principalTable: "artefactos_evaluados",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_documentos_analizados_auditorias_auditoria_id",
                        column: x => x.auditoria_id,
                        principalTable: "auditorias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "hallazgos",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    artefacto_evaluado_id = table.Column<int>(type: "int", nullable: false),
                    tipo = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    descripcion = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    justificacion = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    agente_origen = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hallazgos", x => x.id);
                    table.ForeignKey(
                        name: "FK_hallazgos_artefactos_evaluados_artefacto_evaluado_id",
                        column: x => x.artefacto_evaluado_id,
                        principalTable: "artefactos_evaluados",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "configuraciones_sistema",
                columns: new[] { "id", "clave", "descripcion", "valor" },
                values: new object[] { 1, "path_carpeta_templates", "Ruta de la carpeta de templates de artefactos. Configurable por entorno: reemplazar el valor por la ruta real donde el deployment aloja los templates.", "./templates" });

            migrationBuilder.CreateIndex(
                name: "IX_artefactos_esperados_etapa_id",
                table: "artefactos_esperados",
                column: "etapa_id");

            migrationBuilder.CreateIndex(
                name: "IX_artefactos_evaluados_artefacto_esperado_id",
                table: "artefactos_evaluados",
                column: "artefacto_esperado_id");

            migrationBuilder.CreateIndex(
                name: "IX_artefactos_evaluados_auditoria_id",
                table: "artefactos_evaluados",
                column: "auditoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_auditorias_etapa_id",
                table: "auditorias",
                column: "etapa_id");

            migrationBuilder.CreateIndex(
                name: "IX_auditorias_proyecto_id",
                table: "auditorias",
                column: "proyecto_id");

            migrationBuilder.CreateIndex(
                name: "IX_auditorias_usuario_id",
                table: "auditorias",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_configuraciones_sistema_clave",
                table: "configuraciones_sistema",
                column: "clave",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_documentos_analizados_artefacto_evaluado_id",
                table: "documentos_analizados",
                column: "artefacto_evaluado_id");

            migrationBuilder.CreateIndex(
                name: "IX_documentos_analizados_auditoria_id",
                table: "documentos_analizados",
                column: "auditoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_etapas_procedimiento_id",
                table: "etapas",
                column: "procedimiento_id");

            migrationBuilder.CreateIndex(
                name: "IX_hallazgos_artefacto_evaluado_id",
                table: "hallazgos",
                column: "artefacto_evaluado_id");

            migrationBuilder.CreateIndex(
                name: "IX_informes_auditoria_id",
                table: "informes",
                column: "auditoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_procedimientos_codigo",
                table: "procedimientos",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_proyecto_usuarios_proyecto_id_usuario_id",
                table: "proyecto_usuarios",
                columns: new[] { "proyecto_id", "usuario_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_proyecto_usuarios_usuario_id",
                table: "proyecto_usuarios",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_proyectos_procedimiento_id",
                table: "proyectos",
                column: "procedimiento_id");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_email",
                table: "usuarios",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "configuraciones_sistema");

            migrationBuilder.DropTable(
                name: "documentos_analizados");

            migrationBuilder.DropTable(
                name: "hallazgos");

            migrationBuilder.DropTable(
                name: "informes");

            migrationBuilder.DropTable(
                name: "proyecto_usuarios");

            migrationBuilder.DropTable(
                name: "artefactos_evaluados");

            migrationBuilder.DropTable(
                name: "artefactos_esperados");

            migrationBuilder.DropTable(
                name: "auditorias");

            migrationBuilder.DropTable(
                name: "etapas");

            migrationBuilder.DropTable(
                name: "proyectos");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "procedimientos");
        }
    }
}
