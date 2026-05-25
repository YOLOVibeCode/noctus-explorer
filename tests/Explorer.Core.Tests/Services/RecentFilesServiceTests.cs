using NoctusExplorer.Core.Models;
using NoctusExplorer.Core.Services;
using FluentAssertions;

namespace NoctusExplorer.Core.Tests.Services;

public class RecentFilesServiceTests
{
    [Fact]
    public void Initially_Empty()
    {
        new RecentFilesService().Recent.Should().BeEmpty();
    }

    [Fact]
    public void Access_AddsToTop()
    {
        var svc = new RecentFilesService();
        svc.Access(new PathRef("/a.txt"));
        svc.Access(new PathRef("/b.txt"));
        svc.Recent[0].FullPath.Should().Be("/b.txt");
    }

    [Fact]
    public void Access_Existing_PromotesToTop()
    {
        var svc = new RecentFilesService();
        svc.Access(new PathRef("/a.txt"));
        svc.Access(new PathRef("/b.txt"));
        svc.Access(new PathRef("/a.txt"));
        svc.Recent.Should().HaveCount(2);
        svc.Recent[0].FullPath.Should().Be("/a.txt");
    }

    [Fact]
    public void MaxItems_CapsList()
    {
        var svc = new RecentFilesService { MaxItems = 2 };
        svc.Access(new PathRef("/a.txt"));
        svc.Access(new PathRef("/b.txt"));
        svc.Access(new PathRef("/c.txt"));
        svc.Recent.Should().HaveCount(2);
        svc.Recent[0].FullPath.Should().Be("/c.txt");
        svc.Recent[1].FullPath.Should().Be("/b.txt");
    }

    [Fact]
    public void Clear_Empties()
    {
        var svc = new RecentFilesService();
        svc.Access(new PathRef("/a.txt"));
        svc.Clear();
        svc.Recent.Should().BeEmpty();
    }

    [Fact]
    public void Changed_FiredOnAccess()
    {
        var svc = new RecentFilesService();
        bool fired = false;
        svc.Changed += (_, _) => fired = true;
        svc.Access(new PathRef("/a.txt"));
        fired.Should().BeTrue();
    }

    [Fact]
    public void ToJson_LoadFromJson_RoundTrips()
    {
        var a = new RecentFilesService();
        a.Access(new PathRef("/a.txt"));
        a.Access(new PathRef("/b.txt"));

        var b = new RecentFilesService();
        b.LoadFromJson(a.ToJson());

        b.Recent.Should().HaveCount(2);
        b.Recent[0].FullPath.Should().Be("/b.txt");
    }
}
