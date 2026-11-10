import { fit, ease, sparkle, WHITE } from "../../core/index.js";
import { mote } from "../arcane/_lib.js";
import { COLLECT, pop } from "./_collect.js";

export const meta = {
  ...COLLECT,
  name: "Collect: soul",
  desc: "A soul marble opens on pickup: a teal flash and ring, then wisps spiral up out of it and fade like breath, a twinkle at the top.",
  tags: ["pickup", "collect", "experience", "soul", "one-shot"],
  options: { palette: ["#021412", "#062a28", "#0a4a48", "#138a7a", "#25c8a8", "#8ff5d8", "#f0fff8", "#ffffff"], seed: 6 },
};

const N = 5;
// each wisp's place: a widening spiral rising off the spot
const at = (i, a) => {
  const g = (i / N) * Math.PI * 2 + a * 9, r = 2 + 6 * ease.outQuad(Math.min(1, a / 0.45));
  return [Math.cos(g) * r, Math.sin(g) * r * 0.5 - 13 * ease.outQuad(Math.min(1, a / 0.5))];
};

export function draw(p, t, o) {
  const C = fit(o.palette, 8);
  pop(p, t, C);
  for (let i = 0; i < N; i++) {
    const a = t - 0.03 - i * 0.015, L = 0.48;
    if (a < 0 || a > L) continue;
    const k = 1 - a / L, [x, y] = at(i, a), [x2, y2] = at(i, a + 0.02);
    mote(p, x, y, (x2 - x) * 50, (y2 - y) * 50, i % 2 ? C[6] : C[5], 1, Math.min(1, k * 2.2), 0.45);
    if (k > 0.3) p.px(x, y, i % 2 ? WHITE : C[6]);
    // a faint trail of the spiral behind it
    for (let s = 1; s <= 3; s++) {
      const [tx, ty] = at(i, Math.max(0, a - s * 0.035));
      p.dot(tx, ty, C[4], 0.6 * k * (1 - s / 4));
    }
  }
  if (t > 0.3 && t < 0.52) { const k = Math.sin(((t - 0.3) / 0.22) * Math.PI); sparkle(p, 0, -14, 1 + 2 * k, WHITE, C[5], 0.5 * k); }
}
