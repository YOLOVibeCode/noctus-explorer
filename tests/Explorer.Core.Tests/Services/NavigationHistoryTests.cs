using NoctusExplorer.Core.Models;
using NoctusExplorer.Core.Services;
using FluentAssertions;

namespace NoctusExplorer.Core.Tests.Services;

public class NavigationHistoryTests
{
    private NavigationHistory CreateHistory(string initialPath = "/start")
        => new(new PathRef(initialPath, isDirectory: true));

    [Fact]
    public void Initial_Current_IsStartPath()
    {
        var h = CreateHistory("/home");
        h.Current.FullPath.Should().Be("/home");
    }

    [Fact]
    public void Initial_CannotGoBackOrForward()
    {
        var h = CreateHistory();
        h.CanGoBack.Should().BeFalse();
        h.CanGoForward.Should().BeFalse();
    }

    [Fact]
    public void Push_UpdatesCurrent()
    {
        var h = CreateHistory();
        h.Push(new PathRef("/docs", isDirectory: true));
        h.Current.FullPath.Should().Be("/docs");
    }

    [Fact]
    public void Push_EnablesGoBack()
    {
        var h = CreateHistory();
        h.Push(new PathRef("/docs", isDirectory: true));
        h.CanGoBack.Should().BeTrue();
    }

    [Fact]
    public void GoBack_ReturnsPreviousLocation()
    {
        var h = CreateHistory("/a");
        h.Push(new PathRef("/b", isDirectory: true));
        var result = h.GoBack();
        result.FullPath.Should().Be("/a");
        h.Current.FullPath.Should().Be("/a");
    }

    [Fact]
    public void GoBack_EnablesGoForward()
    {
        var h = CreateHistory("/a");
        h.Push(new PathRef("/b", isDirectory: true));
        h.GoBack();
        h.CanGoForward.Should().BeTrue();
    }

    [Fact]
    public void GoForward_ReturnsNextLocation()
    {
        var h = CreateHistory("/a");
        h.Push(new PathRef("/b", isDirectory: true));
        h.GoBack();
        var result = h.GoForward();
        result.FullPath.Should().Be("/b");
    }

    [Fact]
    public void Push_AfterGoBack_ClearsForwardHistory()
    {
        var h = CreateHistory("/a");
        h.Push(new PathRef("/b", isDirectory: true));
        h.Push(new PathRef("/c", isDirectory: true));
        h.GoBack(); // at /b
        h.Push(new PathRef("/d", isDirectory: true)); // forward (/c) should be gone
        h.CanGoForward.Should().BeFalse();
    }

    [Fact]
    public void GoBack_WhenCannotGoBack_Throws()
    {
        var h = CreateHistory();
        var act = () => h.GoBack();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GoForward_WhenCannotGoForward_Throws()
    {
        var h = CreateHistory();
        var act = () => h.GoForward();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Push_SameAsCurrent_DoesNotDuplicate()
    {
        var h = CreateHistory("/a");
        h.Push(new PathRef("/a", isDirectory: true));
        h.CanGoBack.Should().BeFalse();
    }

    [Fact]
    public void MultipleNavigations_BackAndForward_WorkCorrectly()
    {
        var h = CreateHistory("/a");
        h.Push(new PathRef("/b", isDirectory: true));
        h.Push(new PathRef("/c", isDirectory: true));
        h.Push(new PathRef("/d", isDirectory: true));

        h.GoBack().FullPath.Should().Be("/c");
        h.GoBack().FullPath.Should().Be("/b");
        h.GoForward().FullPath.Should().Be("/c");
        h.Current.FullPath.Should().Be("/c");
    }
}
