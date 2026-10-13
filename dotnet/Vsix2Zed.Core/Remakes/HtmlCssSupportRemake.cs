using System.Text.Json;
using Vsix2Zed.Core.Models;

namespace Vsix2Zed.Core.Remakes;

/// <summary>
/// Detects ecmel.vscode-html-css (or clones) and copies the maintained Zed remake.
/// </summary>
public static class HtmlCssSupportRemake
{
    public const string RemakeId = "html-css-support";

    public static bool Matches(VsCodeManifest manifest)
    {
        if (string.Equals(manifest.Name, "vscode-html-css", StringComparison.OrdinalIgnoreCase)
            && string.Equals(manifest.Publisher, "ecmel", StringComparison.OrdinalIgnoreCase))
            return true;

        // Heuristic: CSS class completion host extensions with these settings
        if (manifest.Contributes is not { ValueKind: JsonValueKind.Object } c) return false;
        if (!c.TryGetProperty("configuration", out var cfg)) return false;
        var raw = cfg.GetRawText();
        return raw.Contains("styleSheets", StringComparison.Ordinal)
               && raw.Contains("enabledLanguages", StringComparison.Ordinal)
               && !string.IsNullOrWhiteSpace(manifest.Main);
    }

    /// <summary>Copies remake sources into outputDir. Returns true if applied.</summary>
    public static bool TryApply(string outputDir, ConversionResult result)
    {
        var source = FindRemakeSource();
        if (source is null)
        {
            result.Gaps.Add(new GapEntry(
                "remake:html-css-support",
                GapKind.ScaffoldOnly,
                "Matched HTML CSS Support pattern but remake template folder was not found",
                "Expected remakes/html-css-support, out/ecmel-vscode-html-css, or VSICODE_HTML_CSS_REMAKE"));
            return false;
        }

        CopyTree(source, outputDir, exclude: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "target", "_unpacked", ".git", "node_modules", "bin", "obj"
        });

        result.Converted.Add($"REMAKE {RemakeId} ← {source}");
        result.Gaps.Add(new GapEntry(
            "main/browser",
            GapKind.ScaffoldOnly,
            "Original vscode host JS replaced by Zed LSP remake (server/server.mjs)",
            RemakeId));
        return true;
    }

    private static string? FindRemakeSource()
    {
        var env = Environment.GetEnvironmentVariable("VSICODE_HTML_CSS_REMAKE");
        if (!string.IsNullOrWhiteSpace(env) && IsRemakeDir(env)) return env;

        // Walk up from the published CLI / plugin root.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            foreach (var relative in new[]
                     {
                         Path.Combine("remakes", "html-css-support"),
                         Path.Combine("out", "ecmel-vscode-html-css"),
                     })
            {
                var candidate = Path.Combine(dir.FullName, relative);
                if (IsRemakeDir(candidate)) return candidate;
            }
            dir = dir.Parent;
        }
        return null;
    }

    private static bool IsRemakeDir(string path) =>
        File.Exists(Path.Combine(path, "extension.toml"))
        && File.Exists(Path.Combine(path, "server", "server.mjs"));

    private static void CopyTree(string src, string dest, HashSet<string> exclude)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.EnumerateFiles(src))
        {
            var name = Path.GetFileName(file);
            if (name.Equals("GAP_REPORT.md", StringComparison.OrdinalIgnoreCase)) continue;
            File.Copy(file, Path.Combine(dest, name), overwrite: true);
        }
        foreach (var sub in Directory.EnumerateDirectories(src))
        {
            var name = Path.GetFileName(sub);
            if (exclude.Contains(name)) continue;
            CopyTree(sub, Path.Combine(dest, name), exclude);
        }
    }
}
