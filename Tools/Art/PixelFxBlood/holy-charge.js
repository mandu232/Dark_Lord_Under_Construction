import { cycle, fit, seg, TAU } from "../../core/index.js";

const HOLY = ["#2a1a05", "#6a4a0c", "#b8860b", "#e8b923", "#ffd95a", "#fff0a8", "#fffbe6"];

export const meta = {
  name: "Holy charge",
  desc: "Seen from above, charging right: a wedge of golden light leads the hero and streaks of light tear away behind.",
  tags: ["status", "top-down"], size: [112, 72], origin: [72, 36], duration: 0.36, loop: false, fps: 30, bg: "dark",
  options: { palette: HOLY, seed: 12 },
};

export function draw(p, t, o) {
  const C = fit(o.palette, 7), f = seg(t, 0, 0.04) * (1 - seg(t, 0.28, 0.36));
  if (f <= 0) return;
  p.arc(0, 0, 26, (u) => 6 * f * Math.sin(Math.PI * u), -1.1, 1.1, (u, v) => (v > 0.6 ? C[6] : C[4]));
  p.glow(14, 0, 22, C[4], 0.45 * f);
  cycle(t, 0.36, 14, o.seed, 0.16, (age, life, r) => {
    const k = age / life, y = r.range(-24, 24), L = r.range(18, 34), x = 12 - Math.abs(y) * 0.5 - k * 40;
    p.line(x, y, x - L, y, (u) => (u < 0.25 ? C[6] : u < 0.6 ? C[4] : C[2]), f * (1 - k * 0.5));
  });
}
