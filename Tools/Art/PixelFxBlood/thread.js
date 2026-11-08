import { fit, seg, TAU } from "../../core/index.js";
import { CRIMSON } from "./_shared.js";

export const meta = {
  name: "Blood thread",
  desc: "Seen from above, running right: a thin wavering thread of blood reaches out to an ally and beads of life run along it.",
  tags: ["heal", "top-down"], size: [104, 20], origin: [6, 10], duration: 0.4, loop: false, fps: 30, bg: "dark",
  options: { palette: CRIMSON },
};
const REACH = 88;

export function draw(p, t, o) {
  const C = fit(o.palette, 7), head = seg(t, 0, 0.12) * REACH, tail = seg(t, 0.2, 0.38) * REACH;
  if (head <= tail) return;
  const wave = (x) => Math.sin((x / REACH) * TAU * 2 + t * 18) * 2.5 * Math.sin((x / REACH) * Math.PI);
  for (let x = tail; x < head; x += 1) p.px(x, wave(x), C[3]);
  for (let i = 0; i < 3; i++) {
    const x = (((t * 2.6 + i / 3) % 1) * REACH);
    if (x < tail || x > head) continue;
    p.px(x, wave(x), C[6]);
    p.px(x - 1, wave(x - 1), C[5]);
    p.light(x, wave(x), C[5], 0.5);
  }
}
