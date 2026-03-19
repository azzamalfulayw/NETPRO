using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddLiveStockFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "51215bee-a075-4db2-b763-cba310e8dce7");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "97218943-d435-4aab-a7bf-18f5c07e8a9a");

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentPrice",
                table: "Stocks",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPriceUpdate",
                table: "Stocks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceChangePercent",
                table: "Stocks",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "6354d205-ed6d-453e-998e-27369156d586", "425560ee-a16b-4ddd-9748-c36a04541f24", "Admin", "ADMIN" },
                    { "755067d9-de66-490d-ae06-17320ca6b2b9", "6a02ee44-6c0d-4553-be13-698d28e1dcd7", "User", "USER" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "6354d205-ed6d-453e-998e-27369156d586");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "755067d9-de66-490d-ae06-17320ca6b2b9");

            migrationBuilder.DropColumn(
                name: "CurrentPrice",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "LastPriceUpdate",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "PriceChangePercent",
                table: "Stocks");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "51215bee-a075-4db2-b763-cba310e8dce7", "de37a045-ba35-4af8-9e75-71fdbeaeae61", "Admin", "ADMIN" },
                    { "97218943-d435-4aab-a7bf-18f5c07e8a9a", "b499eab3-7273-4b04-9b63-dbfc2353229c", "User", "USER" }
                });
        }
    }
}
