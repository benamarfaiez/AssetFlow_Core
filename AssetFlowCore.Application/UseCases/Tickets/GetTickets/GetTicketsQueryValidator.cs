using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.Repositories;
using FluentValidation;

namespace AssetFlowCore.Application.UseCases.Tickets.GetTickets;

public class GetTicketsQueryValidator : AbstractValidator<GetTicketsQuery>
{
    /// <summary>
    /// Borne haute de la taille de page : au-delà, une lecture unique pèserait autant qu'un
    /// export et ruinerait l'intérêt de la pagination.
    /// </summary>
    public const int MaxPageSize = 100;

    public GetTicketsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Le numéro de page doit être supérieur ou égal à 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize)
            .WithMessage($"La taille de page doit être comprise entre 1 et {MaxPageSize}.");

        RuleFor(x => x.Status)
            .IsEnumName(typeof(TicketStatus), caseSensitive: false)
            .When(x => !string.IsNullOrWhiteSpace(x.Status))
            .WithMessage("L'état doit être l'un des suivants : Opened, InProgress ou Closed.");

        RuleFor(x => x.Criticality)
            .IsEnumName(typeof(TicketCriticality), caseSensitive: false)
            .When(x => !string.IsNullOrWhiteSpace(x.Criticality))
            .WithMessage("La criticité doit être l'une des suivantes : Low, Medium ou High.");

        RuleFor(x => x.SortBy)
            .IsEnumName(typeof(TicketSortField), caseSensitive: false)
            .When(x => !string.IsNullOrWhiteSpace(x.SortBy))
            .WithMessage("Le tri doit porter sur l'un des champs suivants : CreatedAt, Criticality, Status ou Title.");
    }
}
