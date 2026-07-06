using FluentValidation;
using MediatR;

namespace AssetFlowCore.Application.Behaviors;

public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators = validators;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);

            // Exécute tous les validateurs correspondants en parallèle
            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken))
            );

            // Regroupe toutes les erreurs détectées
            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            // Si des validations échouent, on lève l'exception de FluentValidation
            if (failures.Count != 0)
            {
                throw new ValidationException(failures);
            }
        }

        // Si tout est valide, on passe au Handler (ou au behavior suivant)
        return await next();
    }
}