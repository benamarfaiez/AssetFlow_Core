using AssetFlowCore.Application.Interfaces;
using AssetFlowCore.Application.Services;
using AssetFlowCore.Application.UseCases.Assets.DecommissionAsset;
using AssetFlowCore.Application.UseCases.Assets.GetAllAssets;
using AssetFlowCore.Application.UseCases.Assets.RegisterAsset;
using AssetFlowCore.Application.UseCases.Tickets.AssignTicket;
using AssetFlowCore.Application.UseCases.Tickets.CloseTicket;
using AssetFlowCore.Application.UseCases.Tickets.CreateTicket;
using AssetFlowCore.Domain.Repositories;
using AssetFlowCore.Infrastructure.Cache;
using AssetFlowCore.Infrastructure.Notifications;
using AssetFlowCore.Infrastructure.Persistence;
using AssetFlowCore.Infrastructure.Persistence.Repositories;
using AssetFlowCore.WebApi.Middlewares;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Infrastructure Données & Réseau
builder.Services.AddDbContext<AssetFlowDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddMemoryCache();
builder.Services.AddSignalR();

builder.Services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AssetFlowDbContext>());

// Repositories (Avec gestion de cache via pattern Décorateur)
builder.Services.AddScoped<IAssetRepository>(provider =>
{
    var rawRepo = new AssetRepository(provider.GetRequiredService<AssetFlowDbContext>());
    return new CachedAssetRepository(rawRepo, provider.GetRequiredService<IMemoryCache>());
});
builder.Services.AddScoped<IMaintenanceTicketRepository, MaintenanceTicketRepository>();

// Moteur d'aiguillage automatique (Stratégies isolées - OCP)
builder.Services.AddSingleton<IAssignmentStrategy, ServerAssignmentStrategy>();
builder.Services.AddSingleton<IAssignmentStrategy, NetworkAssignmentStrategy>();
builder.Services.AddSingleton<IAssignmentStrategy, LaptopHighCriticalityStrategy>();
builder.Services.AddSingleton<IAssignmentStrategy, LaptopStandardStrategy>();
builder.Services.AddSingleton<ITicketAssignmentEngine, TicketAssignmentEngine>();
builder.Services.AddScoped<INotificationService, SignalRNotificationService>();

// Enregistrement explicite des Handlers de Cas d'Usages (Vertical Slices Applicatives)
builder.Services.AddScoped<RegisterAssetHandler>();
builder.Services.AddScoped<GetAllAssetsHandler>();
builder.Services.AddScoped<DecommissionAssetHandler>();
builder.Services.AddScoped<CreateMaintenanceTicketHandler>();
builder.Services.AddScoped<AssignTicketToTechnicianHandler>();
builder.Services.AddScoped<CloseTicketHandler>();

var app = builder.Build();

// Pipeline de Middleware Securisé & Standardisé (RFC 7807)
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.MapHub<TicketHub>("/ticketHub");

// Initialisation "One-Click" : Application automatique des migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AssetFlowDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.Run();