using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelGuide.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddEnglishToPOI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AudioContentEn",
                table: "POIs",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DescriptionEn",
                table: "POIs",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 17, 3, 57, 28, 10, DateTimeKind.Utc).AddTicks(8585));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AudioContentEn",
                table: "POIs");

            migrationBuilder.DropColumn(
                name: "DescriptionEn",
                table: "POIs");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 16, 12, 20, 54, 40, DateTimeKind.Utc).AddTicks(996));
        }
    }
}
