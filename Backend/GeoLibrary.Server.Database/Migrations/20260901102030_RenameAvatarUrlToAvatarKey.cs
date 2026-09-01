using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeoLibrary.Server.Database.Migrations
{
    /// <inheritdoc />
    public partial class RenameAvatarUrlToAvatarKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AvatarUrl",
                table: "Users",
                newName: "AvatarKey");

            // I valori esistenti sono URL firmati (ormai scaduti), non chiavi di oggetto:
            // il codice nuovo li interpreterebbe come chiavi inesistenti su MinIO.
            migrationBuilder.Sql(SqlClearAvatarKeys);
        }

        /// <summary>Azzera le chiavi avatar ereditate dalla vecchia colonna.</summary>
        private const string SqlClearAvatarKeys = "UPDATE \"Users\" SET \"AvatarKey\" = NULL";

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AvatarKey",
                table: "Users",
                newName: "AvatarUrl");
        }
    }
}
