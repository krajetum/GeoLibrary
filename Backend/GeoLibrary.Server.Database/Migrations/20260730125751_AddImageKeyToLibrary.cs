using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeoLibrary.Server.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddImageKeyToLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageKey",
                table: "Libraries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverImageKey",
                table: "Books",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageKey",
                table: "Libraries");

            migrationBuilder.DropColumn(
                name: "CoverImageKey",
                table: "Books");
        }
    }
}
