using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddLivePriceFieldsToStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "6354d205-ed6d-453e-998e-27369156d586");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "755067d9-de66-490d-ae06-17320ca6b2b9");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "3b99ddb3-4fee-4839-bd1f-d5f88fb423be", "d2a5c121-a7d5-43e6-96c8-541d50e77de5", "Admin", "ADMIN" },
                    { "8e2931fb-9545-424e-b9bf-77cc122afeab", "77b2a84e-f742-4648-9311-fa80d2e42e90", "User", "USER" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "3b99ddb3-4fee-4839-bd1f-d5f88fb423be");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "8e2931fb-9545-424e-b9bf-77cc122afeab");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "6354d205-ed6d-453e-998e-27369156d586", "425560ee-a16b-4ddd-9748-c36a04541f24", "Admin", "ADMIN" },
                    { "755067d9-de66-490d-ae06-17320ca6b2b9", "6a02ee44-6c0d-4553-be13-698d28e1dcd7", "User", "USER" }
                });
        }
    }
}
