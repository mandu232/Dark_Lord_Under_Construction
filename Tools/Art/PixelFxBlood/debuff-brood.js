import { burst, fit, seg, ease, TAU, WHITE } from "../../core/index.js";

const BROOD = ["#12041f", "#2d0b45", "#531a78", "#8530b0", "#b65ae0", "#dca0f5", "#f8e8ff"];

export const meta = {
  name: "Binding brood",
  desc: "Seen from above: a dark blood pool spreads under the target, barbed tendrils whip in and coil tight, a pulse runs along them, then they wither into motes.",
  tags: ["debuff", "shadow", "top-down"], size: [72, 72], origin: [36, 36], duration: 0.75, loop: false, fps: 30, bg: "dark",
  options: { palette: BROOD, seed: 23 },
};
const TENDRILS = 6, CINCH = 0.2, FADE = 0.56;

export function draw(p, t, o) {
  const C = fit(o.palette, 7);
  const pool = ease.outCubic(seg(t, 0, 0.14)) * (1 - seg(t, FADE, 0.74));
  if (pool > 0) {
    p.disc(0, 0, 27 * pool, C[1], 0.35);
  }
  burst(TENDRILS, o.seed, (r, i) => {
    const a0 = (i * TAU) / TENDRILS + r.range(-0.2, 0.2), born = r.range(0.01, 0.06), curl = r.range(0.9, 1.3);
    const k = ease.outCubic(seg(t, born, CINCH)), gone = seg(t, FADE, FADE + 0.12);
    if (k <= 0 || gone >= 1) return;
    const R0 = 30, end = 11 + (1 - k) * 19, pts = [], base = [];
    for (let s = 0; s <= 12; s++) {
      const u = s / 12, rad = R0 + (end - R0) * u, ang = a0 + curl * u * u;
      if (u < gone) continue;
      pts.push([Math.cos(ang) * rad, Math.sin(ang) * rad]);
      base.push(u);
    }
    if (pts.length < 2) return;
    const pulse = seg(t, CINCH, FADE) * 1.2;
    for (let s = 1; s < pts.length; s++) {
      const hot = Math.abs(base[s] - pulse) < 0.14;
      const w = 4.2 * (1 - base[s] * 0.75);
      p.beam(pts[s - 1][0], pts[s - 1][1], pts[s][0], pts[s][1], w + 1.4, C[0]);
      p.beam(pts[s - 1][0], pts[s - 1][1], pts[s][0], pts[s][1], w, hot ? C[5] : C[3]);
      p.beam(pts[s - 1][0] - 0.5, pts[s - 1][1] - 0.5, pts[s][0] - 0.5, pts[s][1] - 0.5, w * 0.35, hot ? C[6] : C[4]);
    }
    // Barbs alternate sides so a tendril reads as thorny, not as a stroke.
    for (let s = 2; s < pts.length - 1; s += 2) {
      const dx = pts[s + 1][0] - pts[s - 1][0], dy = pts[s + 1][1] - pts[s - 1][1], len = Math.hypot(dx, dy) || 1, side = s % 4 === 0 ? 1 : -1, b = 3.5 * (1 - base[s] * 0.6);
      p.tri(pts[s][0] - dx / len, pts[s][1] - dy / len, pts[s][0] + dx / len, pts[s][1] + dy / len, pts[s][0] + (-dy / len) * side * (b + 2), pts[s][1] + (dx / len) * side * (b + 2), C[4]);
    }
    const tip = pts[pts.length - 1];
    if (t < FADE) p.light(tip[0], tip[1], C[4], 0.45);
  });
  if (t >= CINCH && t < CINCH + 0.09) {
    const q = 1 - (t - CINCH) / 0.09;
    p.ring(0, 0, 18 * q + 3, 1 + 2 * q, WHITE, q);
    p.glow(0, 0, 28, C[4], 0.7 * q);
  }
  burst(16, o.seed + 1, (r) => {
    const a = r.range(0, TAU), d = r.range(6, 26), born = FADE + r.range(0, 0.04), life = r.range(0.08, 0.11), age = t - born;
    if (age < 0 || age > life) return;
    p.px(Math.cos(a) * (d + age * 35), Math.sin(a) * (d + age * 35), age / life < 0.5 ? C[5] : C[3]);
  });
}
