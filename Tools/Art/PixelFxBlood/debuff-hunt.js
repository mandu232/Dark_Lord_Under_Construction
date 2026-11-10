import { burst, fit, seg, ease, TAU, WHITE } from "../../core/index.js";

const VIOLET = ["#1a0630", "#3d0d63", "#6a1fa0", "#9a3fd8", "#c27af0", "#e3b8ff", "#f8ecff"];

export const meta = {
  name: "Lord's hunt",
  desc: "Seen from above: four violet fangs plunge from outside to pin the target, a sentence ring stamps down with a flash, it spins slowly and thins away with falling violet motes.",
  tags: ["debuff", "mark", "top-down"], size: [72, 72], origin: [36, 36], duration: 0.75, loop: false, fps: 30, bg: "dark",
  options: { palette: VIOLET, seed: 41 },
};
const LAND = 0.12, R = 19;

function fang(p, c, a, d, C, alpha) {
  const dx = Math.cos(a), dy = Math.sin(a), nx = -dy, ny = dx, x = dx * d, y = dy * d, L = 15, W = 5.5;
  p.tri(x + dx * L + nx * W, y + dy * L + ny * W, x + dx * L, y + dy * L, x, y, C[4], alpha);
  p.tri(x + dx * L - nx * W, y + dy * L - ny * W, x + dx * L, y + dy * L, x, y, C[2], alpha);
  p.line(x, y, x + dx * L, y + dy * L, c, alpha);
}

export function draw(p, t, o) {
  const C = fit(o.palette, 7), k = ease.inCubic(seg(t, 0, LAND)), out = 1 - seg(t, 0.5, 0.72);
  if (out <= 0) return;
  const spin = t > LAND ? (t - LAND) * 0.8 : 0;
  for (let i = 0; i < 4; i++) {
    const a = TAU / 8 + (i * TAU) / 4 + spin, d = 25 - k * 9;
    fang(p, t < LAND + 0.06 ? WHITE : C[6], a, d, C, out);
    if (t < LAND) p.line(Math.cos(a) * (d + 11), Math.sin(a) * (d + 11), Math.cos(a) * (d + 18), Math.sin(a) * (d + 18), C[3]);
  }
  if (t >= LAND) {
    const age = t - LAND, grow = ease.outBack(seg(age, 0, 0.07));
    for (let i = 0; i < 4; i++) {
      const a = (i * TAU) / 4 + TAU / 8 + spin, gap = 0.34;
      p.arc(0, 0, (R + 1) * grow, 3, a + gap, a + TAU / 4 - gap, C[2], out);
      p.arc(0, 0, (R + 2) * grow, 1, a + gap, a + TAU / 4 - gap, C[5], out);
      const n = a + TAU / 8;
      p.tri(Math.cos(n - 0.13) * (R + 2) * grow, Math.sin(n - 0.13) * (R + 2) * grow, Math.cos(n + 0.13) * (R + 2) * grow, Math.sin(n + 0.13) * (R + 2) * grow, Math.cos(n) * (R + 7) * grow, Math.sin(n) * (R + 7) * grow, C[4], out);
    }
    p.glowRing(0, 0, R + 1, 6, C[4], 0.22 * out);
    if (age < 0.08) {
      const q = 1 - age / 0.08;
      p.circle(0, 0, 3 + 7 * q, age < 0.04 ? WHITE : C[6]);
      p.glow(0, 0, 32, C[5], 0.85 * q);
          }
  }
  burst(14, o.seed, (r) => {
    const a = r.range(0, TAU), d = r.range(8, 22), born = LAND + r.range(0.02, 0.3), life = r.range(0.14, 0.28), age = t - born;
    if (age < 0 || age > life) return;
    const twinkle = ((age * 30) | 0) % 2 === 0;
    p.px(Math.cos(a) * d, Math.sin(a) * d + age * 18, twinkle ? C[6] : C[4]);
  });
}
