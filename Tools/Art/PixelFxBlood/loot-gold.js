import { TAU, hex } from "../../core/index.js";
import { LOOT, bob, shadow, glint, marble } from "./_loot.js";

export const meta = { ...LOOT, name: "Loot: gold marble", desc: "An amber glass marble full of gold flakes that wheel slowly round inside it, bobbing over its shadow.", tags: ["pickup", "loot", "orb", "marble"], options: {} };
const RAMP = ["#3d2204", "#7a4a0a", "#c08212", "#ffc53d", "#ffe58a", "#fffbe0"];
// Flakes at fixed radii and angles, wheeling a whole turn a loop.
const FLAKES = [[0.25, 0.3], [0.55, 1.9], [0.6, 3.6], [0.45, 5.0], [0.7, 0.9], [0.35, 4.2]];

export function draw(p, t) {
  const y = bob(t), ph = t / LOOT.duration;
  shadow(p, y);
  marble(p, t, y, RAMP, (s) => (s < 0.3 ? 3 : -1));
  FLAKES.forEach(([r, a], i) => {
    const g = a + TAU * ph, x = Math.cos(g) * r * 3.6 + 0.5, yy = y + 0.5 + Math.sin(g) * r * 3.2;
    p.px(x, yy, Math.sin(g) > 0 ? hex("#ffe58a") : i % 2 ? hex("#ffc53d") : hex("#fffbe0"));
  });
  if (glint(t, 0.5) > 0.4) { p.px(-3, y - 2, hex("#ffffff")); p.px(-2, y - 3, hex("#ffffff")); }
}
