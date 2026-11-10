import { lerp, TAU, cycle, fit, ease, smooth, sparkle, WHITE } from "../../core/index.js";
import { mote, glyph } from "../arcane/_lib.js";
import { BLOOD, GOLD, RUNE, GEM_Y, RAD, TOP_Y, drawDais, drawGem } from "./_core.js";

export const meta = {
  name: "Dungeon core",
  desc: "A blood crystal floats over a rune-cut plinth inside two turning rings of violet runes; motes rise from the seal into its heart.",
  tags: ["blood", "arcane", "core", "crystal", "magic", "loop"],
  size: [96, 112],
  origin: [48, 100],
  duration: 3,
  loop: true,
  fps: 20,
  options: { palette: BLOOD, seed: 11 },
};

// Two tilted rings of runes around the crystal, turning opposite ways. Each turns two rune slots a loop, and the
// runes alternate in pairs, so the loop has no seam.
const RINGS = [
  { r: 25, sy: 0.3, tilt: 0.2, dy: 5, turn: 1 / 3, glyphs: 6, first: 0, runes: true },
  { r: 19, sy: 0.24, tilt: -0.32, dy: -7, turn: -1 / 3, glyphs: 6, first: 3, runes: false },
];

function ringPoint(R, g, cy) {
  const x = Math.cos(g) * R.r, y = Math.sin(g) * R.r * R.sy;
  return [x * Math.cos(R.tilt) - y * Math.sin(R.tilt), cy + R.dy + x * Math.sin(R.tilt) + y * Math.cos(R.tilt)];
}

function rings(p, ph, cy, far) {
  for (const R of RINGS) {
    const spin = TAU * R.turn * ph;
    // a dashed band of light, the dashes running with the spin
    for (let g = 0; g < TAU; g += 0.018) {
      if (Math.sin(g) < 0 !== far) continue;
      const dash = Math.sin((g - spin) * R.glyphs * 2) > (R.runes ? -0.2 : 0.3);
      if (!dash) continue;
      const [x, y] = ringPoint(R, g, cy);
      p.px(x, y, far ? RUNE[3] : Math.cos(g + 2.2) > 0.4 ? RUNE[6] : RUNE[5]);
    }
    // runes riding the band
    for (let k = 0; R.runes && k < R.glyphs; k++) {
      const g = spin + ((k + 0.5) / R.glyphs) * TAU;
      if (Math.sin(g) < 0 !== far) continue;
      const [x, y] = ringPoint(R, g, cy);
      glyph(p, R.first + (k % 2), x, y + 1, far ? RUNE[3] : RUNE[6], 1, far ? RUNE[1] : RUNE[2]);
      p.glow(x, y - 1, far ? 5 : 8, RUNE[4], far ? 0.15 : 0.35);
    }
  }
}

export function draw(p, t, o) {
  const C = fit(o.palette, 8), ph = t / meta.duration, pulse = 0.85 + 0.15 * Math.sin(TAU * ph * 2);
  const cy = GEM_Y + 2 * Math.sin(TAU * ph);
  // A faint column of light stands from the seal up through the crystal.
  for (let h = 0; h < 70; h += 7) p.glow(0, TOP_Y - h, 18, RUNE[3], 0.08 * pulse * (1 - h / 90));
  drawDais(p, ph, C);
  rings(p, ph, cy, true);
  // Motes lift off the seal and are drawn up into the crystal's point.
  cycle(t, meta.duration, 18, o.seed, (r) => r.range(0.9, 1.6), (age, life, r) => {
    const q = age / life, g = r.range(0, TAU), r0 = r.range(6, 18);
    const x0 = Math.cos(g) * r0, y0 = TOP_Y + Math.sin(g) * r0 * 0.32;
    const e = ease.inQuad(q), x = lerp(x0, 0, e), y = lerp(y0, cy + RAD * 1.3, ease.outQuad(q));
    const a = Math.min(1, q * 5) * (1 - smooth(0.8, 1, q));
    mote(p, x, y, -x0 * 2, -40, r.chance(0.3) ? RUNE[6] : r.chance(0.5) ? C[6] : RUNE[5], r.range(0.5, 0.9), a, 0.3);
  });
  p.glow(0, cy, RAD * 2.4, C[3], 0.24 * pulse);
  drawGem(p, (TAU / 4) * ph, C, { cy });
  // A heart of light inside the crystal, beating twice a loop and showing through the glass.
  const beat = 0.5 + 0.5 * Math.sin(TAU * ph * 2), hy = cy + 2;
  const hs = Math.round(4 + 2 * beat);
  p.poly([[0, hy - hs], [hs / 2, hy], [0, hy + hs - 1], [-hs / 2, hy]], C[5]);
  p.poly([[0, hy - hs + 2], [1, hy], [0, hy + hs - 3], [-1, hy]], beat > 0.5 ? WHITE : C[6]);
  p.glow(0, hy, 10, C[6], 0.18 + 0.2 * beat);
  rings(p, ph, cy, false);
  // Twinkles hang in the air round it; embers of bronze light fall off the rim.
  cycle(t, meta.duration, 8, o.seed + 3, 0.5, (age, life, r) => {
    const k = Math.sin((age / life) * Math.PI), x = r.range(-30, 30), y = r.range(cy - 26, cy + 20);
    sparkle(p, x, y, 1 + 2 * k, r.chance(0.5) ? RUNE[6] : C[6], RUNE[4], 0.35 * k);
  });
  cycle(t, meta.duration, 6, o.seed + 5, (r) => r.range(0.8, 1.4), (age, life, r) => {
    const k = age / life, x = r.range(-20, 20), y = lerp(TOP_Y - 2, TOP_Y - 26, ease.outQuad(k));
    p.dot(x + Math.sin(k * 6) * 2, y, GOLD[3], Math.sin(k * Math.PI));
  });
}
