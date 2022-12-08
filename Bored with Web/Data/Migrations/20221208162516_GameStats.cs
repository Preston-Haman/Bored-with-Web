using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bored_with_Web.Data.Migrations
{
    public partial class GameStats : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameStatistics",
                columns: table => new
                {
                    Username = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GameRouteId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PlayCount = table.Column<int>(type: "int", nullable: false),
                    Wins = table.Column<int>(type: "int", nullable: false),
                    Losses = table.Column<int>(type: "int", nullable: false),
                    Stalemates = table.Column<int>(type: "int", nullable: false),
                    Forfeitures = table.Column<int>(type: "int", nullable: false),
                    IncompleteCount = table.Column<int>(type: "int", nullable: false),
                    MovesPlayed = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameStatistics", x => new { x.Username, x.GameRouteId });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameStatistics");
        }
    }
}
