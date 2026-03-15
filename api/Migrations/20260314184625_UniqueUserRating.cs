using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class UniqueUserRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Ratings_AppUserId",
                table: "Ratings");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "3580b111-fedb-4ba4-a769-9cf37c7d7554");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "7b01186c-1192-4198-9e39-314c4fe1fe92");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "d2cccecc-bbc9-46d5-a4d4-bf21e445f37f", "461b01c1-858f-4076-b3b6-ee0a18c780e1", "Admin", "ADMIN" },
                    { "dc11bfb3-823f-4767-a87f-b2cd0e268939", "98f3c700-be7f-4967-b9b4-d27389b1915a", "User", "USER" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_AppUserId_StockId",
                table: "Ratings",
                columns: new[] { "AppUserId", "StockId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Ratings_AppUserId_StockId",
                table: "Ratings");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "d2cccecc-bbc9-46d5-a4d4-bf21e445f37f");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "dc11bfb3-823f-4767-a87f-b2cd0e268939");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "3580b111-fedb-4ba4-a769-9cf37c7d7554", "7dbe374c-06b9-4a1f-aa6d-095019e64bab", "User", "USER" },
                    { "7b01186c-1192-4198-9e39-314c4fe1fe92", "91732f6f-d44b-4012-9738-be299f669d25", "Admin", "ADMIN" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_AppUserId",
                table: "Ratings",
                column: "AppUserId");
        }
    }
}
