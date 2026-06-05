using System.Text.Json.Serialization;

namespace AssetFlowCore.WebApi.Requests;

public record CreateTicketRequest([property: JsonRequired] Guid AssetId, string Title, string Description, string Criticality);