using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace dm.Banotto.Migrations
{
    public partial class init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Rounds",
                columns: table => new
                {
                    RoundId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DealerId = table.Column<ulong>(nullable: false),
                    Created = table.Column<DateTime>(nullable: false),
                    RoundStatus = table.Column<int>(nullable: false),
                    RoundType = table.Column<int>(nullable: false),
                    Ends = table.Column<DateTime>(nullable: true),
                    Completed = table.Column<DateTime>(nullable: true),
                    Roll1 = table.Column<int>(nullable: true),
                    Roll2 = table.Column<int>(nullable: true),
                    Roll3 = table.Column<int>(nullable: true),
                    RollSalt = table.Column<string>(nullable: true),
                    RollHash = table.Column<string>(nullable: true),
                    TotalBets = table.Column<int>(nullable: true),
                    TotalAmount = table.Column<int>(nullable: true),
                    TotalStraightWinners = table.Column<int>(nullable: true),
                    TotalAnyWinners = table.Column<int>(nullable: true),
                    TotalSingleWinners = table.Column<int>(nullable: true),
                    TotalWinners = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rounds", x => x.RoundId);
                });

            migrationBuilder.CreateTable(
                name: "Bets",
                columns: table => new
                {
                    BetId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Created = table.Column<DateTime>(nullable: false),
                    UserId = table.Column<ulong>(nullable: false),
                    UserBetMessageId = table.Column<ulong>(nullable: false),
                    BetType = table.Column<int>(nullable: false),
                    Amount = table.Column<int>(nullable: false),
                    IsQuick = table.Column<bool>(nullable: false),
                    Pick1 = table.Column<int>(nullable: true),
                    Pick2 = table.Column<int>(nullable: true),
                    Pick3 = table.Column<int>(nullable: true),
                    PlayType = table.Column<int>(nullable: false),
                    Confirmed = table.Column<bool>(nullable: true),
                    Winner = table.Column<bool>(nullable: true),
                    RoundId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bets", x => x.BetId);
                    table.ForeignKey(
                        name: "FK_Bets_Rounds_RoundId",
                        column: x => x.RoundId,
                        principalTable: "Rounds",
                        principalColumn: "RoundId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bets_RoundId",
                table: "Bets",
                column: "RoundId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bets");

            migrationBuilder.DropTable(
                name: "Rounds");
        }
    }
}
