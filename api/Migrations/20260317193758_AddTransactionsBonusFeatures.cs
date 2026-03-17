using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionsBonusFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "03147c49-80a0-44ff-8b5f-37973936e7f5");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "b7fa5d2a-e506-4610-b218-e8bd88d2b9fd");

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "Transactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "51215bee-a075-4db2-b763-cba310e8dce7", "de37a045-ba35-4af8-9e75-71fdbeaeae61", "Admin", "ADMIN" },
                    { "97218943-d435-4aab-a7bf-18f5c07e8a9a", "b499eab3-7273-4b04-9b63-dbfc2353229c", "User", "USER" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "51215bee-a075-4db2-b763-cba310e8dce7");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "97218943-d435-4aab-a7bf-18f5c07e8a9a");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Transactions");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "03147c49-80a0-44ff-8b5f-37973936e7f5", "50e35d85-6559-4a97-ac18-3c98e2995029", "User", "USER" },
                    { "b7fa5d2a-e506-4610-b218-e8bd88d2b9fd", "3f12728a-bdc2-4346-bc4d-84974201e34e", "Admin", "ADMIN" }
                });
        }
    }
}
