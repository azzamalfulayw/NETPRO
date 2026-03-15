using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddWachList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "d2cccecc-bbc9-46d5-a4d4-bf21e445f37f");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "dc11bfb3-823f-4767-a87f-b2cd0e268939");

            migrationBuilder.CreateTable(
                name: "WatchLists",
                columns: table => new
                {
                    AppUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StockId = table.Column<int>(type: "int", nullable: false),
                    AddedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WatchLists", x => new { x.AppUserId, x.StockId });
                    table.ForeignKey(
                        name: "FK_WatchLists_AspNetUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WatchLists_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "d1f24682-1cee-4681-a712-7f2513704aee", "255660ba-471b-4f59-baf7-56e38d4e0fdd", "Admin", "ADMIN" },
                    { "f79ca478-e82f-4d6c-a9f1-6fd04729c875", "211b5fcf-e668-409d-aa0c-3f4bffddb339", "User", "USER" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_WatchLists_StockId",
                table: "WatchLists",
                column: "StockId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WatchLists");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "d1f24682-1cee-4681-a712-7f2513704aee");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "f79ca478-e82f-4d6c-a9f1-6fd04729c875");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "d2cccecc-bbc9-46d5-a4d4-bf21e445f37f", "461b01c1-858f-4076-b3b6-ee0a18c780e1", "Admin", "ADMIN" },
                    { "dc11bfb3-823f-4767-a87f-b2cd0e268939", "98f3c700-be7f-4967-b9b4-d27389b1915a", "User", "USER" }
                });
        }
    }
}
