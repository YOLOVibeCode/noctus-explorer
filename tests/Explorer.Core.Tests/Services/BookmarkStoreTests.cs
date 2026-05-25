using NoctusExplorer.Core.Models;
using NoctusExplorer.Core.Services;
using FluentAssertions;

namespace NoctusExplorer.Core.Tests.Services;

public class BookmarkStoreTests
{
    [Fact]
    public void Initially_Empty()
    {
        var store = new BookmarkStore();
        store.Bookmarks.Should().BeEmpty();
    }

    [Fact]
    public void Add_AppearsInBookmarks()
    {
        var store = new BookmarkStore();
        var bm = new Bookmark(Guid.NewGuid(), "Home", new PathRef("/home"), null, 0);
        store.Add(bm);
        store.Bookmarks.Should().ContainSingle().Which.Name.Should().Be("Home");
    }

    [Fact]
    public void Remove_ById_RemovesBookmark()
    {
        var store = new BookmarkStore();
        var id = Guid.NewGuid();
        store.Add(new Bookmark(id, "Home", new PathRef("/home"), null, 0));
        store.Remove(id);
        store.Bookmarks.Should().BeEmpty();
    }

    [Fact]
    public void Reorder_ChangesOrder()
    {
        var store = new BookmarkStore();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        store.Add(new Bookmark(id1, "A", new PathRef("/a"), null, 0));
        store.Add(new Bookmark(id2, "B", new PathRef("/b"), null, 1));
        store.Reorder(id2, 0);
        store.Bookmarks[0].Name.Should().Be("B");
        store.Bookmarks[1].Name.Should().Be("A");
    }

    [Fact]
    public void GetGroups_ReturnsDistinctGroupNames()
    {
        var store = new BookmarkStore();
        store.Add(new Bookmark(Guid.NewGuid(), "A", new PathRef("/a"), "Servers", 0));
        store.Add(new Bookmark(Guid.NewGuid(), "B", new PathRef("/b"), "Servers", 1));
        store.Add(new Bookmark(Guid.NewGuid(), "C", new PathRef("/c"), "Local", 2));
        store.Add(new Bookmark(Guid.NewGuid(), "D", new PathRef("/d"), null, 3));
        store.GetGroups().Should().BeEquivalentTo(["Servers", "Local"]);
    }

    [Fact]
    public void BookmarksChanged_FiredOnAdd()
    {
        var store = new BookmarkStore();
        bool fired = false;
        store.BookmarksChanged += (_, _) => fired = true;
        store.Add(new Bookmark(Guid.NewGuid(), "X", new PathRef("/x"), null, 0));
        fired.Should().BeTrue();
    }

    [Fact]
    public void BookmarksChanged_FiredOnRemove()
    {
        var store = new BookmarkStore();
        var id = Guid.NewGuid();
        store.Add(new Bookmark(id, "X", new PathRef("/x"), null, 0));
        bool fired = false;
        store.BookmarksChanged += (_, _) => fired = true;
        store.Remove(id);
        fired.Should().BeTrue();
    }

    [Fact]
    public void ToJson_LoadFromJson_RoundTrips()
    {
        var a = new BookmarkStore();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        a.Add(new Bookmark(id1, "Home", new PathRef("/home"), null, 0));
        a.Add(new Bookmark(id2, "Server", new PathRef("//srv/share"), "Servers", 1));

        var json = a.ToJson();

        var b = new BookmarkStore();
        b.LoadFromJson(json);

        b.Bookmarks.Should().HaveCount(2);
        b.Bookmarks[0].Id.Should().Be(id1);
        b.Bookmarks[0].Name.Should().Be("Home");
        b.Bookmarks[1].Group.Should().Be("Servers");
    }

    [Fact]
    public void LoadFromJson_EmptyString_NoChange()
    {
        var store = new BookmarkStore();
        store.Add(new Bookmark(Guid.NewGuid(), "Home", new PathRef("/h"), null, 0));
        store.LoadFromJson("");
        store.Bookmarks.Should().HaveCount(1);
    }

    [Fact]
    public void LoadFromJson_Garbage_DoesNotThrow()
    {
        var store = new BookmarkStore();
        var act = () => store.LoadFromJson("not json at all {");
        act.Should().NotThrow();
    }
}
