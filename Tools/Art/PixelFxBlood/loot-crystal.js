import { TAU, hex } from "../../core/index.js";
import { LOOT, bob, shadow, glint, marble } from "./_loot.js";

export const meta = { ...LOOT, name: "Loot: crystal marble", desc: "A magenta glass marble with a faceted star of crystal suspended at its heart, turning; a glint flashes across it.", tags: ["pickup", "loot", "orb", "marble"], options: {} };
const RAMP = ["#2a0628", "#5e0f55", "#a51c86", "#e0288f", "#ff8ad0", "#ffe0f3"];

// A four-pointed crystal star at the heart, a quarter turn a loop.
const inner = (s, th, ph) => {
  const star = Math.pow(Math.abs(Math.cos(2 * (th - (TAU / 4) * ph))), 8);
  return s < 0.2 ? 5 : s < 0.22 + 0.5 * star ? 4 : -1;
};

export function draw(p, t) {
  const y = bob(t);
  shadow(p, y);
  marble(p, t, y, RAMP, inner);
  const g = glint(t, 0.62);
  if (g > 0.3) p.light(0, y, hex("#ff8ad0"), 0.6);
  if (g > 0.7) { p.px(-3, y - 3, hex("#ffffff")); p.px(-1, y - 1, hex("#ffffff")); }
}
