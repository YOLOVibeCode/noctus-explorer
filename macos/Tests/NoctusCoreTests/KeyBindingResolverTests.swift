import Testing
@testable import NoctusCore

@Suite struct KeyBindingResolverTests {

    @Test func resolveRegisteredBinding() {
        let r = KeyBindingResolver()
        r.setBinding("pane.copy", chord: KeyChord("F5"))
        #expect(r.resolve(KeyChord("F5")) == "pane.copy")
    }

    @Test func resolveUnknownReturnsNil() {
        let r = KeyBindingResolver()
        #expect(r.resolve(KeyChord("F12")) == nil)
    }

    @Test func setBindingOverrides() {
        let r = KeyBindingResolver()
        r.setBinding("old", chord: KeyChord("F5"))
        r.setBinding("new", chord: KeyChord("F5"))
        #expect(r.resolve(KeyChord("F5")) == "new")
    }

    @Test func getBindingReturnsChord() {
        let r = KeyBindingResolver()
        let chord = KeyChord("P", ctrl: true, shift: true)
        r.setBinding("cmd.palette", chord: chord)
        #expect(r.getBinding("cmd.palette") == chord)
    }

    @Test func removeBindingWorks() {
        let r = KeyBindingResolver()
        r.setBinding("cmd", chord: KeyChord("F5"))
        r.removeBinding("cmd")
        #expect(r.resolve(KeyChord("F5")) == nil)
        #expect(r.getBinding("cmd") == nil)
    }

    @Test func loadBindingsClearsPrevious() {
        let r = KeyBindingResolver()
        r.setBinding("old", chord: KeyChord("F1"))
        r.loadBindings(["new": KeyChord("F2")])
        #expect(r.resolve(KeyChord("F1")) == nil)
        #expect(r.resolve(KeyChord("F2")) == "new")
    }
}
