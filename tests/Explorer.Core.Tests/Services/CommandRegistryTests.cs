using NoctusExplorer.Core.Models;
using NoctusExplorer.Core.Services;
using FluentAssertions;

namespace NoctusExplorer.Core.Tests.Services;

public class CommandRegistryTests
{
    private static CommandDefinition MakeCommand(string id, bool canExecute = true)
    {
        bool executed = false;
        return new CommandDefinition
        {
            Id = id,
            Name = id.Replace('.', ' '),
            CanExecute = () => canExecute,
            Execute = () => executed = true
        };
    }

    [Fact]
    public void Register_AddsCommand()
    {
        var reg = new CommandRegistry();
        reg.Register(MakeCommand("test.cmd"));
        reg.GetById("test.cmd").Should().NotBeNull();
    }

    [Fact]
    public void Register_DuplicateId_Throws()
    {
        var reg = new CommandRegistry();
        reg.Register(MakeCommand("test.cmd"));
        var act = () => reg.Register(MakeCommand("test.cmd"));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetById_UnknownId_ReturnsNull()
    {
        var reg = new CommandRegistry();
        reg.GetById("nope").Should().BeNull();
    }

    [Fact]
    public void GetAll_ReturnsAllRegistered()
    {
        var reg = new CommandRegistry();
        reg.Register(MakeCommand("a"));
        reg.Register(MakeCommand("b"));
        reg.GetAll().Should().HaveCount(2);
    }

    [Fact]
    public void Execute_CallsCommandAction()
    {
        var reg = new CommandRegistry();
        bool wasCalled = false;
        reg.Register(new CommandDefinition
        {
            Id = "test",
            Name = "Test",
            CanExecute = () => true,
            Execute = () => wasCalled = true
        });
        reg.Execute("test");
        wasCalled.Should().BeTrue();
    }

    [Fact]
    public void Execute_WhenCannotExecute_DoesNothing()
    {
        var reg = new CommandRegistry();
        bool wasCalled = false;
        reg.Register(new CommandDefinition
        {
            Id = "test",
            Name = "Test",
            CanExecute = () => false,
            Execute = () => wasCalled = true
        });
        reg.Execute("test");
        wasCalled.Should().BeFalse();
    }

    [Fact]
    public void Execute_UnknownId_DoesNothing()
    {
        var reg = new CommandRegistry();
        var act = () => reg.Execute("nope");
        act.Should().NotThrow();
    }

    [Fact]
    public void CanExecute_ReturnsDelegateResult()
    {
        var reg = new CommandRegistry();
        reg.Register(MakeCommand("yes", canExecute: true));
        reg.Register(MakeCommand("no", canExecute: false));
        reg.CanExecute("yes").Should().BeTrue();
        reg.CanExecute("no").Should().BeFalse();
    }

    [Fact]
    public void CanExecute_UnknownId_ReturnsFalse()
    {
        var reg = new CommandRegistry();
        reg.CanExecute("nope").Should().BeFalse();
    }
}
