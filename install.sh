#!/usr/bin/env bash
set -euo pipefail
SRC="$(cd "$(dirname "$0")" && pwd)"
DEST="${HOME}/.cursor/plugins/local/vsix2zed"

need() { command -v "$1" >/dev/null || { echo "Missing $1 on PATH"; exit 1; }; }
need node; need npm; need dotnet

echo "Source: $SRC"
echo "Dest:   $DEST"
rm -rf "$DEST"
mkdir -p "$(dirname "$DEST")"
# portable copy
if command -v rsync >/dev/null; then
  rsync -a --exclude node_modules --exclude obj --exclude .git --exclude '*.pdb' "$SRC/" "$DEST/"
else
  cp -R "$SRC" "$DEST"
  rm -rf "$DEST/mcp/node_modules" "$DEST/dotnet"/*/obj 2>/dev/null || true
fi

(cd "$DEST/mcp" && npm install --omit=dev)
dotnet publish "$DEST/dotnet/Vsix2Zed.Cli/Vsix2Zed.Cli.csproj" -c Release -o "$DEST/bin" -v q

cat > "$DEST/mcp.json" <<EOF
{
  "mcpServers": {
    "vsix2zed": {
      "command": "node",
      "args": ["$DEST/mcp/server.mjs"],
      "env": { "VSIX2ZED_PLUGIN_ROOT": "$DEST" }
    },
    "rea": {
      "command": "npx",
      "args": ["-y", "rea-agents@6.4.0"]
    }
  }
}
EOF

echo
echo "Installed. Reload Cursor. Enable MCP vsix2zed + rea, skill vsix2zed."
echo "https://github.com/nazem0/vsix2zed-cursor-plugin"
