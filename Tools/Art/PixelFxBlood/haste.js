import { cycle, fit, TAU } from "../../core/index.js";
import { CRIMSON } from "./_shared.js";

export const meta = {
  name: "Blood rush",
  desc: "Seen from above, moving right: ribbons of blood stream away behind the caster, thinning as they go.",
  tags: ["status", "top-down"], size: [80, 48], origin: [56, 24], duration: 0.6, loop: true, fps: 30, bg: "dark",
  options: { palette: CRIMSON, seed: 6 },
};

export function draw(p, t, o) {
  const C = fit(o.palette, 7), ph = t / meta.duration;
  cycle(t, meta.duration, 16, o.seed, 0.34, (age, life, r) => {
    const q = age / life, y = r.range(-15, 15), L = r.range(14, 28) * (1 - q * 0.4), x = 6 - Math.abs(y) * 0.35 - q * 30, wob = Math.sin(q * 5 + y) * 1.2;
    p.line(x, y + wob, x - L, y + wob, (u) => (u < 0.2 ? (q < 0.4 ? C[6] : C[5]) : u < 0.55 ? C[4] : C[2]), (u) => 1 - u * 0.85);
  });
  p.glow(-6, 0, 20, C[4], 0.16 + 0.04 * Math.sin(ph * TAU));
}
