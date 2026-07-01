using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISOAuditAgent.API.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPromptAgente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "prompts_agente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AgenteKey = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Contenido = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EsActiva = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ModificadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Comentario = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prompts_agente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_prompts_agente_usuarios_ModificadoPorUsuarioId",
                        column: x => x.ModificadoPorUsuarioId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_prompts_agente_AgenteKey_EsActiva",
                table: "prompts_agente",
                columns: new[] { "AgenteKey", "EsActiva" });

            migrationBuilder.CreateIndex(
                name: "IX_prompts_agente_AgenteKey_Version",
                table: "prompts_agente",
                columns: new[] { "AgenteKey", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_prompts_agente_ModificadoPorUsuarioId",
                table: "prompts_agente",
                column: "ModificadoPorUsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "prompts_agente");
        }
    }
}
