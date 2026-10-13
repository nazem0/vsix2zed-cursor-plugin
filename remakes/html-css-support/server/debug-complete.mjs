import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const CAN_COMPLETE =
  /(id|class|className|[.#])\s*[=:]?\s*(["'])(?:.(?!\2))*$/is;
const SELECTOR_RE =
  /([.#])(-?[_a-zA-Z]+[\\!+_a-zA-Z0-9-]*)(?=[#.,()\s\[\]\^:*"'>=_a-zA-Z0-9-]*{[^}]*})/g;

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const html = fs.readFileSync(path.join(__dirname, "..", "demo", "index.html"), "utf8");
const css = fs.readFileSync(path.join(__dirname, "..", "demo", "styles.css"), "utf8");

const line = html.split(/\n/).findIndex((l) => l.includes('<button class=""'));
const character = html.split(/\n/)[line].indexOf('class=""') + 'class="'.length;
const lines = html.split(/\n/);
const slice = lines.slice(0, line + 1);
slice[slice.length - 1] = slice[slice.length - 1].slice(0, character);
const before = slice.join("\n");
console.log("BEFORE_TAIL:", JSON.stringify(before.slice(-40)));
console.log("MATCH:", CAN_COMPLETE.exec(before));

const sels = [];
let m;
while ((m = SELECTOR_RE.exec(css))) sels.push(m[1] + m[2]);
console.log("SELS:", sels);
