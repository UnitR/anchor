# AnchorBridge (Swift → C# bridge to Apple FoundationModels)

The C# code on the phone P/Invokes two symbols — `anchor_fm_validate_scene` and
`anchor_fm_free` — exposed by this Swift source. Apple's FoundationModels framework
is Swift-only (it uses `@Generable` macros for guided generation), so we compile the
Swift into an xcframework and link it from the .NET iOS app via `<NativeReference>`.

## Build (one-time, on a Mac with Xcode 17+ and iOS 26 SDK)

```bash
# 1. Create a SwiftPM package or Xcode project wrapping AnchorBridge.swift
#    with a framework target (static lib also works).
# 2. Build the framework for device and simulator:
xcodebuild -project AnchorBridge.xcodeproj -scheme AnchorBridge \
  -configuration Release -sdk iphoneos -destination 'generic/platform=iOS' \
  -derivedDataPath build-device

xcodebuild -project AnchorBridge.xcodeproj -scheme AnchorBridge \
  -configuration Release -sdk iphonesimulator \
  -derivedDataPath build-sim

# 3. Combine into an xcframework:
xcodebuild -create-xcframework \
  -framework build-device/Build/Products/Release-iphoneos/AnchorBridge.framework \
  -framework build-sim/Build/Products/Release-iphonesimulator/AnchorBridge.framework \
  -output AnchorBridge.xcframework
```

Copy `AnchorBridge.xcframework` into `Anchor.iOS/Bridge/` and rebuild the .NET iOS
project. The csproj auto-picks it up.

## Dev-build fallback

If the xcframework isn't present, `FoundationModelsClient.AnalyzeSceneAsync` returns
a permissive `match: true, confidence: 0.5, reason: "bridge-unlinked"` so the full
end-to-end flow stays testable on the simulator (which cannot run Apple Intelligence
anyway). Real deployments require the xcframework.
