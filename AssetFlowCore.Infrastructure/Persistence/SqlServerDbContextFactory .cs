using AssetFlowCore.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AssetFlowCore.Infrastructure.Persistence;

public class SqlServerDbContextFactory(IOptions<DatabaseOptions> options) : IDbContextFactory
{
    private readonly DatabaseOptions _options = options.Value;

    public AssetFlowDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AssetFlowDbContext>()
            .UseSqlServer(_options.ConnectionString)
            .Options;

        return new AssetFlowDbContext(options);
    }
}