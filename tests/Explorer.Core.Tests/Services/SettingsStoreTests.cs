using NoctusExplorer.Core.Services;
using FluentAssertions;

namespace NoctusExplorer.Core.Tests.Services;

public class SettingsStoreTests
{
    [Fact]
    public void Get_UnknownKey_ReturnsDefault()
    {
        var store = new SettingsStore();
        store.Get("nope", 42).Should().Be(42);
    }

    [Fact]
    public void Set_ThenGet_ReturnsValue()
    {
        var store = new SettingsStore();
        store.Set("appearance.theme", "dark");
        store.Get("appearance.theme", "system").Should().Be("dark");
    }

    [Fact]
    public void Set_OverwritesPrevious()
    {
        var store = new SettingsStore();
        store.Set("key", 1);
        store.Set("key", 2);
        store.Get("key", 0).Should().Be(2);
    }

    [Fact]
    public void Subscribe_NotifiedOnChange()
    {
        var store = new SettingsStore();
        string? changedKey = null;
        object? changedValue = null;
        store.Subscribe("appearance", (key, val) => { changedKey = key; changedValue = val; });
        store.Set("appearance.theme", "dark");
        changedKey.Should().Be("appearance.theme");
        changedValue.Should().Be("dark");
    }

    [Fact]
    public void Subscribe_NotNotifiedForDifferentPrefix()
    {
        var store = new SettingsStore();
        bool notified = false;
        store.Subscribe("appearance", (_, _) => notified = true);
        store.Set("general.restoreSession", true);
        notified.Should().BeFalse();
    }

    [Fact]
    public void SaveAndLoad_RoundTrips()
    {
        var path = Path.Combine(Path.GetTempPath(), $"noctus_test_{Guid.NewGuid()}.json");
        try
        {
            var store1 = new SettingsStore();
            store1.Set("general.singleInstance", true);
            store1.Set("appearance.theme", "dark");
            store1.Set("panes.splitRatio", 0.6);
            store1.Save(path);

            var store2 = new SettingsStore();
            store2.Load(path);
            store2.Get("general.singleInstance", false).Should().BeTrue();
            store2.Get("appearance.theme", "system").Should().Be("dark");
            store2.Get("panes.splitRatio", 0.5).Should().Be(0.6);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_NonexistentFile_DoesNotThrow()
    {
        var store = new SettingsStore();
        var act = () => store.Load("/nonexistent/path/settings.json");
        act.Should().NotThrow();
    }

    [Fact]
    public void Get_TypeMismatch_ReturnsDefault()
    {
        var store = new SettingsStore();
        store.Set("key", "not a number");
        store.Get("key", 42).Should().Be(42);
    }
}
