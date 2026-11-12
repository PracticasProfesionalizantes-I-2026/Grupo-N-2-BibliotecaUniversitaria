using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BiblioGest.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AgregarIsbnNormalizado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IsbnNormalizado",
                table: "Libros",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            // Completa IsbnNormalizado para los libros existentes antes de
            // crear el índice único, igual que hace LibroService con los
            // libros nuevos (sin guiones, en mayúsculas).
            migrationBuilder.Sql(
                "UPDATE Libros SET IsbnNormalizado = UPPER(REPLACE(Isbn, '-', ''));");

            migrationBuilder.CreateIndex(
                name: "IX_Libros_IsbnNormalizado",
                table: "Libros",
                column: "IsbnNormalizado",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Libros_IsbnNormalizado",
                table: "Libros");

            migrationBuilder.DropColumn(
                name: "IsbnNormalizado",
                table: "Libros");
        }
    }
}
