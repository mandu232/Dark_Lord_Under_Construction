// The rift between waves: the same tear as rift.js, pulled shut into a scar and held by stitches of stone.
import { fit, cycle, hex, TAU, WHITE } from "../../core/index.js";
import { L, rot, unrot, spine, cracks, meta as open } from "./rift.js";

const D = 2;

export const meta = {
  name: "Invasion rift (sealed)",
  desc: "The rift between waves, seen from above: a dark seam held shut by stone stitches, a slow violet pulse running along it, cold cracks and the odd ember leaking out.",
  tags: ["rift", "portal", "sealed", "ground", "loop"],
  size: [64, 64],
  origin: [32, 32],
  duration: D,
  loop: true,
  fps: 20,
  options: { ...open.options },
};

const STONE = ["#1b1f2c", "#5f6a7c", "#8592a2"].map(hex);

// The seam's half-width along its length: a hair at the ends, a couple of pixels in the middle.
const seam = (v) => 1.6 * Math.pow(Math.max(0, 1 - v * v), 0.75) * (1 + 0.25 * Math.sin(v * 11 + 1.7));

export function draw(p, t, o) {
  const C = fit(o.palette, 8), Lp = fit(o.lip, 5), ph = t / D, breath = 0.5 + 0.5 * Math.sin(TAU * ph);

  // Faint scorch where the tear was.
  for (let y = -26; y <= 26; y++)
    for (let x = -26; x <= 26; x++) {
      const [u, v] = rot(x, y), d = (u / 12) ** 2 + (v / (L + 5)) ** 2;
      if (d < 1) p.blend(x, y, C[0], 0.35 * (1 - d) ** 1.4);
    }

  // The cracks have gone cold: dark grooves, a dim ember only where they meet the seam.
  for (const { pts } of cracks(o.seed)) {
    p.polyline(pts, C[0]);
    const [ax, ay] = pts[0], [bx, by] = pts[1];
    p.line(ax, ay, ax + (bx - ax) * 0.35, ay + (by - ay) * 0.35, Lp[0]);
  }

  // A pulse of violet light runs along the seam once a loop, end to end.
  const run = Math.sin(TAU * ph) * 1.1;
  p.shade(-26, -26, 26, 26, (x, y, out) => {
    const [u, vv] = rot(x, y), v = vv / L;
    if (Math.abs(v) > 0.97) return;
    const w = seam(v), e = Math.abs(u - spine(v)) - w;
    if (e > 1) return;
    if (e > 0) { out.c = Lp[0]; return; }                       // a scarred lip
    const hot = Math.exp(-((v - run) ** 2) / 0.02);
    out.c = Math.abs(u - spine(v)) < 0.6 ? (hot > 0.6 ? C[6] : hot > 0.25 ? C[5] : C[3]) : C[1];
    if (hot > 0.4) out.k = 0.25 * hot;
  });

  // Stone stitches across the seam.
  // pale dressed stone, lit on the upper edge, a shadow under
  for (const v of [-0.62, -0.22, 0.2, 0.58]) {
    const c = spine(v), w = seam(v) + 2.5;
    const [x0, y0] = unrot(c - w, v * L + 1), [x1, y1] = unrot(c + w, v * L + 1);
    p.line(x0, y0, x1, y1, STONE[0]);
    const [a0, b0] = unrot(c - w, v * L), [a1, b1] = unrot(c + w, v * L);
    p.line(a0, b0, a1, b1, STONE[1]);
    p.px(a0, b0, STONE[2]); p.px(a1, b1, STONE[2]);
  }

  // A rare ember leaks out and drifts up.
  cycle(t, D, 3, o.seed * 5 + 1, (g) => g.range(0.6, 1), (age, life, r) => {
    const k = age / life, v = r.range(-0.6, 0.6), [x, y] = unrot(spine(v), v * L);
    p.dot(x + Math.sin(k * 7) * 1.5, y - k * 8, k < 0.3 ? Lp[4] : Lp[3], Math.sin(k * Math.PI));
  });

  const [gx, gy] = unrot(spine(run), run * L);
  p.glow(gx, gy, 8, C[5], 0.3);
  p.glow(0, 0, 18, C[4], 0.08 + 0.05 * breath);
  if (Math.abs(run) < 0.9 && breath > 0.97) p.px(gx, gy, WHITE);
}
