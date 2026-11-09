import { hex } from "../../core/index.js";
import { LOOT, bob, shadow } from "./_loot.js";

export const meta = { ...LOOT, name: "Loot: gold coin", desc: "A small gold coin turning on the spot over its shadow, its lit face narrowing to the rim and back.", tags: ["pickup", "loot"], options: {} };
const OUT = hex("#6b3a08"), RIM = hex("#b56f10"), FACE = hex("#ffd34d"), LIT = hex("#fff0a8");
const WIDTHS = [4, 4, 3, 2, 1, 2, 3, 4];

export function draw(p, t) {
  const y = bob(t);
  shadow(p, y);
  const i = Math.floor((t / LOOT.duration) * WIDTHS.length) % WIDTHS.length, w = WIDTHS[i], H = 4;
  for (let row = -H; row <= H; row++) {
    const span = Math.round(w * Math.sqrt(1 - (row * row) / (H * H + 1)));
    for (let x = -span; x <= span; x++) {
      const edge = x === -span || x === span || Math.abs(row) === H;
      p.px(x, row + y, edge ? OUT : x === span - 1 ? RIM : FACE);
    }
  }
  if (w >= 3) { p.line(-1, -2 + y, -1, 1 + y, LIT); p.px(1, 2 + y, RIM); p.px(0, 2 + y, RIM); }
  else if (w === 2) p.line(0, -2 + y, 0, 2 + y, LIT);
  p.glow(0, y, 7, hex("#ffc53d"), 0.24);
  if (i === 0) { p.px(-2, -3 + y, hex("#ffffff")); p.light(-2, -3 + y, LIT, 0.6); }
}
