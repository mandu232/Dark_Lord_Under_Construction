import { burst, fit, seg, ease, at, TAU, WHITE } from "../../core/index.js";
import { CRIMSON } from "./_shared.js";

export const meta = {
  name: "Blood bite",
  desc: "Seen from above: two small fangs of blood snap together on the target and a few droplets spit out.",
  tags: ["melee", "top-down"], size: [40, 40], origin: [20, 20], duration: 0.26, loop: false, fps: 30, bg: "dark",
  options: { palette: CRIMSON, seed: 10 },
};

export function draw(p, t, o) {
  const C = fit(o.palette, 7), snap = ease.inQuad(seg(t, 0, 0.07)), fade = 1 - seg(t, 0.1, 0.2);
  if (fade > 0) {
    // Two curved jaws close on the target.
    const r = 14 - 8 * snap;
    for (const s of [-1, 1]) {
      const a = (s * Math.PI) / 2;
      p.arc(0, 0, r, (u) => (3.5 * fade + 0.5) * Math.sin(Math.PI * u), a - 1, a + 1, (u, v) => (v > 0.55 ? (t < 0.09 ? WHITE : C[6]) : C[4]));
    }
    p.glow(0, 0, 12, C[4], 0.4 * fade);
  }
  burst(7, o.seed, (r) => {
    const a = r.range(0, TAU), born = 0.06, life = r.range(0.1, 0.18), age = t - born, d = 2 + r.range(70, 120) * age * (1 - age / (life * 2));
    if (age < 0 || age > life) return;
    p.px(Math.cos(a) * d, Math.sin(a) * d, at([C[6], C[5], C[3]], age / life));
  });
}
