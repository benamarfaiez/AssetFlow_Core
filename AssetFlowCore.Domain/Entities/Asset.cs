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
}