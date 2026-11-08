import { burst, fly, fit, seg, ease, at, TAU, WHITE } from "../../core/index.js";
import { CRIMSON } from "./_shared.js";

export const meta = {
  name: "Blood nova",
  desc: "Seen from above: a white-hot core bursts, a crimson shock ring tears out to the full radius, blood streaks fly with it and splatter the floor, then dry away.",
  tags: ["arcane", "top-down"], size: [216, 216], origin: [108, 108], duration: 1, loop: false, fps: 30, bg: "dark",
  options: { palette: CRIMSON, seed: 5 },
};
const MAX = 98, GROW = 0.4;

export function draw(p, t, o) {
  const C = fit(o.palette, 7), k = ease.outQuart(seg(t, 0, GROW)), rad = 6 + k * (MAX - 6), fade = 1 - seg(t, 0.32, 0.56);
  // splatter left on the floor where the ring passed
  burst(44, o.seed, (r) => {
    const a = r.range(0, TAU), d = 22 + Math.pow(r.next(), 0.6) * (MAX - 26), s = r.range(1, 3.6), born = 0.04 + 0.2 * (d / MAX), out = r.range(0.6, 0.95);
    if (t < born || t > out) return;
    const q = 1 - seg(t, out - 0.3, out), x = Math.cos(a) * d, y = Math.sin(a) * d;
    p.disc(x, y, s * Math.min(1, (t - born) * 30), C[2], 0.75 * q);
    if (s > 2.2 && q > 0.6) p.px(x - 1, y - 1, C[4]);
  });
  if (fade > 0) {
    // a wave of blood, not a donut: the rim splashes in lobes and its back edge tears into tendrils
    const b = rad + 5;
    p.shade(-b, -b, b, b, (x, y, out) => {
      const d = Math.hypot(x, y), a = Math.atan2(y, x);
      const rim = rad - 3 + 3 * Math.sin(a * 7 + 1) * fade + 2 * Math.sin(a * 13 + 4);
      const w = (4 + 11 * fade) * (0.55 + 0.45 * Math.sin(a * 9 + 2)) + 5 * Math.max(0, Math.sin(a * 31)) * fade;
      if (d > rim || d < rim - w) return;
      const v = (d - (rim - w)) / w;
      out.c = v > 0.84 ? (t < 0.16 ? WHITE : C[6]) : v > 0.6 ? C[5] : v > 0.3 ? C[4] : C[3];
      out.a = 1;
    });
    p.glowRing(0, 0, rad - 3, 14, C[4], 0.32 * fade);
  }
  if (t < 0.2) {
    const q = 1 - t / 0.2;
    p.circle(0, 0, 16 * q + 2, t < 0.06 ? WHITE : C[5]);
    p.glow(0, 0, 60, C[5], 0.8 * q);
  }
  burst(48, o.seed + 1, (r) => {
    const a = r.range(0, TAU), sp = r.range(170, 480), life = r.range(0.25, 0.55), age = t - r.range(0, 0.05);
    if (age < 0 || age > life) return;
    const dx = Math.cos(a), dy = Math.sin(a), [x, y] = fly(dx * 6, dy * 6, dx * sp, dy * sp, age, 0, 5), q = age / life;
    if (Math.hypot(x, y) > MAX + 4) return;
    p.line(x, y, x - dx * 7 * (1 - q), y - dy * 7 * (1 - q), at([C[6], C[5], C[4], C[3], C[2]], q));
  });
}
