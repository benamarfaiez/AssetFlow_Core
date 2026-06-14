using AssetFlowCore.Application;
using AssetFlowCore.Infrastructure;
using AssetFlowCore.Infrastructure.Notifications;
using AssetFlowCore.WebApi.Middlewares;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

// Pipeline de Middleware Securisé & Standardisé (RFC 7807)
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHub<TicketHub>("/ticketHub");

await app.RunAsync();
public partial class Program { }