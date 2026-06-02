using FluentValidation;

namespace AssetFlowCore.Application.UseCases.Tickets.TransferTicket;

public class RequestTicketTransferCommandValidator : AbstractValidator<RequestTicketTransferCommand>
{
    public RequestTicketTransferCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty().WithMessage("L'identifiant du ticket est requis.");
        RuleFor(x => x.TargetTeam).NotEmpty().WithMessage("L'équipe cible est requise.");
    }
}
