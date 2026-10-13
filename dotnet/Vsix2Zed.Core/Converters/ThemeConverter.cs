using System.Text.Json;
using System.Text.Json.Nodes;
using Vsix2Zed.Core.Models;

namespace Vsix2Zed.Core.Converters;

public static class ThemeConverter
{
    // ponytail: common-token map only; full VS Code→Zed parity needs a maintained table
    private static readonly Dictionary<string, string> ColorMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["editor.background"] = "background",
        ["editor.foreground"] = "text",
        ["editorCursor.foreground"] = "text.accent",
        ["editorLineNumber.foreground"] = "editor.line_number",
        ["editorLineNumber.activeForeground"] = "editor.active_line_number",
        ["editor.lineHighlightBackground"] = "editor.active_line_background",
        ["editorWidget.background"] = "elevated_surface.background",
        ["editorSuggestWidget.background"] = "elevated_surface.background",
        ["sideBar.background"] = "panel.background",
        ["activityBar.background"] = "panel.background",
        ["statusBar.background"] = "status_bar.background",
        ["titleBar.activeBackground"] = "title_bar.background",
        ["tab.activeBackground"] = "tab.active_background",
        ["tab.inactiveBackground"] = "tab.inactive_background",
        ["terminal.background"] = "terminal.background",
        ["terminal.foreground"] = "terminal.foreground",
        ["focusBorder"] = "border.focused",
        ["panel.border"] = "border",
        ["input.background"] = "editor.background",
        ["button.background"] = "element.selected",
        ["list.activeSelectionBackground"] = "element.selected",
        ["list.hoverBackground"] = "element.hover",
    };

    public static int Convert(
        string extensionRoot,
        string outputDir,
        VsCodeManifest manifest,
        ConversionResult result)
    {
        if (manifest.Contributes is not { ValueKind: JsonValueKind.Object })
            return 0;
        if (!manifest.Contributes.Value.TryGetProperty("themes", out var themes)
            || themes.ValueKind != JsonValueKind.Array)
            return 0;

        var themeEntries = new List<string>();
        var familyThemes = new JsonArray();
        var unmapped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var count = 0;

        foreach (var entry in themes.EnumerateArray())
        {
            var pathRel = entry.TryGetProperty("path", out var p) ? p.GetString() : null;
            var label = entry.TryGetProperty("label", out var l) ? l.GetString() : null;
            var uiTheme = entry.TryGetProperty("uiTheme", out var u) ? u.GetString() : "vs-dark";
            if (string.IsNullOrWhiteSpace(pathRel)) continue;

            var abs = Path.GetFullPath(Path.Combine(extensionRoot, pathRel.Replace('/', Path.DirectorySeparatorChar)));
            if (!File.Exists(abs))
            {
                result.Gaps.Add(new GapEntry("themes", GapKind.Partial, "Theme file missing in VSIX", pathRel));
                continue;
            }

            var vsTheme = JsonNode.Parse(File.ReadAllText(abs)) as JsonObject;
            if (vsTheme is null) continue;

            var appearance = uiTheme is "vs" or "hc-light" ? "light" : "dark";
            var name = label
                ?? vsTheme["name"]?.GetValue<string>()
                ?? Path.GetFileNameWithoutExtension(pathRel);

            var style = new JsonObject();
            if (vsTheme["colors"] is JsonObject colors)
            {
                foreach (var (key, value) in colors)
                {
                    if (value is null) continue;
                    var hex = value.GetValue<string>();
                    if (ColorMap.TryGetValue(key, out var zedKey))
                        SetNested(style, zedKey, hex);
                    else
                        unmapped.Add(key);
                }
            }

            // Minimal required-ish defaults so Zed can load something
            style["background"] ??= JsonValue.Create(appearance == "dark" ? "#1e1e1e" : "#ffffff");
            style["text"] ??= JsonValue.Create(appearance == "dark" ? "#d4d4d4" : "#000000");

            familyThemes.Add(new JsonObject
            {
                ["name"] = name,
                ["appearance"] = appearance,
                ["style"] = style,
            });
            count++;
        }

        if (count == 0) return 0;

        var themesDir = Path.Combine(outputDir, "themes");
        Directory.CreateDirectory(themesDir);
        var outFile = Path.Combine(themesDir, "converted.json");
        var family = new JsonObject
        {
            ["$schema"] = "https://zed.dev/schema/themes/v0.2.0.json",
            ["name"] = manifest.DisplayName ?? manifest.Name ?? "Converted",
            ["author"] = manifest.AuthorName,
            ["themes"] = familyThemes,
        };
        File.WriteAllText(outFile, family.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        themeEntries.Add("./themes/converted.json");
        result.Converted.Add($"themes ({count}) → themes/converted.json");

        if (unmapped.Count > 0)
        {
            result.Gaps.Add(new GapEntry(
                "themes",
                GapKind.Partial,
                $"{unmapped.Count} VS Code color keys had no Zed mapping",
                string.Join(", ", unmapped.OrderBy(x => x).Take(40)) + (unmapped.Count > 40 ? ", …" : "")));
        }

        result.Converted.Add($"theme_paths:{string.Join(';', themeEntries)}");
        return count;
    }

    private static void SetNested(JsonObject root, string dottedKey, string value)
    {
        // Zed uses flat keys with dots in JSON (e.g. "editor.line_number"), not nested objects.
        root[dottedKey] = value;
    }
}
