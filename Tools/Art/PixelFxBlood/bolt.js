import { fit, seg, ease, WHITE } from "../../core/index.js";
import { CRIMSON } from "./_shared.js";

export const meta = {
  name: "Blood bolt",
  desc: "Seen from above, flying right: a small drop of blood darts at the target with a short tail and pops on arrival.",
  tags: ["projectile", "top-down"], size: [100, 32], origin: [12, 16], duration: 0.24, loop: false, fps: 30, bg: "dark",
  options: { palette: CRIMSON },
};
const REACH = 72, FLIGHT = 0.09;

export function draw(p, t, o) {
  const C = fit(o.palette, 7), tip = ease.outQuad(seg(t, 0, FLIGHT)) * REACH;
  if (t <= FLIGHT) {
    p.line(Math.max(0, tip - 20), 0, tip - 4, 0, C[3]);
    p.tri(tip - 7, -2, tip - 7, 2, tip, 0, C[5]);
    p.px(tip, 0, C[6]);
    p.glow(tip - 3, 0, 8, C[4], 0.45);
    return;
  }
  const k = seg(t, FLIGHT, 0.22), f = 1 - k;
  if (f <= 0) return;
  p.ring(REACH, 0, 2 + ease.outCubic(k) * 8, 1, k < 0.4 ? WHITE : C[5], f);
  p.glow(REACH, 0, 10, C[5], 0.5 * f);
}
