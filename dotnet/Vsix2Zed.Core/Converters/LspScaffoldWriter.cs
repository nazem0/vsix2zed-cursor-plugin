using System.Text;
using System.Text.Json;
using Vsix2Zed.Core.Models;

namespace Vsix2Zed.Core.Converters;

public static class LspScaffoldWriter
{
    public static bool ShouldScaffold(VsCodeManifest manifest, IReadOnlyList<ClassifiedContribute> classified) =>
        classified.Any(c =>
            (c.Key is "language-server" or "languages") && c.Action == CapabilityAction.Scaffold)
        || CapabilityClassifier.HasLanguageClientHint(manifest);

    public static void Write(
        string outputDir,
        VsCodeManifest manifest,
        ConversionResult result)
    {
        var crate = SanitizeCrate(manifest.Name ?? "converted_ext");
        var id = SanitizeId(manifest.Name ?? "converted-ext");

        File.WriteAllText(Path.Combine(outputDir, "Cargo.toml"), $$"""
            [package]
            name = "{{crate}}"
            version = "{{manifest.Version ?? "0.0.1"}}"
            edition = "2021"

            [lib]
            crate-type = ["cdylib"]

            [dependencies]
            zed_extension_api = "0.7.0"
            """);

        var srcDir = Path.Combine(outputDir, "src");
        Directory.CreateDirectory(srcDir);

        var langHints = CollectLanguageIds(manifest);
        var hintComment = langHints.Count > 0
            ? string.Join(", ", langHints)
            : "(no contributes.languages ids found — fill in manually)";

        File.WriteAllText(Path.Combine(srcDir, "lib.rs"), $$"""
            use zed_extension_api::{self as zed, Result, LanguageServerId, Worktree};

            struct ConvertedExtension;

            impl zed::Extension for ConvertedExtension {
                fn new() -> Self {
                    Self
                }

                // Languages hinted from VSIX: {{hintComment}}
                // TODO: set binary name / download logic from GAP_REPORT + original extension assets.
                fn language_server_command(
                    &mut self,
                    _language_server_id: &LanguageServerId,
                    worktree: &Worktree,
                ) -> Result<zed::Command> {
                    let path = worktree
                        .which("REPLACE_WITH_LANGUAGE_SERVER_BINARY")
                        .ok_or_else(|| "language server binary not found on PATH".to_string())?;
                    Ok(zed::Command {
                        command: path,
                        args: vec![],
                        env: Default::default(),
                    })
                }
            }

            zed::register_extension!(ConvertedExtension);
            """);

        result.Converted.Add("lsp scaffold → Cargo.toml + src/lib.rs");
        result.Gaps.Add(new GapEntry(
            "language-server",
            GapKind.ScaffoldOnly,
            "Rust Extension stub emitted; binary download/args must be filled from original extension evidence",
            hintComment));
    }

    private static List<string> CollectLanguageIds(VsCodeManifest manifest)
    {
        var ids = new List<string>();
        if (manifest.Contributes is not { ValueKind: JsonValueKind.Object }) return ids;
        if (!manifest.Contributes.Value.TryGetProperty("languages", out var languages)) return ids;
        if (languages.ValueKind != JsonValueKind.Array) return ids;
        foreach (var lang in languages.EnumerateArray())
        {
            if (lang.TryGetProperty("id", out var id) && id.GetString() is { } s)
                ids.Add(s);
        }
        return ids;
    }

    private static string SanitizeCrate(string name)
    {
        var sb = new StringBuilder();
        foreach (var ch in name.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch)) sb.Append(ch);
            else if (ch is '-' or '_') sb.Append('_');
        }
        if (sb.Length == 0 || char.IsDigit(sb[0])) sb.Insert(0, "ext_");
        return sb.ToString();
    }

    private static string SanitizeId(string name) =>
        new string(name.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray())
            .Trim('-');
}
