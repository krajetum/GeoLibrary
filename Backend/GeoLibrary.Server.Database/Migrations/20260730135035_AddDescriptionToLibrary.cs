using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeoLibrary.Server.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddDescriptionToLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Libraries",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Libraries");
        }
    }
}
