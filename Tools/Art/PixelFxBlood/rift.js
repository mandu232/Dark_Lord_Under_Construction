// The swirling well inside the tear borrows the log-spiral arms of pixelfx's arcane/portal.
import { fit, cycle, smooth, ease, sparkle, Rng, TAU, WHITE } from "../../core/index.js";
import { tone, mote } from "../arcane/_lib.js";

const D = 2;

export const meta = {
  name: "Invasion rift",
  desc: "A jagged tear in the floor seen from above: a violet void swirls inward, its crimson lip burns white in spots, light pulses out along cracks, motes are pulled in.",
  tags: ["rift", "portal", "spawn", "arcane", "ground", "loop"],
  size: [64, 64],
  origin: [32, 32],
  duration: D,
  loop: true,
  fps: 20,
  options: {
    palette: ["#07020d", "#1a0630", "#36105a", "#5a1a8a", "#8a2fb8", "#c25ae0", "#f0b0ff", "#ffffff"],
    lip: ["#2a0510", "#7a0e24", "#c8243c", "#ff5a5a", "#ffc0b0"],
    seed: 7,
  },
};

export const L = 19, W = 8.5, TILT = 0.42;   // half-length, half-width and lean of the tear (about one 32 px floor cell)
export const rot = (x, y) => [x * Math.cos(TILT) + y * Math.sin(TILT), -x * Math.sin(TILT) + y * Math.cos(TILT)];
export const unrot = (u, v) => [u * Math.cos(TILT) - v * Math.sin(TILT), u * Math.sin(TILT) + v * Math.cos(TILT)];

// The tear's spine bends a little; its edges are jagged and breathe twice a loop.
export const spine = (v) => 2.4 * Math.sin(v * 2.3 + 0.4);
export const width = (v, ph, side) => {
  const base = W * Math.pow(Math.max(0, 1 - v * v), 0.75);
  const jag = 0.2 * Math.sin(v * 11 + side * 1.7) + 0.12 * Math.sin(v * 23 + side * 0.6) + 0.06 * Math.sin(v * 7 + TAU * ph * 2 + side);
  return base * (1 + jag) * (0.94 + 0.06 * Math.sin(TAU * ph * 2));
};

// Cracks branching out over the floor from the lip, fixed by the seed.
export function cracks(seed) {
  const r = new Rng(seed), out = [];
  for (let i = 0; i < 9; i++) {
    const v = r.range(-0.85, 0.85), side = r.sign(), [u0, v0] = [spine(v) + side * W * (1 - v * v) * 0.9, v * L];
    let [x, y] = unrot(u0, v0), a = Math.atan2(y, x) + r.range(-0.5, 0.5);
    const pts = [[x, y]], n = r.int(3, 5);
    for (let k = 0; k < n; k++) {
      a += r.range(-0.7, 0.7);
      const s = r.range(2.5, 4.5);
      x = Math.max(-29, Math.min(29, x + Math.cos(a) * s)); y = Math.max(-29, Math.min(29, y + Math.sin(a) * s));
      pts.push([x, y]);
    }
    out.push({ pts, delay: r.next() });
  }
  return out;
}

export function draw(p, t, o) {
  const C = fit(o.palette, 8), Lp = fit(o.lip, 5), ph = t / D, pulse = 0.8 + 0.2 * Math.sin(TAU * ph * 2);

  // Scorched stone round the tear, darkest near the lip.
  for (let y = -30; y <= 30; y++)
    for (let x = -30; x <= 30; x++) {
      const [u, v] = rot(x, y), d = (u / (W + 12)) ** 2 + (v / (L + 8)) ** 2;
      if (d < 1) p.blend(x, y, C[0], 0.55 * (1 - d) ** 1.2);
    }

  // Cracks in the floor, light running out along them from the tear.
  for (const { pts, delay } of cracks(o.seed)) {
    // a dark groove in the stone; a pulse of light runs out along it once a loop and dies
    p.polyline(pts, C[0]);
    for (let k = 0; k < pts.length - 1; k++) {
      const [ax, ay] = pts[k], [bx, by] = pts[k + 1], u = k / (pts.length - 1);
      const run = Math.pow(0.5 + 0.5 * Math.cos(TAU * (ph - u * 0.35 - delay)), 6) * (1 - 0.6 * u);
      if (k === 0) p.line(ax, ay, lerp2(ax, bx, 0.5), lerp2(ay, by, 0.5), Lp[1]);
      if (run > 0.25) p.line(ax, ay, bx, by, run > 0.7 ? Lp[3] : Lp[2], Math.min(1, run * 1.5));
      p.glowLine(ax, ay, bx, by, 3, Lp[2], 0.04 + 0.22 * run);
    }
  }

  // The tear: halo, crimson lip, white-hot rim, then the swirling void.
  p.shade(-30, -30, 30, 30, (x, y, out) => {
    const [u, vv] = rot(x, y), v = vv / L;
    if (Math.abs(v) > 1.05) return;
    const side = u > spine(v) ? 1 : -1, w = width(v, ph, side), e = Math.abs(u - spine(v)) - w;
    if (e > 2.5) return;
    // the hot spots slide along the lip, one each side
    const hot = Math.max(Math.exp(-((v - Math.sin(TAU * ph)) ** 2) / 0.03), Math.exp(-((v + Math.sin(TAU * ph + 1)) ** 2) / 0.03));
    if (e > 1.5) { if (hot > 0.3) { out.c = Lp[2]; out.a = 0.4 * hot; } return; }
    if (e > 0.5) { out.c = hot > 0.4 ? Lp[3] : Lp[1]; return; }
    if (e > -0.6) { out.c = hot > 0.5 ? WHITE : hot > 0.2 ? Lp[4] : Lp[3]; out.k = 0.2 + 0.4 * hot; return; }
    if (e > -1.6) { out.c = hot > 0.3 ? C[6] : C[5]; out.k = 0.1; return; }
    // inside: coordinates across the tear (s) and along it, spiral arms flowing inward twice a loop
    const s = (u - spine(v)) / Math.max(1, w), rr = Math.hypot(s, v * 1.2) || 1e-3, th = Math.atan2(v * 1.2, s);
    const z = -Math.log(Math.max(0.05, rr)), phi = 2 * th + 5 * z - TAU * 2 * ph;
    const arm = Math.pow(0.5 + 0.5 * Math.cos(phi), 8), thin = Math.pow(0.5 + 0.5 * Math.cos(phi + Math.PI), 14);
    const edge = smooth(0.55, 1, rr), deep = smooth(0.15, 0.6, rr);
    const st = 0.04 + 0.35 * edge + arm * (0.45 + 0.4 * edge) * deep + 0.25 * thin * deep;
    out.c = tone(C, st, x, y, 0, 0.6);
    if (arm > 0.8 && rr > 0.5) out.k = 0.12;
  });

  // Specks deep in the void, swept round and sinking.
  cycle(t, D, 10, o.seed * 3 + 1, (g) => g.range(0.8, 1.4), (age, life, r) => {
    const q = age / life, v = r.range(-0.7, 0.7) * (1 - q), side = r.sign();
    const u = spine(v) + side * width(v, ph, side) * 0.6 * (1 - ease.inQuad(q));
    const [x, y] = unrot(u, v * L);
    if (Math.sin(age * 37 + r.range(0, TAU)) > -0.2) p.dot(x, y, q < 0.4 ? C[6] : C[5], 1 - q);
  });

  // Motes and grit pulled in from round about, spinning as they fall.
  cycle(t, D, 30, o.seed * 3 + 2, (g) => g.range(0.5, 0.9), (age, life, r) => {
    const q = age / life, a0 = r.range(0, TAU), d0 = r.range(18, 29), spin = r.range(0.6, 1.4) * r.sign();
    const pos = (k) => { const a = a0 + spin * k * k, d = d0 * (1 - Math.pow(k, 1.3)); return [Math.cos(a) * d, Math.sin(a) * d]; };
    const [x, y] = pos(q), [x2, y2] = pos(Math.min(1, q + 0.02));
    const [u, vv] = rot(x, y), v = vv / L, inside = Math.abs(v) < 1 && Math.abs(u - spine(v)) < width(v, ph, 1);
    if (inside) return;
    const hue = r.next(), c = hue < 0.2 ? WHITE : hue < 0.5 ? C[6] : hue < 0.75 ? Lp[4] : C[5];
    mote(p, x, y, (x2 - x) / (0.02 * life), (y2 - y) / (0.02 * life), c, r.range(0.7, 1.1), Math.min(1, q * 5), 0.45);
  });

  // Twinkles over the lip, and the light of it on the stone.
  cycle(t, D, 5, o.seed * 3 + 4, 0.4, (age, life, r) => {
    const k = Math.sin((age / life) * Math.PI), v = r.range(-0.8, 0.8), side = r.sign();
    const [x, y] = unrot(spine(v) + side * (width(v, ph, side) + 1), v * L);
    sparkle(p, x, y, 1 + 2 * k, Lp[4], Lp[3], 0.4 * k);
  });
  p.glow(0, 0, 30, C[4], 0.16 * pulse);
  p.glow(0, 0, 16, C[5], 0.18 * pulse);
  for (let k = -2; k <= 2; k++) {
    const [x, y] = unrot(spine(k / 3), (k / 3) * L);
    p.glow(x, y, 9, Lp[3], 0.16 * pulse);
  }
}

const lerp2 = (a, b, k) => a + (b - a) * k;
