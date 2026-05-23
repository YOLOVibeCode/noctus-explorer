import Testing
@testable import NoctusCore

@Suite struct KeyChordTests {

    @Test func parseSimpleKey() {
        let chord = KeyChord.parse("F5")
        #expect(chord?.key == "F5")
        #expect(chord?.ctrl == false)
    }

    @Test func parseCtrlShiftP() {
        let chord = KeyChord.parse("Ctrl+Shift+P")
        #expect(chord?.key == "P")
        #expect(chord?.ctrl == true)
        #expect(chord?.shift == true)
    }

    @Test func parseCmdOption() {
        let chord = KeyChord.parse("Cmd+Opt+C")
        #expect(chord?.cmd == true)
        #expect(chord?.alt == true)
        #expect(chord?.key == "C")
    }

    @Test func parseEmptyReturnsNil() {
        #expect(KeyChord.parse("") == nil)
    }

    @Test func toStringRoundTrips() {
        let chord = KeyChord("P", ctrl: true, shift: true)
        let text = chord.description
        #expect(text == "Ctrl+Shift+P")
    }

    @Test func equalitySameChord() {
        let a = KeyChord("F5")
        let b = KeyChord("f5")
        #expect(a == b)
    }

    @Test func equalityDifferentModifiers() {
        let a = KeyChord("P", ctrl: true)
        let b = KeyChord("P", shift: true)
        #expect(a != b)
    }
}
