namespace AssetFlowCore.Application.UseCases.Assets.RegisterAsset;

public record RegisterAssetCommand(string Name, string SerialNumber, string Type);