using DiskDiff.Core.Models;
using FluentAssertions;

namespace DiskDiff.Core.Tests.Models;

public sealed class SnapshotEntryFactoryTests
{
    [Fact]
    public void Directory_root_has_empty_parent_to_avoid_cycle()
    {
        var entry = SnapshotEntry.Directory("c:\\");

        entry.Path.Should().Be("C:\\");
        entry.ParentPath.Should().BeEmpty();
        entry.Name.Should().Be("C:\\");
        entry.EntryType.Should().Be(EntryType.Directory);
    }

    [Fact]
    public void Directory_derives_name_and_parent_from_normalized_path()
    {
        var entry = SnapshotEntry.Directory("c:\\Users\\Administrator\\AppData\\");

        entry.Path.Should().Be("C:\\Users\\Administrator\\AppData");
        entry.ParentPath.Should().Be("C:\\Users\\Administrator");
        entry.Name.Should().Be("AppData");
    }

    [Fact]
    public void File_derives_name_and_parent_from_normalized_path()
    {
        var entry = SnapshotEntry.File(
            "c:\\Users\\Administrator\\AppData\\Local\\pagefile.sys",
            logicalBytes: 10,
            allocatedBytes: 16);

        entry.Path.Should().Be("C:\\Users\\Administrator\\AppData\\Local\\pagefile.sys");
        entry.ParentPath.Should().Be("C:\\Users\\Administrator\\AppData\\Local");
        entry.Name.Should().Be("pagefile.sys");
        entry.EntryType.Should().Be(EntryType.File);
    }
}
