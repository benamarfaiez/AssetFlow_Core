using AssetFlowCore.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AssetFlowCore.WebApi.Middlewares;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";
        var problemDetails = new ProblemDetails();

        switch (exception)
        {
            case DomainException domainEx:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Title = "Règle métier violée";
                problemDetails.Detail = domainEx.Message;
                break;

            case DbUpdateConcurrencyException:
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                problemDetails.Status = StatusCodes.Status409Conflict;
                problemDetails.Title = "Concurrence d'accès détectée";
                problemDetails.Detail = "Cette ressource a été mise à jour par un autre utilisateur. Veuillez recharger les données.";
                break;

            default:
                logger.LogError(exception, "Une erreur système non gérée s'est produite.");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                problemDetails.Status = StatusCodes.Status500InternalServerError;
                problemDetails.Title = "Erreur interne du serveur";
                problemDetails.Detail = $"{exception.Message}";
                break;
        }

        var result = JsonSerializer.Serialize(problemDetails);
        return context.Response.WriteAsync(result);
    }
}