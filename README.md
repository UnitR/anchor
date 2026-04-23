# Anchor

A .NET 10 MAUI executive-function reset tool for neurodiverse individuals. Combines
three clinically-grounded intervention mechanisms into a single, deliberately
non-dismissible interrupt:

1. **Physical anchor** (Barkley 1997, 2012 — externalization of executive function)
2. **Interoception check** (Mahler 2019; Hupfeld et al. 2019)
3. **Implementation intention checkpoint** (Gollwitzer 1999; Gawrilow & Gollwitzer 2008 — ADHD children, specifically validated)

See `/Users/arc-angel/.claude/plans/1-2-and-3-structured-pillow.md` for the full plan and citations.

## Projects

| Project | Target | Purpose |
|---|---|---|
| `Anchor.Shared` | `net10.0` | Models, scheduler, wire protocol, HMAC pairing, SQLite storage, Vision feature-print similarity, verdict logic. Pure, portable, fully unit-tested. |
| `Anchor.Shared.Tests` | `net10.0` | xUnit tests for scheduler, similarity, HMAC, verdict. |
| `Anchor.Desktop` | `net10.0-maccatalyst; net10.0-windows10.0.19041.0` | MAUI desktop app with non-dismissible overlay and three-step interrupt flow. |
| `Anchor.iOS` | `net10.0-ios` (iOS 26+) | MAUI companion app — Vision + FoundationModels + motion freshness validation. |

## Build

```bash
# 1. Install .NET 10 MAUI workloads (desktop/iOS targets require these)
dotnet workload install maui

# 2. Build and test the shared library (no workloads needed)
dotnet build Anchor.Shared.Tests/Anchor.Shared.Tests.csproj
dotnet test  Anchor.Shared.Tests/Anchor.Shared.Tests.csproj --no-build

# 3. Desktop (on macOS with Xcode 17+)
dotnet build Anchor.Desktop/Anchor.Desktop.csproj -f net10.0-maccatalyst

# 4. Desktop (on Windows 11 with the Windows App SDK workload)
dotnet build Anchor.Desktop/Anchor.Desktop.csproj -f net10.0-windows10.0.19041.0

# 5. iOS (macOS + Xcode 17 + iPhone 15 Pro/newer running iOS 26)
# First build the Swift bridge: see Anchor.iOS/Bridge/README.md.
dotnet build Anchor.iOS/Anchor.iOS.csproj -f net10.0-ios
```

## Foundation Models bridge

Apple's FoundationModels framework is Swift-only. The repo includes
`Anchor.iOS/Bridge/AnchorBridge.swift` which wraps `LanguageModelSession` with a
`@Generable` output schema and exposes a C ABI (`anchor_fm_validate_scene`). C#
P/Invokes this via `[DllImport("__Internal")]`. Build the bridge as an xcframework
and drop it in `Anchor.iOS/Bridge/AnchorBridge.xcframework`. Without the bridge,
the validator enters a soft-pass dev mode so the rest of the flow remains testable.

## Why the non-dismissible overlay

Executive dysfunction in ADHD/autistic populations stems from weak *internal*
inhibitory and motivational signals (Barkley's unifying theory). The clinical
remedy is to externalize cues and motivation *at the point of performance*. A
dismissible notification silently fails this population — it is trivially
overridden in hyperfocus. Anchor therefore applies three-gate validation on a
separate device so that clearing the interrupt genuinely requires standing up,
walking, and looking at a real object — the smallest action that guarantees the
user has physically disengaged from the workflow.

Emergency bypass exists; its cost is a logged 60s cooldown. Abusing it is
self-defeating (it takes longer than walking to the kitchen).

## Known limitations

- Ctrl-Alt-Del on Windows and Force-Quit on macOS cannot be blocked without a
  kernel driver. Both are treated as implicit emergency bypass.
- Apple Intelligence + Foundation Models are only available on iPhone 15 Pro
  and newer. iOS 26+ required.
- Android and Linux are intentionally out of scope (Apple on-device AI is a
  hard dependency).
