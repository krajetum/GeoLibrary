using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeoLibrary.Server.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddVisibilityFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                table: "Libraries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                table: "Books",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // I libri inseriti dal form prima d'ora hanno 0 copie, valore che il form non accetta più.
            migrationBuilder.Sql("UPDATE \"Books\" SET \"TotalCopies\" = 1 WHERE \"TotalCopies\" = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsHidden",
                table: "Libraries");

            migrationBuilder.DropColumn(
                name: "IsHidden",
                table: "Books");
        }
    }
}
