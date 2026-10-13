using System.Text.Json;
using Vsix2Zed.Core.Models;

namespace Vsix2Zed.Core.Converters;

public static class SnippetConverter
{
    public static IReadOnlyList<string> Convert(
        string extensionRoot,
        string outputDir,
        VsCodeManifest manifest,
        ConversionResult result)
    {
        var paths = new List<string>();
        if (manifest.Contributes is not { ValueKind: JsonValueKind.Object })
            return paths;
        if (!manifest.Contributes.Value.TryGetProperty("snippets", out var snippets)
            || snippets.ValueKind != JsonValueKind.Array)
            return paths;

        var outDir = Path.Combine(outputDir, "snippets");
        Directory.CreateDirectory(outDir);
        var n = 0;

        foreach (var entry in snippets.EnumerateArray())
        {
            var pathRel = entry.TryGetProperty("path", out var p) ? p.GetString() : null;
            var language = entry.TryGetProperty("language", out var lang) ? lang.GetString() : null;
            if (string.IsNullOrWhiteSpace(pathRel)) continue;

            var abs = Path.GetFullPath(Path.Combine(extensionRoot, pathRel.Replace('/', Path.DirectorySeparatorChar)));
            if (!File.Exists(abs))
            {
                result.Gaps.Add(new GapEntry("snippets", GapKind.Partial, "Snippet file missing", pathRel));
                continue;
            }

            // Zed: lowercase language name.json, or snippets.json for global
            var fileName = string.IsNullOrWhiteSpace(language)
                ? "snippets.json"
                : $"{SanitizeLanguageFile(language)}.json";

            var dest = Path.Combine(outDir, fileName);
            // Merge if multiple contribute to same language
            if (File.Exists(dest))
            {
                MergeSnippetFiles(abs, dest);
            }
            else
            {
                File.Copy(abs, dest, overwrite: true);
            }

            var rel = $"./snippets/{fileName}";
            if (!paths.Contains(rel, StringComparer.OrdinalIgnoreCase))
                paths.Add(rel);
            n++;
        }

        if (n > 0)
            result.Converted.Add($"snippets ({n} source file(s))");

        return paths;
    }

    private static string SanitizeLanguageFile(string language)
    {
        var s = language.Trim().ToLowerInvariant();
        // Zed removes path separators from language names
        return s.Replace('/', ' ').Replace('\\', ' ').Replace("  ", " ").Trim();
    }

    private static void MergeSnippetFiles(string source, string dest)
    {
        using var srcDoc = JsonDocument.Parse(File.ReadAllText(source));
        using var destDoc = JsonDocument.Parse(File.ReadAllText(dest));
        var map = new Dictionary<string, JsonElement>();
        foreach (var prop in destDoc.RootElement.EnumerateObject())
            map[prop.Name] = prop.Value.Clone();
        foreach (var prop in srcDoc.RootElement.EnumerateObject())
            map[prop.Name] = prop.Value.Clone();

        using var stream = File.Create(dest);
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
        writer.WriteStartObject();
        foreach (var (k, v) in map)
        {
            writer.WritePropertyName(k);
            v.WriteTo(writer);
        }
        writer.WriteEndObject();
    }
}
