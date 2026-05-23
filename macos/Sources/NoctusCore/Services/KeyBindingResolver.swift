import Foundation

public final class KeyBindingResolver {
    private var chordToCommand: [KeyChord: String] = [:]
    private var commandToChord: [String: KeyChord] = [:]

    public init() {}

    public func loadBindings(_ bindings: [String: KeyChord]) {
        chordToCommand.removeAll()
        commandToChord.removeAll()
        for (commandId, chord) in bindings {
            chordToCommand[chord] = commandId
            commandToChord[commandId] = chord
        }
    }

    public func resolve(_ chord: KeyChord) -> String? {
        chordToCommand[chord]
    }

    public func getBinding(_ commandId: String) -> KeyChord? {
        commandToChord[commandId]
    }

    public func setBinding(_ commandId: String, chord: KeyChord) {
        if let oldChord = commandToChord[commandId] {
            chordToCommand.removeValue(forKey: oldChord)
        }
        if let oldCmd = chordToCommand[chord] {
            commandToChord.removeValue(forKey: oldCmd)
        }
        chordToCommand[chord] = commandId
        commandToChord[commandId] = chord
    }

    public func removeBinding(_ commandId: String) {
        if let chord = commandToChord.removeValue(forKey: commandId) {
            chordToCommand.removeValue(forKey: chord)
        }
    }
}
