using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISOAuditAgent.API.Migrations
{
    /// <inheritdoc />
    public partial class AgregarConsumoTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "consumo_tokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AuditoriaId = table.Column<int>(type: "int", nullable: true),
                    AgenteKey = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Modelo = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TokensEntrada = table.Column<int>(type: "int", nullable: false),
                    TokensSalida = table.Column<int>(type: "int", nullable: false),
                    TokensTotal = table.Column<int>(type: "int", nullable: false),
                    FechaHoraUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consumo_tokens", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_consumo_tokens_AgenteKey",
                table: "consumo_tokens",
                column: "AgenteKey");

            migrationBuilder.CreateIndex(
                name: "IX_consumo_tokens_FechaHoraUtc",
                table: "consumo_tokens",
                column: "FechaHoraUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "consumo_tokens");
        }
    }
}
