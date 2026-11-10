import { TAU, hex } from "../../core/index.js";
import { LOOT, bob, shadow, glint, marble } from "./_loot.js";

export const meta = { ...LOOT, name: "Loot: blood marble", desc: "A glass marble with a ribbon of blood curling slowly inside, bobbing over its shadow; a glint crosses its highlight.", tags: ["pickup", "loot", "orb", "marble"], options: {} };
const RAMP = ["#2a0309", "#5c0816", "#a3112a", "#e5233f", "#ff7a8a", "#ffd6dc"];

// One fat ribbon of blood, curled in an S, turning once a loop.
const inner = (s, th, ph) => (s < 0.85 && Math.cos(th - 2.2 * s - TAU * ph) > 0.55 ? (s < 0.45 ? 4 : 3) : -1);

export function draw(p, t) {
  const y = bob(t);
  shadow(p, y);
  marble(p, t, y, RAMP, inner);
  if (glint(t, 0.3) > 0.4) { p.px(-3, y - 2, hex("#ffffff")); p.px(-2, y - 3, hex("#ffffff")); }
}
