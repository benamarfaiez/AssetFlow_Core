using AssetFlowCore.Application.UseCases.Tickets.GetTickets;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.Repositories;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;
using DomainTeam = AssetFlowCore.Domain.Entities.Team;

namespace AssetFlowCore.UnitTests.Application.UseCases.Tickets;

public class GetTicketsHandlerTests
{
    private readonly Mock<IMaintenanceTicketRepository> _ticketRepository = new();
    private readonly GetTicketsHandler _handler;

    public GetTicketsHandlerTests()
    {
        _ticketRepository
            .Setup(r => r.SearchAsync(It.IsAny<TicketSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<MaintenanceTicket>([], 0));

        _handler = new GetTicketsHandler(_ticketRepository.Object);
    }

    [Fact]
    public async Task Handle_ShouldTranslateFilterNamesIntoDomainEnums()
    {
        var teamId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        var query = new GetTicketsQuery("inprogress", "high", teamId, assetId, "title", SortDescending: false, Page: 2, PageSize: 5);

        await _handler.Handle(query, CancellationToken.None);

        _ticketRepository.Verify(r => r.SearchAsync(
            It.Is<TicketSearchCriteria>(c =>
                c.Status == TicketStatus.InProgress &&
                c.Criticality == TicketCriticality.High &&
                c.AssignedTeamId == teamId &&
                c.AssetId == assetId &&
                c.SortBy == TicketSortField.Title &&
                !c.SortDescending &&
                c.Page == 2 &&
                c.PageSize == 5),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutFilter_ShouldSearchEverythingSortedByCreationDateDescending()
    {
        await _handler.Handle(new GetTicketsQuery(), CancellationToken.None);

        _ticketRepository.Verify(r => r.SearchAsync(
            It.Is<TicketSearchCriteria>(c =>
                c.Status == null &&
                c.Criticality == null &&
                c.AssignedTeamId == null &&
                c.AssetId == null &&
                c.SortBy == TicketSortField.CreatedAt &&
                c.SortDescending &&
                c.Page == 1 &&
                c.PageSize == 20),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReportTheTotalCountAndDeriveTheNumberOfPages()
    {
        var team = new DomainTeam("Équipe", "Server", "High");
        var ticket = new MaintenanceTicket(Guid.NewGuid(), Guid.NewGuid(), "Titre", "Description", TicketCriticality.High, team.Id);
        _ticketRepository
            .Setup(r => r.SearchAsync(It.IsAny<TicketSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<MaintenanceTicket>([ticket], 137));

        var result = await _handler.Handle(new GetTicketsQuery(PageSize: 20), CancellationToken.None);

        result.TotalCount.Should().Be(137);
        result.TotalPages.Should().Be(7, "une page incomplète compte pour une page entière");
        result.Items.Should().ContainSingle().Which.Title.Should().Be("Titre");
    }

    [Fact]
    public async Task Handle_WhenNoTicketMatches_ShouldReturnAnEmptyPage()
    {
        var result = await _handler.Handle(new GetTicketsQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }
}

public class GetTicketsQueryValidatorTests
{
    private readonly GetTicketsQueryValidator _validator = new();

    [Fact]
    public void Validate_WithDefaultQuery_ShouldPass()
        => _validator.TestValidate(new GetTicketsQuery()).ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithInvalidPage_ShouldReportError(int page)
        => _validator.TestValidate(new GetTicketsQuery(Page: page))
                     .ShouldHaveValidationErrorFor(x => x.Page);

    [Theory]
    [InlineData(0)]
    [InlineData(GetTicketsQueryValidator.MaxPageSize + 1)]
    public void Validate_WithInvalidPageSize_ShouldReportError(int pageSize)
        => _validator.TestValidate(new GetTicketsQuery(PageSize: pageSize))
                     .ShouldHaveValidationErrorFor(x => x.PageSize);

    [Fact]
    public void Validate_WithUnknownStatus_ShouldReportError()
        => _validator.TestValidate(new GetTicketsQuery(Status: "Archivé"))
                     .ShouldHaveValidationErrorFor(x => x.Status);

    [Fact]
    public void Validate_WithUnknownSortField_ShouldReportError()
        => _validator.TestValidate(new GetTicketsQuery(SortBy: "Couleur"))
                     .ShouldHaveValidationErrorFor(x => x.SortBy);

    [Theory]
    [InlineData("opened")]
    [InlineData("CLOSED")]
    public void Validate_WithKnownStatus_ShouldPass(string status)
        => _validator.TestValidate(new GetTicketsQuery(Status: status))
                     .ShouldNotHaveValidationErrorFor(x => x.Status);
}
