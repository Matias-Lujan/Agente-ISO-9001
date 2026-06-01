using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISOAuditAgent.API.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDefaultConfiguracionSistemaSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "configuraciones_sistema",
                keyColumn: "id",
                keyValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "configuraciones_sistema",
                columns: new[] { "id", "clave", "descripcion", "valor" },
                values: new object[] { 1, "path_carpeta_templates", "Ruta de la carpeta de templates de artefactos. Configurable por entorno: reemplazar el valor por la ruta real donde el deployment aloja los templates.", "./templates" });
        }
    }
}
