import { burst, fit, seg, ease, at, TAU, WHITE } from "../../core/index.js";
import { CRIMSON } from "./_shared.js";

export const meta = {
  name: "Death burst",
  desc: "Seen from above: a body gives out in a flash, blood flies outward in thick drops and leaves a spatter that dries away.",
  tags: ["impact", "top-down"], size: [80, 80], origin: [40, 40], duration: 0.6, loop: false, fps: 30, bg: "dark",
  options: { palette: CRIMSON, seed: 16 },
};

export function draw(p, t, o) {
  const C = fit(o.palette, 7);
  burst(14, o.seed, (r) => {
    const a = r.range(0, TAU), d = r.range(6, 30), s = r.range(1, 3), born = 0.03 + d / 300, out = r.range(0.42, 0.58);
    if (t < born || t > out) return;
    const f = 1 - seg(t, out - 0.2, out);
    p.disc(Math.cos(a) * d, Math.sin(a) * d, s, C[2], 0.8 * f);
  });
  if (t < 0.08) {
    const q = 1 - t / 0.08;
    p.circle(0, 0, 4 + 8 * q, t < 0.04 ? WHITE : C[6]);
    p.glow(0, 0, 26, C[5], 0.8 * q);
  }
  const k = seg(t, 0, 0.2);
  if (k > 0 && k < 1) p.ring(0, 0, 4 + ease.outCubic(k) * 26, 2 * (1 - k) + 1, k < 0.4 ? C[6] : C[4], 1 - k);
  burst(18, o.seed + 1, (r) => {
    const a = r.range(0, TAU), life = r.range(0.14, 0.32), speed = r.range(90, 210), d = 4 + speed * t * (1 - t / (life * 2)), s = r.range(0.6, 1.8);
    if (t > life) return;
    const k2 = t / life;
    p.circle(Math.cos(a) * d, Math.sin(a) * d, s * (1 - k2 * 0.6), at([C[6], C[5], C[4], C[3]], k2));
  });
}
