using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BiblioGest.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RestrictDeleteBehaviorReservas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservas_Lectores_LectorId",
                table: "Reservas");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservas_Libros_LibroId",
                table: "Reservas");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservas_Lectores_LectorId",
                table: "Reservas",
                column: "LectorId",
                principalTable: "Lectores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservas_Libros_LibroId",
                table: "Reservas",
                column: "LibroId",
                principalTable: "Libros",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservas_Lectores_LectorId",
                table: "Reservas");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservas_Libros_LibroId",
                table: "Reservas");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservas_Lectores_LectorId",
                table: "Reservas",
                column: "LectorId",
                principalTable: "Lectores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservas_Libros_LibroId",
                table: "Reservas",
                column: "LibroId",
                principalTable: "Libros",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
