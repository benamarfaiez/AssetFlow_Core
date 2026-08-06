using MediatR;

namespace AssetFlowCore.Application.UseCases.Assets.RestoreAssetToService;

public record RestoreAssetToServiceCommand(Guid Id, string Reason) : IRequest;
