namespace AssetFlowCore.Application.DTOs;

public record TeamResponseDto(Guid Id, string Name, string? Description, bool IsActive, DateTime CreatedAt);
