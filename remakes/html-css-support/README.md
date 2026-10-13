# HTML CSS Support (Zed remake)

Remake of [`ecmel.vscode-html-css`](https://marketplace.visualstudio.com/items?itemName=ecmel.vscode-html-css) for **Zed**.

Built per [Developing Extensions](https://github.com/zed-industries/zed/blob/main/docs/src/extensions/developing-extensions.md): `extension.toml` + Rust/`wasm32-wasip2` launcher + Node LSP. Sidecar JS is embedded and written into `extensions/work/<id>` on launch (Zed does not package arbitrary `server/` files).

## Features

- `class` / `id` / `className` completions from workspace CSS (and remote URLs)
- Go to definition for local selectors

**Smoke:** `node server/lsp-smoke.mjs` → `LSP smoke OK: btn, btn-primary, card`

See [INSTALL.md](INSTALL.md).

## Layout

```
extension.toml
Cargo.toml
src/lib.rs          # WASM: language_server_command
server/server.mjs   # Node LSP (embedded into WASM)
demo/               # manual test
LICENSE.md
```

## Settings (optional)

```json
{
  "lsp": {
    "html-css-support": {
      "settings": {
        "styleSheets": ["**/*.css", "src/**/*.scss"]
      }
    }
  }
}
```

## Provenance

| | |
|--|--|
| Original | ecmel.vscode-html-css 2.0.14 (MIT) — see `LICENSE.md` |
| Remake | Vsix2Zed — Node LSP + Zed WASM launcher |
| Not ported | Validate command diagnostics; languages beyond HTML |
