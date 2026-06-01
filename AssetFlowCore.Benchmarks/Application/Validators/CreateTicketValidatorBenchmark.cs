using AssetFlowCore.Application.UseCases.Tickets.CreateTicket;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace AssetFlowCore.Benchmarks.Application.Validators;

/// <summary>
/// Mesure le coût de la validation FluentValidation sur CreateMaintenanceTicketCommand.
/// Compare : commande valide vs invalide (early-exit vs collecte de toutes les erreurs).
/// Critique car la validation s'exécute sur chaque requête HTTP entrante.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CreateTicketValidatorBenchmark
{
    private CreateMaintenanceTicketValidator _validator = null!;
    private CreateMaintenanceTicketCommand _validCommand = null!;
    private CreateMaintenanceTicketCommand _invalidAssetId = null!;
    private CreateMaintenanceTicketCommand _invalidTitle = null!;
    private CreateMaintenanceTicketCommand _invalidCriticality = null!;
    private CreateMaintenanceTicketCommand _allFieldsInvalid = null!;

    [GlobalSetup]
    public void Setup()
    {
        _validator = new CreateMaintenanceTicketValidator();

        _validCommand = new CreateMaintenanceTicketCommand(
            Guid.NewGuid(), "Panne réseau critique", "Description détaillée", "High");

        _invalidAssetId = new CreateMaintenanceTicketCommand(
            Guid.Empty, "Titre", "Description", "Medium");

        _invalidTitle = new CreateMaintenanceTicketCommand(
            Guid.NewGuid(), "", "Description", "Low");

        _invalidCriticality = new CreateMaintenanceTicketCommand(
            Guid.NewGuid(), "Titre", "Description", "ULTRA_CRITIQUE");

        _allFieldsInvalid = new CreateMaintenanceTicketCommand(
            Guid.Empty, "", "", "INVALID");
    }

    [Benchmark(Baseline = true, Description = "Validate — commande valide (happy path)")]
    public bool ValidateValid()
        => _validator.Validate(_validCommand).IsValid;

    [Benchmark(Description = "Validate — AssetId vide (Guid.Empty)")]
    public bool ValidateInvalidAssetId()
        => _validator.Validate(_invalidAssetId).IsValid;

    [Benchmark(Description = "Validate — titre vide")]
    public bool ValidateInvalidTitle()
        => _validator.Validate(_invalidTitle).IsValid;

    [Benchmark(Description = "Validate — criticité inconnue")]
    public bool ValidateInvalidCriticality()
        => _validator.Validate(_invalidCriticality).IsValid;

    [Benchmark(Description = "Validate — tous les champs invalides (max erreurs)")]
    public int ValidateAllFieldsInvalid()
        => _validator.Validate(_allFieldsInvalid).Errors.Count;

    [Benchmark(Description = "ValidateAsync — commande valide (async overhead)")]
    public async Task<bool> ValidateValidAsync()
        => (await _validator.ValidateAsync(_validCommand)).IsValid;
}