using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GeoLibrary.Server.Database.Migrations
{
    /// <inheritdoc />
    public partial class BookCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "Books",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BookCategories",
                columns: table => new
                {
                    BookId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookCategories", x => new { x.BookId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_BookCategories_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookCategories_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Name", "Slug" },
                values: new object[,]
                {
                    { new Guid("18e3f450-e6d9-426a-a346-9be2544624f4"), "Arte", "art" },
                    { new Guid("2bd7d3fa-d983-4c7f-874c-459f6ba3988f"), "Fantascienza", "science-fiction" },
                    { new Guid("4549113b-348b-4a70-8e77-997ad5236ba4"), "Poesia", "poetry" },
                    { new Guid("4c86ceb7-77b3-4450-81e5-a6ca7b297721"), "Fantasy", "fantasy" },
                    { new Guid("85e7682b-d147-495c-b1b0-3f550eb896a1"), "Tecnologia", "technology" },
                    { new Guid("86194ae9-2f73-4d8b-83f4-c58fb4c9f322"), "Thriller", "thriller" },
                    { new Guid("9384bd91-3151-41c9-aa11-2bfdbd15f83a"), "Diritto", "law" },
                    { new Guid("9b184f35-d69e-49ff-ada5-49d7f5f0b231"), "Narrativa", "fiction" },
                    { new Guid("b92335fe-c8ef-46e6-8146-ce1741f68b71"), "Romanzi", "romance" },
                    { new Guid("b9728b09-5beb-4a2a-bbe3-8307a5ebeec3"), "Scienza", "science" },
                    { new Guid("e31badcc-6021-4f60-b768-13aedad23e60"), "Filosofia", "philosophy" },
                    { new Guid("f02b09b5-2e5b-4e65-b5e7-d64ebc8ba119"), "Storia", "history" },
                    { new Guid("f6a3b357-9644-4b57-9c9f-54feca152ac7"), "Biografia", "biography" },
                    { new Guid("fc74ad2c-70a7-4ac0-bf39-70b545b9e8ba"), "Giallo", "mystery" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookCategories_CategoryId",
                table: "BookCategories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug",
                table: "Categories",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookCategories");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "Books");
        }
    }
}
