import { hex } from "../../core/index.js";
import { LOOT, bob, shadow, glint } from "./_loot.js";

export const meta = { ...LOOT, name: "Loot: dark crystal", desc: "A faceted magenta shard hovering over its shadow; a glint flashes on its lit face.", tags: ["pickup", "loot"], options: {} };
const ROWS = [
  "...aa..",
  "..acba.",
  "..acbba",
  ".accbba",
  ".accbda",
  "accbbda",
  ".acbdda",
  ".abbda.",
  "..adda.",
  "...aa..",
];
const PAL = { a: hex("#3a0a3a"), b: hex("#e0288f"), c: hex("#ff8ad0"), d: hex("#8e1466") };

export function draw(p, t) {
  const y = bob(t);
  shadow(p, y);
  p.sprite(ROWS, PAL, -3, -5 + y);
  p.glow(0, y, 7, hex("#ff3aa5"), 0.26);
  const g = glint(t, 0.62);
  if (g > 0.3) { p.px(0, -3 + y, hex("#ffffff")); p.light(0, -3 + y, hex("#ff8ad0"), 0.7); }
  if (g > 0.7) { p.px(-1, -3 + y, hex("#ffffff")); p.px(1, -3 + y, hex("#ffffff")); p.px(0, -4 + y, hex("#ffffff")); p.px(0, -2 + y, hex("#ffffff")); }
}
