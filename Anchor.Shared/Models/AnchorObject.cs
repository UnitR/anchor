namespace Anchor.Shared.Models;

/// <summary>
/// A personal object pre-registered by the user as a physical anchor.
/// Must be in a specific named room; validated on-device by Vision + Foundation Models.
/// </summary>
public sealed record AnchorObject(
    Guid Id,
    string Name,
    string Room,
    IReadOnlyList<float[]> ReferenceFeaturePrints,
    IReadOnlyList<string> ExpectedVisionClasses,
    DateTimeOffset RegisteredAt);
