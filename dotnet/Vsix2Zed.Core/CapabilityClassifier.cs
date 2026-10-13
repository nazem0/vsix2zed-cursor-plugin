using System.Text.Json;
using Vsix2Zed.Core.Models;

namespace Vsix2Zed.Core;

public enum CapabilityAction
{
    Convert,
    Scaffold,
    Gap
}

public sealed record ClassifiedContribute(string Key, CapabilityAction Action, string Note);

public static class CapabilityClassifier
{
    private static readonly Dictionary<string, (CapabilityAction Action, string Note)> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["themes"] = (CapabilityAction.Convert, "Color themes → Zed theme JSON"),
        ["iconThemes"] = (CapabilityAction.Gap, "Icon themes: Phase 2"),
        ["snippets"] = (CapabilityAction.Convert, "Snippets → Zed snippets JSON"),
        ["languages"] = (CapabilityAction.Scaffold, "Language contributes → LSP/language scaffold hints"),
        ["grammars"] = (CapabilityAction.Gap, "TextMate grammars need Tree-sitter ports"),
        ["configuration"] = (CapabilityAction.Gap, "No Zed equivalent for VS Code configuration UI"),
        ["commands"] = (CapabilityAction.Gap, "No vscode.commands surface in Zed extensions"),
        ["keybindings"] = (CapabilityAction.Gap, "Zed keymaps are separate; not imported from VSIX"),
        ["menus"] = (CapabilityAction.Gap, "No VS Code menu contribution model"),
        ["views"] = (CapabilityAction.Gap, "No tree-view / custom view API"),
        ["viewsContainers"] = (CapabilityAction.Gap, "No view containers API"),
        ["webviews"] = (CapabilityAction.Gap, "No webview API"),
        ["customEditors"] = (CapabilityAction.Gap, "No custom editor API"),
        ["notebooks"] = (CapabilityAction.Gap, "No notebook API"),
        ["debuggers"] = (CapabilityAction.Scaffold, "Debugger → Phase 2 DAP stub (scaffolded as gap detail)"),
        ["breakpoints"] = (CapabilityAction.Gap, "Debugger UI contributions unsupported"),
        ["problemMatchers"] = (CapabilityAction.Gap, "Task problem matchers unsupported"),
        ["taskDefinitions"] = (CapabilityAction.Gap, "Use Zed tasks.json manually"),
        ["colors"] = (CapabilityAction.Gap, "Custom workbench colors unsupported"),
        ["authentication"] = (CapabilityAction.Gap, "No auth provider API"),
        ["walkthroughs"] = (CapabilityAction.Gap, "No walkthrough API"),
        ["chatParticipants"] = (CapabilityAction.Gap, "No VS Code chat participant API"),
        ["languageModelChatProviders"] = (CapabilityAction.Gap, "No VS Code LM provider API"),
    };

    public static IReadOnlyList<ClassifiedContribute> Classify(VsCodeManifest manifest)
    {
        var list = new List<ClassifiedContribute>();
        if (manifest.Contributes is not { ValueKind: JsonValueKind.Object } contributes)
            return list;

        foreach (var prop in contributes.EnumerateObject())
        {
            if (Map.TryGetValue(prop.Name, out var mapped))
                list.Add(new ClassifiedContribute(prop.Name, mapped.Action, mapped.Note));
            else
                list.Add(new ClassifiedContribute(prop.Name, CapabilityAction.Gap, "Unrecognized contribute; not mapped to Zed"));
        }

        // Runtime JS entry → gap unless only declarative assets
        if (!string.IsNullOrWhiteSpace(manifest.Main) || !string.IsNullOrWhiteSpace(manifest.Browser))
        {
            list.Add(new ClassifiedContribute(
                "main/browser",
                CapabilityAction.Gap,
                "Extension JS/TS host code cannot run on Zed; only declarative/scaffold outputs apply"));
        }

        if (HasLanguageClientHint(manifest))
        {
            list.Add(new ClassifiedContribute(
                "language-server",
                CapabilityAction.Scaffold,
                "vscode-languageclient / LSP dependency detected → Rust language_server_command scaffold"));
        }

        return list;
    }

    public static bool HasLanguageClientHint(VsCodeManifest manifest)
    {
        if (manifest.Dependencies is not { ValueKind: JsonValueKind.Object } deps)
            return false;
        foreach (var p in deps.EnumerateObject())
        {
            if (p.Name.Contains("languageclient", StringComparison.OrdinalIgnoreCase)
                || p.Name.Contains("vscode-languageserver", StringComparison.OrdinalIgnoreCase)
                || p.Name.Equals("vscode-languageclient", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
