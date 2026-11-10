// Legacy procedural prototype. Do not export it over the hand-finished Rowen game atlas.
// Same 96 x 96 frame and centred pivot as the sword soldier, feet 10 px below centre, facing right.
import { fit, hex, TAU, WHITE } from "../../core/index.js";

const FEET = 10, FPS = 12;
const HOLY = fit(["#2a1a05", "#6a4a0c", "#b8860b", "#e8b923", "#ffd95a", "#fff0a8", "#fffbe6"], 7);
const K = Object.fromEntries(Object.entries({
  o: "#1a1626", s: "#f1c7a0", S: "#c9906c", h: "#f2c14e", H: "#fff0a8", d: "#b9862c", E: "#2a2050", m: "#7a1a2a",
  A: "#e6ecf3", a: "#a9b4c4", n: "#6f7a8e", T: "#3a6fd0", t: "#6aa0f0", c: "#24418a", g: "#f2c14e", G: "#fff1a0",
  B: "#eef6ff", b: "#9fb3cc",
}).map(([k, v]) => [k, hex(v)]));

const HEAD = [
  "...ooooo..",
  "..oHHhhhoo",
  ".oHhhhhhho",
  "ohhhhhssso",
  "ohhhhssEso",
  "ohhdhsssso",
  ".ohdsssso.",
  "..oooooo..",
];
const ROAR = HEAD.map((r, i) => (i === 4 ? "ohhhhsEsso" : i === 5 ? "ohhdhssmso" : r));
const BOWED = HEAD.map((r, i) => (i === 4 ? "ohhhhsssso" : i === 5 ? "ohhdhsEsso" : r));
const TORSO = [
  ".oAAAAo.",
  "oAaTTAAo",
  "oaTtTTao",
  "oaTTTtao",
  "oggGgggo",
  ".oTTTTo.",
];

// How every pose departs from standing at rest. sa = sword angle (0 points forward, + turns down).
const REST = { lift: 0, crouch: 0, lean: 0, stride: 0, sa: 1.0, cape: 0.2, glow: 0, head: HEAD, alpha: 1, trail: null, motes: 0 };
const P = (o) => ({ ...REST, ...o });
const range = (n, f) => Array.from({ length: n }, (_, i) => f(i, n > 1 ? i / (n - 1) : 0));
const lerp = (a, b, k) => a + (b - a) * k;

export const CLIPS = [
  ["idle", range(6, (i) => P({ crouch: [0, 0, 1, 1, 1, 0][i], cape: 0.2 + 0.1 * Math.sin((i / 6) * TAU), sa: 1.0 + 0.05 * [0, 0, 1, 1, 1, 0][i] }))],
  ["run", range(8, (i) => { const s = Math.sin((i / 8) * TAU); return P({ stride: s, crouch: Math.abs(s) > 0.7 ? 0 : 1, lean: 1, sa: 2.4, cape: 0.7 + 0.2 * s }); })],
  ["windup", range(4, (i) => P({ crouch: 2, lean: i % 2 ? -2 : -1, sa: 2.8, cape: 0.5, glow: 0.4 + 0.2 * (i % 2) }))],
  ["dash", range(4, (i) => P({ crouch: 1, lean: 2, stride: i % 2 ? 1 : -1, sa: 0.15, cape: 1, glow: 0.6 }))],
  ["cleave", range(8, (i, u) => i < 5 ? P({ crouch: 1, sa: lerp(1.0, -2.0, i / 4), glow: i / 5 })
    : i === 5 ? P({ crouch: 1, sa: -2.0, glow: 0.9 })
    : i === 6 ? P({ crouch: 2, lean: 2, sa: 1.2, glow: 1, trail: [-2.0, 1.2], cape: 0.8 })
    : P({ crouch: 2, lean: 2, sa: 1.4, glow: 0.5, trail: [0.4, 1.4], cape: 0.6 }))],
  ["cast", range(6, (i) => P({ sa: -1.57, glow: 0.8 + 0.2 * Math.sin((i / 6) * TAU), cape: 0.4 + 0.1 * Math.sin((i / 6) * TAU), motes: 2 }))],
  ["roar", range(8, (i) => i < 3 ? P({ crouch: 2, lean: -1, sa: 1.6, glow: i / 3 })
    : P({ lean: -1, head: ROAR, sa: -0.6, glow: 1, cape: 0.9 + 0.1 * (i % 2), motes: 3 }))],
  ["leap", range(10, (i) => [
    P({ crouch: 2, sa: 1.8 }), P({ crouch: 3, sa: 2.0, glow: 0.3 }),
    P({ lift: 6, stride: 0.6, sa: 1.2, cape: 0.8 }), P({ lift: 14, sa: 0.2, cape: 1 }),
    P({ lift: 22, sa: -1.2, cape: 1, glow: 0.5 }), P({ lift: 26, sa: -2.0, cape: 1, glow: 0.8 }),
    P({ lift: 26, sa: -2.0, cape: 0.9, glow: 1 }), P({ lift: 20, sa: -1.0, cape: 0.6, glow: 1, trail: [-2.0, -1.0] }),
    P({ lift: 10, sa: 0.6, cape: 0.4, glow: 1, trail: [-1.0, 0.6] }), P({ lift: 3, crouch: 1, sa: 1.3, glow: 1 }),
  ][i])],
  ["slam", range(4, (i) => P({ crouch: i === 0 ? 3 : 2, lean: 1, sa: 1.57, glow: [1, 0.8, 0.5, 0.3][i], cape: [0.9, 0.5, 0.3, 0.2][i], motes: i < 2 ? 3 : 0 }))],
  ["wave", range(8, (i) => i < 4 ? P({ crouch: 1, sa: lerp(1.0, 2.9, i / 3), glow: i / 4 })
    : i === 4 ? P({ crouch: 2, lean: -1, sa: 2.9, glow: 1 })
    : i === 5 ? P({ lean: 2, sa: -0.3, glow: 1, trail: [2.9, -0.3], cape: 0.9 })
    : P({ lean: 2, sa: i === 6 ? -0.1 : 0.3, glow: i === 6 ? 0.6 : 0.3, cape: 0.6 }))],
  ["recover", range(6, (i) => P({ crouch: i % 3 === 0 ? 3 : 2, lean: 1, sa: 1.45, cape: 0.1 }))],
  ["die", range(8, (i) => i < 2 ? P({ crouch: 2 + i, sa: 1.5 })
    : P({ crouch: 4, sa: 1.57, head: i >= 4 ? BOWED : HEAD, glow: Math.max(0, 0.6 - (i - 2) * 0.15), cape: 0.1, motes: i < 6 ? 2 : 0 }))],
  ["attack", range(6, (i) => P([{ sa: -0.8 }, { crouch: 1, sa: -1.2, glow: 0.3 }, { lean: 1, sa: 0.2, trail: [-1.2, 0.2], glow: 0.5 },
    { lean: 1, sa: 1.2, trail: [0.2, 1.2], glow: 0.3 }, { lean: 1, sa: 1.3 }, { sa: 1.1 }][i]))],
  ["hit", range(3, (i) => P({ lean: [-2, -1, 0][i], crouch: 1, sa: 1.6 }))],
];
const FRAMES = CLIPS.flatMap(([, poses]) => poses);

export const meta = {
  name: "Rowen",
  desc: "Rowen, the west-wind hero, every animation on one sheet: idle, run, charge, cleave, judgement, roar, leap and slam, sword wave, recovery, death, attack, hit.",
  tags: ["character", "hero", "boss", "sheet"],
  size: [96, 96],
  origin: [48, 48],
  duration: FRAMES.length / FPS,
  loop: false,
  fps: FPS,
  bg: "dark",
  options: { seed: 3 },
};

function sword(p, x, y, a, glow, A) {
  const dx = Math.cos(a), dy = Math.sin(a), px = -dy, py = dx, L = 9;
  p.line(x - dx * 2, y - dy * 2, x, y, K.n, A);                                   // grip
  p.line(x - px * 1.5, y - py * 1.5, x + px * 1.5, y + py * 1.5, K.g, A);         // crossguard
  p.line(x + dx, y + dy, x + dx * L, y + dy * L, K.B, A);                          // blade
  p.line(x + dx + px, y + dy + py, x + dx * (L - 1) + px, y + dy * (L - 1) + py, K.b, A);
  p.px(x + dx * (L + 1), y + dy * (L + 1), WHITE);
  if (glow > 0) p.glowLine(x + dx, y + dy, x + dx * L, y + dy * L, 4, HOLY[4], 0.35 * glow * A);
}

export function draw(p, t, o) {
  const f = Math.min(FRAMES.length - 1, Math.round(t * FPS)), q = FRAMES[f], A = q.alpha;
  const ground = FEET, gy = ground - q.lift, legLen = Math.max(1, 4 - Math.min(3, q.crouch)), hipY = gy - legLen;
  const L = q.lean, torsoY = hipY - 6, headY = torsoY - 8 + (q.crouch >= 3 ? 1 : 0);

  // ground shadow, smaller as he rises
  const sw = Math.max(2, 6 - q.lift / 6);
  for (let x = -sw; x <= sw; x++) p.blend(x, ground + 1, K.o, 0.35);
  for (let x = -sw + 1; x < sw; x++) p.blend(x, ground + 2, K.o, 0.2);

  // cape behind, streaming back as he moves
  const c = q.cape;
  p.poly([[L - 3, torsoY + 1], [L - 1, torsoY + 1], [L - 2 - c, gy - 1], [L - 5 - c * 4, gy - 1 - c * 4]], K.c, A);
  p.line(L - 3, torsoY + 1, L - 5 - c * 4, gy - 1 - c * 4, K.T, A);

  // legs: back then front, stride swings them
  const step = Math.round(2 * q.stride);
  for (const [x, col] of [[-1 - step, K.n], [1 + step, K.a]]) {
    const lift = q.lift > 0 && q.lift < 24 ? 1 : 0;
    p.rect(x - 1, hipY, 2, legLen - lift, col, A);
    p.line(x - 1, gy - lift, x + 1, gy - lift, K.o, A);
  }
  p.sprite(TORSO, K, L - 4, torsoY, { a: A });
  p.sprite(q.head, K, L - 5, headY, { a: A });

  // the sword arm, then light trails and motes
  const sx = L + 2, sy = torsoY + 1, hx = sx + Math.round(Math.cos(q.sa) * 3), hy = sy + Math.round(Math.sin(q.sa) * 3);
  p.line(sx, sy, hx, hy, K.A, A);
  // a swept band of light from where the blade was to where it is now, brightest at the blade
  if (q.trail) {
    const [a0, a1] = q.trail;
    for (let k = 0; k <= 1; k += 0.04) {
      const a = a0 + (a1 - a0) * k;
      for (let r = 6; r <= 11; r++) p.blend(sx + Math.cos(a) * r, sy + Math.sin(a) * r, k > 0.7 ? HOLY[6] : HOLY[5], (0.1 + 0.5 * k) * A);
      if (k > 0.4) p.px(sx + Math.cos(a) * 12, sy + Math.sin(a) * 12, k > 0.8 ? WHITE : HOLY[6]);
    }
  }
  sword(p, hx, hy, q.sa, q.glow, A);
  p.px(hx, hy, K.a);
  for (let i = 0; i < q.motes; i++) {
    const k = ((f * 0.37 + i * 0.41) % 1), x = (i % 2 ? 1 : -1) * (3 + i * 2) + L, y = gy - 4 - k * 18;
    p.px(x, y, k < 0.5 ? HOLY[6] : HOLY[4]);
    p.glow(x, y, 3, HOLY[4], 0.3 * (1 - k));
  }
}
