using NoctusExplorer.Core.Models;
using NoctusExplorer.Core.Services;
using FluentAssertions;

namespace NoctusExplorer.Core.Tests.Services;

public class DropStackServiceTests
{
    [Fact]
    public void Initially_Empty()
    {
        var stack = new DropStackService();
        stack.Items.Should().BeEmpty();
    }

    [Fact]
    public void Add_SingleItem_AppearsInItems()
    {
        var stack = new DropStackService();
        var path = new PathRef("/test/file.txt");
        stack.Add([path]);
        stack.Items.Should().ContainSingle().Which.Should().Be(path);
    }

    [Fact]
    public void Add_MultipleItems_AllAppear()
    {
        var stack = new DropStackService();
        var a = new PathRef("/a.txt");
        var b = new PathRef("/b.txt");
        stack.Add([a, b]);
        stack.Items.Should().HaveCount(2);
    }

    [Fact]
    public void Add_DuplicateItem_NotAddedTwice()
    {
        var stack = new DropStackService();
        var path = new PathRef("/test/file.txt");
        stack.Add([path]);
        stack.Add([path]);
        stack.Items.Should().HaveCount(1);
    }

    [Fact]
    public void Remove_ExistingItem_RemovesIt()
    {
        var stack = new DropStackService();
        var path = new PathRef("/test/file.txt");
        stack.Add([path]);
        stack.Remove(path);
        stack.Items.Should().BeEmpty();
    }

    [Fact]
    public void Remove_NonexistentItem_DoesNothing()
    {
        var stack = new DropStackService();
        var act = () => stack.Remove(new PathRef("/nope"));
        act.Should().NotThrow();
    }

    [Fact]
    public void Clear_RemovesAllItems()
    {
        var stack = new DropStackService();
        stack.Add([new PathRef("/a"), new PathRef("/b")]);
        stack.Clear();
        stack.Items.Should().BeEmpty();
    }

    [Fact]
    public void ItemsChanged_FiredOnAdd()
    {
        var stack = new DropStackService();
        bool fired = false;
        stack.ItemsChanged += (_, _) => fired = true;
        stack.Add([new PathRef("/a")]);
        fired.Should().BeTrue();
    }

    [Fact]
    public void ItemsChanged_FiredOnRemove()
    {
        var stack = new DropStackService();
        var path = new PathRef("/a");
        stack.Add([path]);
        bool fired = false;
        stack.ItemsChanged += (_, _) => fired = true;
        stack.Remove(path);
        fired.Should().BeTrue();
    }

    [Fact]
    public void ItemsChanged_FiredOnClear()
    {
        var stack = new DropStackService();
        stack.Add([new PathRef("/a")]);
        bool fired = false;
        stack.ItemsChanged += (_, _) => fired = true;
        stack.Clear();
        fired.Should().BeTrue();
    }
}
