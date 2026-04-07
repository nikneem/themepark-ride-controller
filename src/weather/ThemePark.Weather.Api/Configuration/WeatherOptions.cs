namespace ThemePark.Weather.Api.Configuration;

public sealed class WeatherOptions
{
    public const string SectionName = "Weather";

    public int SimulationIntervalSeconds { get; init; } = 60;
    public int CalmWeight { get; init; } = 60;
    public int MildWeight { get; init; } = 30;
    public int SevereWeight { get; init; } = 10;
    public string[] Zones { get; init; } = ["Zone-A", "Zone-B", "Zone-C"];
}
