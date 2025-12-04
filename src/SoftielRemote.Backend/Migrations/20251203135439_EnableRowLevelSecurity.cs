using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftielRemote.Backend.Migrations
{
    /// <inheritdoc />
    public partial class EnableRowLevelSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Row Level Security (RLS) etkinleştir - Supabase güvenlik gereksinimi
            migrationBuilder.Sql(@"
                -- Agents tablosu için RLS etkinleştir
                ALTER TABLE ""Agents"" ENABLE ROW LEVEL SECURITY;
                
                -- ConnectionRequests tablosu için RLS etkinleştir
                ALTER TABLE ""ConnectionRequests"" ENABLE ROW LEVEL SECURITY;
                
                -- __EFMigrationsHistory tablosu için RLS etkinleştir
                ALTER TABLE ""__EFMigrationsHistory"" ENABLE ROW LEVEL SECURITY;
                
                -- Service role için 'allow all' policy'leri ekle
                -- Not: Supabase'de service_role kullanılıyor, bu yüzden tüm işlemler için izin veriyoruz
                
                -- Agents tablosu için policy
                CREATE POLICY ""Allow all for service_role"" ON ""Agents""
                    FOR ALL
                    TO service_role
                    USING (true)
                    WITH CHECK (true);
                
                -- ConnectionRequests tablosu için policy
                CREATE POLICY ""Allow all for service_role"" ON ""ConnectionRequests""
                    FOR ALL
                    TO service_role
                    USING (true)
                    WITH CHECK (true);
                
                -- __EFMigrationsHistory tablosu için policy
                CREATE POLICY ""Allow all for service_role"" ON ""__EFMigrationsHistory""
                    FOR ALL
                    TO service_role
                    USING (true)
                    WITH CHECK (true);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // RLS policy'lerini kaldır
            migrationBuilder.Sql(@"
                DROP POLICY IF EXISTS ""Allow all for service_role"" ON ""Agents"";
                DROP POLICY IF EXISTS ""Allow all for service_role"" ON ""ConnectionRequests"";
                DROP POLICY IF EXISTS ""Allow all for service_role"" ON ""__EFMigrationsHistory"";
                
                -- RLS'yi devre dışı bırak
                ALTER TABLE ""Agents"" DISABLE ROW LEVEL SECURITY;
                ALTER TABLE ""ConnectionRequests"" DISABLE ROW LEVEL SECURITY;
                ALTER TABLE ""__EFMigrationsHistory"" DISABLE ROW LEVEL SECURITY;
            ");
        }
    }
}
