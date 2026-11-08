import { burst, fit, seg, at, WHITE } from "../../core/index.js";
import { CRIMSON } from "./_shared.js";

export const meta = {
  name: "Hit spark",
  desc: "Seen from above, struck from the left: a hard white star on contact and a fan of sparks thrown onward in the direction of the blow.",
  tags: ["impact", "top-down"], size: [48, 48], origin: [24, 24], duration: 0.2, loop: false, fps: 30, bg: "dark",
  options: { palette: CRIMSON, seed: 15 },
};

export function draw(p, t, o) {
  const C = fit(o.palette, 7), f = 1 - seg(t, 0, 0.09);
  if (f > 0) {
    const L = 10 * f + 2, W = 2.5 * f + 0.5;
    p.poly([[-L, 0], [0, -W], [L, 0], [0, W]], t < 0.04 ? WHITE : C[6]);
    p.poly([[0, -L * 0.6], [W, 0], [0, L * 0.6], [-W, 0]], t < 0.04 ? WHITE : C[5]);
    p.glow(0, 0, 11, C[5], 0.55 * f);
  }
  burst(8, o.seed, (r) => {
    const a = r.range(-0.9, 0.9), life = r.range(0.08, 0.17), speed = r.range(90, 170), d = 3 + speed * t * (1 - t / (life * 2));
    if (t > life) return;
    const c = Math.cos(a), s = Math.sin(a);
    p.line(c * d, s * d, c * (d - 2), s * (d - 2), at([C[6], C[5], C[3]], t / life));
  });
}
