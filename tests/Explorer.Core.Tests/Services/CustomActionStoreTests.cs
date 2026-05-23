using NoctusExplorer.Core.Models;
using NoctusExplorer.Core.Services;
using FluentAssertions;

namespace NoctusExplorer.Core.Tests.Services;

public class CustomActionStoreTests
{
    private static CustomAction MakeAction(string label = "Test", int order = 0)
        => new(Guid.NewGuid(), label, null, null, order,
            new ActionConditions(), ActionType.RunProgram,
            new Dictionary<string, string> { ["program"] = "echo" },
            false, true);

    [Fact]
    public void Initially_Empty()
    {
        var store = new CustomActionStore();
        store.Actions.Should().BeEmpty();
    }

    [Fact]
    public void Add_AppearsInActions()
    {
        var store = new CustomActionStore();
        store.Add(MakeAction("Open in VS Code"));
        store.Actions.Should().ContainSingle().Which.Label.Should().Be("Open in VS Code");
    }

    [Fact]
    public void Update_ReplacesExistingAction()
    {
        var store = new CustomActionStore();
        var action = MakeAction("Old Name");
        store.Add(action);
        store.Update(action with { Label = "New Name" });
        store.Actions.Should().ContainSingle().Which.Label.Should().Be("New Name");
    }

    [Fact]
    public void Remove_ById_RemovesAction()
    {
        var store = new CustomActionStore();
        var action = MakeAction();
        store.Add(action);
        store.Remove(action.Id);
        store.Actions.Should().BeEmpty();
    }

    [Fact]
    public void Reorder_ChangesPosition()
    {
        var store = new CustomActionStore();
        var a = MakeAction("A", 0);
        var b = MakeAction("B", 1);
        store.Add(a);
        store.Add(b);
        store.Reorder(b.Id, 0);
        store.Actions[0].Label.Should().Be("B");
    }

    [Fact]
    public void ActionsChanged_FiredOnAdd()
    {
        var store = new CustomActionStore();
        bool fired = false;
        store.ActionsChanged += (_, _) => fired = true;
        store.Add(MakeAction());
        fired.Should().BeTrue();
    }

    [Fact]
    public void Duplicate_CreatesNewIdWithSameConfig()
    {
        var store = new CustomActionStore();
        var action = MakeAction("Original");
        store.Add(action);
        var dupe = store.Duplicate(action.Id);
        dupe.Should().NotBeNull();
        dupe!.Id.Should().NotBe(action.Id);
        dupe.Label.Should().Be("Original (Copy)");
        store.Actions.Should().HaveCount(2);
    }
}
