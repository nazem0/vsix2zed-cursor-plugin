# Vsix2Zed Cursor Plugin

Transform VS Code `.vsix` extensions into [Zed](https://zed.dev) extensions from Cursor.

- **AI** = Cursor host agent (no OpenAI/Anthropic key in this plugin)
- **Convert** = bundled `bin/vsix2zed` (.NET 8)
- **Reverse engineering** = optional [REA](https://www.npmjs.com/package/rea-agents) MCP (`npx rea-agents@6.4.0`), registered as `rea`
- **Repo:** https://github.com/nazem0/vsix2zed-cursor-plugin

Related remake example: https://github.com/nazem0/zed-html-css-support

## Requirements (fresh machine)

| Tool | Why |
|------|-----|
| [Cursor](https://cursor.com) | Host agent + plugin loader |
| [Node.js 18+](https://nodejs.org) | MCP servers (`vsix2zed` + first-time `npx` for REA) |
| [.NET 8 runtime or SDK](https://dotnet.microsoft.com/download) | Run `bin/vsix2zed` |
| Rust + `wasm32-wasip2` (optional) | Only when building WASM remakes for Zed |

## Install into Cursor

### A) Local plugin (recommended while developing)

```powershell
# Windows
git clone https://github.com/nazem0/vsix2zed-cursor-plugin.git
cd vsix2zed-cursor-plugin
.\install.ps1
```

```bash
# macOS / Linux
git clone https://github.com/nazem0/vsix2zed-cursor-plugin.git
cd vsix2zed-cursor-plugin
chmod +x install.sh && ./install.sh
```

Then in Cursor: **Developer: Reload Window** → Customize → enable MCP **vsix2zed** and **rea**, skill **vsix2zed**.

`install.*` copies this folder to `~/.cursor/plugins/local/vsix2zed`, runs `npm install` in `mcp/`, and publishes the CLI into `bin/` if needed.

### B) From this checkout without copying

Point a Cursor MCP entry at `mcp/server.mjs` with:

```json
{
  "mcpServers": {
    "vsix2zed": {
      "command": "node",
      "args": ["FULL/PATH/vsix2zed-cursor-plugin/mcp/server.mjs"],
      "env": { "VSIX2ZED_PLUGIN_ROOT": "FULL/PATH/vsix2zed-cursor-plugin" }
    },
    "rea": {
      "command": "npx",
      "args": ["-y", "rea-agents@6.4.0"]
    }
  }
}
```

## Usage

In chat: *“Port this VSIX to Zed: C:\\path\\to\\ext.vsix”*

Agent flow: `analyze_vsix` → `convert_vsix` → remake gaps → Install Dev Extension in Zed.

### CLI (no Cursor)

```bash
./bin/vsix2zed analyze path/to.vsix
./bin/vsix2zed convert path/to.vsix -o ./out/my-ext --force
```

## Layout

```
.cursor-plugin/plugin.json
mcp.json                 # vsix2zed + rea
mcp/server.mjs
bin/vsix2zed[.exe]       # published converter
dotnet/                  # source (rebuild if needed)
remakes/html-css-support # bundled known remake
skills/vsix2zed/
rules/
```

## License

MIT — see [LICENSE](LICENSE).
