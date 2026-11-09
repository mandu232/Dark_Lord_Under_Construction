import { burst, fit, seg, ease, at, TAU, WHITE } from "../../core/index.js";
import { CRIMSON } from "./_shared.js";

export const meta = {
  name: "Blood execution",
  desc: "Seen from above: two pale cuts cross the target in quick succession, white-hot then bleeding out to crimson, and blood sprays along each cut.",
  tags: ["debuff", "slash", "top-down"], size: [72, 72], origin: [36, 36], duration: 0.6, loop: false, fps: 30, bg: "dark",
  options: { palette: CRIMSON, seed: 58 },
};
const CUTS = [[0.0, -2.36], [0.09, -0.79]], L = 27, DRAW = 0.05, HOLD = 0.3;

export function draw(p, t, o) {
  const C = fit(o.palette, 7);
  CUTS.forEach(([born, a], n) => {
    const age = t - born;
    if (age < 0 || age > HOLD) return;
    const dx = Math.cos(a), dy = Math.sin(a), k = ease.outQuad(seg(age, 0, DRAW)), thin = 1 - seg(age, DRAW, HOLD);
    const hx = -dx * L + dx * 2 * L * k, hy = -dy * L + dy * 2 * L * k;
    const tail = seg(age, DRAW, HOLD), sx = -dx * L + dx * 2 * L * tail, sy = -dy * L + dy * 2 * L * tail;
    p.beam(sx, sy, hx, hy, 0.8 + 2.8 * thin, at([WHITE, C[6], C[5], C[4], C[3]], seg(age, 0, HOLD)));
    p.glowLine(sx, sy, hx, hy, 6, C[5], 0.5 * thin);
    if (age < 0.05) p.glow(hx, hy, 12, WHITE, 0.6);
    burst(16, o.seed + n, (r) => {
      const u = r.range(-1, 1), side = r.next() < 0.5 ? -1 : 1, speed = r.range(30, 95), life = r.range(0.14, 0.34), start = 0.02 + ((u + 1) / 2) * DRAW, k2 = t - born - start;
      if (k2 < 0 || k2 > life) return;
      const d = speed * k2 * (1 - (k2 / life) * 0.45), x = dx * L * u - dy * side * d + dx * d * 0.25, y = dy * L * u + dx * side * d + dy * d * 0.25;
      p.circle(x, y, r.range(0.5, 1.5) * (1 - (k2 / life) * 0.6), at([C[6], C[5], C[4], C[3], C[2]], k2 / life));
    });
  });
  if (t >= 0.09 && t < 0.16) {
    const q = 1 - (t - 0.09) / 0.07;
    p.circle(0, 0, 2 + 7 * q, t < 0.125 ? WHITE : C[6]);
    p.glow(0, 0, 30, C[5], 0.85 * q);
  }
  const pool = seg(t, 0.14, 0.24) * (1 - seg(t, 0.42, 0.58));
  if (pool > 0) burst(9, o.seed + 9, (r) => {
    const a = r.range(0, TAU), d = r.range(3, 20);
    p.disc(Math.cos(a) * d, Math.sin(a) * d, r.range(1, 2.6), C[2], 0.75 * pool);
  });
}
