import { fit, seg, ease, TAU } from "../../core/index.js";

const HOLY = ["#2a1a05", "#6a4a0c", "#b8860b", "#e8b923", "#ffd95a", "#fff0a8", "#fffbe6"];

export const meta = {
  name: "Holy mark",
  desc: "Seen from above: a warning circle on the floor, the fixed edge shows where light will fall while a second ring closes in on it as the time runs out.",
  tags: ["warning", "top-down"], size: [64, 64], origin: [32, 32], duration: 0.7, loop: false, fps: 30, bg: "dark",
  options: { palette: HOLY },
};
const R = 26;

export function draw(p, t, o) {
  if (t >= meta.duration - 0.03) return;
  const C = fit(o.palette, 7), k = seg(t, 0, meta.duration * 0.96), near = k > 0.75;
  p.disc(0, 0, R, C[3], 0.1 + 0.16 * k);
  p.ring(0, 0, R, 1, near ? C[6] : C[4]);
  if (k < 1) p.ring(0, 0, 2 + ease.outQuad(k) * (R - 2), 1, near ? C[6] : C[5]);
  for (let i = 0; i < 4; i++) {
    const a = (i * TAU) / 4 + k, c = Math.cos(a), s = Math.sin(a);
    p.line(c * (R + 1), s * (R + 1), c * (R + 4), s * (R + 4), C[5]);
  }
  p.glowRing(0, 0, R, 5, C[4], 0.14 + 0.2 * k);
}
