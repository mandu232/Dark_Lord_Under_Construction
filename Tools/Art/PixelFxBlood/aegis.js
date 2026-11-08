import { fit, TAU } from "../../core/index.js";
import { CRIMSON } from "./_shared.js";

export const meta = {
  name: "Crimson aegis",
  desc: "Seen from above: a double sigil circle with six marks turns round the caster while a crown of thorns counter-rotates inside, pulsing with light.",
  tags: ["shield", "top-down"], size: [88, 88], origin: [44, 44], duration: 1.5, loop: true, fps: 30, bg: "dark",
  options: { palette: CRIMSON },
};
const R = 36;

export function draw(p, t, o) {
  const C = fit(o.palette, 7), ph = t / meta.duration, pulse = 0.5 + 0.5 * Math.sin(ph * TAU * 2);
  p.disc(0, 0, R, C[3], 0.1 + 0.08 * pulse);
  p.ring(0, 0, R + 2, 2, C[5]);
  p.ring(0, 0, R - 5, 1, C[3]);
  // Twelve thorns turn against the ring; a twelfth of a turn per loop is seamless, the marks turn a sixth.
  for (let i = 0; i < 12; i++) {
    const a = (-ph * TAU) / 12 + (i * TAU) / 12, b = a + 0.2, c = Math.cos(a), s = Math.sin(a);
    const lit = pulse > 0.75 ? C[5] : C[4];
    p.line(c * (R - 6), s * (R - 6), Math.cos(b) * (R - 18), Math.sin(b) * (R - 18), lit);
    p.line(Math.cos(a + 0.4) * (R - 6), Math.sin(a + 0.4) * (R - 6), Math.cos(b) * (R - 18), Math.sin(b) * (R - 18), C[3]);
  }
  for (let i = 0; i < 6; i++) {
    const a = (ph * TAU) / 6 + (i * TAU) / 6, cx = Math.cos(a), sy = Math.sin(a), x = cx * (R - 2), y = sy * (R - 2);
    p.poly([[x + cx * 4, y + sy * 4], [x - sy * 3, y + cx * 3], [x - cx * 4, y - sy * 4], [x + sy * 3, y - cx * 3]], C[6]);
    p.light(x, y, C[5], 0.4);
  }
  p.glowRing(0, 0, R, 8, C[4], 0.22 + 0.16 * pulse);
}
