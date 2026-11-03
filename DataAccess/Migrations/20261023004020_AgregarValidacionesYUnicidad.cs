using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BiblioGest.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AgregarValidacionesYUnicidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Prestamos_LectorId",
                table: "Prestamos");

            migrationBuilder.CreateIndex(
                name: "IX_Prestamos_LectorId_LibroId",
                table: "Prestamos",
                columns: new[] { "LectorId", "LibroId" },
                unique: true,
                filter: "\"Estado\" = 'Activo'");

            migrationBuilder.CreateIndex(
                name: "IX_Libros_Isbn",
                table: "Libros",
                column: "Isbn",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lectores_Email",
                table: "Lectores",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Prestamos_LectorId_LibroId",
                table: "Prestamos");

            migrationBuilder.DropIndex(
                name: "IX_Libros_Isbn",
                table: "Libros");

            migrationBuilder.DropIndex(
                name: "IX_Lectores_Email",
                table: "Lectores");

            migrationBuilder.CreateIndex(
                name: "IX_Prestamos_LectorId",
                table: "Prestamos",
                column: "LectorId");
        }
    }
}
