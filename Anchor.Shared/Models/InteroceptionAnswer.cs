namespace Anchor.Shared.Models;

public enum InteroceptionBand
{
    Recent,      // within 1h
    Moderate,    // 1-3h
    Stale,       // 3-6h
    Critical     // 6h+
}

/// <summary>
/// Four-signal body-state snapshot. All four must be answered — no defaults,
/// per Mahler's interoceptive training model.
/// </summary>
public sealed record InteroceptionAnswer(
    Guid SessionId,
    DateTimeOffset AnsweredAt,
    InteroceptionBand LastWater,
    InteroceptionBand LastFood,
    InteroceptionBand LastBathroom,
    InteroceptionBand LastStoodUp);
