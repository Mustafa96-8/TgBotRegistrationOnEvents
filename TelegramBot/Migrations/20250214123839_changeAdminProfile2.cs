using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TelegramBot.Migrations
{
    /// <inheritdoc />
    public partial class changeAdminProfile2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminProfiles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false),
                    role = table.Column<int>(type: "INTEGER", nullable: false),
                    LastProfileMessageId = table.Column<int>(type: "INTEGER", nullable: true),
                    AdminState = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentEvent = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Persons",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false),
                    role = table.Column<int>(type: "INTEGER", nullable: false),
                    LastProfileMessageId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Persons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false),
                    role = table.Column<int>(type: "INTEGER", nullable: false),
                    LastProfileMessageId = table.Column<int>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                    UserState = table.Column<int>(type: "INTEGER", nullable: false),
                    HeIsEighteen = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsRegistered = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdminProfileEvent",
                columns: table => new
                {
                    AdminProfileId = table.Column<long>(type: "INTEGER", nullable: false),
                    EventId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminProfileEvent", x => new { x.AdminProfileId, x.EventId });
                    table.ForeignKey(
                        name: "FK_AdminProfileEvent_AdminProfiles_AdminProfileId",
                        column: x => x.AdminProfileId,
                        principalTable: "AdminProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdminProfileEvent_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserProfileEvent",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserProfileId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfileEvent", x => new { x.EventId, x.UserProfileId });
                    table.ForeignKey(
                        name: "FK_UserProfileEvent_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserProfileEvent_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminProfileEvent_EventId",
                table: "AdminProfileEvent",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfileEvent_UserProfileId",
                table: "UserProfileEvent",
                column: "UserProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminProfileEvent");

            migrationBuilder.DropTable(
                name: "Persons");

            migrationBuilder.DropTable(
                name: "UserProfileEvent");

            migrationBuilder.DropTable(
                name: "AdminProfiles");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "UserProfiles");
        }
    }
}
