// Swift bridge to Apple FoundationModels (iOS 26+).
// Build as an xcframework and place the output at Anchor.Mobile/Bridge/AnchorBridge.xcframework
// so the csproj's <NativeReference> picks it up.
//
// Why a Swift bridge: FoundationModels is Swift-only and leverages @Generable macros
// for guided generation. We expose a plain C ABI that C# P/Invokes via [DllImport("__Internal")].
//
// Build steps (see README.md in this folder):
//   swift build --arch arm64 -c release
//   xcodebuild -create-xcframework -framework <path>.framework -output AnchorBridge.xcframework

import Foundation
#if canImport(FoundationModels)
import FoundationModels
#endif

@Generable
struct SceneVerdict: Codable {
    @Guide(description: "true if the photo shows the expected object AND the visible surroundings match the expected room")
    var match: Bool
    @Guide(description: "confidence from 0 to 1")
    var confidence: Double
    @Guide(description: "short reason for the verdict")
    var reason: String
}

@_cdecl("anchor_fm_validate_scene")
public func anchor_fm_validate_scene(
    _ tagsCsv: UnsafePointer<CChar>?,
    _ expectedObject: UnsafePointer<CChar>?,
    _ expectedRoom: UnsafePointer<CChar>?
) -> UnsafeMutablePointer<CChar>? {
    guard let tags = tagsCsv.flatMap({ String(cString: $0) }),
          let object = expectedObject.flatMap({ String(cString: $0) }),
          let room = expectedRoom.flatMap({ String(cString: $0) })
    else { return returnJson(match: false, confidence: 0, reason: "bad-args") }

    #if canImport(FoundationModels)
    let prompt = """
    A photo was just captured. Vision framework detected the following tags: \(tags).
    Expected object: "\(object)". Expected room: "\(room)".

    Decide strictly:
      - Does the image clearly contain the expected object?
      - Does the surrounding scene match the expected room (e.g. bathroom has a sink, kitchen has counters, etc.)?

    Return a SceneVerdict.
    """

    let sem = DispatchSemaphore(value: 0)
    var out: String = "{\"match\":false,\"confidence\":0,\"reason\":\"no-response\"}"

    Task {
        do {
            let session = LanguageModelSession()
            let response = try await session.respond(to: prompt, generating: SceneVerdict.self)
            let verdict = response.content
            out = "{\"match\":\(verdict.match),\"confidence\":\(verdict.confidence),\"reason\":\"\(verdict.reason.replacingOccurrences(of: "\"", with: "'"))\"}"
        } catch {
            out = "{\"match\":false,\"confidence\":0,\"reason\":\"fm-error\"}"
        }
        sem.signal()
    }
    sem.wait()
    return strdupCString(out)
    #else
    return returnJson(match: false, confidence: 0, reason: "foundationmodels-unavailable")
    #endif
}

@_cdecl("anchor_fm_free")
public func anchor_fm_free(_ ptr: UnsafeMutablePointer<CChar>?) {
    free(ptr)
}

private func returnJson(match: Bool, confidence: Double, reason: String) -> UnsafeMutablePointer<CChar>? {
    let json = "{\"match\":\(match),\"confidence\":\(confidence),\"reason\":\"\(reason)\"}"
    return strdupCString(json)
}

private func strdupCString(_ s: String) -> UnsafeMutablePointer<CChar>? {
    return s.withCString { strdup($0) }
}