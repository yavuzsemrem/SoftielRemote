using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftielRemote.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddBackendRegistry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BackendRegistry",
                columns: table => new
                {
                    BackendId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PublicUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    LocalIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    LastSeen = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackendRegistry", x => x.BackendId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BackendRegistry_IsActive",
                table: "BackendRegistry",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_BackendRegistry_LastSeen",
                table: "BackendRegistry",
                column: "LastSeen");

            // Row Level Security (RLS) etkinleştir
            migrationBuilder.Sql(@"
                ALTER TABLE ""BackendRegistry"" ENABLE ROW LEVEL SECURITY;
                
                CREATE POLICY ""Allow all for service_role"" ON ""BackendRegistry""
                    FOR ALL
                    TO service_role
                    USING (true)
                    WITH CHECK (true);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP POLICY IF EXISTS ""Allow all for service_role"" ON ""BackendRegistry"";
                ALTER TABLE ""BackendRegistry"" DISABLE ROW LEVEL SECURITY;
            ");

            migrationBuilder.DropTable(
                name: "BackendRegistry");
        }
    }
}
