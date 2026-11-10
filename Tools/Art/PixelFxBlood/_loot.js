import { TAU, hex, WHITE } from "../../core/index.js";

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

const R = 4.6;
// A glass marble, lit from the upper left: a dark rim, clear glass a step lighter inside it, light gathering in the
// lower right where it passes through, a hard white highlight and a softer second one. `inner(s, th, ph)` paints
// what's suspended in the glass (s 0..1 from the centre, th the angle): return a ramp index or -1 for clear glass.
export function marble(p, t, y, ramp, inner) {
  const C = ramp.map(hex), ph = t / LOOT.duration, cy = y;   // the centre pixel is (0, cy)
  p.shade(-5, cy - 5, 6, cy + 6, (x, yy, out) => {
    const dx = x - 0.5, dy = yy - cy - 0.5, d = Math.hypot(dx, dy) / R;
    if (d > 1) return;
    if (d > 0.84) { out.c = C[0]; return; }
    const s = d / 0.84, th = Math.atan2(dy, dx), lit = (dx + dy) / R;
    // deep glass, a shade lighter in the middle; a crescent of light where it leaves the glass, bottom right
    let i = s < 0.5 ? 2 : 1;
    if (lit > 0.5 && s > 0.62) i = lit > 0.75 ? 4 : 3;
    const k = inner(s, th, ph, dx, dy);
    if (k >= 0) i = Math.max(i, k);
    out.c = C[i];
  });
  // the highlight: a two-pixel window glint top left, a pinprick bottom right
  p.px(-2, cy - 2, WHITE);
  p.px(-1, cy - 3, C[5]);
  p.px(-3, cy - 1, C[4]);
  p.px(2, cy + 2, C[4]);
  p.glow(0.5, cy + 0.5, 8, C[3], 0.26);
  p.glow(-1.5, cy - 1.5, 3, C[5], 0.3);
}
