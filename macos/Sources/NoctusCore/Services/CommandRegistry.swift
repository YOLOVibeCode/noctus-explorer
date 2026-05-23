import Foundation

public final class CommandRegistry {
    private var commands: [String: CommandDefinition] = [:]

    public init() {}

    public func register(_ command: CommandDefinition) {
        precondition(commands[command.id] == nil, "Command '\(command.id)' already registered")
        commands[command.id] = command
    }

    public func getById(_ id: String) -> CommandDefinition? {
        commands[id]
    }

    public func getAll() -> [CommandDefinition] {
        Array(commands.values)
    }

    public func canExecute(_ id: String) -> Bool {
        commands[id]?.canExecute() ?? false
    }

    public func execute(_ id: String) {
        guard let cmd = commands[id], cmd.canExecute() else { return }
        cmd.execute()
    }
}
