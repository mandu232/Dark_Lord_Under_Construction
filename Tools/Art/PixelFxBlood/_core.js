// Shared by core-gem.js and core-collapse.js: the rune plinth and the floating blood crystal.
import { TAU, lerp, clamp, fit, WHITE } from "../../core/index.js";
import { glyph } from "../arcane/_lib.js";

// Blood ramp dark to white, a cold stone ramp, bronze trim and the violet of the carved runes.
export const BLOOD = ["#14030a", "#3a0714", "#700b1f", "#ad1229", "#e22338", "#ff5f62", "#ffc1b8", "#ffffff"];
export const STONE = ["#0c090f", "#1a1420", "#2b2230", "#413444", "#5d4c5c", "#857080"];
export const GOLD = fit(["#684239", "#b77c54", "#f2bf81", "#ffe5bd"], 4);
export const RUNE = fit(["#12061f", "#2a0d45", "#4b1a78", "#7a2fb8", "#a95ae8", "#d39bff", "#f3dcff"], 7);
const SLATE = fit(["#0d0f17", "#1b1f2c", "#2c3141", "#424a5c", "#5f6a7c", "#8592a2"], 6);

// The crystal floats this high over the floor; RAD is its half-width scale.
export const GEM_Y = -47, RAD = 14;
export const TOP_Y = -15;   // the plinth's upper face
const SY = 0.32;            // how flat a floor circle looks from above

// A tall eight-sided bipyramid: a short crown, a wide girdle, a long point below.
const ring = (n, radius, height, turn = 0) => Array.from({ length: n }, (_, i) => {
  const a = ((i + turn) / n) * TAU;
  return [Math.cos(a) * radius, height, Math.sin(a) * radius];
});
const N = 8;
const VERTS = [...ring(N, 0.5, 0.95, 0.5), ...ring(N, 0.8, 0.15), [0, 1.75, 0], [0, -1.5, 0]];
const UP = 2 * N, DOWN = 2 * N + 1;
const FACES = [];
for (let i = 0; i < N; i++) {
  const j = (i + 1) % N;
  FACES.push([N + i, N + j, j], [N + i, j, i]);       // the shoulders, girdle to crown
  FACES.push([i, j, UP]);                              // the crown point
  FACES.push([N + j, N + i, DOWN]);                    // the long point below
}

// A solid cylinder seen from above and in front: a lit top face, a side lit from the left.
function drum(p, rx, ry, yTop, yBot, top, sides, edge) {
  p.shade(-rx - 1, yTop - ry - 1, rx + 1, yBot + ry + 1, (x, y, out) => {
    const u = x / rx;
    if (Math.abs(u) > 1) return;
    const h = ry * Math.sqrt(1 - u * u), dTop = ((y - yTop) / ry) ** 2 + u * u;
    if (dTop <= 1) {
      out.c = dTop > 0.8 && y < yTop ? edge : top;
      return;
    }
    if (y < yTop || y > yBot + h) return;
    out.c = y > yBot + h - 1 ? sides[0] : u < -0.45 ? sides[3] : u < 0.15 ? sides[2] : sides[1];
  });
}

// Points on a flat floor circle at angle g, radius r, centred on (0, y).
const at = (g, r, y) => [Math.cos(g) * r, y + Math.sin(g) * r * SY];

// The plinth: two round drums of dressed stone, bronze bands, a circle of runes cut into each top and lit from inside.
export function drawDais(p, ph, C, lit = 1, broken = 0) {
  const S = SLATE, R = RUNE, pulse = lit * (0.75 + 0.25 * Math.cos(TAU * ph));
  // A soft shadow pooled under the stone, heavier to the lower right.
  for (let y = -12; y <= 6; y++)
    for (let x = -40; x <= 44; x++) {
      const d = ((x - 4) / 40) ** 2 + ((y + 2) / 9) ** 2;
      p.blend(x, y, S[0], 0.7 * (1 - clamp(d)) ** 1.4);
    }
  drum(p, 31, 9, -7, -2, S[2], S, S[4]);
  drum(p, 22, 7, TOP_Y, -7, S[3], S, S[5]);
  // Bronze bands round the front of each drum.
  for (const [r, ry, y] of [[31, 9, -2], [22, 7, -7]])
    for (let g = 0.15; g < Math.PI - 0.15; g += 0.02) {
      const x = Math.cos(g) * r, yy = y + Math.sin(g) * ry;
      p.px(x, yy - 1, Math.cos(g) > 0.35 ? GOLD[0] : GOLD[1]);
    }
  // Studs on the lower band, the lit ones catching a highlight.
  for (let k = 0; k < 7; k++) {
    const g = 0.35 + (k / 6) * (Math.PI - 0.7), x = Math.cos(g) * 31, y = -3 + Math.sin(g) * 9;
    p.px(x, y - 1, Math.cos(g) < -0.2 ? GOLD[3] : GOLD[2]);
  }
  // The rune circle on the lower step, between the two drums: dark grooves, glowing glyphs.
  const glow = broken ? 0 : pulse;
  for (let k = 0; k < 10; k++) {
    const g = ((k + 0.5) / 10) * TAU;
    if (Math.sin(g) < 0.25) continue;     // only the front arc reads; the sides foreshorten to clutter
    const [x, y] = at(g, 27, -7);
    const hot = glow > 0 && ((k + Math.floor(ph * 10)) % 5 === 0);
    glyph(p, k, x, y + 1, broken ? S[1] : hot ? R[6] : R[4], 1, broken ? 0 : R[1]);
    if (glow > 0) p.glow(x, y - 1, hot ? 8 : 5, R[4], (hot ? 0.45 : 0.18) * glow);
  }
  // The seal on the top face: a double ring, a star of lines and a socket of light under the crystal.
  for (const [r, c] of [[18, broken ? S[1] : R[3]], [12, broken ? S[1] : R[4]]])
    for (let g = 0; g < TAU; g += 0.025) {
      const [x, y] = at(g, r, TOP_Y);
      p.px(x, y, Math.sin(g) < 0 ? scaleStep(c, S, broken) : c);
    }
  for (let k = 0; k < 5; k++) {
    const a0 = (k / 5) * TAU - Math.PI / 2, a1 = ((k + 2) / 5) * TAU - Math.PI / 2;
    const [x0, y0] = at(a0, 12, TOP_Y), [x1, y1] = at(a1, 12, TOP_Y);
    p.line(x0, y0, x1, y1, broken ? S[2] : R[2]);
  }
  p.ellipse(0, TOP_Y, 5, 1.8, broken ? S[1] : C[2]);
  p.ellipse(0, TOP_Y, 3, 1, broken ? S[0] : C[5]);
  if (!broken) {
    p.glow(0, TOP_Y, 26, R[3], 0.22 * pulse);
    p.glow(0, TOP_Y, 10, C[4], 0.5 * pulse);
  } else {
    p.polyline([[-19, -16], [-13, -13], [-8, -12], [-6, -9], [-9, -5], [-7, -1]], S[0]);
    p.polyline([[20, -15], [12, -12], [13, -10], [8, -7]], S[0]);
    p.polyline([[26, -6], [21, -4], [23, -2], [18, 2]], S[0]);
  }
}
// Far halves of the seal sit a step darker, as if sunk in the stone.
const scaleStep = (c, S, broken) => (broken ? S[0] : RUNE[RUNE.indexOf(c) - 1] ?? c);

// Solid facets turn through a fixed light from the upper left; a hot heart shines through the faces turned away.
export function drawGem(p, spin, C, { cx = 0, cy = GEM_Y, heat = 0 } = {}) {
  const tilt = 0.32;
  // Turned and tipped in 3D, y up and z towards us, about the crystal's own centre.
  const V3 = VERTS.map(([x, y, z]) => {
    const x1 = x * Math.cos(spin) + z * Math.sin(spin), z1 = -x * Math.sin(spin) + z * Math.cos(spin);
    return [x1, y * Math.cos(tilt) - z1 * Math.sin(tilt), y * Math.sin(tilt) + z1 * Math.cos(tilt)];
  });
  const faces = FACES.map((idx) => {
    const [a, b, c] = idx.map((i) => V3[i]);
    const u = [b[0] - a[0], b[1] - a[1], b[2] - a[2]], v = [c[0] - a[0], c[1] - a[1], c[2] - a[2]];
    let n = [u[1] * v[2] - u[2] * v[1], u[2] * v[0] - u[0] * v[2], u[0] * v[1] - u[1] * v[0]];
    // the crystal is convex round the origin, so the outward normal points away from it whatever the winding
    const mid = [0, 1, 2].map((k) => (a[k] + b[k] + c[k]) / 3);
    if (n[0] * mid[0] + n[1] * mid[1] + n[2] * mid[2] < 0) n = n.map((q) => -q);
    return { f: [a, b, c].map(([x, y]) => [cx + x * RAD, cy - y * RAD]), n, z: mid[2] };
  }).filter(({ n }) => n[2] > 0).sort((a, b) => a.z - b.z);
  const P3 = V3.map(([x, y]) => [cx + x * RAD, cy - y * RAD]);
  // A dark outline first, so the silhouette holds against the glow.
  for (const [dx, dy] of [[-1, 0], [1, 0], [0, -1], [0, 1]])
    for (const { f } of faces) p.poly(f.map(([x, y]) => [x + dx, y + dy]), C[0]);
  for (const { f, n } of faces) {
    const [nx, ny, nz] = n, L = Math.hypot(nx, ny, nz);
    const light = (-0.55 * nx + 0.6 * ny + 0.58 * nz) / L;
    // Faces turned from the light still show the red fire inside.
    const shade = clamp(0.12 + 0.78 * light + heat + (light < 0.1 ? 0.1 : 0));
    p.poly(f, C[1 + Math.round(shade * 5)]);
    if (light > 0.7) p.line(f[0][0], f[0][1], lerp(f[0][0], f[2][0], 0.7), lerp(f[0][1], f[2][1], 0.7), C[7]);
  }
  // The heart of the crystal and a hard glint on the crown.
  p.glow(cx, cy, RAD * 0.9, C[5], 0.32 + heat);
  const [tx, ty] = P3[UP];
  p.px(tx, ty + 1, WHITE);
}
