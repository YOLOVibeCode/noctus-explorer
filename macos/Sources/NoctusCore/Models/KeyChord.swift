import Foundation

public struct KeyChord: Equatable, Hashable, CustomStringConvertible {
    public let key: String
    public let ctrl: Bool
    public let shift: Bool
    public let alt: Bool
    public let cmd: Bool

    public init(_ key: String, ctrl: Bool = false, shift: Bool = false, alt: Bool = false, cmd: Bool = false) {
        precondition(!key.isEmpty, "Key cannot be empty")
        self.key = key.uppercased()
        self.ctrl = ctrl
        self.shift = shift
        self.alt = alt
        self.cmd = cmd
    }

    public var description: String {
        var parts: [String] = []
        if cmd { parts.append("Cmd") }
        if ctrl { parts.append("Ctrl") }
        if alt { parts.append("Alt") }
        if shift { parts.append("Shift") }
        parts.append(key)
        return parts.joined(separator: "+")
    }

    public static func parse(_ text: String) -> KeyChord? {
        guard !text.isEmpty else { return nil }
        let parts = text.split(separator: "+").map { $0.trimmingCharacters(in: .whitespaces) }
        var ctrl = false, shift = false, alt = false, cmd = false
        var key: String?

        for part in parts {
            switch part.uppercased() {
            case "CTRL": ctrl = true
            case "SHIFT": shift = true
            case "ALT", "OPT", "OPTION": alt = true
            case "CMD", "COMMAND": cmd = true
            default:
                guard key == nil else { return nil }
                key = part
            }
        }

        guard let k = key else { return nil }
        return KeyChord(k, ctrl: ctrl, shift: shift, alt: alt, cmd: cmd)
    }
}
