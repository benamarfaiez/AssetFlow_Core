namespace AssetFlowCore.Infrastructure.Persistence;

public interface IDbContextFactory
{
    AssetFlowDbContext Create();
}