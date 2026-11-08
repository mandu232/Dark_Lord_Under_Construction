import { burst, fit, seg, ease, at, WHITE } from "../../core/index.js";
import { CRIMSON } from "./_shared.js";

export const meta = {
  name: "Blood needle",
  desc: "Seen from above, flying right: the needle slime spits a long thin needle of hardened blood; it flies dead straight, sticks in the target and spatters.",
  tags: ["projectile", "top-down"], size: [124, 28], origin: [14, 14], duration: 0.3, loop: false, fps: 30, bg: "dark",
  options: { palette: CRIMSON, seed: 14 },
};
const REACH = 96, FLIGHT = 0.1;

export function draw(p, t, o) {
  const C = fit(o.palette, 7), tip = ease.outQuad(seg(t, 0, FLIGHT)) * REACH;
  // A small muzzle spit where the needle left the slime.
  const spit = 1 - seg(t, 0, 0.1);
  if (spit > 0) {
    p.circle(0, 0, 3 * spit + 0.5, C[5]);
    p.glow(0, 0, 8, C[4], 0.4 * spit);
  }
  if (t <= FLIGHT) {
    // Long and thin: the needle itself, then a few broken dashes of wake behind it.
    p.line(Math.max(0, tip - 16), 0, tip, 0, C[5]);
    if (tip > 10) p.line(tip - 10, -1, tip - 3, -1, C[6]);
    if (tip > 12) p.line(tip - 12, 1, tip - 4, 1, C[3]);
    p.px(tip, 0, WHITE);
    for (let i = 1; i < 4; i++) if (tip - 16 - i * 8 >= 0) p.line(tip - 16 - i * 8, 0, tip - 12 - i * 8, 0, i < 2 ? C[4] : C[2]);
    p.glow(tip - 6, 0, 8, C[4], 0.4);
    return;
  }
  // Stuck in the target: the shaft stays a moment, then the hit spatters back.
  const k = seg(t, FLIGHT, 0.28), f = 1 - k;
  if (f <= 0) return;
  const stick = 1 - seg(t, FLIGHT + 0.03, 0.2);
  if (stick > 0) p.line(REACH - 14 * stick, 0, REACH, 0, C[4]);
  if (k < 0.3) p.circle(REACH, 0, 3 * (1 - k / 0.3) + 0.5, k < 0.15 ? WHITE : C[6]);
  p.glow(REACH, 0, 9, C[5], 0.45 * f);
  burst(7, o.seed, (r) => {
    const a = Math.PI + r.range(-1, 1), life = r.range(0.08, 0.18), age = t - FLIGHT, d = 1 + r.range(70, 120) * age * (1 - age / (life * 2));
    if (age > life) return;
    p.px(REACH + Math.cos(a) * d, Math.sin(a) * d, at([C[6], C[5], C[3]], age / life));
  });
}
