using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeoLibrary.Server.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddLibraryIdToBookDailyView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LibraryId",
                table: "BookDailyViewEntity",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_BookDailyViewEntity_LibraryId",
                table: "BookDailyViewEntity",
                column: "LibraryId");

            migrationBuilder.AddForeignKey(
                name: "FK_BookDailyViewEntity_Libraries_LibraryId",
                table: "BookDailyViewEntity",
                column: "LibraryId",
                principalTable: "Libraries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookDailyViewEntity_Libraries_LibraryId",
                table: "BookDailyViewEntity");

            migrationBuilder.DropIndex(
                name: "IX_BookDailyViewEntity_LibraryId",
                table: "BookDailyViewEntity");

            migrationBuilder.DropColumn(
                name: "LibraryId",
                table: "BookDailyViewEntity");
        }
    }
}
