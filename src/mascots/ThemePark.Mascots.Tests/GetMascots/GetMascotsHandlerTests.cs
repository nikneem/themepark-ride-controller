using Moq;
using ThemePark.Mascots.Abstractions.DataTransferObjects;
using ThemePark.Mascots.Features.GetMascots;
using ThemePark.Mascots.Models;
using ThemePark.Mascots.State;

namespace ThemePark.Mascots.Tests.GetMascots;

public sealed class GetMascotsHandlerTests
{
    private readonly Mock<IMascotStateStore> _store = new();
    private readonly GetMascotsHandler _handler;

    public GetMascotsHandlerTests()
    {
        _handler = new GetMascotsHandler(_store.Object);
    }

    [Fact]
    public async Task HandleAsync_ReturnsMascots_WhenStoreHasMascots()
    {
        var mascots = new List<Mascot>
        {
            new("mascot-001", "Buddy", "ZoneA", false, null),
            new("mascot-002", "Luna", "ZoneB", true, Guid.NewGuid()),
        };
        _store.Setup(s => s.GetAll()).Returns(mascots);

        var result = await _handler.HandleAsync(new GetMascotsQuery());

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        Assert.Equal("mascot-001", result.Value[0].MascotId);
        Assert.Equal("mascot-002", result.Value[1].MascotId);
    }

    [Fact]
    public async Task HandleAsync_ReturnsEmptyList_WhenNoMascots()
    {
        _store.Setup(s => s.GetAll()).Returns(new List<Mascot>());

        var result = await _handler.HandleAsync(new GetMascotsQuery());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task HandleAsync_MapsAllFieldsCorrectly()
    {
        var rideId = Guid.NewGuid();
        var mascots = new List<Mascot>
        {
            new("mascot-001", "Buddy", "ZoneA", true, rideId),
        };
        _store.Setup(s => s.GetAll()).Returns(mascots);

        var result = await _handler.HandleAsync(new GetMascotsQuery());

        var dto = result.Value![0];
        Assert.Equal("mascot-001", dto.MascotId);
        Assert.Equal("Buddy", dto.Name);
        Assert.Equal("ZoneA", dto.CurrentZone);
        Assert.True(dto.IsInRestrictedZone);
        Assert.Equal(rideId, dto.AffectedRideId);
    }
}
