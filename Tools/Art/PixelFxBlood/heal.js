import { burst, fit, seg, ease, TAU } from "../../core/index.js";
import { CRIMSON } from "./_shared.js";

export const meta = {
  name: "Blood mend",
  desc: "Seen from above: droplets of blood spiral in to the centre and are drunk in; a soft ring pulses out as the wound closes.",
  tags: ["heal", "top-down"], size: [56, 56], origin: [28, 28], duration: 0.8, loop: false, fps: 30, bg: "dark",
  options: { palette: CRIMSON, seed: 4 },
};
const IN = 0.42;

export function draw(p, t, o) {
  const C = fit(o.palette, 7);
  burst(14, o.seed, (r) => {
    const a0 = r.range(0, TAU), d0 = r.range(16, 25), born = r.range(0, 0.18), life = r.range(0.2, IN - born), age = t - born;
    if (age < 0 || age > life) return;
    const q = ease.inQuad(age / life), a = a0 + q * 2.2, d = d0 * (1 - q), x = Math.cos(a) * d, y = Math.sin(a) * d, b = a - 0.35;
    p.circle(x, y, q < 0.6 ? 1.4 : 0.8, C[5]);
    p.px(x, y, C[6]);
    p.px(Math.cos(b) * (d + 1), Math.sin(b) * (d + 1), C[3]);
  });
  const g = seg(t, IN - 0.06, IN + 0.02) * (1 - seg(t, IN + 0.02, IN + 0.18));
  if (g > 0) {
    p.circle(0, 0, 1 + 1.5 * g, C[5]);
    p.glow(0, 0, 14, C[5], 0.5 * g);
  }
  const k = seg(t, IN, 0.76);
  if (k > 0 && k < 1) {
    const rad = 4 + ease.outCubic(k) * 13, f = 1 - k;
    p.ring(0, 0, rad, 1, f > 0.6 ? C[5] : C[3], f);
    p.glowRing(0, 0, rad, 4, C[4], 0.2 * f);
  }
}
