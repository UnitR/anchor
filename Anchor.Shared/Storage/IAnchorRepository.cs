using Anchor.Shared.Models;

namespace Anchor.Shared.Storage;

public interface IAnchorRepository
{
    Task SaveIntentionAsync(IntentionRecord record, CancellationToken ct = default);
    Task<IntentionRecord?> GetIntentionAsync(Guid sessionId, CancellationToken ct = default);

    Task AddInteroceptionAsync(InteroceptionAnswer answer, CancellationToken ct = default);

    Task UpsertAnchorObjectAsync(AnchorObject obj, CancellationToken ct = default);
    Task<IReadOnlyList<AnchorObject>> ListAnchorsAsync(CancellationToken ct = default);
    Task<AnchorObject?> GetAnchorAsync(Guid id, CancellationToken ct = default);

    Task LogEventAsync(SessionEvent ev, CancellationToken ct = default);
    Task<IReadOnlyList<SessionEvent>> EventsForSessionAsync(Guid sessionId, CancellationToken ct = default);

    Task<byte[]?> GetPairingSecretAsync(CancellationToken ct = default);
    Task SetPairingSecretAsync(byte[] secret, CancellationToken ct = default);
}
