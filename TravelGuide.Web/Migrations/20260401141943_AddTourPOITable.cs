using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelGuide.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddTourPOITable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TourPOIs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TourId = table.Column<int>(type: "int", nullable: false),
                    PoiId = table.Column<int>(type: "int", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourPOIs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourPOIs_POIs_PoiId",
                        column: x => x.PoiId,
                        principalTable: "POIs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TourPOIs_Tours_TourId",
                        column: x => x.TourId,
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TourPOIs_PoiId",
                table: "TourPOIs",
                column: "PoiId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPOIs_TourId",
                table: "TourPOIs",
                column: "TourId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TourPOIs");
        }
    }
}
