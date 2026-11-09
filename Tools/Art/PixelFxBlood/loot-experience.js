import { LOOT, bob, shadow } from "./_loot.js";
import { TAU, hex } from "../../core/index.js";

export const meta = { ...LOOT, name: "Loot: soul wisp", desc: "A teal soul flame flickering over its shadow, its tip licking side to side, a single mote circling it.", tags: ["pickup", "loot"], options: {} };
const OUT = hex("#0a4a48"), MID = hex("#25c8a8"), HOT = hex("#8ff5d8"), CORE = hex("#f0fff8");
const WIDE = [1, 2, 3, 3, 4, 4, 3];

export function draw(p, t) {
  const phase = (t / LOOT.duration) * TAU, y = bob(t);
  shadow(p, y);
  // Rows run top to bottom; only the upper rows sway, so the base of the flame holds still.
  WIDE.forEach((w, i) => {
    const row = -4 + i + y, sway = Math.round(Math.sin(phase * 2 + i * 0.7) * (1 - i / 6) * 1.5);
    for (let x = -w; x <= w; x++) {
      const dist = Math.abs(x), edge = dist === w;
      p.px(x + sway, row, edge ? OUT : dist >= w - 1 ? MID : i >= 3 && dist === 0 ? CORE : HOT);
    }
  });
  for (let x = -2; x <= 2; x++) p.px(x, 3 + y, OUT);
  p.glow(0, y, 7, MID, 0.3);
  const a = phase * 2, mx = Math.round(Math.cos(a) * 7), my = Math.round(Math.sin(a) * 2) - 1 + y;
  p.px(mx, my, Math.sin(a) > 0 ? CORE : MID);
}
