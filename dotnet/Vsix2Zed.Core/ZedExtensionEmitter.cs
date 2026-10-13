using System.Text;
using Vsix2Zed.Core.Models;

namespace Vsix2Zed.Core;

public static class ZedExtensionEmitter
{
    public static void WriteExtensionToml(
        string outputDir,
        VsCodeManifest manifest,
        IReadOnlyList<string> themePaths,
        IReadOnlyList<string> snippetPaths,
        bool hasLspScaffold)
    {
        var id = SanitizeId(manifest.Name ?? "converted-ext");
        var sb = new StringBuilder();
        sb.AppendLine($"id = \"{EscapeToml(id)}\"");
        sb.AppendLine($"name = \"{EscapeToml(manifest.DisplayName ?? manifest.Name ?? id)}\"");
        sb.AppendLine($"version = \"{EscapeToml(manifest.Version ?? "0.0.1")}\"");
        sb.AppendLine("schema_version = 1");
        sb.AppendLine($"authors = [\"{EscapeToml(manifest.AuthorName)}\"]");
        sb.AppendLine($"description = \"{EscapeToml(manifest.Description ?? $"Converted from {manifest.ExtensionId}")}\"");
        var repo = manifest.RepositoryUrl ?? "https://example.com/converted-from-vsix";
        sb.AppendLine($"repository = \"{EscapeToml(repo)}\"");

        if (themePaths.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"themes = [{string.Join(", ", themePaths.Select(p => $"\"{EscapeToml(p)}\""))}]");
        }

        if (snippetPaths.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"snippets = [{string.Join(", ", snippetPaths.Select(p => $"\"{EscapeToml(p)}\""))}]");
        }

        if (hasLspScaffold)
        {
            sb.AppendLine();
            sb.AppendLine("# language servers: fill [language_servers.*] after editing src/lib.rs");
            sb.AppendLine("# See GAP_REPORT.md");
        }

        File.WriteAllText(Path.Combine(outputDir, "extension.toml"), sb.ToString());
    }

    private static string SanitizeId(string name) =>
        new string(name.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray())
            .Trim('-');

    private static string EscapeToml(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
