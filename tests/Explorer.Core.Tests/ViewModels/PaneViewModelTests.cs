using NoctusExplorer.Core.Abstractions;
using NoctusExplorer.Core.Models;
using NoctusExplorer.Core.ViewModels;
using NSubstitute;
using FluentAssertions;

namespace NoctusExplorer.Core.Tests.ViewModels;

public class PaneViewModelTests
{
    private static PaneViewModel CreatePane()
    {
        var shell = Substitute.For<IShellService>();
        var watcher = Substitute.For<IFileWatcher>();
        shell.GetSpecialFolder(SpecialFolder.Home).Returns(new PathRef("/home", isDirectory: true));
        shell.GetDisplayName(Arg.Any<PathRef>()).Returns(ci => ci.Arg<PathRef>().DisplayName);
        return new PaneViewModel(shell, watcher);
    }

    [Fact]
    public void Initially_NoTabs()
    {
        var pane = CreatePane();
        pane.Tabs.Should().BeEmpty();
        pane.ActiveTab.Should().BeNull();
    }

    [Fact]
    public void AddTab_CreatesTabAndActivatesIt()
    {
        var pane = CreatePane();
        var tab = pane.AddTab();
        pane.Tabs.Should().HaveCount(1);
        pane.ActiveTab.Should().Be(tab);
    }

    [Fact]
    public void AddTab_WithLocation_SetsTabLocation()
    {
        var pane = CreatePane();
        var loc = new PathRef("/docs", isDirectory: true);
        var tab = pane.AddTab(loc);
        tab.Location.Should().Be(loc);
    }

    [Fact]
    public void CloseTab_RemovesTab()
    {
        var pane = CreatePane();
        var tab = pane.AddTab();
        pane.CloseTab(tab.Id);
        pane.Tabs.Should().BeEmpty();
    }

    [Fact]
    public void CloseTab_ActiveTab_ActivatesNeighbor()
    {
        var pane = CreatePane();
        var tab1 = pane.AddTab();
        var tab2 = pane.AddTab();
        pane.CloseTab(tab2.Id);
        pane.ActiveTab.Should().Be(tab1);
    }

    [Fact]
    public void ActivateTab_SwitchesActiveTab()
    {
        var pane = CreatePane();
        var tab1 = pane.AddTab();
        var tab2 = pane.AddTab();
        pane.ActivateTab(tab1.Id);
        pane.ActiveTab.Should().Be(tab1);
    }

    [Fact]
    public void RestoreTabs_PopulatesFromState()
    {
        var pane = CreatePane();
        var states = new List<TabState>
        {
            new(0, new PathRef("/a", isDirectory: true)),
            new(1, new PathRef("/b", isDirectory: true), ViewMode.List)
        };
        pane.RestoreTabs(states);
        pane.Tabs.Should().HaveCount(2);
        pane.Tabs[1].ViewMode.Should().Be(ViewMode.List);
    }
}
