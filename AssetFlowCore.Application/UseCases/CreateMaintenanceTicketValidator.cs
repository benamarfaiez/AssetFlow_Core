using AssetFlowCore.Domain.Enums;
using FluentValidation;

namespace AssetFlowCore.Application.UseCases;

public class CreateMaintenanceTicketValidator : AbstractValidator<CreateMaintenanceTicketCommand>
{
    public CreateMaintenanceTicketValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.Criticality).IsEnumName(typeof(TicketCriticality), caseSensitive: false);
    }
}