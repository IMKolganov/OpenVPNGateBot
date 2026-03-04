using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataGateVPNBot.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class LocalizationTextSeedData_ChooseOpenVpnServer : Migration
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
                    { 61, "ChooseOpenVpnServer", 1, "Choose an OpenVPN server:" },
                    { 62, "ChooseOpenVpnServer", 3, "Выберите сервер OpenVPN:" },
                    { 63, "ChooseOpenVpnServer", 2, "Επιλέξτε διακομιστή OpenVPN:" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "xgb_botvpndev",
                table: "LocalizationTexts",
                keyColumn: "Id",
                keyValue: 61);

            migrationBuilder.DeleteData(
                schema: "xgb_botvpndev",
                table: "LocalizationTexts",
                keyColumn: "Id",
                keyValue: 62);

            migrationBuilder.DeleteData(
                schema: "xgb_botvpndev",
                table: "LocalizationTexts",
                keyColumn: "Id",
                keyValue: 63);
        }
    }
}
