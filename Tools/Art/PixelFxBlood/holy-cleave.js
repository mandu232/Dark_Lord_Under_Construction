import { burst, fit, seg, ease, at, WHITE } from "../../core/index.js";

const HOLY = ["#2a1a05", "#6a4a0c", "#b8860b", "#e8b923", "#ffd95a", "#fff0a8", "#fffbe6"];

export const meta = {
  name: "Holy cleave",
  desc: "Seen from above, facing right: one wide blade of golden light sweeps the half circle in front of the hero, exactly the area it hits, and throws sparks.",
  tags: ["melee", "top-down"], size: [128, 128], origin: [64, 64], duration: 0.42, loop: false, fps: 30, bg: "dark",
  options: { palette: HOLY, seed: 11 },
};
const R = 42, SWEEP = 0.13, A0 = -Math.PI / 2, A1 = Math.PI / 2;

export function draw(p, t, o) {
  const C = fit(o.palette, 7), head = A0 + (A1 - A0) * ease.outCubic(seg(t, 0, SWEEP));
  const tail = A0 + (A1 - A0) * ease.inQuad(seg(t, 0.06, 0.32)), fade = 1 - seg(t, 0.2, 0.34);
  if (head > tail && fade > 0) {
    const width = (u) => (15 * fade + 1) * Math.sin(Math.PI * Math.min(1, u * 0.85 + 0.15));
    p.arc(0, 0, R, width, tail, head, (u, v) => (v > 0.8 ? (t < 0.14 ? WHITE : C[6]) : v > 0.5 ? C[5] : v > 0.25 ? C[4] : C[2]));
    const m = (tail + head) / 2;
    p.glow(Math.cos(m) * (R - 6), Math.sin(m) * (R - 6), 30, C[4], 0.35 * fade);
  }
  burst(16, o.seed, (r) => {
    const a = r.range(A0, A1), born = 0.02 + ((a - A0) / (A1 - A0)) * SWEEP, life = r.range(0.12, 0.22), age = t - born, d = R - 3 + r.range(50, 110) * age;
    if (age < 0 || age > life) return;
    p.line(Math.cos(a) * d, Math.sin(a) * d, Math.cos(a) * (d - 3), Math.sin(a) * (d - 3), at([C[6], C[5], C[3]], age / life));
  });
}
