using NoctusExplorer.Core.Models;
using FluentAssertions;

namespace NoctusExplorer.Core.Tests.Models;

public class PathRefTests
{
    [Fact]
    public void Constructor_ValidPath_SetsProperties()
    {
        var p = new PathRef("/Users/me/file.txt");
        p.FullPath.Should().Be("/Users/me/file.txt");
        p.DisplayName.Should().Be("file.txt");
        p.IsDirectory.Should().BeFalse();
    }

    [Fact]
    public void Constructor_DirectoryFlag_PreservedInProperty()
    {
        var p = new PathRef("/Users/me/docs", isDirectory: true);
        p.IsDirectory.Should().BeTrue();
    }

    [Fact]
    public void Constructor_CustomDisplayName_Used()
    {
        var p = new PathRef("/Users/me/docs", displayName: "My Documents");
        p.DisplayName.Should().Be("My Documents");
    }

    [Fact]
    public void Constructor_NullPath_Throws()
    {
        var act = () => new PathRef(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_EmptyPath_Throws()
    {
        var act = () => new PathRef("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equality_SamePath_CaseInsensitive()
    {
        var a = new PathRef("/Users/ME/File.txt");
        var b = new PathRef("/Users/me/file.txt");
        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentPaths_NotEqual()
    {
        var a = new PathRef("/Users/me/a.txt");
        var b = new PathRef("/Users/me/b.txt");
        a.Should().NotBe(b);
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_SamePath_SameHash()
    {
        var a = new PathRef("/Users/me/File.TXT");
        var b = new PathRef("/Users/me/file.txt");
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void GetParent_ReturnsParentDirectory()
    {
        var p = new PathRef("/Users/me/docs/file.txt");
        var parent = p.GetParent();
        parent.Should().NotBeNull();
        parent!.FullPath.Should().Be("/Users/me/docs");
        parent.IsDirectory.Should().BeTrue();
    }

    [Fact]
    public void NormalizePath_BackslashesConvertedToForward()
    {
        var p = new PathRef("C:\\Users\\me\\file.txt");
        p.FullPath.Should().Be("C:/Users/me/file.txt");
    }

    [Fact]
    public void NormalizePath_TrailingSlashRemoved()
    {
        var p = new PathRef("/Users/me/docs/");
        p.FullPath.Should().Be("/Users/me/docs");
    }

    [Fact]
    public void PlatformHandle_PreservedWhenProvided()
    {
        var handle = new byte[] { 1, 2, 3 };
        var p = new PathRef("/test", platformHandle: handle);
        p.PlatformHandle.Should().BeEquivalentTo(handle);
    }

    [Fact]
    public void ToString_ReturnsFullPath()
    {
        var p = new PathRef("/Users/me/file.txt");
        p.ToString().Should().Be("/Users/me/file.txt");
    }
}
