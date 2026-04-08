using ThemePark.Shared.Domain;

namespace ThemePark.Shared.Tests;

public class ZoneTests
{
    [Theory]
    [InlineData("Zone-A")]
    [InlineData("Zone-B")]
    [InlineData("Zone-C")]
    public void Zone_ValidValue_ParseSucceeds(string value)
    {
        var zone = Zone.Parse(value);
        Assert.Equal(value, zone.Value);
        Assert.Equal(value, zone.ToString());
    }

    /// <summary>
    /// Verifies that constructing a <see cref="Zone"/> with an out-of-range value throws.
    /// </summary>
    [Theory]
    [InlineData("Zone-D")]
    [InlineData("zone-a")]
    [InlineData("")]
    [InlineData("ZONE-A")]
    public void Zone_InvalidValue_ThrowsArgumentException(string value)
    {
        Assert.Throws<ArgumentException>(() => Zone.Parse(value));
    }

    [Theory]
    [InlineData("Zone-A", true)]
    [InlineData("Zone-B", true)]
    [InlineData("Zone-C", true)]
    [InlineData("Zone-D", false)]
    [InlineData(null, false)]
    public void Zone_TryParse_ReturnsExpectedResult(string? value, bool expectedResult)
    {
        var result = Zone.TryParse(value, out var zone);
        Assert.Equal(expectedResult, result);
        if (expectedResult)
            Assert.Equal(value, zone.Value);
    }

    [Fact]
    public void Zone_ImplicitStringConversion_ReturnsValue()
    {
        var zone = Zone.Parse("Zone-B");
        string s = zone;
        Assert.Equal("Zone-B", s);
    }
}
