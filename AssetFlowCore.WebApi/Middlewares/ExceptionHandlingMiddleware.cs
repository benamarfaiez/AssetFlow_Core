using AssetFlowCore.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            case FluentValidation.ValidationException validationEx:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Title = "Validation de la requête échouée";
                problemDetails.Detail = "Une ou plusieurs erreurs de validation se sont produites.";

                // On peuple le dictionnaire standard d'erreurs pour l'API
                var errors = validationEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    );
                problemDetails.Extensions["errors"] = errors;
                break;

            case ArgumentException argumentEx:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Title = "Données d'entrée invalides";
                problemDetails.Detail = argumentEx.Message;
                break;

            // Avant DomainException, dont NotFoundException dérive : l'ordre des cas fait foi.
            case NotFoundException notFoundEx:
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                problemDetails.Status = StatusCodes.Status404NotFound;
                problemDetails.Title = "Ressource introuvable";
                problemDetails.Detail = notFoundEx.Message;
                break;

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
                // Le message d'exception peut porter des détails d'implémentation (requête SQL,
                // chemin de fichier, nom de serveur) : il est journalisé mais jamais renvoyé au client.
                // L'identifiant de trace permet de relier la réponse à l'entrée de journal correspondante.
                logger.LogError(exception, "Une erreur système non gérée s'est produite. TraceId : {TraceId}", context.TraceIdentifier);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                problemDetails.Status = StatusCodes.Status500InternalServerError;
                problemDetails.Title = "Erreur interne du serveur";
                problemDetails.Detail = "Une erreur inattendue s'est produite. Contactez le support en communiquant l'identifiant de trace.";
                problemDetails.Extensions["traceId"] = context.TraceIdentifier;
                break;
        }

        // WriteAsJsonAsync respecte les conventions d'écriture JSON de .NET.
        // L'écriture est volontairement NON annulable : nous sommes dans le gestionnaire
        // d'exceptions, et l'abandon du client est justement une cause fréquente d'exception.
        // Passer context.RequestAborted — déjà annulé dans ce cas — ferait lever une
        // OperationCanceledException depuis le bloc catch, masquant l'exception d'origine
        // sans qu'aucun code ne puisse la traiter.
        return context.Response.WriteAsJsonAsync(problemDetails, CancellationToken.None);
    }
}