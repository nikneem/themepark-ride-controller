using System.Text.Json;
using System.Text.Json.Serialization;

namespace ThemePark.EventContracts.Serialization;

/// <summary>
/// Shared JSON serializer options for all event contracts:
/// camelCase property naming + string enum values.
/// </summary>
public static class EventContractsJsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}
