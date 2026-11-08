import { cycle, fit, TAU } from "../../core/index.js";
import { CRIMSON } from "./_shared.js";

export const meta = {
  name: "Blood ward",
  desc: "Seen from above: a translucent film of blood ringed by three arcs turning round the caster, with droplets orbiting the rim.",
  tags: ["shield", "top-down"], size: [64, 64], origin: [32, 32], duration: 1.2, loop: true, fps: 30, bg: "dark",
  options: { palette: CRIMSON, seed: 2 },
};
const R = 25;

export function draw(p, t, o) {
  const C = fit(o.palette, 7), ph = t / meta.duration, pulse = Math.sin(ph * TAU * 2);
  p.disc(0, 0, R - 1, C[3], 0.16 + 0.03 * pulse);
  p.ring(0, 0, R - 1, 1, C[2]);
  for (let i = 0; i < 3; i++) {
    const a = ph * TAU + (i * TAU) / 3;
    p.arc(0, 0, R + 1, (u) => 1 + 2.4 * Math.sin(Math.PI * u), a, a + 1.5, (u, v) => (v > 0.6 ? C[6] : C[4]));
  }
  p.glowRing(0, 0, R, 7, C[4], 0.24 + 0.05 * pulse);
  cycle(t, meta.duration, 9, o.seed, 0.7, (age, life, r) => {
    const q = age / life, a = r.range(0, TAU) - ph * TAU * 2, d = R + 3 - q * 7;
    p.px(Math.cos(a) * d, Math.sin(a) * d, q < 0.5 ? C[5] : C[3]);
  });
}
