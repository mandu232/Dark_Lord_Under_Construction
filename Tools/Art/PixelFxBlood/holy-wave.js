// Rowen's sword wave in flight, facing right: a crescent of gold light with a fraying trail. The game moves and rotates it.
import { fit, cycle, TAU, WHITE } from "../../core/index.js";

const HOLY = ["#2a1a05", "#6a4a0c", "#b8860b", "#e8b923", "#ffd95a", "#fff0a8", "#fffbe6"];

export const meta = {
  name: "Holy sword wave",
  desc: "A crescent of gold light flying right, white along its leading edge, shimmering, a fraying trail of sparks behind it.",
  tags: ["holy", "projectile", "slash", "loop"],
  size: [48, 48],
  origin: [24, 24],
  duration: 0.4,
  loop: true,
  fps: 20,
  bg: "dark",
  options: { palette: HOLY, seed: 6 },
};

export function draw(p, t, o) {
  const C = fit(o.palette, 7), ph = t / meta.duration, shimmer = 0.5 + 0.5 * Math.sin(TAU * ph * 2);
  // the crescent: an arc bulging forward, thick in the middle, tapering to points
  for (let g = -1.15; g <= 1.15; g += 0.02) {
    const w = 3.5 * Math.cos(g * 1.3), x = Math.cos(g) * 12 - 6, y = Math.sin(g) * 14;
    for (let d = 0; d < w; d += 0.5) {
      const k = d / w;
      p.px(x - d, y, k < 0.25 ? WHITE : k < 0.6 ? C[5] : C[4]);
    }
    p.blend(x - w - 1, y, C[3], 0.5);
  }
  p.glow(2, 0, 16, C[4], 0.35 + 0.15 * shimmer);
  // sparks shed behind it, drifting back and winking out
  cycle(t, meta.duration, 10, o.seed, (g) => g.range(0.2, 0.35), (age, life, r) => {
    const k = age / life, y = r.range(-12, 12), x = Math.cos(y / 14) * 12 - 9 - k * r.range(10, 18);
    p.px(x, y, k < 0.4 ? C[6] : C[4]);
  });
}
