using AssetFlowCore.Application.Interfaces;
using AssetFlowCore.Application.Services;
using AssetFlowCore.Application.UseCases.Assets.DecommissionAsset;
using AssetFlowCore.Application.UseCases.Assets.GetAllAssets;
using AssetFlowCore.Application.UseCases.Assets.RegisterAsset;
using AssetFlowCore.Application.UseCases.Team.CreateTeam;
using AssetFlowCore.Application.UseCases.Team.DeleteTeam;
using AssetFlowCore.Application.UseCases.Team.GetTeam;
using AssetFlowCore.Application.UseCases.Team.UpdateTeam;
using AssetFlowCore.Application.UseCases.Tickets.AssignTicket;
using AssetFlowCore.Application.UseCases.Tickets.CloseTicket;
using AssetFlowCore.Application.UseCases.Tickets.CreateTicket;
using AssetFlowCore.Application.UseCases.Tickets.GetTicket;
using AssetFlowCore.Application.UseCases.Tickets.TransferTicket;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AssetFlowCore.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddValidatorsFromAssembly(typeof(CreateMaintenanceTicketValidator).Assembly);
        services.AddValidatorsFromAssembly(typeof(CreateTeamCommandValidator).Assembly);
        // Moteur d'aiguillage automatique (Stratégies isolées - OCP)
        services.AddScoped<IAssignmentStrategy, ServerAssignmentStrategy>();
        services.AddScoped<IAssignmentStrategy, NetworkAssignmentStrategy>();
        services.AddScoped<IAssignmentStrategy, LaptopHighCriticalityStrategy>();
        services.AddScoped<IAssignmentStrategy, LaptopStandardStrategy>();
        services.AddScoped<ITicketAssignmentEngine, TicketAssignmentEngine>();

        // Enregistrement explicite des Handlers de Cas d'Usages (Vertical Slices Applicatives)
        services.AddScoped<RegisterAssetHandler>();
        services.AddScoped<GetAllAssetsHandler>();
        services.AddScoped<DecommissionAssetHandler>();
        services.AddScoped<CreateMaintenanceTicketHandler>();
        services.AddScoped<AssignTicketToTechnicianHandler>();
        services.AddScoped<CloseTicketHandler>();
        services.AddScoped<RequestTicketTransferCommandHandler>();
        services.AddScoped<GetTicketHandler>();
        services.AddScoped<CreateTeamCommandHandler>();
        services.AddScoped<GetTeamHandler>();
        services.AddScoped<DeleteTeamCommandHandler>();
        services.AddScoped<UpdateTeamCommandHandler>();

        return services;
    }
}