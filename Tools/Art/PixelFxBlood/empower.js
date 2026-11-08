import { fit, TAU } from "../../core/index.js";
import { CRIMSON } from "./_shared.js";

export const meta = {
  name: "Blood empower",
  desc: "Seen from above: a minion surges with blood power, four sharp chevrons circling it inside a pulsing ring.",
  tags: ["status", "top-down"], size: [56, 56], origin: [28, 28], duration: 0.9, loop: true, fps: 30, bg: "dark",
  options: { palette: CRIMSON },
};
const R = 19;

export function draw(p, t, o) {
  const C = fit(o.palette, 7), ph = t / meta.duration, pulse = 0.5 + 0.5 * Math.sin(ph * TAU * 2);
  p.ring(0, 0, R + 3, 1, pulse > 0.6 ? C[5] : C[3]);
  p.glowRing(0, 0, R + 3, 5, C[4], 0.18 + 0.14 * pulse);
  // A quarter turn per loop is seamless with four chevrons.
  for (let i = 0; i < 4; i++) {
    const a = (ph * TAU) / 4 + (i * TAU) / 4, c = Math.cos(a), s = Math.sin(a), x = c * R, y = s * R;
    p.line(x - c * 3 - s * 3, y - s * 3 + c * 3, x + c * 2, y + s * 2, C[6]);
    p.line(x - c * 3 + s * 3, y - s * 3 - c * 3, x + c * 2, y + s * 2, C[5]);
  }
}
