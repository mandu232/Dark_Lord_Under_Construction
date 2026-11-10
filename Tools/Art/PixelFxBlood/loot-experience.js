import { TAU, hex } from "../../core/index.js";
import { LOOT, bob, shadow, marble } from "./_loot.js";

export const meta = { ...LOOT, name: "Loot: soul marble", desc: "A teal glass marble with a soul flame flickering inside it, a single mote circling it outside.", tags: ["pickup", "loot", "orb", "marble"], options: {} };
const RAMP = ["#062a28", "#0a4a48", "#138a7a", "#25c8a8", "#8ff5d8", "#f0fff8"];

export function draw(p, t) {
  const ph = t / LOOT.duration, y = bob(t);
  shadow(p, y);
  // A little flame standing inside the glass, its tip swaying.
  marble(p, t, y, RAMP, (s, th, ph2, dx, dy) => {
    // a teardrop flame: a flickering tip, a round body, a white heart low down
    const sway = Math.sin(TAU * ph * 2) * (dy < -1 ? 0.7 : 0);
    const w = dy < -2.6 || dy > 1.8 ? 0 : dy < -1 ? 0.6 : dy < 1 ? 1.4 : 0.9, ax = Math.abs(dx - sway);
    if (ax < 0.5 && dy > -0.6 && dy < 1) return 5;
    return ax < w ? 4 : -1;
  });
  const a = ph * TAU * 2, mx = Math.round(Math.cos(a) * 7), my = Math.round(Math.sin(a) * 2) - 1 + y;
  p.px(mx, my, Math.sin(a) > 0 ? hex("#f0fff8") : hex("#25c8a8"));
}
