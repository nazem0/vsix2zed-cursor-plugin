using Vsix2Zed.Core.Converters;
using Vsix2Zed.Core.Models;
using Vsix2Zed.Core.Remakes;

namespace Vsix2Zed.Core;

public sealed class ConversionOptions
{
    public required string VsixPath { get; init; }
    public required string OutputDirectory { get; init; }
    public bool Force { get; init; }
}

public static class ConversionPipeline
{
    public static ConversionResult Run(ConversionOptions options)
    {
        if (Directory.Exists(options.OutputDirectory) && Directory.EnumerateFileSystemEntries(options.OutputDirectory).Any())
        {
            if (!options.Force)
                throw new InvalidOperationException(
                    $"Output directory is not empty: {options.OutputDirectory}. Use --force to overwrite.");
        }

        Directory.CreateDirectory(options.OutputDirectory);

        var root = Unpacker.Unpack(options.VsixPath);
        try
        {
            var manifest = ManifestReader.Read(root);
            var classified = CapabilityClassifier.Classify(manifest);
            var result = new ConversionResult
            {
                OutputDirectory = options.OutputDirectory,
                ExtensionId = manifest.ExtensionId,
            };

            // Remakes win over declarative convert when we have a known port.
            if (HtmlCssSupportRemake.Matches(manifest)
                && HtmlCssSupportRemake.TryApply(options.OutputDirectory, result))
            {
                foreach (var c in classified.Where(c => c.Action == CapabilityAction.Gap))
                {
                    if (c.Key is "main/browser" or "commands" or "configuration")
                        continue; // covered / superseded by remake
                    result.Gaps.Add(new GapEntry(c.Key, GapKind.Unsupported, c.Note));
                }
                GapReporter.Write(options.OutputDirectory, result, manifest);
                result.Converted.Add("GAP_REPORT.md");
                return result;
            }

            foreach (var c in classified.Where(c => c.Action == CapabilityAction.Gap))
            {
                result.Gaps.Add(new GapEntry(c.Key, GapKind.Unsupported, c.Note));
            }

            foreach (var c in classified.Where(c => c.Action == CapabilityAction.Scaffold && c.Key is "debuggers"))
            {
                result.Gaps.Add(new GapEntry(c.Key, GapKind.ScaffoldOnly, c.Note + " (not emitted in v1)"));
            }

            ThemeConverter.Convert(root, options.OutputDirectory, manifest, result);
            // Pull theme path marker out of Converted list
            var themePaths = result.Converted
                .Where(x => x.StartsWith("theme_paths:", StringComparison.Ordinal))
                .SelectMany(x => x["theme_paths:".Length..].Split(';', StringSplitOptions.RemoveEmptyEntries))
                .ToList();
            result.Converted.RemoveAll(x => x.StartsWith("theme_paths:", StringComparison.Ordinal));

            var snippetPaths = SnippetConverter.Convert(root, options.OutputDirectory, manifest, result).ToList();

            var lsp = LspScaffoldWriter.ShouldScaffold(manifest, classified);
            if (lsp)
                LspScaffoldWriter.Write(options.OutputDirectory, manifest, result);

            ZedExtensionEmitter.WriteExtensionToml(
                options.OutputDirectory,
                manifest,
                themePaths,
                snippetPaths,
                hasLspScaffold: lsp);

            result.Converted.Add("extension.toml");
            GapReporter.Write(options.OutputDirectory, result, manifest);
            result.Converted.Add("GAP_REPORT.md");
            return result;
        }
        finally
        {
            // leave temp unpack for debugging? delete parent of root if under vsix2zed temp
            TryDeleteTemp(root);
        }
    }

    private static void TryDeleteTemp(string extensionRoot)
    {
        try
        {
            var marker = Path.Combine(Path.GetTempPath(), "vsix2zed");
            if (extensionRoot.StartsWith(marker, StringComparison.OrdinalIgnoreCase))
            {
                var top = Directory.GetParent(extensionRoot);
                while (top is not null && !string.Equals(top.FullName.TrimEnd(Path.DirectorySeparatorChar),
                           marker.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
                {
                    if (top.Parent is null) break;
                    if (string.Equals(top.Parent.FullName.TrimEnd(Path.DirectorySeparatorChar),
                            marker.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
                    {
                        Directory.Delete(top.FullName, recursive: true);
                        break;
                    }
                    top = top.Parent;
                }
            }
        }
        catch
        {
            // ponytail: best-effort temp cleanup
        }
    }
}
