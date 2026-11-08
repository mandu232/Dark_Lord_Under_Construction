import { burst, fit, seg, ease, at, TAU, WHITE } from "../../core/index.js";
import { CRIMSON } from "./_shared.js";

export const meta = {
  name: "Blood lance",
  desc: "Seen from above, flying right: a slender spear of solid blood shoots from the caster, leaves a thinning trail, and bursts at its target.",
  tags: ["projectile", "top-down"], size: [136, 40], origin: [24, 20], duration: 0.34, loop: false, fps: 30, bg: "dark",
  options: { palette: CRIMSON, seed: 9 },
};
const REACH = 96, FLIGHT = 0.12;

export function draw(p, t, o) {
  const C = fit(o.palette, 7), k = ease.outQuad(seg(t, 0, FLIGHT)), tip = k * REACH;
  // the trail thins from the caster towards the tip
  const fade = 1 - seg(t, FLIGHT * 0.6, 0.26), tail = Math.max(0, tip - 58) + seg(t, FLIGHT, 0.26) * 40;
  if (fade > 0 && tip > tail) {
    p.beam(tail, 0, tip - 8, 0, 1 + fade, C[3], fade);
    p.line(tail, 0, tip - 8, 0, C[5], fade);
    p.glowLine(tail, 0, tip, 0, 6, C[4], 0.3 * fade);
  }
  if (t <= FLIGHT) {
    // a long kite: lit upper edge, shaded lower edge, pale point
    p.tri(tip - 22, 0, tip - 12, -3.5, tip, 0, C[5]);
    p.tri(tip - 22, 0, tip - 12, 3.5, tip, 0, C[3]);
    p.line(tip - 18, 0, tip, 0, C[6]);
    p.px(tip, 0, WHITE);
    p.glow(tip - 6, 0, 12, C[5], 0.5);
    return;
  }
  const hit = seg(t, FLIGHT, 0.3), h = 1 - hit;
  if (h > 0) {
    p.ring(REACH, 0, 2 + ease.outCubic(hit) * 13, 1 + h, hit < 0.5 ? C[6] : C[4], h);
    if (hit < 0.3) p.circle(REACH, 0, 4 * (1 - hit / 0.3) + 1, hit < 0.15 ? WHITE : C[6]);
    p.glow(REACH, 0, 16, C[5], 0.55 * h);
  }
  burst(10, o.seed, (r) => {
    const a = Math.PI + r.range(-1.2, 1.2), life = r.range(0.1, 0.2), age = t - FLIGHT, d = 2 + r.range(70, 130) * age * (1 - age / (life * 2));
    if (age > life) return;
    p.px(REACH + Math.cos(a) * d, Math.sin(a) * d, at([C[6], C[5], C[3]], age / life));
  });
}
