using AssetFlowCore.WebApi.Middlewares;
using AssetFlowCore.WebApi.Requests;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace AssetFlowCore.IntegrationTests.WebApi.Middlewares;

public class ExceptionHandlingMiddlewareTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Request_WhenDomainExceptionIsThrown_ShouldReturn400WithProblemDetails()
    {
        // Arrange
        var uniqueSerial = $"SER-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        var payload = new RegisterAssetRequest("Machine de Test Middleware", uniqueSerial, "Server");

        var firstResponse = await _client.PostAsJsonAsync("/api/assets", payload);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created, "Le premier enregistrement doit réussir pour initialiser le doublon.");

        // Act
        var response = await _client.PostAsJsonAsync("/api/assets", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "La base contient déjà ce numéro de série, une DomainException doit être levée.");

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Be("Règle métier violée");
        problemDetails.Detail.Should().Contain("Ce numéro de série constructeur est déjà enregistré dans le parc.");
    }

    /// <summary>
    /// Couvre la correction 1.7 : le message d'exception brut était recopié dans le corps de la
    /// réponse 500, exposant des détails d'implémentation au client.
    /// </summary>
    [Fact]
    public async Task Request_WhenUnhandledExceptionIsThrown_ShouldReturn500WithoutLeakingMessage()
    {
        // Arrange
        const string secretMessage = "Login failed for user 'sa' — Server=sql-prod-01;Password=Str0ng!";
        var logger = new CapturingLogger();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException(secretMessage),
            logger);

        var context = new DefaultHttpContext { TraceIdentifier = "trace-42" };
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert : la réponse ne divulgue rien
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        body.Should().NotContain(secretMessage);
        body.Should().NotContain("sql-prod-01");

        using var payload = JsonDocument.Parse(body);
        payload.RootElement.GetProperty("title").GetString().Should().Be("Erreur interne du serveur");
        payload.RootElement.GetProperty("traceId").GetString().Should().Be("trace-42");

        // Assert : l'exception reste journalisée pour l'exploitation
        logger.LoggedExceptions.Should().ContainSingle()
              .Which.Message.Should().Be(secretMessage);
    }

    /// <summary>Journal minimal capturant les exceptions transmises, sans dépendance de simulacre.</summary>
    private sealed class CapturingLogger : ILogger<ExceptionHandlingMiddleware>
    {
        public List<Exception> LoggedExceptions { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (exception is not null)
            {
                LoggedExceptions.Add(exception);
            }
        }
    }
}