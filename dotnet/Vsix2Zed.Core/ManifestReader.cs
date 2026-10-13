using System.Text.Json;
using Vsix2Zed.Core.Models;

namespace Vsix2Zed.Core;

public static class ManifestReader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static VsCodeManifest Read(string extensionRoot)
    {
        var path = Path.Combine(extensionRoot, "package.json");
        if (!File.Exists(path))
            throw new FileNotFoundException("package.json missing", path);

        var json = File.ReadAllText(path);
        var manifest = JsonSerializer.Deserialize<VsCodeManifest>(json, Options)
            ?? throw new InvalidOperationException("Failed to parse package.json");

        if (string.IsNullOrWhiteSpace(manifest.Name))
            throw new InvalidOperationException("package.json missing required 'name'.");

        return manifest;
    }
}
