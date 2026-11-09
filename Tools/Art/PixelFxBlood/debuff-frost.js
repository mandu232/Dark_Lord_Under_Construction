import { burst, fit, seg, ease, TAU, WHITE } from "../../core/index.js";

const ICE = ["#0b1a3a", "#163a70", "#2a6fb0", "#4fb0e6", "#8fdcf5", "#c8f2ff", "#f2fdff"];

export const meta = {
  name: "Frost prison",
  desc: "Seen from above: frost snaps inward over the floor, a cage of ice blades slams shut round the target, glints, then shatters outward.",
  tags: ["debuff", "ice", "top-down"], size: [72, 72], origin: [36, 36], duration: 0.75, loop: false, fps: 30, bg: "dark",
  options: { palette: ICE, seed: 11 },
};
const SHUT = 0.14, BREAK = 0.56, BLADES = 9;

export function draw(p, t, o) {
  const C = fit(o.palette, 7);
  const frost = seg(t, 0, SHUT) * (1 - seg(t, 0.5, 0.74));
  if (frost > 0) p.disc(0, 0, 27, C[2], 0.16 * frost);
  if (t < SHUT) {
    const k = ease.outQuad(t / SHUT);
    // Frost shards rush inward from all sides rather than a plain closing ring.
    burst(14, o.seed + 7, (r, i) => {
      const a = (i * TAU) / 14 + r.range(-0.1, 0.1), d = r.range(30, 34) - k * 11, dx = Math.cos(a), dy = Math.sin(a), s = r.range(2.5, 4);
      p.tri(dx * d - dy * s, dy * d + dx * s, dx * d + dy * s, dy * d - dx * s, dx * (d - 6), dy * (d - 6), C[5]);
      p.px(dx * (d - 5), dy * (d - 5), WHITE);
    });
    p.glow(0, 0, 30, C[4], 0.3 * k);
  }
  // Blades point at the target; the far ones are drawn first and dimmer so the cage has depth.
  burst(BLADES, o.seed, (r, i) => {
    const a = (i * TAU) / BLADES + r.range(-0.12, 0.12), L = r.range(10, 15), W = r.range(4, 6), d = r.range(23, 27);
    const born = r.range(SHUT - 0.05, SHUT + 0.02), g = ease.outBack(seg(t, born, born + 0.06)) * (1 - seg(t, BREAK, BREAK + 0.03));
    if (g <= 0) return;
    const dx = Math.cos(a), dy = Math.sin(a), nx = -dy, ny = dx, x = dx * d, y = dy * d, l = L * g, w = W * g;
    const tx = x - dx * l, ty = y - dy * l, mx = x - dx * l * 0.3, my = y - dy * l * 0.3;
    p.tri(x, y, mx + nx * w, my + ny * w, tx, ty, dy < 0 ? C[3] : C[4]);
    p.tri(x, y, mx - nx * w, my - ny * w, tx, ty, dy < 0 ? C[1] : C[2]);
    p.line(mx, my, tx, ty, C[6]);
    p.line(mx + nx * w, my + ny * w, tx, ty, C[5]);
    if (((t * 30 + i * 5) | 0) % 11 === 0 && t > SHUT + 0.08) { p.px(mx, my, WHITE); p.light(mx, my, C[5], 0.6); }
  });
  if (t >= SHUT && t < SHUT + 0.08) {
    const q = 1 - (t - SHUT) / 0.08;
    p.ring(0, 0, 6 + (1 - q) * 18, 1 + q * 2, WHITE, q);
    p.glow(0, 0, 30, C[5], 0.7 * q);
  }
  if (t >= BREAK - 0.02 && t < BREAK + 0.05) p.glow(0, 0, 26, C[5], 0.55);
  burst(28, o.seed + 3, (r) => {
    const a = r.range(0, TAU), speed = r.range(50, 130), born = BREAK + r.range(0, 0.04), life = r.range(0.08, 0.15), age = t - born;
    if (age < 0 || age > life) return;
    const d = r.range(8, 20) + speed * age, x = Math.cos(a) * d, y = Math.sin(a) * d;
    p.line(x, y, x - Math.cos(a) * 2, y - Math.sin(a) * 2, age / life < 0.5 ? C[6] : C[3]);
  });
}
