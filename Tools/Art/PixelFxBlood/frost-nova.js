import { burst, fit, seg, ease, TAU, WHITE } from "../../core/index.js";
import { CRIMSON } from "./_shared.js";

export const meta = {
  name: "Blood frost nova",
  desc: "Seen from above: a pale ring races out and frozen blood crystals burst radially behind it in rings, glint, then crack and shrink away.",
  tags: ["ice", "top-down"], size: [216, 216], origin: [108, 108], duration: 1.3, loop: false, fps: 30, bg: "dark",
  options: { palette: CRIMSON, ice: "#ffe6ec", seed: 7 },
};
const MAX = 98, GROW = 0.42;

export function draw(p, t, o) {
  const C = fit(o.palette, 7), ice = parseInt(o.ice.slice(1), 16), k = ease.outCubic(seg(t, 0, GROW)), rad = 6 + k * (MAX - 6);
  const frost = 1 - seg(t, 0.7, 1.2);
  // frosted floor behind the front
  if (frost > 0) p.disc(0, 0, rad, C[1], 0.32 * frost);
  const front = 1 - seg(t, 0.34, 0.56);
  if (front > 0) {
    p.ring(0, 0, rad, 2 + 3 * front, t < 0.3 ? WHITE : ice);
    p.ring(0, 0, rad - 5, 2, C[5], front);
    p.glowRing(0, 0, rad, 12, C[5], 0.4 * front);
  }
  // crystals are kites pointing away from the caster: lit side, shaded side, pale tip
  burst(45, o.seed, (r, i) => {
    // three orderly rows: 8, 15 and 22 big crystals, each jittered a little
    const row = i < 8 ? 0 : i < 23 ? 1 : 2, n = [8, 15, 22][row], first = [0, 8, 23][row];
    const d = 18 + row * 26 + r.range(-3, 3), a = ((i - first + row * 0.5) * TAU) / n + r.range(-0.07, 0.07), L = r.range(20, 30), W = r.range(4.5, 7);
    const born = 0.02 + GROW * ease.inQuad(d / MAX) * 0.8, die = r.range(0.72, 1.12), g = seg(t, born, born + 0.07) * (1 - seg(t, die, die + 0.12));
    if (g <= 0) return;
    const dx = Math.cos(a), dy = Math.sin(a), nx = -dy, ny = dx, x = dx * d, y = dy * d, l = L * g, w = W * g;
    const tx = x + dx * l, ty = y + dy * l, mx = x + dx * l * 0.3, my = y + dy * l * 0.3;
    p.tri(x, y, mx + nx * w, my + ny * w, tx, ty, C[4]);
    p.tri(x, y, mx - nx * w, my - ny * w, tx, ty, C[2]);
    p.line(mx, my, tx, ty, ice);
    p.line(x, y, mx, my, C[5]);
    p.line(mx + nx * w, my + ny * w, tx, ty, C[5]);
    if (((t * 30 + i * 7) | 0) % 23 === 0 && t > born + 0.1) { p.px(mx, my, WHITE); p.light(mx, my, C[5], 0.5); }
  });
  if (t < 0.16) {
    const q = 1 - t / 0.16;
    p.circle(0, 0, 13 * q + 2, WHITE);
    p.glow(0, 0, 50, C[5], 0.8 * q);
  }
  // chips fly when the crystals crack
  burst(40, o.seed + 2, (r) => {
    const born = r.range(0.75, 1.08), life = r.range(0.12, 0.2), age = t - born, a = r.range(0, TAU), d0 = r.range(24, 92);
    if (age < 0 || age > life) return;
    const d = d0 + age * 70;
    p.px(Math.cos(a) * d, Math.sin(a) * d, age / life < 0.5 ? ice : C[3]);
  });
}
