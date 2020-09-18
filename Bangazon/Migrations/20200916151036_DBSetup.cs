using Microsoft.EntityFrameworkCore.Migrations;

namespace Bangazon.Migrations
{
    public partial class DBSetup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "00000000-ffff-ffff-ffff-ffffffffffff",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "c50b87fb-da26-433b-83e0-8ba82fbdf308", "AQAAAAEAACcQAAAAEJej5eGqF8SusRbv2ayAaKiXbAG/nsB1ljtrvYg2RKhvW/AOMCPnMNaLLNzugQiGnQ==" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "00000000-ffff-ffff-ffff-ffffffffffff",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "a1fb311f-11c6-40dc-ac62-4a560cd0d55d", "AQAAAAEAACcQAAAAEDXGzgod9OAtauuWI4gtKoCInSFxN/sTt1VBe6knOLTff2GJDiKCFAPuR0wJvtxYPA==" });
        }
    }
}
