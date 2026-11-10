import { fit, sparkle, WHITE } from "../../core/index.js";
import { COLLECT, pop, burst } from "./_collect.js";

export const meta = {
  ...COLLECT,
  name: "Collect: bloodstone",
  desc: "A bloodstone marble cracks on pickup: a magenta flash and ring, faceted shards tumble out flashing as they turn, then wink out with a glint.",
  tags: ["pickup", "collect", "crystal", "one-shot"],
  options: { palette: ["#1a0416", "#3a0a3a", "#6e1258", "#a51c86", "#e0288f", "#ff8ad0", "#ffe0f3", "#ffffff"], seed: 5 },
};

export function draw(p, t, o) {
  const C = fit(o.palette, 8);
  pop(p, t, C);
  // shards: a little diamond turning end over end, its lit face flashing every other step
  burst(t, 7, o.seed, { up: 70, out: 60, drag: 4, g: 200, life: [0.35, 0.5] }, (x, y, age, L, landed, r) => {
    const k = 1 - age / L, step = Math.floor(age * (landed ? 0 : 24) + r.range(0, 4)) % 4, lit = step === 0;
    if (k < 0.25) { if (k > 0.1) sparkle(p, x, y, 1 + (k > 0.18 ? 1 : 0), C[6], C[5], 0.25); return; }
    if (step % 2 === 0) { p.px(x, y - 1, C[5]); p.px(x, y, lit ? WHITE : C[4]); p.px(x, y + 1, C[3]); }
    else { p.px(x - 1, y, C[5]); p.px(x, y, C[4]); p.px(x + 1, y, C[3]); }
    p.glow(x, y, 3, C[4], (lit ? 0.5 : 0.25) * k);
  });
  // the cross of a hard gem glint lingers a moment longer
  if (t > 0.08 && t < 0.24) sparkle(p, 0, 0, 4 * (1 - (t - 0.08) / 0.16) + 1, WHITE, C[5], 0.6);
}
