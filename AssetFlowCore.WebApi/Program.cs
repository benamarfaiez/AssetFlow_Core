using AssetFlowCore.Application;
using AssetFlowCore.Aspire.ServiceDefaults;
using AssetFlowCore.Infrastructure;
using AssetFlowCore.Infrastructure.Notifications;
using AssetFlowCore.Infrastructure.Persistence;
using AssetFlowCore.WebApi.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
// Aspire permet de configurer les options du DbContext via une surcharge dédiée
builder.AddSqlServerDbContext<AssetFlowDbContext>("assetflow-db", configureDbContextOptions: options =>
{
    options.UseSqlServer(b =>
        b.MigrationsAssembly("AssetFlowCore.Infrastructure")
    );
});

builder.Services.AddControllers()
.AddJsonOptions(options =>
{
    // Permet à l'API de convertir automatiquement les strings en Enums dans les requêtes/réponses
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Lot 7 (décision 0.1) : annuaire d'entreprise Entra ID, OIDC, jetons JWT Bearer.
// Authority/Audience proviennent de la configuration (User Secrets, variables d'environnement,
// configuration de déploiement) — jamais du dépôt. Tant que le tenant n'est pas enregistré
// (étape 7.0, hors code), ces valeurs restent vides et l'échec ne survient qu'à la première
// requête protégée (récupération différée des métadonnées OIDC), pas au démarrage.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Authentication:Entra:Authority"];
        options.Audience = builder.Configuration["Authentication:Entra:Audience"];
        // Valeur par défaut cohérente avec des « App Roles » Entra ID assignables à des groupes
        // (décision 0.1 : rôles dérivés des groupes d'annuaire). Si l'exploitation choisit plutôt
        // des groupes de sécurité classiques (revendication `groups`, GUID), cette clé de
        // configuration absorbe l'écart sans modification de code — voir étape 7.0.
        options.TokenValidationParameters.RoleClaimType = builder.Configuration["Authentication:Entra:RoleClaimType"] ?? "roles";
        options.TokenValidationParameters.NameClaimType = "name";
        options.Events = new JwtBearerEvents
        {
            // Étape 7.1 bis : un WebSocket ne porte pas d'en-tête Authorization, le client
            // SignalR place donc le jeton en chaîne de requête pour /ticketHub uniquement.
            // Arbitrage assumé : ce jeton atterrit dans les journaux d'accès du serveur/proxy.
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/ticketHub"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
        };
    });
builder.Services.AddAuthorization();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAspireDashboardAndSwagger", policy =>
    {
        policy.WithOrigins(allowedOrigins!)
          .AllowAnyMethod()
          .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowAspireDashboardAndSwagger");
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<TicketHub>("/ticketHub");

app.MapDefaultEndpoints();
await app.RunAsync();
public partial class Program { }
