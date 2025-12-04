using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftielRemote.Backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Agents",
                columns: table => new
                {
                    DeviceId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MachineName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    OperatingSystem = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastSeen = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConnectionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    TcpPort = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agents", x => x.DeviceId);
                });

            migrationBuilder.CreateTable(
                name: "ConnectionRequests",
                columns: table => new
                {
                    ConnectionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TargetDeviceId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequesterId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RequesterName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    RequesterIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectionRequests", x => x.ConnectionId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Agents_ConnectionId",
                table: "Agents",
                column: "ConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_LastSeen",
                table: "Agents",
                column: "LastSeen");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionRequests_RequestedAt",
                table: "ConnectionRequests",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionRequests_TargetDeviceId",
                table: "ConnectionRequests",
                column: "TargetDeviceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Agents");

            migrationBuilder.DropTable(
                name: "ConnectionRequests");
        }
    }
}
