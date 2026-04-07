using Microsoft.Extensions.Options;
using ThemePark.Shared.Enums;
using ThemePark.Weather.Api.Configuration;
using ThemePark.Weather.Api.Services;

namespace ThemePark.Weather.Tests.Services;

public sealed class WeatherSimulationEngineTests
{
    private static WeatherSimulationEngine CreateEngine(WeatherOptions? options = null)
    {
        options ??= new WeatherOptions();
        return new WeatherSimulationEngine(Options.Create(options));
    }

    [Fact]
    public void InitialCondition_is_Calm()
    {
        var engine = CreateEngine();
        Assert.Equal(WeatherSeverity.Calm, engine.CurrentCondition.Severity);
        Assert.Empty(engine.CurrentCondition.AffectedZones);
    }

    [Fact]
    public void GenerateCondition_with_100_percent_Calm_weight_always_returns_Calm()
    {
        var engine = CreateEngine(new WeatherOptions { CalmWeight = 1, MildWeight = 0, SevereWeight = 0 });
        for (var i = 0; i < 50; i++)
            Assert.Equal(WeatherSeverity.Calm, engine.GenerateCondition().Severity);
    }

    [Fact]
    public void GenerateCondition_with_100_percent_Mild_weight_always_returns_Mild()
    {
        var engine = CreateEngine(new WeatherOptions { CalmWeight = 0, MildWeight = 1, SevereWeight = 0 });
        for (var i = 0; i < 50; i++)
            Assert.Equal(WeatherSeverity.Mild, engine.GenerateCondition().Severity);
    }

    [Fact]
    public void GenerateCondition_with_100_percent_Severe_weight_always_returns_Severe()
    {
        var engine = CreateEngine(new WeatherOptions { CalmWeight = 0, MildWeight = 0, SevereWeight = 1 });
        for (var i = 0; i < 50; i++)
            Assert.Equal(WeatherSeverity.Severe, engine.GenerateCondition().Severity);
    }

    [Fact]
    public void GenerateCondition_Calm_produces_empty_affected_zones()
    {
        var engine = CreateEngine(new WeatherOptions { CalmWeight = 1, MildWeight = 0, SevereWeight = 0 });
        var condition = engine.GenerateCondition();
        Assert.Equal(WeatherSeverity.Calm, condition.Severity);
        Assert.Empty(condition.AffectedZones);
    }

    [Fact]
    public void GenerateCondition_Mild_produces_non_empty_affected_zones()
    {
        var engine = CreateEngine(new WeatherOptions { CalmWeight = 0, MildWeight = 1, SevereWeight = 0 });
        for (var i = 0; i < 20; i++)
        {
            var condition = engine.GenerateCondition();
            Assert.NotEmpty(condition.AffectedZones);
        }
    }

    [Fact]
    public void GenerateCondition_Severe_produces_non_empty_affected_zones()
    {
        var engine = CreateEngine(new WeatherOptions { CalmWeight = 0, MildWeight = 0, SevereWeight = 1 });
        for (var i = 0; i < 20; i++)
        {
            var condition = engine.GenerateCondition();
            Assert.NotEmpty(condition.AffectedZones);
        }
    }

    [Fact]
    public void GenerateCondition_updates_CurrentCondition()
    {
        var engine = CreateEngine(new WeatherOptions { CalmWeight = 0, MildWeight = 0, SevereWeight = 1 });
        var condition = engine.GenerateCondition();
        Assert.Equal(condition.Severity, engine.CurrentCondition.Severity);
        Assert.Equal(condition.GeneratedAt, engine.CurrentCondition.GeneratedAt);
    }

    [Fact]
    public void ForceCondition_overrides_CurrentCondition()
    {
        var engine = CreateEngine();
        var forced = new ThemePark.Weather.Models.WeatherCondition(
            WeatherSeverity.Severe, ["Zone-A"], DateTimeOffset.UtcNow);

        engine.ForceCondition(forced);

        Assert.Equal(WeatherSeverity.Severe, engine.CurrentCondition.Severity);
        Assert.Contains("Zone-A", engine.CurrentCondition.AffectedZones);
    }
}
