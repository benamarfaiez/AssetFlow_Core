namespace AssetFlowCore.Infrastructure.Configuration;

public class CacheOptions
{
    public const string SectionName = "Cache";
    public int TeamsExpirationMinutes { get; set; } = 5;
}
