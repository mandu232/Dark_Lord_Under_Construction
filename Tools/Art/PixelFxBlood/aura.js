import { cycle, fit, TAU } from "../../core/index.js";
import { CRIMSON } from "./_shared.js";

export const meta = {
  name: "Blood aura",
  desc: "Seen from above: a quiet buff on the floor under a minion, a thin ring breathing while three motes orbit it.",
  tags: ["status", "top-down"], size: [40, 40], origin: [20, 20], duration: 1.2, loop: true, fps: 30, bg: "dark",
  options: { palette: CRIMSON, seed: 13 },
};
const R = 13;

export function draw(p, t, o) {
  const C = fit(o.palette, 7), ph = t / meta.duration, breathe = Math.sin(ph * TAU);
  p.ring(0, 0, R + breathe, 1, C[3], 0.8);
  p.glowRing(0, 0, R + breathe, 3, C[4], 0.12 + 0.06 * breathe);
  for (let i = 0; i < 3; i++) {
    const a = ph * TAU + (i * TAU) / 3;
    p.px(Math.cos(a) * (R + breathe), Math.sin(a) * (R + breathe), C[6]);
    p.px(Math.cos(a - 0.2) * (R + breathe), Math.sin(a - 0.2) * (R + breathe), C[4]);
  }
  cycle(t, meta.duration, 5, o.seed, 0.6, (age, life, r) => {
    const q = age / life, a = r.range(0, TAU), d = R - 2 - q * 8;
    p.px(Math.cos(a) * d, Math.sin(a) * d, q < 0.5 ? C[4] : C[2]);
  });
}
