// Shared by core-gem.js and core-collapse.js: the stone dais and the cut ruby.
import { TAU, lerp, clamp, fit } from "../../core/index.js";

// Blood ramp dark to white, and a stone ramp faintly warmed by the crimson light.
export const BLOOD = ["#14030a", "#3a0714", "#700b1f", "#ad1229", "#e22338", "#ff5f62", "#ffc1b8", "#ffffff"];
export const STONE = ["#0c090f", "#1a1420", "#2b2230", "#413444", "#5d4c5c", "#857080"];
export const GOLD = fit(["#684239", "#b77c54", "#f2bf81", "#ffe5bd"], 4);
const SLATE = fit(["#101620", "#202936", "#34424e", "#4a5f68", "#708188"], 5);
const ENAMEL = fit(["#351921", "#611f2c", "#983341"], 3);

export const GEM_Y = -50, RAD = 17;
const SEAT_Y = -16;
const hexagon = (radius, height) => Array.from({ length: 6 }, (_, i) => {
  const angle = (i / 6) * TAU;
  return [Math.cos(angle) * radius, height, Math.sin(angle) * radius];
});
const VERTS = [...hexagon(0.55, 1.05), ...hexagon(1.03, 0.27), [0, -1.35, 0]];
const TABLE = [5, 4, 3, 2, 1, 0];
const CROWN = TABLE.map((i) => {
  const next = (i + 1) % 6;
  return [next, next + 6, i + 6, i];
});
const PAVILION = TABLE.map((i) => [12, i + 6, ((i + 1) % 6) + 6]);

// The altar borrows its blue-grey masonry, crimson panels and bronze trim from the dungeon tiles.
export function drawDais(p, ph, C, lit = 1, broken = 0) {
  const S = SLATE;
  // A soft cast shadow trails right, with its darkest pixels tucked under the stone.
  for (let y = -10; y <= 7; y++) {
    for (let x = -33; x <= 43; x++) {
      const distance = ((x - 5) / 38) ** 2 + ((y + 1) / 9) ** 2;
      p.blend(x, y, S[0], 0.76 * (1 - clamp(distance)) ** 1.4);
    }
  }
  // A clipped stone footprint and two dressed courses, with the right bevel in shadow.
  p.poly([[-23, -16], [23, -16], [32, -9], [32, -4], [25, 0], [-25, 0], [-32, -4], [-32, -9]], S[0]);
  p.poly([[-25, -8], [25, -8], [31, -5], [24, -1], [-24, -1], [-31, -5]], S[2]);
  p.line(-24, -1, 24, -1, S[3]);
  p.poly([[-27, -15], [-20, -11], [-20, -3], [-27, -6]], S[2]);
  p.poly([[20, -11], [27, -15], [27, -6], [20, -3]], S[1]);
  p.poly([[-20, -12], [20, -12], [20, -3], [-20, -3]], S[1]);
  p.line(-20, -3, 20, -3, S[3]);
  p.polyline([[-29, -5], [-24, -2], [-4, -2]], S[3]);
  p.polyline([[4, -2], [24, -2], [30, -5]], S[1]);
  p.polyline([[-26, -8], [-23, -6], [-21, -6]], S[3]);
  p.line(21, -5, 26, -7, S[0]);

  // Recessed enamel and an engraved diamond echo the stained glass and carpet borders.
  p.poly([[-15, -10], [15, -10], [17, -7], [15, -4], [-15, -4], [-17, -7]], S[0]);
  p.poly([[-14, -9], [14, -9], [15, -7], [14, -5], [-14, -5], [-15, -7]], ENAMEL[broken ? 0 : 1]);
  p.line(-12, -9, 12, -9, GOLD[0]);
  for (const side of [-1, 1]) {
    p.line(side * 19, -10, side * 19, -4, S[3]);
    p.line(side * 24, -10, side * 24, -8, GOLD[0]);
    p.px(side * 10, -7, GOLD[1]);
  }
  const pulse = lit * (0.75 + 0.25 * Math.cos(TAU * ph));
  p.poly([[0, -10], [4, -7], [0, -4], [-4, -7]], GOLD[broken ? 0 : 1]);
  p.poly([[0, -9], [2, -7], [0, -5], [-2, -7]], ENAMEL[broken ? 0 : 2]);

  // A chamfered top slab frames the carved ritual seal.
  p.poly([[-20, -22], [20, -22], [28, -16], [28, -14], [21, -10], [-21, -10], [-28, -14], [-28, -16]], S[0]);
  p.poly([[-19, -21], [19, -21], [27, -16], [21, -12], [-21, -12], [-27, -16]], S[3]);
  p.poly([[19, -21], [27, -16], [21, -12], [17, -13], [24, -16], [17, -20]], S[1]);
  p.poly([[-18, -20], [18, -20], [24, -16], [19, -13], [-19, -13], [-24, -16]], S[2]);
  p.polyline([[-26, -16], [-19, -21], [-4, -21]], S[4]);
  p.line(-2, -21, 18, -21, S[3]);
  p.line(-20, -11, 3, -11, S[3]);
  p.line(4, -11, 20, -11, S[2]);
  for (const side of [-1, 1]) {
    p.polyline([[side * 22, -16], [side * 17, -19], [side * 10, -19]], GOLD[0]);
    p.line(side * 20, -16, side * 15, -16, GOLD[1]);
  }
  p.poly([[0, SEAT_Y - 4], [10, SEAT_Y], [0, SEAT_Y + 4], [-10, SEAT_Y]], GOLD[broken ? 0 : 1]);
  p.poly([[0, SEAT_Y - 3], [8, SEAT_Y], [0, SEAT_Y + 3], [-8, SEAT_Y]], ENAMEL[broken ? 0 : 1]);
  if (!broken) {
    p.line(-4, SEAT_Y - 1, 0, SEAT_Y - 3, GOLD[2]);
    p.glow(0, SEAT_Y + 1, 16, C[4], 0.46 * pulse);
  } else {
    p.polyline([[-17, -21], [-13, -17], [-7, -16], [-4, -12], [-7, -8], [-5, -4]], S[0]);
    p.polyline([[19, -19], [12, -16], [13, -14], [8, -12]], S[0]);
    p.polyline([[18, -10], [14, -7], [16, -4], [12, -2]], S[0]);
  }
}

// Solid facets turn through a fixed light. Only the crown facing the light catches a sharp glint.
export function drawGem(p, spin, C, { cx = 0, cy = GEM_Y, heat = 0 } = {}) {
  const tilt = 0.48;
  const P3 = VERTS.map(([x, y, z]) => {
    const x1 = x * Math.cos(spin) + z * Math.sin(spin), z1 = -x * Math.sin(spin) + z * Math.cos(spin);
    const y2 = y * Math.cos(tilt) - z1 * Math.sin(tilt), z2 = y * Math.sin(tilt) + z1 * Math.cos(tilt);
    return [cx + x1 * RAD, cy - y2 * RAD, z2 * RAD];
  });
  for (const indices of [...PAVILION, ...CROWN, TABLE]) {
    const face = indices.map((i) => P3[i]);
    const [a, b, c] = face;
    const [ux, uy, uz] = [b[0] - a[0], a[1] - b[1], b[2] - a[2]];
    const [vx, vy, vz] = [c[0] - a[0], a[1] - c[1], c[2] - a[2]];
    const [nx, ny, nz] = [uy * vz - uz * vy, uz * vx - ux * vz, ux * vy - uy * vx];
    if (nz <= 0) continue;
    const light = (-0.58 * nx + 0.65 * ny + 0.49 * nz) / Math.hypot(nx, ny, nz);
    const shade = clamp(0.31 + 0.55 * light + heat);
    p.poly(face, C[1 + Math.round(shade * 5)]);
    if (face.length === 4 && light > 0.73) {
      const shoulder = face[3];
      p.line(lerp(a[0], shoulder[0], 0.14), lerp(a[1], shoulder[1], 0.14),
        lerp(a[0], shoulder[0], 0.76), lerp(a[1], shoulder[1], 0.76), C[6]);
    }
  }
}
