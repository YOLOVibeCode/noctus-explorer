using NoctusExplorer.Core.Models;
using NoctusExplorer.Core.Services;
using FluentAssertions;

namespace NoctusExplorer.Core.Tests.Services;

public class KeyBindingResolverTests
{
    [Fact]
    public void Resolve_RegisteredBinding_ReturnsCommandId()
    {
        var resolver = new KeyBindingResolver();
        resolver.SetBinding("pane.copyToOther", new KeyChord("F5"));
        resolver.Resolve(new KeyChord("F5")).Should().Be("pane.copyToOther");
    }

    [Fact]
    public void Resolve_UnregisteredChord_ReturnsNull()
    {
        var resolver = new KeyBindingResolver();
        resolver.Resolve(new KeyChord("F12")).Should().BeNull();
    }

    [Fact]
    public void SetBinding_OverridesPrevious()
    {
        var resolver = new KeyBindingResolver();
        resolver.SetBinding("old.command", new KeyChord("F5"));
        resolver.SetBinding("new.command", new KeyChord("F5"));
        resolver.Resolve(new KeyChord("F5")).Should().Be("new.command");
    }

    [Fact]
    public void GetBinding_ReturnsChordForCommand()
    {
        var resolver = new KeyBindingResolver();
        var chord = new KeyChord("P", ctrl: true, shift: true);
        resolver.SetBinding("tools.commandPalette", chord);
        resolver.GetBinding("tools.commandPalette").Should().Be(chord);
    }

    [Fact]
    public void GetBinding_UnknownCommand_ReturnsNull()
    {
        var resolver = new KeyBindingResolver();
        resolver.GetBinding("nope").Should().BeNull();
    }

    [Fact]
    public void LoadBindings_PopulatesAll()
    {
        var resolver = new KeyBindingResolver();
        resolver.LoadBindings(new Dictionary<string, KeyChord>
        {
            ["file.newTab"] = new KeyChord("T", ctrl: true),
            ["file.closeTab"] = new KeyChord("W", ctrl: true)
        });
        resolver.Resolve(new KeyChord("T", ctrl: true)).Should().Be("file.newTab");
        resolver.Resolve(new KeyChord("W", ctrl: true)).Should().Be("file.closeTab");
    }

    [Fact]
    public void LoadBindings_ClearsPrevious()
    {
        var resolver = new KeyBindingResolver();
        resolver.SetBinding("old", new KeyChord("F1"));
        resolver.LoadBindings(new Dictionary<string, KeyChord>
        {
            ["new"] = new KeyChord("F2")
        });
        resolver.Resolve(new KeyChord("F1")).Should().BeNull();
        resolver.Resolve(new KeyChord("F2")).Should().Be("new");
    }

    [Fact]
    public void RemoveBinding_RemovesMapping()
    {
        var resolver = new KeyBindingResolver();
        resolver.SetBinding("cmd", new KeyChord("F5"));
        resolver.RemoveBinding("cmd");
        resolver.Resolve(new KeyChord("F5")).Should().BeNull();
        resolver.GetBinding("cmd").Should().BeNull();
    }
}
