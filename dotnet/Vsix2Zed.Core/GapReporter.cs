using System.Text;
using Vsix2Zed.Core.Models;

namespace Vsix2Zed.Core;

public static class GapReporter
{
    public static string Write(string outputDir, ConversionResult result, VsCodeManifest manifest)
    {
        var path = Path.Combine(outputDir, "GAP_REPORT.md");
        var sb = new StringBuilder();
        sb.AppendLine($"# Gap report: {manifest.DisplayName ?? manifest.Name}");
        sb.AppendLine();
        sb.AppendLine($"- Source: `{manifest.ExtensionId}` v{manifest.Version ?? "?"}");
        sb.AppendLine($"- Converted: {result.Converted.Count} item(s)");
        sb.AppendLine($"- Gaps: {result.Gaps.Count}");
        sb.AppendLine();
        sb.AppendLine("This is **not** a lossless VSIX→Zed clone. Zed has no VS Code extension host.");
        sb.AppendLine();

        if (result.Converted.Count > 0)
        {
            sb.AppendLine("## Converted");
            sb.AppendLine();
            foreach (var c in result.Converted)
                sb.AppendLine($"- {c}");
            sb.AppendLine();
        }

        sb.AppendLine("## Gaps");
        sb.AppendLine();
        sb.AppendLine("| Contribute | Kind | Reason | Detail |");
        sb.AppendLine("|------------|------|--------|--------|");
        foreach (var g in result.Gaps.OrderBy(g => g.ContributeKey, StringComparer.OrdinalIgnoreCase))
        {
            sb.AppendLine($"| `{Escape(g.ContributeKey)}` | {g.Kind} | {Escape(g.Reason)} | {Escape(g.Detail ?? "")} |");
        }

        if (result.Gaps.Count == 0)
            sb.AppendLine("| _(none)_ | | | |");

        File.WriteAllText(path, sb.ToString());
        return path;
    }

    private static string Escape(string s) => s.Replace("|", "\\|").Replace("\n", " ");
}
