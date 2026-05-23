import Testing
@testable import NoctusCore

@Suite struct CommandRegistryTests {

    func makeCommand(_ id: String, canExec: Bool = true) -> CommandDefinition {
        CommandDefinition(id: id, name: id, canExecute: { canExec }, execute: {})
    }

    @Test func registerAndGetById() {
        let reg = CommandRegistry()
        reg.register(makeCommand("test"))
        #expect(reg.getById("test") != nil)
    }

    @Test func getByIdUnknownReturnsNil() {
        let reg = CommandRegistry()
        #expect(reg.getById("nope") == nil)
    }

    @Test func getAllReturnsAll() {
        let reg = CommandRegistry()
        reg.register(makeCommand("a"))
        reg.register(makeCommand("b"))
        #expect(reg.getAll().count == 2)
    }

    @Test func executeCallsAction() {
        let reg = CommandRegistry()
        var called = false
        reg.register(CommandDefinition(id: "t", name: "t", execute: { called = true }))
        reg.execute("t")
        #expect(called == true)
    }

    @Test func executeWhenCannotDoesNothing() {
        let reg = CommandRegistry()
        var called = false
        reg.register(CommandDefinition(id: "t", name: "t", canExecute: { false }, execute: { called = true }))
        reg.execute("t")
        #expect(called == false)
    }

    @Test func canExecuteReturnsCorrectly() {
        let reg = CommandRegistry()
        reg.register(makeCommand("yes", canExec: true))
        reg.register(makeCommand("no", canExec: false))
        #expect(reg.canExecute("yes") == true)
        #expect(reg.canExecute("no") == false)
        #expect(reg.canExecute("nope") == false)
    }
}
