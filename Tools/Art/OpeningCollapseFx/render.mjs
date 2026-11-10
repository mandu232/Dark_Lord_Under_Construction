// Tutorial corridor collapse: rock chunks, their shadow and a billowing dust cloud, drawn pixelfx-style in code.
// Standalone: `node Tools/Art/OpeningCollapseFx/render.mjs [previewDir]`.
// Writes Assets/Resources/OpeningCollapse/{rocks,dust}.png; OpeningCollapseFx slices them by the sizes below (16 px per unit).
import { writeFileSync, mkdirSync } from "node:fs";
import { deflateSync } from "node:zlib";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

export const ROCK = 16, ROCKS = 8, DUST_W = 64, DUST_H = 48, DUST_FRAMES = 12;
const OUT = join(dirname(fileURLToPath(import.meta.url)), "../../../Assets/Resources/OpeningCollapse");
const TAU = Math.PI * 2;
const clamp = (v, a = 0, b = 1) => Math.min(b, Math.max(a, v));
const hex = (h) => [1, 3, 5].map((i) => parseInt(h.slice(i, i + 2), 16) / 255);
const ramp = (list) => list.map(hex);
const outCubic = (k) => 1 - (1 - k) ** 3;
const BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]].map((r) => r.map((v) => (v + .5) / 16));
function rng(seed) {
  let s = seed >>> 0;
  const next = () => { s = (s + 0x6d2b79f5) >>> 0; let t = s; t = Math.imul(t ^ (t >>> 15), t | 1); t ^= t + Math.imul(t ^ (t >>> 7), t | 61); return ((t ^ (t >>> 14)) >>> 0) / 4294967296; };
  return { next, range: (a, b) => a + (b - a) * next() };
}
// The dungeon's warm brown stone, dark to light, and the dust it throws up.
const STONE = ramp(["#140c0a", "#2e2018", "#4c3828", "#6e5440", "#94765a", "#b89a78"]);
const DUST = ramp(["#3a2c22", "#5c4836", "#806650", "#a68a6c", "#c8b092"]);

class Painter {
  constructor(w, h) { Object.assign(this, { w, h }); this.buf = new Float32Array(w * h * 4); }
  blend(x, y, c, a) {
    const ix = Math.round(x), iy = Math.round(y);
    a = clamp(a);
    if (a <= 0 || ix < 0 || iy < 0 || ix >= this.w || iy >= this.h) return;
    const i = (iy * this.w + ix) * 4, da = this.buf[i + 3], oa = a + da * (1 - a);
    for (let k = 0; k < 3; k++) this.buf[i + k] = (c[k] * a + this.buf[i + k] * da * (1 - a)) / oa;
    this.buf[i + 3] = oa;
  }
  px(x, y, c) { this.blend(x, y, c, 1); }
  bytes() {
    const out = Buffer.alloc(this.w * this.h * 4);
    for (let i = 0; i < this.w * this.h * 4; i++) out[i] = Math.round(clamp(this.buf[i]) * 255);
    return out;
  }
}

// An irregular chunk: noisy radius outline, lit from the top-left in hard ramp steps, a dark rim and a crack.
function rock(p, ox, oy, seed) {
  const r = rng(seed), n = 7, radii = Array.from({ length: n }, () => r.range(.62, 1)), base = r.range(5, 7), turn = r.range(0, TAU);
  const radius = (a) => { const f = ((a - turn) / TAU * n % n + n) % n, i = Math.floor(f), k = f - i; return base * (radii[i] * (1 - k) + radii[(i + 1) % n] * k); };
  const cx = ROCK / 2 - .5, cy = ROCK / 2 - .5;
  const inside = (x, y) => Math.hypot(x - cx, y - cy) <= radius(Math.atan2(y - cy, x - cx));
  for (let y = 0; y < ROCK; y++) for (let x = 0; x < ROCK; x++) {
    if (!inside(x, y)) continue;
    const edge = !inside(x - 1, y) || !inside(x + 1, y) || !inside(x, y - 1) || !inside(x, y + 1);
    // Light comes from the top-left: brighter there, a darker step toward the bottom-right.
    const light = ((cx - x) + (cy - y)) / (base * 2) + .5 + r.range(-.06, .06);
    const step = edge ? (light > .75 ? 2 : 0) : light > .82 ? 5 : light > .6 ? 4 : light > .35 ? 3 : 2;
    p.px(ox + x, oy + y, STONE[step]);
  }
  // A crack across the face, darker than the stone around it.
  let x = cx + r.range(-2, 0), y = cy + r.range(-2, 0);
  for (let i = 0; i < 4; i++) { if (inside(x, y)) p.px(ox + x, oy + y, STONE[1]); x += r.next() < .6 ? 1 : 0; y += 1; }
}

// One frame of the dust cloud, anchored at the bottom centre: puffs burst out fast, swell, rise and dither away.
function dust(p, ox, t) {
  const r = rng(77), puffs = Array.from({ length: 9 }, (_, i) => ({
    a: (i / 9) * Math.PI + r.range(-.2, .2), dist: r.range(9, 22), size: r.range(7, 12), delay: r.range(0, .12), rise: r.range(8, 20),
  }));
  const bottom = DUST_H - 4, cx = DUST_W / 2;
  for (const puff of puffs) {
    const k = clamp((t - puff.delay) / (1 - puff.delay)); if (k <= 0) continue;
    const spread = outCubic(clamp(k * 2.2));
    const x = cx + Math.cos(puff.a) * puff.dist * spread, y = bottom - 6 - Math.sin(puff.a) * puff.dist * .7 * spread - puff.rise * outCubic(k);
    const rad = puff.size * (.45 + .75 * outCubic(k)), fade = 1 - clamp((k - .35) / .65);
    for (let dy = -Math.ceil(rad); dy <= rad; dy++) for (let dx = -Math.ceil(rad); dx <= rad; dx++) {
      const d = Math.hypot(dx, dy / .8) / rad; if (d > 1) continue;
      const ix = Math.round(ox + x + dx), iy = Math.round(y + dy);
      if (ix < ox || ix >= ox + DUST_W) continue;
      // Truly translucent, in 8 alpha steps; only the rim is dithered. Lit on top, dark at the base.
      if (d > .82 && BAYER[iy & 3][ix & 3] < .5) continue;
      const shade = clamp(.5 - dy / rad * .5 + (1 - fade) * .15);
      const step = shade > .8 ? 4 : shade > .58 ? 3 : shade > .35 ? 2 : 1;
      p.blend(ix, iy, DUST[step], Math.round(fade * (.8 - d * d * .35) * 8) / 8);
    }
  }
}

function crc32(buf) {
  let c, crc = ~0;
  for (const b of buf) { c = (crc ^ b) & 255; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; crc = (crc >>> 8) ^ c; }
  return ~crc >>> 0;
}
function png(w, h, rgba) {
  const chunk = (type, data) => {
    const out = Buffer.alloc(12 + data.length), td = Buffer.concat([Buffer.from(type), data]);
    out.writeUInt32BE(data.length, 0); td.copy(out, 4); out.writeUInt32BE(crc32(td), 8 + data.length); return out;
  };
  const raw = Buffer.alloc((w * 4 + 1) * h);
  for (let y = 0; y < h; y++) rgba.copy(raw, y * (w * 4 + 1) + 1, y * w * 4, (y + 1) * w * 4);
  const ihdr = Buffer.alloc(13); ihdr.writeUInt32BE(w, 0); ihdr.writeUInt32BE(h, 4); ihdr[8] = 8; ihdr[9] = 6;
  return Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk("IHDR", ihdr), chunk("IDAT", deflateSync(raw)), chunk("IEND", Buffer.alloc(0))]);
}
function preview(file, w, h, rgba, scale = 4) {
  const W = w * scale, H = h * scale, out = Buffer.alloc(W * H * 4), bg = [40, 30, 28];
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
    const s = (Math.floor(y / scale) * w + Math.floor(x / scale)) * 4, a = rgba[s + 3] / 255, o = (y * W + x) * 4;
    for (let k = 0; k < 3; k++) out[o + k] = Math.round(rgba[s + k] * a + bg[k] * (1 - a)); out[o + 3] = 255;
  }
  writeFileSync(file, png(W, H, out));
}

mkdirSync(OUT, { recursive: true });
const rocks = new Painter(ROCK * ROCKS, ROCK);
for (let i = 0; i < ROCKS; i++) rock(rocks, i * ROCK, 0, 4100 + i * 13);
const cloud = new Painter(DUST_W * DUST_FRAMES, DUST_H);
// One strip, left to right; each frame is clipped to its own column.
for (let f = 0; f < DUST_FRAMES; f++) dust(cloud, f * DUST_W, .06 + .94 * f / (DUST_FRAMES - 1));
const sheets = { rocks: [rocks, ROCK * ROCKS, ROCK], dust: [cloud, DUST_W * DUST_FRAMES, DUST_H] };
for (const [name, [painter, w, h]] of Object.entries(sheets)) {
  const rgba = painter.bytes();
  writeFileSync(join(OUT, name + ".png"), png(w, h, rgba));
  if (process.argv[2]) { mkdirSync(process.argv[2], { recursive: true }); preview(join(process.argv[2], name + ".png"), w, h, rgba, name === "rocks" ? 6 : 3); }
}
console.log(`Wrote collapse rocks and dust to ${OUT}`);
