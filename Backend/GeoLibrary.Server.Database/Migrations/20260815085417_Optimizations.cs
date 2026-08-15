using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeoLibrary.Server.Database.Migrations
{
    /// <inheritdoc />
    public partial class Optimizations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookDailyViewEntity_Books_BookId",
                table: "BookDailyViewEntity");

            migrationBuilder.DropForeignKey(
                name: "FK_BookDailyViewEntity_Libraries_LibraryId",
                table: "BookDailyViewEntity");

            migrationBuilder.DropForeignKey(
                name: "FK_LibraryDailyViewEntity_Libraries_LibraryId",
                table: "LibraryDailyViewEntity");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LibraryDailyViewEntity",
                table: "LibraryDailyViewEntity");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BookDailyViewEntity",
                table: "BookDailyViewEntity");

            migrationBuilder.RenameTable(
                name: "LibraryDailyViewEntity",
                newName: "LibraryDailyViews");

            migrationBuilder.RenameTable(
                name: "BookDailyViewEntity",
                newName: "BookDailyViews");

            migrationBuilder.RenameIndex(
                name: "IX_BookDailyViewEntity_LibraryId",
                table: "BookDailyViews",
                newName: "IX_BookDailyViews_LibraryId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LibraryDailyViews",
                table: "LibraryDailyViews",
                columns: new[] { "LibraryId", "Date" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_BookDailyViews",
                table: "BookDailyViews",
                columns: new[] { "BookId", "LibraryId", "Date" });

            migrationBuilder.AddForeignKey(
                name: "FK_BookDailyViews_Books_BookId",
                table: "BookDailyViews",
                column: "BookId",
                principalTable: "Books",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BookDailyViews_Libraries_LibraryId",
                table: "BookDailyViews",
                column: "LibraryId",
                principalTable: "Libraries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LibraryDailyViews_Libraries_LibraryId",
                table: "LibraryDailyViews",
                column: "LibraryId",
                principalTable: "Libraries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookDailyViews_Books_BookId",
                table: "BookDailyViews");

            migrationBuilder.DropForeignKey(
                name: "FK_BookDailyViews_Libraries_LibraryId",
                table: "BookDailyViews");

            migrationBuilder.DropForeignKey(
                name: "FK_LibraryDailyViews_Libraries_LibraryId",
                table: "LibraryDailyViews");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LibraryDailyViews",
                table: "LibraryDailyViews");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BookDailyViews",
                table: "BookDailyViews");

            migrationBuilder.RenameTable(
                name: "LibraryDailyViews",
                newName: "LibraryDailyViewEntity");

            migrationBuilder.RenameTable(
                name: "BookDailyViews",
                newName: "BookDailyViewEntity");

            migrationBuilder.RenameIndex(
                name: "IX_BookDailyViews_LibraryId",
                table: "BookDailyViewEntity",
                newName: "IX_BookDailyViewEntity_LibraryId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LibraryDailyViewEntity",
                table: "LibraryDailyViewEntity",
                columns: new[] { "LibraryId", "Date" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_BookDailyViewEntity",
                table: "BookDailyViewEntity",
                columns: new[] { "BookId", "Date" });

            migrationBuilder.AddForeignKey(
                name: "FK_BookDailyViewEntity_Books_BookId",
                table: "BookDailyViewEntity",
                column: "BookId",
                principalTable: "Books",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BookDailyViewEntity_Libraries_LibraryId",
                table: "BookDailyViewEntity",
                column: "LibraryId",
                principalTable: "Libraries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LibraryDailyViewEntity_Libraries_LibraryId",
                table: "LibraryDailyViewEntity",
                column: "LibraryId",
                principalTable: "Libraries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
