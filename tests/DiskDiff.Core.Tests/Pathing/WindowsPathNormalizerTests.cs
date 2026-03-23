using DiskDiff.Core.Pathing;
using FluentAssertions;

namespace DiskDiff.Core.Tests.Pathing;

public sealed class WindowsPathNormalizerTests
{
    [Theory]
    [InlineData("c:\\windows\\", "C:\\windows")]
    [InlineData("C:\\", "C:\\")]
    [InlineData("c:\\users\\admin\\", "C:\\users\\admin")]
    public void Normalize_returns_canonical_windows_paths(string rawPath, string expected)
    {
        WindowsPathNormalizer.Normalize(rawPath).Should().Be(expected);
    }

    [Fact]
    public void Normalize_preserves_segment_casing_from_full_path()
    {
        WindowsPathNormalizer.Normalize("c:\\Users\\Administrator\\AppData\\Local\\Temp\\")
            .Should()
            .Be("C:\\Users\\Administrator\\AppData\\Local\\Temp");
    }

    [Fact]
    public void Normalize_preserves_root_without_trimming_it_to_empty()
    {
        WindowsPathNormalizer.Normalize("c:\\").Should().Be("C:\\");
    }
}
