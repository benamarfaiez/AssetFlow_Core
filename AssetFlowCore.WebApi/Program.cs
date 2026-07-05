using AssetFlowCore.Application;
using AssetFlowCore.Aspire.ServiceDefaults;
using AssetFlowCore.Infrastructure;
using AssetFlowCore.Infrastructure.Notifications;
using AssetFlowCore.Infrastructure.Persistence;
using AssetFlowCore.WebApi.Middlewares;
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

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAspireDashboardAndSwagger", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .WithHeaders("Content-Type", "Authorization")
              .AllowCredentials();
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
app.UseAuthorization();
app.MapControllers();
app.MapHub<TicketHub>("/ticketHub");

app.MapDefaultEndpoints();
await app.RunAsync();
public partial class Program { }