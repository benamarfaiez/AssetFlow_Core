using AssetFlowCore.Application.Behaviors;
using AssetFlowCore.Application.Interfaces;
using AssetFlowCore.Application.Services;
using AssetFlowCore.Application.UseCases.Assets.DecommissionAsset;
using AssetFlowCore.Application.UseCases.Assets.GetAllAssets;
using AssetFlowCore.Application.UseCases.Assets.GetAsset;
using AssetFlowCore.Application.UseCases.Assets.RegisterAsset;
using AssetFlowCore.Application.UseCases.Assets.RestoreAssetToService;
using AssetFlowCore.Application.UseCases.Team.ActivateTeam;
using AssetFlowCore.Application.UseCases.Team.CreateTeam;
using AssetFlowCore.Application.UseCases.Team.DeactivateTeam;
using AssetFlowCore.Application.UseCases.Team.DeleteTeam;
using AssetFlowCore.Application.UseCases.Team.GetTeam;
using AssetFlowCore.Application.UseCases.Team.GetTeams;
using AssetFlowCore.Application.UseCases.Team.UpdateTeam;
using AssetFlowCore.Application.UseCases.Tickets.AssignTicket;
using AssetFlowCore.Application.UseCases.Tickets.CloseTicket;
using AssetFlowCore.Application.UseCases.Tickets.CreateTicket;
using AssetFlowCore.Application.UseCases.Tickets.GetTicket;
using AssetFlowCore.Application.UseCases.Tickets.GetTickets;
using AssetFlowCore.Application.UseCases.Tickets.TransferTicket;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace AssetFlowCore.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(CreateMaintenanceTicketValidator).Assembly);
        services.AddValidatorsFromAssembly(typeof(CreateTeamCommandValidator).Assembly);
        // Moteur d'aiguillage automatique (Stratégies isolées - OCP)
        services.AddScoped<IAssignmentStrategy, ServerAssignmentStrategy>();
        services.AddScoped<IAssignmentStrategy, NetworkAssignmentStrategy>();
        services.AddScoped<IAssignmentStrategy, LaptopHighCriticalityStrategy>();
        services.AddScoped<IAssignmentStrategy, LaptopStandardStrategy>();
        services.AddScoped<ITicketAssignmentEngine, TicketAssignmentEngine>();

        // Lot 7 : provisionnement « just-in-time » de l'utilisateur authentifié (décision 0.2)
        services.AddScoped<ICurrentUserService, CurrentUserProvisioningService>();

        // Enregistrement explicite des Handlers de Cas d'Usages (Vertical Slices Applicatives)
        services.AddScoped<RegisterAssetHandler>();
        services.AddScoped<GetAllAssetsHandler>();
        services.AddScoped<GetAssetHandler>();
        services.AddScoped<DecommissionAssetHandler>();
        services.AddScoped<RestoreAssetToServiceHandler>();
        services.AddScoped<CreateMaintenanceTicketHandler>();
        services.AddScoped<AssignTicketToTechnicianHandler>();
        services.AddScoped<CloseTicketHandler>();
        services.AddScoped<RequestTicketTransferCommandHandler>();
        services.AddScoped<GetTicketHandler>();
        services.AddScoped<GetTicketsHandler>();
        services.AddScoped<CreateTeamCommandHandler>();
        services.AddScoped<GetTeamHandler>();
        services.AddScoped<GetTeamsHandler>();
        services.AddScoped<DeleteTeamCommandHandler>();
        services.AddScoped<UpdateTeamCommandHandler>();
        services.AddScoped<ActivateTeamCommandHandler>();
        services.AddScoped<DeactivateTeamCommandHandler>();

        // Enregistrement MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        return services;
    }
}