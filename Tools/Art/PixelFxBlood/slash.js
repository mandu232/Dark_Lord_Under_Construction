import { burst, fly, fit, seg, ease, at, WHITE } from "../../core/index.js";
import { CRIMSON } from "./_shared.js";

export const meta = {
  name: "Blood slash",
  desc: "Seen from above: a crescent of blood whips across the front, thins from the tail and flings droplets outward.",
  tags: ["melee", "top-down"], size: [104, 104], origin: [52, 52], duration: 0.42, loop: false, fps: 30, bg: "dark",
  options: { palette: CRIMSON, seed: 3 },
};
const R = 34, SWEEP = 0.1, A0 = -1.25, A1 = 1.25;

export function draw(p, t, o) {
  const C = fit(o.palette, 7), head = A0 + (A1 - A0) * ease.outCubic(seg(t, 0, SWEEP));
  const tail = A0 + (A1 - A0) * ease.inQuad(seg(t, 0.05, 0.34)), fade = 1 - seg(t, 0.2, 0.36);
  if (head > tail && fade > 0) {
    const W = 11 * fade + 1;
    const width = (u) => W * Math.sin(Math.PI * Math.min(1, u * 0.86 + 0.14));
    p.arc(0, 0, R, width, tail, head, (u, v) => (v > 0.82 ? (t < 0.14 ? WHITE : C[6]) : v > 0.5 ? C[5] : v > 0.22 ? C[4] : C[2]));
    const m = (tail + head) / 2;
    p.glow(Math.cos(m) * (R - 4), Math.sin(m) * (R - 4), 24, C[4], 0.32 * fade);
  }
  burst(16, o.seed, (r) => {
    const a = r.range(A0, A1), born = 0.02 + ((a - A0) / (A1 - A0)) * SWEEP, life = r.range(0.14, 0.28), age = t - born;
    if (age < 0 || age > life) return;
    const sp = r.range(70, 170), dx = Math.cos(a), dy = Math.sin(a);
    const [x, y] = fly(dx * R, dy * R, dx * sp, dy * sp, age, 0, 7), k = age / life;
    p.line(x, y, x - dx * 3 * (1 - k), y - dy * 3 * (1 - k), at([C[5], C[4], C[3], C[2]], k));
  });
}
