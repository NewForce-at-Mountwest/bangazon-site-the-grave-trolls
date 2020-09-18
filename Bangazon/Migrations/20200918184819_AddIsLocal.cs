using Microsoft.EntityFrameworkCore.Migrations;

namespace Bangazon.Migrations
{
    public partial class AddIsLocal : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isLocal",
                table: "Product",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "00000000-ffff-ffff-ffff-ffffffffffff",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "ddd7ace9-de01-4968-9301-91738eb9065e", "AQAAAAEAACcQAAAAECuzy96ZRewPIrqMUhV921FklY5jOkeOxPPu+l3IUIy8Rm5w9oqQV56XpNZqg+byKw==" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isLocal",
                table: "Product");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "00000000-ffff-ffff-ffff-ffffffffffff",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "c50b87fb-da26-433b-83e0-8ba82fbdf308", "AQAAAAEAACcQAAAAEJej5eGqF8SusRbv2ayAaKiXbAG/nsB1ljtrvYg2RKhvW/AOMCPnMNaLLNzugQiGnQ==" });
        }
    }
}
