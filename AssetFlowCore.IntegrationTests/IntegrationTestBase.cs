using System;
using Microsoft.EntityFrameworkCore;
using AssetFlowCore.Infrastructure.Persistence;

namespace AssetFlowCore.IntegrationTests;

public abstract class IntegrationTestBase
{
    protected static AssetFlowDbContext CreateInMemoryDbContext(string? databaseName = null)
    {
        var name = databaseName ?? Guid.NewGuid().ToString();

        var options = new DbContextOptionsBuilder<AssetFlowDbContext>()
            .UseInMemoryDatabase(databaseName: name)
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AssetFlowDbContext(options);
    }
}