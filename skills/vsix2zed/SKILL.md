---
name: vsix2zed
description: >-
  Transform a VS Code .vsix into a Zed extension using the bundled Vsix2Zed MCP.
  Use when the user mentions VSIX, VS Code extension → Zed, port/convert to Zed,
  or Install Dev Extension for a converted package.
---

# Vsix2Zed (fresh Cursor)

You are the AI (Cursor host). Tools are deterministic. **No user AI API key.**

## Prerequisites (tell the user if missing)

- Node.js 18+
- .NET 8 runtime or SDK (`dotnet --info`)
- For WASM remakes: Rust + `wasm32-wasip2`
- This plugin install (see README) — MCP `vsix2zed` + optional MCP `rea`

## Workflow

1. `plugin_info` — confirm CLI + remakes present
2. `analyze_vsix` on the `.vsix`
3. Follow `strategy` / `nextSteps`
4. `convert_vsix` (force only if user allows overwrite)
5. `read_gap_report`
6. `zed_extension_rules` before any remake code
7. If host JS/native is opaque → `rea_guidance`, then **REA MCP** tools (`open_binary`, `analyze_javascript_application`, …)
8. Remake gaps into Zed-native shape; smoke with `smoke_node_lsp` when applicable
9. User installs output via Zed → **Install Dev Extension**

## Strategy table

| strategy | Action |
|----------|--------|
| `declarative-convert` | Convert enough (themes/snippets) |
| `lsp-scaffold` | Fill `language_server_command` (work-dir download/embed) |
| `remake:html-css-support` | Bundled remake applied |
| `ai-remake-candidate` | You remake host logic for Zed |

## Hard rules

- Zed does not run `vscode.*` extension host JS
- No arbitrary `server/` files in the Zed package archive — materialize under `extensions/work/<id>`
- Docs: https://zed.dev/docs/extensions/developing-extensions
- Bundled remake reference: https://github.com/nazem0/zed-html-css-support
