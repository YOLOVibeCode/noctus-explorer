import Foundation

public final class CommandDefinition {
    public let id: String
    public let name: String
    public let description: String
    public let defaultBinding: KeyChord?
    public let canExecute: () -> Bool
    public let execute: () -> Void

    public init(id: String, name: String, description: String = "",
                defaultBinding: KeyChord? = nil,
                canExecute: @escaping () -> Bool = { true },
                execute: @escaping () -> Void) {
        self.id = id
        self.name = name
        self.description = description
        self.defaultBinding = defaultBinding
        self.canExecute = canExecute
        self.execute = execute
    }
}
