import { burst, fit, seg, ease, at, TAU, WHITE } from "../../core/index.js";

const BRAND = ["#260702", "#5e1505", "#a82e08", "#e8590f", "#ff9423", "#ffd25e", "#fff6c9"];

export const meta = {
  name: "Trap brand",
  desc: "Seen from above: a burning sigil stamps onto the floor with a flash, its diamond and four barbs glow white-hot then cool as embers fly off it.",
  tags: ["debuff", "fire", "top-down"], size: [64, 64], origin: [32, 32], duration: 0.7, loop: false, fps: 30, bg: "dark",
  options: { palette: BRAND, seed: 31 },
};
const STAMP = 0.08;

export function draw(p, t, o) {
  const C = fit(o.palette, 7);
  if (t < STAMP) {
    const k = ease.inQuad(t / STAMP), r = 26 - 14 * k;
    // The sigil drops in oversized before it stamps, so the first frames already read as the brand.
    for (let i = 0; i < 4; i++) {
      const a = (i * TAU) / 4, b = a + TAU / 4;
      p.beam(Math.cos(a) * r, Math.sin(a) * r, Math.cos(b) * r, Math.sin(b) * r, 1.6, C[5]);
    }
    p.glow(0, 0, r + 6, C[4], 0.3);
    return;
  }
  const age = t - STAMP, cool = seg(age, 0, 0.42), out = 1 - seg(t, 0.52, 0.68);
  if (out <= 0) return;
  const c = at([WHITE, C[6], C[5], C[4], C[3], C[2]], cool);
  const R = 15, S = 8;
  const rot = 0.18 * (1 - ease.outCubic(seg(age, 0, 0.12)));
  const v = (r, a) => [Math.cos(a + rot) * r, Math.sin(a + rot) * r];
  // Stepped fade so the sigil dissolves by dither rather than popping off.
  const alpha = out;
  p.disc(0, 0, 20, C[2], 0.14 * out);
  for (let i = 0; i < 4; i++) {
    const a = (i * TAU) / 4, b = a + TAU / 4, [x0, y0] = v(R, a), [x1, y1] = v(R, b);
    p.beam(x0, y0, x1, y1, 2.4, c, alpha);
    const [ix0, iy0] = v(S, a), [ix1, iy1] = v(S, b);
    p.beam(ix0, iy0, ix1, iy1, 1.4, C[4], alpha);
    const m = a + TAU / 8, [bx0, by0] = v(R * 0.72, m), [bx1, by1] = v(R + 7, m);
    p.tri(bx0 + Math.cos(m + TAU / 4) * 2.5, by0 + Math.sin(m + TAU / 4) * 2.5, bx0 - Math.cos(m + TAU / 4) * 2.5, by0 - Math.sin(m + TAU / 4) * 2.5, bx1, by1, cool < 0.5 ? c : C[3], alpha);
    p.light(x0, y0, C[4], 0.35 * out);
    // A small tongue of flame on each corner, flickering with time.
    const h = (4 + 3 * Math.sin(age * 40 + i * 2)) * out;
    p.tri(x0 - 2, y0, x0 + 2, y0, x0 + Math.sin(age * 30 + i), y0 - h, C[5], alpha);
  }
  p.glow(0, 0, 22, C[4], 0.45 * out * (1 - cool * 0.5));
  if (age < 0.07) {
    const q = 1 - age / 0.07;
    p.circle(0, 0, 4 + 7 * q, age < 0.035 ? WHITE : C[6]);
    p.glow(0, 0, 32, C[5], 0.9 * q);
    p.ring(0, 0, 8 + (1 - q) * 19, 1 + q, C[6], q);
  }
  burst(22, o.seed, (r) => {
    const a = r.range(0, TAU), d0 = r.range(4, 18), speed = r.range(25, 80), born = r.range(0, 0.14), life = r.range(0.16, 0.36), k = age - born;
    if (k < 0 || k > life) return;
    const d = d0 + speed * k * (1 - (k / life) * 0.4), x = Math.cos(a) * d, y = Math.sin(a) * d;
    p.px(x, y, at([C[6], C[5], C[4], C[3], C[2]], k / life));
    if (k / life < 0.4) p.light(x, y, C[4], 0.25);
  });
}
