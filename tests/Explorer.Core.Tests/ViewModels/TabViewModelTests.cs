using NoctusExplorer.Core.Abstractions;
using NoctusExplorer.Core.Models;
using NoctusExplorer.Core.ViewModels;
using NSubstitute;
using FluentAssertions;

namespace NoctusExplorer.Core.Tests.ViewModels;

public class TabViewModelTests
{
    private static (TabViewModel vm, IShellService shell, IFileWatcher watcher) CreateTab(string path = "/home")
    {
        var shell = Substitute.For<IShellService>();
        var watcher = Substitute.For<IFileWatcher>();
        var location = new PathRef(path, isDirectory: true);
        shell.GetDisplayName(Arg.Any<PathRef>()).Returns(ci => ci.Arg<PathRef>().DisplayName);
        shell.EnumerateAsync(Arg.Any<PathRef>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<FileEntry>>([]));

        var vm = new TabViewModel(0, location, shell, watcher);
        return (vm, shell, watcher);
    }

    [Fact]
    public void Constructor_SetsInitialLocation()
    {
        var (vm, _, _) = CreateTab("/home");
        vm.Location.FullPath.Should().Be("/home");
    }

    [Fact]
    public async Task NavigateAsync_UpdatesLocation()
    {
        var (vm, shell, _) = CreateTab();
        var target = new PathRef("/docs", isDirectory: true);
        shell.EnumerateAsync(target, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<FileEntry>>([]));

        await vm.NavigateAsync(target);
        vm.Location.Should().Be(target);
    }

    [Fact]
    public async Task NavigateAsync_PopulatesEntries()
    {
        var (vm, shell, _) = CreateTab();
        var target = new PathRef("/docs", isDirectory: true);
        var entries = new List<FileEntry>
        {
            new(new PathRef("/docs/file.txt"), "file.txt", ".txt", 100,
                DateTimeOffset.Now, DateTimeOffset.Now, false, false, "Document")
        };
        shell.EnumerateAsync(target, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<FileEntry>>(entries));

        await vm.NavigateAsync(target);
        vm.Entries.Should().HaveCount(1);
        vm.Entries[0].Name.Should().Be("file.txt");
    }

    [Fact]
    public async Task NavigateAsync_WatchesNewDirectory()
    {
        var (vm, shell, watcher) = CreateTab();
        var target = new PathRef("/docs", isDirectory: true);
        shell.EnumerateAsync(target, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<FileEntry>>([]));

        await vm.NavigateAsync(target);
        watcher.Received(1).Watch(target);
    }

    [Fact]
    public async Task NavigateAsync_UnwatchesPreviousDirectory()
    {
        var (vm, shell, watcher) = CreateTab("/home");
        var target = new PathRef("/docs", isDirectory: true);
        shell.EnumerateAsync(target, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<FileEntry>>([]));

        var original = vm.Location;
        await vm.NavigateAsync(target);
        watcher.Received(1).Unwatch(original);
    }

    [Fact]
    public async Task GoBackAsync_NavigatesToPreviousLocation()
    {
        var (vm, shell, _) = CreateTab("/a");
        var b = new PathRef("/b", isDirectory: true);
        shell.EnumerateAsync(Arg.Any<PathRef>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<FileEntry>>([]));

        await vm.NavigateAsync(b);
        await vm.GoBackAsync();
        vm.Location.FullPath.Should().Be("/a");
    }

    [Fact]
    public async Task GoForwardAsync_NavigatesToNextLocation()
    {
        var (vm, shell, _) = CreateTab("/a");
        var b = new PathRef("/b", isDirectory: true);
        shell.EnumerateAsync(Arg.Any<PathRef>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<FileEntry>>([]));

        await vm.NavigateAsync(b);
        await vm.GoBackAsync();
        await vm.GoForwardAsync();
        vm.Location.FullPath.Should().Be("/b");
    }

    [Fact]
    public async Task GoUpAsync_NavigatesToParent()
    {
        var (vm, shell, _) = CreateTab("/home/user/docs");
        shell.EnumerateAsync(Arg.Any<PathRef>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<FileEntry>>([]));

        await vm.GoUpAsync();
        vm.Location.FullPath.Should().Be("/home/user");
    }

    [Fact]
    public void UpdateSelectionSummary_NoSelection_ShowsItemCount()
    {
        var (vm, _, _) = CreateTab();
        vm.Entries.Add(new FileEntry(new PathRef("/a.txt"), "a.txt", ".txt", 100,
            DateTimeOffset.Now, DateTimeOffset.Now, false, false, "Doc"));
        vm.UpdateSelectionSummary();
        vm.SelectionSummary.Should().Be("1 items");
    }

    [Fact]
    public void UpdateSelectionSummary_WithSelection_ShowsCountAndSize()
    {
        var (vm, _, _) = CreateTab();
        var entry = new FileEntry(new PathRef("/a.txt"), "a.txt", ".txt", 2048,
            DateTimeOffset.Now, DateTimeOffset.Now, false, false, "Doc");
        vm.Entries.Add(entry);
        vm.Selection.Add(entry);
        vm.UpdateSelectionSummary();
        vm.SelectionSummary.Should().Be("1 selected (2.0 KB)");
    }

    [Fact]
    public void SetFilter_ActivatesFilter()
    {
        var (vm, _, _) = CreateTab();
        vm.SetFilter("*.txt");
        vm.FilterText.Should().Be("*.txt");
        vm.IsFilterActive.Should().BeTrue();
    }

    [Fact]
    public void ClearFilter_DeactivatesFilter()
    {
        var (vm, _, _) = CreateTab();
        vm.SetFilter("*.txt");
        vm.ClearFilter();
        vm.FilterText.Should().BeEmpty();
        vm.IsFilterActive.Should().BeFalse();
    }

    [Fact]
    public void TogglePreview_FlipsVisibility()
    {
        var (vm, _, _) = CreateTab();
        vm.IsPreviewVisible.Should().BeFalse();
        vm.TogglePreview();
        vm.IsPreviewVisible.Should().BeTrue();
        vm.TogglePreview();
        vm.IsPreviewVisible.Should().BeFalse();
    }
}
