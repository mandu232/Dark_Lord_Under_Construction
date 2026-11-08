import { burst, fit, seg, ease, at, TAU, WHITE } from "../../core/index.js";
import { CRIMSON } from "./_shared.js";

export const meta = {
  name: "Blood pulse",
  desc: "Seen from above: a small hard pop, then two thin ripples chase each other out and vanish. Light and quick, nothing like the heavy blood wave of the nova.",
  tags: ["impact", "top-down"], size: [96, 96], origin: [48, 48], duration: 0.42, loop: false, fps: 30, bg: "dark",
  options: { palette: CRIMSON, seed: 8 },
};
const MAX = 40;

export function draw(p, t, o) {
  const C = fit(o.palette, 7);
  for (const [delay, life, w] of [[0, 0.3, 2], [0.09, 0.3, 1]]) {
    const k = seg(t, delay, delay + life);
    if (k <= 0 || k >= 1) continue;
    const rad = 3 + ease.outCubic(k) * (MAX - 3), f = 1 - k;
    p.ring(0, 0, rad, w, k < 0.35 ? C[6] : k < 0.7 ? C[5] : C[3]);
    p.glowRing(0, 0, rad, 4, C[4], 0.28 * f);
  }
  if (t < 0.1) {
    const q = 1 - t / 0.1;
    p.circle(0, 0, 2 + 4 * q, t < 0.04 ? WHITE : C[6]);
    p.glow(0, 0, 18, C[5], 0.6 * q);
  }
  burst(8, o.seed, (r) => {
    const a = r.range(0, TAU), life = r.range(0.14, 0.24), speed = r.range(90, 150);
    if (t > life) return;
    const k = t / life, d = 4 + speed * t * (1 - k * 0.4);
    p.px(Math.cos(a) * d, Math.sin(a) * d, at([C[6], C[5], C[3]], k));
  });
}
