using NoctusExplorer.Core.Models;
using NoctusExplorer.Core.Services;
using FluentAssertions;

namespace NoctusExplorer.Core.Tests.Services;

public class RecentLocationsServiceTests
{
    [Fact]
    public void Initially_Empty()
    {
        var svc = new RecentLocationsService();
        svc.Recent.Should().BeEmpty();
    }

    [Fact]
    public void Visit_AddsToTop()
    {
        var svc = new RecentLocationsService();
        svc.Visit(new PathRef("/a"));
        svc.Visit(new PathRef("/b"));

        svc.Recent[0].FullPath.Should().Be("/b");
        svc.Recent[1].FullPath.Should().Be("/a");
    }

    [Fact]
    public void Visit_Existing_MovesToTop()
    {
        var svc = new RecentLocationsService();
        svc.Visit(new PathRef("/a"));
        svc.Visit(new PathRef("/b"));
        svc.Visit(new PathRef("/a"));

        svc.Recent.Should().HaveCount(2);
        svc.Recent[0].FullPath.Should().Be("/a");
        svc.Recent[1].FullPath.Should().Be("/b");
    }

    [Fact]
    public void Visit_SameAsTop_NoChange()
    {
        var svc = new RecentLocationsService();
        bool fired = false;
        svc.Visit(new PathRef("/a"));
        svc.Changed += (_, _) => fired = true;
        svc.Visit(new PathRef("/a"));   // same — no change
        fired.Should().BeFalse();
        svc.Recent.Should().HaveCount(1);
    }

    [Fact]
    public void MaxItems_CapsTheList()
    {
        var svc = new RecentLocationsService { MaxItems = 3 };
        svc.Visit(new PathRef("/a"));
        svc.Visit(new PathRef("/b"));
        svc.Visit(new PathRef("/c"));
        svc.Visit(new PathRef("/d"));

        svc.Recent.Should().HaveCount(3);
        svc.Recent[0].FullPath.Should().Be("/d");
        svc.Recent[2].FullPath.Should().Be("/b");
    }

    [Fact]
    public void Clear_EmptiesList()
    {
        var svc = new RecentLocationsService();
        svc.Visit(new PathRef("/a"));
        svc.Visit(new PathRef("/b"));
        svc.Clear();
        svc.Recent.Should().BeEmpty();
    }

    [Fact]
    public void Changed_FiredOnVisit()
    {
        var svc = new RecentLocationsService();
        bool fired = false;
        svc.Changed += (_, _) => fired = true;
        svc.Visit(new PathRef("/a"));
        fired.Should().BeTrue();
    }

    [Fact]
    public void ToJson_LoadFromJson_RoundTrips()
    {
        var a = new RecentLocationsService();
        a.Visit(new PathRef("/x"));
        a.Visit(new PathRef("/y"));

        var b = new RecentLocationsService();
        b.LoadFromJson(a.ToJson());

        b.Recent.Should().HaveCount(2);
        b.Recent[0].FullPath.Should().Be("/y");
        b.Recent[1].FullPath.Should().Be("/x");
    }

    [Fact]
    public void LoadFromJson_Garbage_DoesNotThrow()
    {
        var svc = new RecentLocationsService();
        var act = () => svc.LoadFromJson("garbage");
        act.Should().NotThrow();
    }
}
