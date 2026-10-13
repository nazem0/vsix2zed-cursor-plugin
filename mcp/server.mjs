#!/usr/bin/env node
/**
 * Vsix2Zed MCP — self-contained (plugin root has bin/vsix2zed + remakes/).
 * AI = Cursor host agent. Optional REA MCP is registered separately in mcp.json.
 */
import { spawn } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
} from "@modelcontextprotocol/sdk/types.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

function pluginRoot() {
  if (process.env.VSIX2ZED_PLUGIN_ROOT)
    return path.resolve(process.env.VSIX2ZED_PLUGIN_ROOT);
  if (process.env.CURSOR_PLUGIN_ROOT)
    return path.resolve(process.env.CURSOR_PLUGIN_ROOT);
  return path.resolve(__dirname, "..");
}

function cliPath(root) {
  const win = path.join(root, "bin", "vsix2zed.exe");
  const unix = path.join(root, "bin", "vsix2zed");
  if (process.platform === "win32" && fs.existsSync(win)) return win;
  if (fs.existsSync(unix)) return unix;
  if (fs.existsSync(win)) return win;
  return null;
}

function run(cmd, args, { cwd, env } = {}) {
  return new Promise((resolve, reject) => {
    const child = spawn(cmd, args, {
      cwd,
      windowsHide: true,
      env: { ...process.env, ...env },
    });
    let stdout = "";
    let stderr = "";
    child.stdout.on("data", (d) => (stdout += d.toString()));
    child.stderr.on("data", (d) => (stderr += d.toString()));
    child.on("error", reject);
    child.on("close", (code) => resolve({ code: code ?? 1, stdout, stderr }));
  });
}

async function invokeCli(root, cliArgs) {
  const exe = cliPath(root);
  const remake = path.join(root, "remakes", "html-css-support");
  const env = {
    VSICODE_HTML_CSS_REMAKE: fs.existsSync(remake) ? remake : "",
  };
  if (exe) {
    return run(exe, cliArgs, { cwd: root, env });
  }
  const csproj = path.join(root, "dotnet", "Vsix2Zed.Cli", "Vsix2Zed.Cli.csproj");
  if (!fs.existsSync(csproj)) {
    throw new Error(
      `vsix2zed binary missing under ${path.join(root, "bin")} and no source at ${csproj}. Run install.ps1 / install.sh.`,
    );
  }
  await run("dotnet", ["publish", csproj, "-c", "Release", "-o", path.join(root, "bin"), "-v", "q"], {
    cwd: root,
  });
  const again = cliPath(root);
  if (!again) throw new Error("dotnet publish succeeded but vsix2zed binary not found in bin/");
  return run(again, cliArgs, { cwd: root, env });
}

const ZED_RULES = `Zed extension packaging (official docs: https://zed.dev/docs/extensions/developing-extensions):
- extension.toml: id, name, version, schema_version=1, authors, description, repository
- LSP / context / debug servers need Rust → wasm32-wasip2 + zed_extension_api
- Packaged files allowlist: extension.toml, extension.wasm, grammars, themes, icon themes, languages, debug schemas, snippets
- No arbitrary server/*.mjs in the archive — write into extensions/work/<id> at runtime (include_str! or npm download)
- Dev install: Extensions → Install Dev Extension; WASM IO cwd is work/<id>
`;

const REA_HINT = `REA MCP is registered as "rea" in this plugin (npx rea-agents@6.4.0).
Use when VSIX host JS / native binaries are opaque:
- open_binary, analyze_javascript_application, inspect_artifact, etc.
Extract intent only — remake for Zed; do not try to run vscode.* APIs.
REA needs Node + network for first npx pull; optional analysis engines (Ghidra/Hopper) only if already installed.
`;

const server = new Server(
  { name: "vsix2zed", version: "0.1.0" },
  { capabilities: { tools: {} } },
);

server.setRequestHandler(ListToolsRequestSchema, async () => ({
  tools: [
    {
      name: "analyze_vsix",
      description:
        "Classify a VS Code .vsix for Zed porting. Returns strategy + next steps. No files written.",
      inputSchema: {
        type: "object",
        properties: { vsix_path: { type: "string" } },
        required: ["vsix_path"],
      },
    },
    {
      name: "convert_vsix",
      description:
        "Deterministic convert: themes/snippets, LSP scaffold, known remakes, GAP_REPORT.md.",
      inputSchema: {
        type: "object",
        properties: {
          vsix_path: { type: "string" },
          output_dir: { type: "string" },
          force: { type: "boolean", default: false },
        },
        required: ["vsix_path", "output_dir"],
      },
    },
    {
      name: "read_gap_report",
      description: "Read GAP_REPORT.md from a convert output directory.",
      inputSchema: {
        type: "object",
        properties: { output_dir: { type: "string" } },
        required: ["output_dir"],
      },
    },
    {
      name: "list_known_remakes",
      description: "List bundled remake templates.",
      inputSchema: { type: "object", properties: {} },
    },
    {
      name: "zed_extension_rules",
      description: "Zed packaging rules for remakes.",
      inputSchema: { type: "object", properties: {} },
    },
    {
      name: "rea_guidance",
      description: "When/how to use the bundled REA MCP.",
      inputSchema: { type: "object", properties: {} },
    },
    {
      name: "smoke_node_lsp",
      description: "Run node on a smoke script path.",
      inputSchema: {
        type: "object",
        properties: { script_path: { type: "string" } },
        required: ["script_path"],
      },
    },
    {
      name: "plugin_info",
      description: "Paths and readiness of this Vsix2Zed Cursor plugin install.",
      inputSchema: { type: "object", properties: {} },
    },
  ],
}));

server.setRequestHandler(CallToolRequestSchema, async (request) => {
  const root = pluginRoot();
  const name = request.params.name;
  const args = request.params.arguments ?? {};

  try {
    if (name === "zed_extension_rules") return text(ZED_RULES);
    if (name === "rea_guidance") return text(REA_HINT);
    if (name === "plugin_info") {
      const remake = path.join(root, "remakes", "html-css-support");
      return text(
        JSON.stringify(
          {
            plugin_root: root,
            cli: cliPath(root),
            remake_html_css: fs.existsSync(remake) ? remake : null,
            repo: "https://github.com/nazem0/vsix2zed-cursor-plugin",
            requires: [".NET 8 runtime or SDK", "Node.js 18+", "optional: Rust/wasm32-wasip2 for remake builds"],
          },
          null,
          2,
        ),
      );
    }
    if (name === "list_known_remakes") {
      const remake = path.join(root, "remakes", "html-css-support");
      return text(
        JSON.stringify(
          {
            remakes: [
              {
                id: "html-css-support",
                match: "ecmel.vscode-html-css / styleSheets+enabledLanguages",
                bundled: fs.existsSync(remake),
                template: remake,
                public_reference: "https://github.com/nazem0/zed-html-css-support",
              },
            ],
          },
          null,
          2,
        ),
      );
    }
    if (name === "read_gap_report") {
      const p = path.join(path.resolve(args.output_dir), "GAP_REPORT.md");
      if (!fs.existsSync(p)) throw new Error(`Missing ${p}`);
      return text(fs.readFileSync(p, "utf8"));
    }
    if (name === "smoke_node_lsp") {
      const script = path.resolve(args.script_path);
      if (!fs.existsSync(script)) throw new Error(`Missing ${script}`);
      const r = await run("node", [script]);
      return text(`exit ${r.code}\n${r.stdout}${r.stderr}`);
    }
    if (name === "analyze_vsix") {
      const r = await invokeCli(root, ["analyze", path.resolve(args.vsix_path)]);
      if (r.code !== 0) throw new Error(r.stderr || r.stdout || `exit ${r.code}`);
      return text(r.stdout.trim());
    }
    if (name === "convert_vsix") {
      const cliArgs = [
        "convert",
        path.resolve(args.vsix_path),
        "-o",
        path.resolve(args.output_dir),
      ];
      if (args.force) cliArgs.push("--force");
      const r = await invokeCli(root, cliArgs);
      if (r.code !== 0 && r.code !== 2) throw new Error(r.stderr || r.stdout || `exit ${r.code}`);
      return text(r.stdout.trim() || `exit ${r.code}`);
    }
    throw new Error(`Unknown tool: ${name}`);
  } catch (e) {
    return {
      content: [{ type: "text", text: String(e?.message || e) }],
      isError: true,
    };
  }
});

function text(t) {
  return { content: [{ type: "text", text: t }] };
}

await server.connect(new StdioServerTransport());
