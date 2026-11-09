import { hex } from "../../core/index.js";
import { LOOT, bob, shadow, glint } from "./_loot.js";

export const meta = { ...LOOT, name: "Loot: blood drop", desc: "A fat bead of blood bobbing over its shadow; a highlight glints across it.", tags: ["pickup", "loot"], options: {} };
const ROWS = [
  "...a...",
  "...a...",
  "..aba..",
  "..abb..",
  ".abcbb.",
  "abccbbd",
  "abccbbd",
  "abbbbbd",
  ".abbbd.",
  "..ddd..",
];
const PAL = { a: hex("#4a0612"), b: hex("#c8102e"), c: hex("#ff4a63"), d: hex("#8a0a1e") };

export function draw(p, t) {
  const y = bob(t);
  shadow(p, y);
  p.sprite(ROWS, PAL, -3, -5 + y);
  p.glow(0, y, 7, hex("#ee1c40"), 0.28);
  if (glint(t, 0.3) > 0.4) { p.px(-1, y, hex("#ffffff")); p.px(-1, y + 1, hex("#ffc2ca")); }
}
