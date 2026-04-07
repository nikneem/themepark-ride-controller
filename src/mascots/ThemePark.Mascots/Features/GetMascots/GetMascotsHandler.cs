using ThemePark.Mascots.Abstractions.DataTransferObjects;
using ThemePark.Mascots.State;
using ThemePark.Shared;
using ThemePark.Shared.Cqrs;

namespace ThemePark.Mascots.Features.GetMascots;

public sealed class GetMascotsHandler(IMascotStateStore store)
    : IQueryHandler<GetMascotsQuery, OperationResult<IReadOnlyList<MascotDto>>>
{
    public Task<OperationResult<IReadOnlyList<MascotDto>>> HandleAsync(
        GetMascotsQuery query,
        CancellationToken cancellationToken = default)
    {
        var mascots = store.GetAll()
            .Select(m => new MascotDto(m.MascotId, m.Name, m.CurrentZone, m.IsInRestrictedZone, m.AffectedRideId))
            .ToList();
        return Task.FromResult(OperationResult<IReadOnlyList<MascotDto>>.Success(mascots));
    }
}
