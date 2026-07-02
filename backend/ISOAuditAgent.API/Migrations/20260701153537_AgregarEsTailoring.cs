using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISOAuditAgent.API.Migrations
{
    /// <inheritdoc />
    public partial class AgregarEsTailoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsTailoring",
                table: "artefactos_esperados",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            // Marca el artefacto de tailoring existente (FR 29 en PR 11-13) para que
            // TailoringReader lo ubique por el marco y no por un "FR 29" hardcodeado.
            // En una BD nueva el seed ya lo setea; esto cubre las BD ya pobladas.
            migrationBuilder.Sql(
                "UPDATE artefactos_esperados SET EsTailoring = TRUE WHERE Codigo = 'FR 29';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EsTailoring",
                table: "artefactos_esperados");
        }
    }
}
