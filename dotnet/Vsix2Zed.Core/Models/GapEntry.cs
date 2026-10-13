namespace Vsix2Zed.Core.Models;

public enum GapKind
{
    Unsupported,
    ScaffoldOnly,
    Partial
}

public sealed record GapEntry(
    string ContributeKey,
    GapKind Kind,
    string Reason,
    string? Detail = null);
