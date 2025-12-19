using System.Data;
using Npgsql;

namespace SoftielRemote.Backend.Api.Data;

public interface IDbConnectionFactory
{
    IDbConnection Create();
}

public sealed class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _cs;

    public DbConnectionFactory(IConfiguration config)
    {
        _cs = config.GetConnectionString("Postgres")
              ?? throw new InvalidOperationException("ConnectionStrings:Postgres configuration key is missing. Please add it to appsettings.json");
    }

    public IDbConnection Create() => new NpgsqlConnection(_cs);
}
