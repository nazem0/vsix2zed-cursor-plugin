using System.Text.Json;
using System.Text.Json.Serialization;
using Vsix2Zed.Core.Models;
using Vsix2Zed.Core.Remakes;

namespace Vsix2Zed.Core;

public sealed class AnalysisReport
{
    public required string VsixPath { get; init; }
    public required string ExtensionId { get; init; }
    public required string Name { get; init; }
    public string? Publisher { get; init; }
    public string? Version { get; init; }
    public string? DisplayName { get; init; }
    public bool HasMainOrBrowser { get; init; }
    public bool HasLanguageClientHint { get; init; }
    public bool MatchesHtmlCssRemake { get; init; }
    public required string Strategy { get; init; }
    public required IReadOnlyList<ClassifiedContributeDto> Contributes { get; init; }
    public required IReadOnlyList<string> NextSteps { get; init; }
}

public sealed class ClassifiedContributeDto
{
    public required string Key { get; init; }
    public required string Action { get; init; }
    public required string Note { get; init; }
}

public static class AnalysisPipeline
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static AnalysisReport Analyze(string vsixPath)
    {
        if (!File.Exists(vsixPath))
            throw new FileNotFoundException("VSIX not found", vsixPath);

        var root = Unpacker.Unpack(vsixPath);
        try
        {
            var manifest = ManifestReader.Read(root);
            var classified = CapabilityClassifier.Classify(manifest);
            var remake = HtmlCssSupportRemake.Matches(manifest);
            var hasHost = !string.IsNullOrWhiteSpace(manifest.Main)
                          || !string.IsNullOrWhiteSpace(manifest.Browser);
            var lspHint = CapabilityClassifier.HasLanguageClientHint(manifest);

            var strategy = remake
                ? "remake:html-css-support"
                : hasHost && !lspHint
                    ? "ai-remake-candidate"
                    : lspHint
                        ? "lsp-scaffold"
                        : "declarative-convert";

            var next = new List<string>();
            if (remake)
            {
                next.Add("Call convert_vsix — known remake will be applied.");
                next.Add("Install output via Zed → Install Dev Extension.");
            }
            else if (strategy == "declarative-convert")
            {
                next.Add("Call convert_vsix for themes/snippets.");
                next.Add("Review GAP_REPORT.md; ignore host-JS gaps if none.");
            }
            else if (strategy == "lsp-scaffold")
            {
                next.Add("Call convert_vsix to emit Rust LSP scaffold.");
                next.Add("Fill language_server_command (download-into-work-dir like zed/extensions/html).");
                next.Add("Use REA if the VSIX ships opaque native/JS server binaries.");
            }
            else
            {
                next.Add("Call convert_vsix for declarative bits + GAP_REPORT.");
                next.Add("Cursor agent remakes host logic into Zed WASM + optional Node LSP.");
                next.Add("Follow Zed packaging rules (no arbitrary server/ sidecars in the archive).");
                next.Add("Use REA MCP on extension.js / native helpers when behavior is unclear.");
            }

            return new AnalysisReport
            {
                VsixPath = Path.GetFullPath(vsixPath),
                ExtensionId = manifest.ExtensionId,
                Name = manifest.Name ?? "unknown",
                Publisher = manifest.Publisher,
                Version = manifest.Version,
                DisplayName = manifest.DisplayName,
                HasMainOrBrowser = hasHost,
                HasLanguageClientHint = lspHint,
                MatchesHtmlCssRemake = remake,
                Strategy = strategy,
                Contributes = classified.Select(c => new ClassifiedContributeDto
                {
                    Key = c.Key,
                    Action = c.Action.ToString(),
                    Note = c.Note,
                }).ToList(),
                NextSteps = next,
            };
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* temp */ }
        }
    }

    public static string ToJson(AnalysisReport report) =>
        JsonSerializer.Serialize(report, JsonOptions);
}
