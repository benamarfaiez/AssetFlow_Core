using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.ValueObjects;

namespace AssetFlowCore.Domain.Entities;

public class Asset
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public SerialNumber SerialNumber { get; private set; }
    public AssetType Type { get; private set; }
    public AssetStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<MaintenanceTicket> _tickets = [];
    public IReadOnlyCollection<MaintenanceTicket> Tickets => _tickets.AsReadOnly();

    // Pour EF Core
    private Asset()
    {
        SerialNumber = null!;
        Name = null!;
    }

    public Asset(Guid id, string name, SerialNumber serialNumber, AssetType type)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Le nom de l'actif ne peut pas être vide.", nameof(name));

        Id = id;
        Name = name.Trim();
        SerialNumber = serialNumber;
        Type = type;
        Status = AssetStatus.InService;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsDown()
    {
        if (Status == AssetStatus.Decommissioned)
            throw new DomainException("Impossible de mettre en panne un actif mis au rebut.");

        Status = AssetStatus.Down;
    }

    public void MarkInMaintenance()
    {
        if (Status != AssetStatus.Down)
            throw new DomainException("L'actif doit être en panne avant d'entrer en maintenance.");

        Status = AssetStatus.InMaintenance;
    }

    public void RestoreToService()
    {
        Status = AssetStatus.InService;
    }

    public void Decommission()
    {
        Status = AssetStatus.Decommissioned;
    }

    /// <summary>
    /// Remet en service un actif mis au rebut (décision 0.4, Lot 2 bis) : transition distincte de
    /// <see cref="RestoreToService"/>, laquelle sert la fin de réparation (Down/InMaintenance →
    /// InService, sans garde) et non l'annulation d'une mise au rebut. Le motif n'est pas
    /// persisté (aucun champ dédié, à la différence de l'historique de transfert de ticket) mais
    /// sa présence est requise, validée ici et non par FluentValidation : le pipeline MediatR ne
    /// s'applique pas aux commandes sans retour (<c>IRequest</c> void) dans ce projet.
    /// </summary>
    public void RestoreFromDecommission(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Le motif de remise en service est obligatoire.", nameof(reason));

        if (Status != AssetStatus.Decommissioned)
            throw new DomainException("Seul un actif mis au rebut peut être remis en service.");

        Status = AssetStatus.InService;
    }
}