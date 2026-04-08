using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ThemePark.ControlCenter.Domain;
using ThemePark.ControlCenter.Sse;
using ThemePark.EventContracts.Events;
using ThemePark.EventContracts.Serialization;

namespace ThemePark.ControlCenter.Tests.SseEndpoint;

/// <summary>
/// Integration tests for the <c>GET /api/events/stream</c> SSE endpoint.
/// Verifies that events broadcast via <see cref="SseConnectionManager"/> are pushed to connected
/// SSE clients, and that client disconnects do not prevent subsequent clients from receiving
/// events.
/// </summary>
public sealed class SseStreamTests : IClassFixture<ControlCenterApiFactory>
{
    private readonly ControlCenterApiFactory _factory;
    private readonly SseConnectionManager _sseManager;

    public SseStreamTests(ControlCenterApiFactory factory)
    {
        _factory = factory;
        _sseManager = factory.Services.GetRequiredService<SseConnectionManager>();
    }

    /// <summary>
    /// Connecting to the SSE stream and triggering a status transition delivers
    /// the event as a correctly serialised <c>data:</c> SSE message to the client.
    /// </summary>
    [Fact]
    public async Task SseStream_EventWrittenToChannel_IsDeliveredToConnectedClient()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var expected = new RideStatusChangedEvent(
            RideId: Guid.NewGuid(),
            PreviousStatus: "Idle",
            NewStatus: "PreFlight",
            WorkflowStep: "StartPreFlightActivity",
            ChangedAt: DateTimeOffset.UtcNow);

        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/events/stream");
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
        using var reader = new StreamReader(stream);

        // Broadcast after the connection is established so the per-connection channel exists.
        var json = JsonSerializer.Serialize(expected, EventContractsJsonOptions.Default);
        _sseManager.BroadcastEvent(new SseEvent("ride-status-changed", json, DateTimeOffset.UtcNow));

        var dataLine = await ReadNextDataLineAsync(reader, cts.Token);

        Assert.NotNull(dataLine);
        var payload = dataLine["data:".Length..].Trim();
        var received = JsonSerializer.Deserialize<RideStatusChangedEvent>(payload, EventContractsJsonOptions.Default);

        Assert.NotNull(received);
        Assert.Equal(expected.RideId, received.RideId);
        Assert.Equal("PreFlight", received.NewStatus);
        Assert.Equal("StartPreFlightActivity", received.WorkflowStep);
    }

    /// <summary>
    /// After one client disconnects from the SSE stream, a new client can still
    /// connect and receive subsequent events — the connection manager remains usable.
    /// </summary>
    [Fact]
    public async Task SseStream_AfterClientDisconnects_NewClientReceivesSubsequentEvents()
    {
        using var outerCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        var event1 = new RideStatusChangedEvent(
            RideId: Guid.NewGuid(),
            PreviousStatus: "Idle",
            NewStatus: "PreFlight",
            WorkflowStep: "StartPreFlightActivity",
            ChangedAt: DateTimeOffset.UtcNow);

        var event2 = new RideStatusChangedEvent(
            RideId: Guid.NewGuid(),
            PreviousStatus: "PreFlight",
            NewStatus: "Loading",
            WorkflowStep: "StartLoadingActivity",
            ChangedAt: DateTimeOffset.UtcNow);

        // --- Client A: connect, receive event1, then disconnect cleanly ---
        using (var clientA = _factory.CreateClient())
        using (var ctsA = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
        {
            using var requestA = new HttpRequestMessage(HttpMethod.Get, "/api/events/stream");
            using var responseA = await clientA.SendAsync(requestA, HttpCompletionOption.ResponseHeadersRead, ctsA.Token);
            responseA.EnsureSuccessStatusCode();

            await using var streamA = await responseA.Content.ReadAsStreamAsync(ctsA.Token);
            using var readerA = new StreamReader(streamA);

            var json1 = JsonSerializer.Serialize(event1, EventContractsJsonOptions.Default);
            _sseManager.BroadcastEvent(new SseEvent("ride-status-changed", json1, DateTimeOffset.UtcNow));

            var lineA = await ReadNextDataLineAsync(readerA, ctsA.Token);
            Assert.NotNull(lineA);

            var payloadA = lineA["data:".Length..].Trim();
            var receivedA = JsonSerializer.Deserialize<RideStatusChangedEvent>(payloadA, EventContractsJsonOptions.Default);
            Assert.Equal(event1.RideId, receivedA!.RideId);

            // Client A cleanly goes out of scope here — response and connection are disposed.
        }

        // Allow the server to finish processing client A's disconnection.
        await Task.Delay(100, outerCts.Token);

        // --- Client B: connect, broadcast event2, should receive it ---
        using var clientB = _factory.CreateClient();
        using var ctsB = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var requestB = new HttpRequestMessage(HttpMethod.Get, "/api/events/stream");
        using var responseB = await clientB.SendAsync(requestB, HttpCompletionOption.ResponseHeadersRead, ctsB.Token);
        responseB.EnsureSuccessStatusCode();

        await using var streamB = await responseB.Content.ReadAsStreamAsync(ctsB.Token);
        using var readerB = new StreamReader(streamB);

        var json2 = JsonSerializer.Serialize(event2, EventContractsJsonOptions.Default);
        _sseManager.BroadcastEvent(new SseEvent("ride-status-changed", json2, DateTimeOffset.UtcNow));

        var lineB = await ReadNextDataLineAsync(readerB, ctsB.Token);
        Assert.NotNull(lineB);

        var payloadB = lineB["data:".Length..].Trim();
        var receivedB = JsonSerializer.Deserialize<RideStatusChangedEvent>(payloadB, EventContractsJsonOptions.Default);
        Assert.Equal(event2.RideId, receivedB!.RideId);
        Assert.Equal("Loading", receivedB.NewStatus);
    }

    /// <summary>Reads lines from an SSE stream until a <c>data:</c> line is found or the token is cancelled.</summary>
    private static async Task<string?> ReadNextDataLineAsync(StreamReader reader, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is not null && line.StartsWith("data:", StringComparison.Ordinal))
                return line;
        }
        return null;
    }
}
