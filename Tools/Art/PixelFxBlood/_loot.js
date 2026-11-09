import { TAU, hex } from "../../core/index.js";

// Shared by the four loot drops: a contact shadow and a small bob so they sit on the same floor as the characters.
export const LOOT = { size: [20, 20], origin: [10, 10], duration: 1.2, loop: true, fps: 15, bg: "dark" };
export function bob(t) { return Math.round(Math.sin((t / LOOT.duration) * TAU)); }
export function shadow(p, lift) {
  const w = 3 - (lift < 0 ? 1 : 0);
  for (let x = -w; x <= w; x++) p.blend(x, 7, hex("#000000"), 0.35);
  for (let x = -w + 1; x < w; x++) p.blend(x, 8, hex("#000000"), 0.2);
}
// A glint walks across once per loop and rests the remaining time.
export function glint(t, start) { return Math.max(0, 1 - Math.abs(((t / LOOT.duration) - start) / 0.06)); }
