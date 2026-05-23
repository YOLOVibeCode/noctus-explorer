using NoctusExplorer.Core.Models;
using NoctusExplorer.Core.Services;
using FluentAssertions;

namespace NoctusExplorer.Core.Tests.Services;

public class CustomActionEngineTests
{
    private static FileEntry MakeFile(string path, string ext = ".txt")
        => new(
            new PathRef(path),
            System.IO.Path.GetFileName(path),
            ext, 1024,
            DateTimeOffset.Now, DateTimeOffset.Now,
            false, false, "Document");

    private static FileEntry MakeFolder(string path)
        => new(
            new PathRef(path, isDirectory: true),
            System.IO.Path.GetFileName(path),
            "", null,
            DateTimeOffset.Now, DateTimeOffset.Now,
            false, false, "Folder");

    private static CustomAction MakeAction(
        ActionConditions? conditions = null,
        ActionType actionType = ActionType.RunProgram)
        => new(
            Guid.NewGuid(), "Test", null, null, 0,
            conditions ?? new ActionConditions(),
            actionType,
            new Dictionary<string, string> { ["program"] = "echo", ["arguments"] = "{path}" },
            false, true);

    [Fact]
    public void Evaluate_NoConditions_ReturnsTrue()
    {
        var engine = new CustomActionEngine();
        var action = MakeAction();
        engine.Evaluate(action, [MakeFile("/test.txt")], new PathRef("/other")).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_DisabledAction_ReturnsFalse()
    {
        var engine = new CustomActionEngine();
        var action = MakeAction() with { Enabled = false };
        engine.Evaluate(action, [MakeFile("/test.txt")], new PathRef("/other")).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_FilesOnly_ExcludesFolders()
    {
        var engine = new CustomActionEngine();
        var action = MakeAction(new ActionConditions(AppliesTo: FileType.Files));
        engine.Evaluate(action, [MakeFolder("/docs")], new PathRef("/other")).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_FoldersOnly_ExcludesFiles()
    {
        var engine = new CustomActionEngine();
        var action = MakeAction(new ActionConditions(AppliesTo: FileType.Folders));
        engine.Evaluate(action, [MakeFile("/test.txt")], new PathRef("/other")).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_ExtensionFilter_MatchesCorrectExtension()
    {
        var engine = new CustomActionEngine();
        var action = MakeAction(new ActionConditions(Extensions: [".jpg", ".png"]));
        engine.Evaluate(action, [MakeFile("/photo.jpg", ".jpg")], new PathRef("/other")).Should().BeTrue();
        engine.Evaluate(action, [MakeFile("/doc.txt", ".txt")], new PathRef("/other")).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_SingleOnly_RejectsMultipleSelection()
    {
        var engine = new CustomActionEngine();
        var action = MakeAction(new ActionConditions(SelectionCount: SelectionCount.Single));
        engine.Evaluate(action, [MakeFile("/a.txt"), MakeFile("/b.txt")], new PathRef("/other")).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_MultipleOnly_RejectsSingleSelection()
    {
        var engine = new CustomActionEngine();
        var action = MakeAction(new ActionConditions(SelectionCount: SelectionCount.Multiple));
        engine.Evaluate(action, [MakeFile("/a.txt")], new PathRef("/other")).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_PathContains_MatchesSubstring()
    {
        var engine = new CustomActionEngine();
        var action = MakeAction(new ActionConditions(PathContains: "Projects"));
        engine.Evaluate(action, [MakeFile("/Users/me/Projects/app/main.cs")], new PathRef("/other")).Should().BeTrue();
        engine.Evaluate(action, [MakeFile("/tmp/test.txt")], new PathRef("/other")).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_EmptySelection_ReturnsFalse()
    {
        var engine = new CustomActionEngine();
        var action = MakeAction();
        engine.Evaluate(action, [], new PathRef("/other")).Should().BeFalse();
    }

    // Variable expansion tests
    [Fact]
    public void ExpandVariables_Path_ExpandsToFullPath()
    {
        var engine = new CustomActionEngine();
        var result = engine.ExpandVariables("{path}",
            [MakeFile("/Users/me/file.txt")],
            new PathRef("/other"));
        result.Should().Be("/Users/me/file.txt");
    }

    [Fact]
    public void ExpandVariables_Filename_ExpandsToNameWithExtension()
    {
        var engine = new CustomActionEngine();
        var result = engine.ExpandVariables("{filename}",
            [MakeFile("/Users/me/file.txt")],
            new PathRef("/other"));
        result.Should().Be("file.txt");
    }

    [Fact]
    public void ExpandVariables_Basename_ExpandsToNameWithoutExtension()
    {
        var engine = new CustomActionEngine();
        var result = engine.ExpandVariables("{basename}",
            [MakeFile("/Users/me/photo.jpg", ".jpg")],
            new PathRef("/other"));
        result.Should().Be("photo");
    }

    [Fact]
    public void ExpandVariables_Ext_ExpandsToExtensionWithoutDot()
    {
        var engine = new CustomActionEngine();
        var result = engine.ExpandVariables("{ext}",
            [MakeFile("/Users/me/photo.jpg", ".jpg")],
            new PathRef("/other"));
        result.Should().Be("jpg");
    }

    [Fact]
    public void ExpandVariables_Folder_ExpandsToParentDirectory()
    {
        var engine = new CustomActionEngine();
        var result = engine.ExpandVariables("{folder}",
            [MakeFile("/Users/me/file.txt")],
            new PathRef("/other"));
        result.Should().Be("/Users/me");
    }

    [Fact]
    public void ExpandVariables_OtherPane_ExpandsToInactivePanePath()
    {
        var engine = new CustomActionEngine();
        var result = engine.ExpandVariables("{other_pane}",
            [MakeFile("/Users/me/file.txt")],
            new PathRef("/backup"));
        result.Should().Be("/backup");
    }

    [Fact]
    public void ExpandVariables_Paths_ExpandsAllSelectedOnePerLine()
    {
        var engine = new CustomActionEngine();
        var result = engine.ExpandVariables("{paths}",
            [MakeFile("/a.txt"), MakeFile("/b.txt")],
            new PathRef("/other"));
        result.Should().Be("/a.txt\n/b.txt");
    }

    [Fact]
    public void ExpandVariables_PathsQuoted_ExpandsAllQuoted()
    {
        var engine = new CustomActionEngine();
        var result = engine.ExpandVariables("{paths:quoted}",
            [MakeFile("/a.txt"), MakeFile("/b.txt")],
            new PathRef("/other"));
        result.Should().Be("\"/a.txt\" \"/b.txt\"");
    }

    [Fact]
    public void ExpandVariables_MixedTemplate_ExpandsAll()
    {
        var engine = new CustomActionEngine();
        var result = engine.ExpandVariables("code \"{path}\" --goto 1",
            [MakeFile("/Users/me/main.cs")],
            new PathRef("/other"));
        result.Should().Be("code \"/Users/me/main.cs\" --goto 1");
    }
}
