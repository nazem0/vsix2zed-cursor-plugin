# Install HTML CSS Support (Zed remake)

Follows [Developing Extensions](https://github.com/zed-industries/zed/blob/main/docs/src/extensions/developing-extensions.md).

## Prerequisites

- [Rust via rustup](https://www.rust-lang.org/tools/install) (Zed installs `wasm32-wasip2` when needed)
- Node.js on PATH (LSP runs under Zed’s Node)
- On Windows: MinGW (WinLibs) or MSVC Build Tools so the WASM launcher can compile

## Install as a dev extension

1. Open Zed
2. Extensions page → **Install Dev Extension**, or Command Palette → `zed: install dev extension`
3. Select this folder:
   `D:\Development\Private\VSCodeExtensionsTransformer\out\ecmel-vscode-html-css`
4. Wait for compile (first time may take a minute)
5. Open `demo` (`demo\index.html` + `demo\styles.css`)
6. Cursor inside `<button class="` → completions (`ctrl-space`)

Troubleshoot: `zed: open log`, or relaunch with `zed --foreground`.

## Verify without Zed

```powershell
node .\server\lsp-smoke.mjs
```

Expected: `LSP smoke OK: btn, btn-primary, card`

## Optional settings

```json
{
  "lsp": {
    "html-css-support": {
      "settings": {
        "styleSheets": ["**/*.css", "**/*.scss"]
      }
    }
  }
}
```
