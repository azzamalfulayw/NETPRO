using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddWachListNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "d1f24682-1cee-4681-a712-7f2513704aee");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "f79ca478-e82f-4d6c-a9f1-6fd04729c875");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "WatchLists",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "050462db-8b11-471e-9dae-8fc284f165b2", "35bb9038-4dc9-4e1f-8656-4d5f87187732", "Admin", "ADMIN" },
                    { "5f672f74-3fe9-4e77-8b74-e5151782352b", "36a4a0bc-2686-4d52-81f4-4c6c5db7a92a", "User", "USER" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "050462db-8b11-471e-9dae-8fc284f165b2");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "5f672f74-3fe9-4e77-8b74-e5151782352b");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "WatchLists");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "d1f24682-1cee-4681-a712-7f2513704aee", "255660ba-471b-4f59-baf7-56e38d4e0fdd", "Admin", "ADMIN" },
                    { "f79ca478-e82f-4d6c-a9f1-6fd04729c875", "211b5fcf-e668-409d-aa0c-3f4bffddb339", "User", "USER" }
                });
        }
    }
}
