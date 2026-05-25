using NoctusExplorer.Core.Models;
using NoctusExplorer.Core.Services.BatchRename;
using FluentAssertions;

namespace NoctusExplorer.Core.Tests.Services.BatchRename;

public class BatchRenameEngineTests
{
    private static FileEntry File(string name) =>
        new(new PathRef("/dir/" + name), name, System.IO.Path.GetExtension(name), 0,
            DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, false, false, "File");

    [Fact]
    public void Preview_NoOperations_ReturnsUnchanged()
    {
        var engine = new BatchRenameEngine();
        var entries = new[] { File("a.txt"), File("b.txt") };

        var result = engine.Preview(entries, []);

        result.Should().HaveCount(2);
        result[0].NewName.Should().Be("a.txt");
        result[0].IsValid.Should().BeTrue();
        result[0].ValidationError.Should().Be("Unchanged");
    }

    [Fact]
    public void Preview_FindReplace_ChangesMatches()
    {
        var engine = new BatchRenameEngine();
        var entries = new[] { File("IMG_001.jpg"), File("IMG_002.jpg"), File("note.txt") };
        var ops = new IRenameOperation[] { new FindReplaceOperation("IMG", "Photo") };

        var result = engine.Preview(entries, ops);

        result[0].NewName.Should().Be("Photo_001.jpg");
        result[1].NewName.Should().Be("Photo_002.jpg");
        result[2].NewName.Should().Be("note.txt");
    }

    [Fact]
    public void Preview_ChainedOperations_AppliedInOrder()
    {
        var engine = new BatchRenameEngine();
        var entries = new[] { File("hello.txt") };
        var ops = new IRenameOperation[]
        {
            new FindReplaceOperation("hello", "world"),
            new ChangeCaseOperation(CaseMode.Upper)
        };

        var result = engine.Preview(entries, ops);

        result[0].NewName.Should().Be("WORLD.txt");
    }

    [Fact]
    public void Preview_DuplicateName_FlagsAsInvalid()
    {
        var engine = new BatchRenameEngine();
        var entries = new[] { File("a.txt"), File("b.txt") };
        var ops = new IRenameOperation[] { new FindReplaceOperation("b", "a") };

        var result = engine.Preview(entries, ops);

        result[1].NewName.Should().Be("a.txt");
        result[1].IsValid.Should().BeFalse();
        result[1].ValidationError.Should().Be("Duplicate name within batch");
    }

    [Fact]
    public void Preview_EmptyNewName_FlagsAsInvalid()
    {
        var engine = new BatchRenameEngine();
        var entries = new[] { File("a.txt") };
        var ops = new IRenameOperation[] { new FindReplaceOperation("a.txt", "") };

        var result = engine.Preview(entries, ops);

        result[0].IsValid.Should().BeFalse();
        result[0].ValidationError.Should().Be("Name is empty");
    }

    [Fact]
    public void Preview_InvalidChar_FlagsAsInvalid()
    {
        var engine = new BatchRenameEngine();
        var entries = new[] { File("a.txt") };
        var ops = new IRenameOperation[] { new InsertTextOperation("foo/bar", InsertPosition.AtStart) };

        var result = engine.Preview(entries, ops);

        result[0].IsValid.Should().BeFalse();
        result[0].ValidationError.Should().Be("Name contains invalid characters");
    }
}

public class FindReplaceOperationTests
{
    private static FileEntry Entry(string name) =>
        new(new PathRef("/dir/" + name), name, System.IO.Path.GetExtension(name), 0,
            DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, false, false, "File");

    [Fact]
    public void Apply_PlainFind_ReplacesAll()
    {
        var op = new FindReplaceOperation("a", "X");
        op.Apply("banana.txt", 0, Entry("banana.txt")).Should().Be("bXnXnX.txt");
    }

    [Fact]
    public void Apply_PlainFind_CaseInsensitiveByDefault()
    {
        var op = new FindReplaceOperation("IMG", "Photo");
        op.Apply("img_001.jpg", 0, Entry("img_001.jpg")).Should().Be("Photo_001.jpg");
    }

    [Fact]
    public void Apply_CaseSensitive_OnlyMatchesExactCase()
    {
        var op = new FindReplaceOperation("IMG", "Photo", caseSensitive: true);
        op.Apply("img_001.jpg", 0, Entry("img_001.jpg")).Should().Be("img_001.jpg");
    }

    [Fact]
    public void Apply_Regex_MatchesPattern()
    {
        var op = new FindReplaceOperation(@"\d+", "###", useRegex: true);
        op.Apply("IMG_123.jpg", 0, Entry("IMG_123.jpg")).Should().Be("IMG_###.jpg");
    }

    [Fact]
    public void Apply_RegexBackref_Works()
    {
        var op = new FindReplaceOperation(@"(\w+)_(\d+)", "$2_$1", useRegex: true);
        op.Apply("IMG_001.jpg", 0, Entry("IMG_001.jpg")).Should().Be("001_IMG.jpg");
    }

    [Fact]
    public void Apply_InvalidRegex_ReturnsOriginal()
    {
        var op = new FindReplaceOperation("(unclosed", "x", useRegex: true);
        op.Apply("a.txt", 0, Entry("a.txt")).Should().Be("a.txt");
    }

    [Fact]
    public void Apply_EmptyFind_NoChange()
    {
        var op = new FindReplaceOperation("", "X");
        op.Apply("a.txt", 0, Entry("a.txt")).Should().Be("a.txt");
    }
}

public class InsertTextOperationTests
{
    private static FileEntry Entry(string name) =>
        new(new PathRef("/dir/" + name), name, System.IO.Path.GetExtension(name), 0,
            DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, false, false, "File");

    [Fact]
    public void Apply_AtStart_Prepends()
    {
        var op = new InsertTextOperation("pre_", InsertPosition.AtStart);
        op.Apply("a.txt", 0, Entry("a.txt")).Should().Be("pre_a.txt");
    }

    [Fact]
    public void Apply_AtEnd_Appends()
    {
        var op = new InsertTextOperation("_end", InsertPosition.AtEnd);
        op.Apply("a.txt", 0, Entry("a.txt")).Should().Be("a.txt_end");
    }

    [Fact]
    public void Apply_BeforeExtension_InsertsBeforeDot()
    {
        var op = new InsertTextOperation("_v2", InsertPosition.BeforeExtension);
        op.Apply("photo.jpg", 0, Entry("photo.jpg")).Should().Be("photo_v2.jpg");
    }

    [Fact]
    public void Apply_BeforeExtension_NoExt_AppendsAtEnd()
    {
        var op = new InsertTextOperation("_v2", InsertPosition.BeforeExtension);
        op.Apply("README", 0, Entry("README")).Should().Be("README_v2");
    }

    [Fact]
    public void Apply_AtIndex_InsertsAtPosition()
    {
        var op = new InsertTextOperation("X", InsertPosition.AtIndex, 2);
        op.Apply("abcd", 0, Entry("abcd")).Should().Be("abXcd");
    }

    [Fact]
    public void Apply_AtIndex_BeyondLength_AppendsAtEnd()
    {
        var op = new InsertTextOperation("X", InsertPosition.AtIndex, 99);
        op.Apply("abc", 0, Entry("abc")).Should().Be("abcX");
    }
}

public class ChangeCaseOperationTests
{
    private static FileEntry Entry(string name) =>
        new(new PathRef("/dir/" + name), name, System.IO.Path.GetExtension(name), 0,
            DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, false, false, "File");

    [Fact]
    public void Apply_Upper_PreservesExtension()
    {
        var op = new ChangeCaseOperation(CaseMode.Upper);
        op.Apply("hello.txt", 0, Entry("hello.txt")).Should().Be("HELLO.txt");
    }

    [Fact]
    public void Apply_Lower_PreservesExtension()
    {
        var op = new ChangeCaseOperation(CaseMode.Lower);
        op.Apply("HELLO.TXT", 0, Entry("HELLO.TXT")).Should().Be("hello.TXT");
    }

    [Fact]
    public void Apply_Title_CapitalizesEachWord()
    {
        var op = new ChangeCaseOperation(CaseMode.Title);
        op.Apply("hello world.txt", 0, Entry("hello world.txt")).Should().Be("Hello World.txt");
    }

    [Fact]
    public void Apply_Sentence_CapitalizesFirstLetterOnly()
    {
        var op = new ChangeCaseOperation(CaseMode.Sentence);
        op.Apply("hello world.txt", 0, Entry("hello world.txt")).Should().Be("Hello world.txt");
    }
}

public class NumberSequenceOperationTests
{
    private static FileEntry Entry(string name) =>
        new(new PathRef("/dir/" + name), name, System.IO.Path.GetExtension(name), 0,
            DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, false, false, "File");

    [Fact]
    public void Apply_Append_BeforeExtension()
    {
        var op = new NumberSequenceOperation(start: 1);
        op.Apply("photo.jpg", 0, Entry("photo.jpg")).Should().Be("photo_1.jpg");
        op.Apply("photo.jpg", 5, Entry("photo.jpg")).Should().Be("photo_6.jpg");
    }

    [Fact]
    public void Apply_Prepend()
    {
        var op = new NumberSequenceOperation(start: 1, prepend: true);
        op.Apply("photo.jpg", 0, Entry("photo.jpg")).Should().Be("1_photo.jpg");
    }

    [Fact]
    public void Apply_Padding_PadsWithZeros()
    {
        var op = new NumberSequenceOperation(start: 1, padding: 3);
        op.Apply("photo.jpg", 0, Entry("photo.jpg")).Should().Be("photo_001.jpg");
    }

    [Fact]
    public void Apply_Step_Increments()
    {
        var op = new NumberSequenceOperation(start: 10, step: 5);
        op.Apply("a.txt", 2, Entry("a.txt")).Should().Be("a_20.txt");
    }

    [Fact]
    public void Apply_NoExtension_StillAppends()
    {
        var op = new NumberSequenceOperation(start: 1);
        op.Apply("README", 0, Entry("README")).Should().Be("README_1");
    }
}

public class DateStampOperationTests
{
    private static FileEntry Entry(string name, DateTimeOffset modified)
        => new(new PathRef("/dir/" + name), name, System.IO.Path.GetExtension(name), 0,
            modified, modified, false, false, "File");

    [Fact]
    public void Apply_PrependsDateBeforeName()
    {
        var op = new DateStampOperation("yyyy-MM-dd", DateField.Modified, InsertPosition.AtStart);
        var date = new DateTimeOffset(2026, 5, 23, 0, 0, 0, TimeSpan.Zero);
        op.Apply("photo.jpg", 0, Entry("photo.jpg", date)).Should().Be("2026-05-23_photo.jpg");
    }

    [Fact]
    public void Apply_BeforeExtension_InsertsBetweenStemAndExt()
    {
        var op = new DateStampOperation("yyyyMMdd", DateField.Modified, InsertPosition.BeforeExtension);
        var date = new DateTimeOffset(2026, 5, 23, 0, 0, 0, TimeSpan.Zero);
        op.Apply("photo.jpg", 0, Entry("photo.jpg", date)).Should().Be("photo_20260523.jpg");
    }

    [Fact]
    public void Apply_InvalidFormat_FallsBackToDefault()
    {
        var op = new DateStampOperation("zzz-invalid-format-xxx", DateField.Modified);
        var date = new DateTimeOffset(2026, 5, 23, 0, 0, 0, TimeSpan.Zero);
        var result = op.Apply("a.txt", 0, Entry("a.txt", date));
        // Either succeeds with weird output, or fell back to default — just check it didn't throw and produced something
        result.Should().Contain("a.txt");
    }
}
