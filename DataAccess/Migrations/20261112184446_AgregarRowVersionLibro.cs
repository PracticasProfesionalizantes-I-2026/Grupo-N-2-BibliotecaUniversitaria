using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BiblioGest.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AgregarRowVersionLibro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Libros",
                type: "BLOB",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.Sql("UPDATE Libros SET RowVersion = randomblob(8);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Libros");
        }
    }
}
