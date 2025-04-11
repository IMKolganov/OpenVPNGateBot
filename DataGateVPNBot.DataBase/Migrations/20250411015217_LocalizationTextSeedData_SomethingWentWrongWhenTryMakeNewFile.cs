using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataGateVPNBot.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class LocalizationTextSeedData_SomethingWentWrongWhenTryMakeNewFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "xgb_botvpndev",
                table: "LocalizationTexts",
                columns: new[] { "Id", "Key", "Language", "Text" },
                values: new object[,]
                {
                    { 64, "SomethingWentWrongWhenTryMakeNewFile", 1, "Something went wrong while trying to create a new file." },
                    { 65, "SomethingWentWrongWhenTryMakeNewFile", 3, "Произошла ошибка при попытке создать новый файл." },
                    { 66, "SomethingWentWrongWhenTryMakeNewFile", 2, "Κάτι πήγε στραβά κατά την προσπάθεια δημιουργίας νέου αρχείου." }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "xgb_botvpndev",
                table: "LocalizationTexts",
                keyColumn: "Id",
                keyValue: 64);

            migrationBuilder.DeleteData(
                schema: "xgb_botvpndev",
                table: "LocalizationTexts",
                keyColumn: "Id",
                keyValue: 65);

            migrationBuilder.DeleteData(
                schema: "xgb_botvpndev",
                table: "LocalizationTexts",
                keyColumn: "Id",
                keyValue: 66);
        }
    }
}
