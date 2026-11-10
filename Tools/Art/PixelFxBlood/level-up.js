// Level up, sized for the game's small top-down lord (about 12 x 20 px). The pillar and climbing rings follow
// pixelfx's ui/level-up; the floor runes and colours are the dungeon core's. No text: a rank sigil says it.
import { fit, clamp, ease, seg, smooth, rng, rand, sparkle, TAU, WHITE } from "../../core/index.js";
import { glyph, mote } from "../arcane/_lib.js";
import { RUNE } from "./_core.js";

const D = 1.6, REL = 0.18, R = 15, SY = 0.4, TOP = 46;

export const meta = {
  name: "Level up",
  desc: "A violet rune circle opens round the lord's feet, a blood-gold pillar bursts up through them with rings climbing it, a gold rank sigil rises overhead and breaks into sparkles.",
  tags: ["level", "reward", "blood", "rune", "one-shot"],
  size: [64, 80],
  origin: [32, 66],
  duration: D,
  loop: false,
  fps: 24,
  options: { palette: ["#1a0308", "#4a0612", "#8a0a1e", "#c8243c", "#ee5a3a", "#ffb84d", "#ffeaa8", "#ffffff"], seed: 9 },
};

const at = (g, r = R) => [Math.cos(g) * r, Math.sin(g) * r * SY];

// The floor circle: drawn in as a sweep while power gathers, runes lit, then thinning away. far = the half behind the lord.
function circle(p, t, far) {
  const sweep = TAU * ease.outCubic(seg(t, 0, REL)), fade = 1 - seg(t, 0.95, 1.4);
  if (fade <= 0) return;
  for (let g = 0; g < sweep; g += 0.03) {
    const a = g - Math.PI / 2;
    if (Math.sin(a) < 0 !== far) continue;
    const [x, y] = at(a);
    p.dot(x, y, far ? RUNE[4] : RUNE[5], fade);
    if (g % 0.5 < 0.03) p.dot(...at(a, R - 4), RUNE[3], fade);
  }
  for (let k = 0; k < 6; k++) {
    const a = (k / 6) * TAU - Math.PI / 2 + 0.3 * t;
    if (Math.sin(a) < 0 !== far || (k / 6) * TAU > sweep) continue;
    const [x, y] = at(a, R + 4), hot = t > REL && t < REL + 0.12;
    glyph(p, k, x, y + 1, hot ? WHITE : far ? RUNE[4] : RUNE[6], fade, RUNE[1]);
    p.glow(x, y - 1, 5, RUNE[4], 0.3 * fade);
  }
}

// The pillar: fast up, then it narrows to a thread and goes; strands of light scroll up inside it.
function pillar(p, t, C) {
  if (t < REL) return;
  // a see-through column, so the lord stays visible inside it: translucent fill, solid strands and edges only
  const a = t - REL, h = TOP * ease.outExpo(clamp(a / 0.12)), w = 4.5 * (1 + 0.5 * Math.exp(-a * 12)) * (1 - smooth(0.25, 0.85, a));
  if (w < 0.4) return;
  for (let x = -Math.ceil(w); x <= Math.ceil(w); x++) {
    const u = Math.abs(x + 0.5) / w;
    if (u > 1) continue;
    for (let y = 0; y > -h; y--) {
      // each column carries its own streaks rising at its own speed
      const v = -y / TOP, fade = 1 - v * v, sp = 5 + 5 * rand(x, 2);
      const strand = ((y * 0.07 + a * sp + rand(x, 1)) % 1 + 1) % 1 < 0.22;
      p.blend(x, y, u < 0.4 ? C[6] : C[4], 0.4 * fade);
      if (strand && u < 0.7 && fade > 0.25) p.px(x, y, u < 0.3 && fade > 0.6 ? WHITE : C[6]);
      else if (u > 0.8 && fade > 0.55) p.blend(x, y, C[3], 0.7 * fade);
    }
  }
  p.glow(0, -h * 0.4, 18, C[4], 0.35 * (1 - smooth(0.2, 0.9, a)));
  p.glow(0, 0, 10, C[5], 0.5 * Math.exp(-a * 5));
}

// Two rings climb the pillar and thin out at the top.
function rings(p, t, C, far) {
  for (let i = 0; i < 2; i++) {
    const k = seg(t, REL + 0.04 + i * 0.12, REL + 0.5 + i * 0.12);
    if (k <= 0 || k >= 1) continue;
    const y = -TOP * 0.85 * ease.outQuad(k), r = 9 - 3 * k;
    for (let g = 0; g < TAU; g += 0.05) {
      if (Math.sin(g) < 0 !== far) continue;
      p.dot(Math.cos(g) * r, y + Math.sin(g) * r * SY, far ? C[5] : C[6], 1 - k * k);
    }
  }
}

// The rank sigil: a gold double chevron that pops in over the head, rises, then breaks into sparkles.
function sigil(p, t, C) {
  const k = seg(t, REL + 0.12, 1.15);
  if (k <= 0) return;
  const y = -30 - 10 * ease.outQuad(k), s = k < 0.08 ? 1 + 0.3 * (1 - k / 0.08) : 1;
  if (k < 1) {
    for (const dy of [0, 4]) {
      const pts = [[-5 * s, y + dy + 3 * s], [0, y + dy - 2 * s], [5 * s, y + dy + 3 * s]];
      p.polyline(pts.map(([x, yy]) => [x, yy + 1]), C[1]);
      p.polyline(pts, dy ? C[5] : C[6]);
      p.polyline(pts.map(([x, yy]) => [x, yy - 1]), dy ? C[4] : WHITE);
    }
    p.glow(0, y + 2, 9, C[5], 0.45);
    return;
  }
  // the pop
  const b = seg(t, 1.15, 1.45);
  if (b >= 1) return;
  for (let i = 0; i < 6; i++) {
    const a = (i / 6) * TAU, d = 4 + 8 * ease.outCubic(b);
    sparkle(p, Math.cos(a) * d, y + 2 + Math.sin(a) * d * 0.7, 1 + (b < 0.5 ? 1 : 0), i % 2 ? WHITE : C[6], C[5], 0.3 * (1 - b));
  }
}

export function draw(p, t, o) {
  const C = fit(o.palette, 8);
  // Power gathers: motes slide in across the floor to the feet.
  for (let i = 0; i < 10; i++) {
    const r = rng(o.seed, i), born = r.range(0, 0.08), life = REL - born, q = (t - born) / life;
    if (q < 0 || q >= 1) continue;
    const g = r.range(0, TAU), d = (R + 10) * (1 - ease.inCubic(q)), [x, y] = at(g, d);
    mote(p, x, y, -Math.cos(g) * 80, -Math.sin(g) * 30, r.chance(0.4) ? RUNE[6] : C[5], 0.6, 1, 0.3);
  }
  circle(p, t, true);
  rings(p, t, C, true);
  pillar(p, t, C);
  rings(p, t, C, false);
  circle(p, t, false);
  // Embers rise and sway off the circle while the light holds.
  for (let i = 0; i < 14; i++) {
    const r = rng(o.seed + 1, i), born = REL + r.range(0, 0.7), life = r.range(0.5, 0.8), q = (t - born) / life;
    if (q < 0 || q >= 1) continue;
    const [x0, y0] = at(r.range(0, TAU), r.range(3, R));
    p.dot(x0 + Math.sin(q * 6 + i) * 1.5, y0 - q * r.range(16, 30), q < 0.4 ? C[6] : r.chance(0.5) ? C[4] : RUNE[5], Math.sin(q * Math.PI) * 1.4);
  }
  if (t >= REL && t < REL + 0.05) p.circle(0, -2, 4, WHITE);
  sigil(p, t, C);
}
