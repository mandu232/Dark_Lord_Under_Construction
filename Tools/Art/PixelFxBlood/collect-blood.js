import { fit, WHITE } from "../../core/index.js";
import { COLLECT, pop, burst } from "./_collect.js";

export const meta = {
  ...COLLECT,
  name: "Collect: blood",
  desc: "A blood marble bursts on pickup: a red flash and ring, fat droplets splash out, fall and leave small splats that sink away.",
  tags: ["pickup", "collect", "blood", "one-shot"],
  options: { palette: ["#1a0206", "#4a0612", "#8a0a1e", "#c8102e", "#ee1c40", "#ff6a7a", "#ffc2ca", "#ffffff"], seed: 3 },
};

export function draw(p, t, o) {
  const C = fit(o.palette, 8);
  pop(p, t, C);
  // droplets: a bright head and a darker tail while flying, a flat splat once they land
  burst(t, 9, o.seed, { up: 80, out: 50, life: [0.38, 0.52] }, (x, y, age, L, landed, r) => {
    const k = 1 - age / L;
    if (landed) {
      p.line(x - 1, y, x + 1, y, k > 0.4 ? C[3] : C[2], Math.min(1, k * 2.5));
      if (k > 0.6) p.px(x, y - 1, C[4]);
      return;
    }
    p.px(x, y, k > 0.6 ? C[5] : C[4]);
    p.dot(x, y + 1, C[3], 0.9);
    if (r.chance(0.4)) p.px(x - 1, y, C[3]);
    p.glow(x, y, 3, C[4], 0.3 * k);
  });
  // a soft red mist hanging where it burst
  if (t > 0.04 && t < 0.4) p.disc(0, 0, 3 + 10 * t, C[2], 0.35 * (1 - t / 0.4));
  if (t < 0.03) p.px(0, 0, WHITE);
}
