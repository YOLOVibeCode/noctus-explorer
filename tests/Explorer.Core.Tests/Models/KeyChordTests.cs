using NoctusExplorer.Core.Models;
using FluentAssertions;

namespace NoctusExplorer.Core.Tests.Models;

public class KeyChordTests
{
    [Fact]
    public void Parse_SimpleKey_ReturnsChord()
    {
        var chord = KeyChord.Parse("F5");
        chord.Key.Should().Be("F5");
        chord.Ctrl.Should().BeFalse();
        chord.Shift.Should().BeFalse();
        chord.Alt.Should().BeFalse();
    }

    [Fact]
    public void Parse_CtrlShiftP_ReturnsModifiers()
    {
        var chord = KeyChord.Parse("Ctrl+Shift+P");
        chord.Key.Should().Be("P");
        chord.Ctrl.Should().BeTrue();
        chord.Shift.Should().BeTrue();
        chord.Alt.Should().BeFalse();
    }

    [Fact]
    public void Parse_AltLeft_ReturnsModifiers()
    {
        var chord = KeyChord.Parse("Alt+Left");
        chord.Key.Should().Be("LEFT");
        chord.Alt.Should().BeTrue();
    }

    [Fact]
    public void Parse_EmptyString_Throws()
    {
        var act = () => KeyChord.Parse("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Parse_NoKey_Throws()
    {
        var act = () => KeyChord.Parse("Ctrl+Shift");
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Parse_MultipleKeys_Throws()
    {
        var act = () => KeyChord.Parse("A+B");
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void ToString_RoundTrips()
    {
        var chord = new KeyChord("P", ctrl: true, shift: true);
        chord.ToString().Should().Be("Ctrl+Shift+P");
        KeyChord.Parse(chord.ToString()).Should().Be(chord);
    }

    [Fact]
    public void Equality_SameChord_AreEqual()
    {
        var a = new KeyChord("F5");
        var b = new KeyChord("f5");
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentModifiers_NotEqual()
    {
        var a = new KeyChord("P", ctrl: true);
        var b = new KeyChord("P", shift: true);
        a.Should().NotBe(b);
    }

    [Fact]
    public void TryParse_ValidInput_ReturnsTrue()
    {
        KeyChord.TryParse("Ctrl+A", out var result).Should().BeTrue();
        result!.Key.Should().Be("A");
        result.Ctrl.Should().BeTrue();
    }

    [Fact]
    public void TryParse_InvalidInput_ReturnsFalse()
    {
        KeyChord.TryParse("", out var result).Should().BeFalse();
        result.Should().BeNull();
    }
}
