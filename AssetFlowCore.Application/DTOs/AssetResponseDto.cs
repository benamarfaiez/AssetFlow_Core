namespace AssetFlowCore.Application.DTOs;

public record AssetResponseDto(Guid Id, string Name, string SerialNumber, string Type, string Status, DateTime CreatedAt);
