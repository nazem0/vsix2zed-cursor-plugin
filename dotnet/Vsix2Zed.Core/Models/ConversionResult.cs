namespace Vsix2Zed.Core.Models;

public sealed class ConversionResult
{
    public required string OutputDirectory { get; init; }
    public required string ExtensionId { get; init; }
    public List<string> Converted { get; } = [];
    public List<GapEntry> Gaps { get; } = [];
    public bool WroteAnything => Converted.Count > 0;
}
