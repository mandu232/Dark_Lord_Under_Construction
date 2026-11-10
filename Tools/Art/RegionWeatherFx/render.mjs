// Region arrival weather (snow on the snowfield, embers in the volcano...), drawn in code as seamless tiles.
// Standalone, pixelfx-style: `node Tools/Art/RegionWeatherFx/render.mjs [previewDir]`.
// Tiles go to Assets/Resources/RegionWeather/<region index>.png; RegionRelocationPresenter scrolls two layers of each.
import { writeFileSync, mkdirSync } from "node:fs";
import { deflateSync } from "node:zlib";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const SIZE = 128;
const OUT = join(dirname(fileURLToPath(import.meta.url)), "../../../Assets/Resources/RegionWeather");
const clamp = (v, a = 0, b = 1) => Math.min(b, Math.max(a, v));
const hex = (h) => [1, 3, 5].map((i) => parseInt(h.slice(i, i + 2), 16) / 255);
const ramp = (list) => list.map(hex);
const BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]].map((r) => r.map((v) => (v + .5) / 16));
function rng(seed) {
  let s = seed >>> 0;
  const next = () => { s = (s + 0x6d2b79f5) >>> 0; let t = s; t = Math.imul(t ^ (t >>> 15), t | 1); t ^= t + Math.imul(t ^ (t >>> 7), t | 61); return ((t ^ (t >>> 14)) >>> 0) / 4294967296; };
  return { next, range: (a, b) => a + (b - a) * next(), int: (a, b) => Math.floor(a + (b - a + 1) * next()) };
}

const SNOW = ramp(["#3f6f9e", "#8fb8de", "#d4ecff", "#ffffff"]);
const ASH = ramp(["#3a3230", "#5e524c", "#8a7c70", "#b8aa98"]);
const EMBER = ramp(["#7a1604", "#c83a06", "#ff7a12", "#ffc43a", "#fff6c0"]);
const FIREFLY = ramp(["#2a6a4a", "#6fd0a0", "#c8ffd8", "#ffffff"]);
const LEAF = ramp(["#24503e", "#3e7a5c", "#6aaa82"]);
const HOLY = ramp(["#b8860b", "#e8b923", "#ffd95a", "#fffbe6"]);
const MEADOW = ramp(["#5a4a1c", "#8a7a2e", "#b8a84a", "#e0d47a"]);

// Wraps on both axes so the tile repeats without seams when the presenter scrolls it.
class Tile {
  constructor() { this.buf = new Float32Array(SIZE * SIZE * 4); }
  blend(x, y, c, a) {
    a = clamp(a); if (a <= 0) return;
    const ix = ((Math.round(x) % SIZE) + SIZE) % SIZE, iy = ((Math.round(y) % SIZE) + SIZE) % SIZE;
    const i = (iy * SIZE + ix) * 4, da = this.buf[i + 3], oa = a + da * (1 - a);
    for (let k = 0; k < 3; k++) this.buf[i + k] = (c[k] * a + this.buf[i + k] * da * (1 - a)) / oa;
    this.buf[i + 3] = oa;
  }
  px(x, y, c) { this.blend(x, y, c, 1); }
  dpx(x, y, c, a) { const ix = ((Math.round(x) % SIZE) + SIZE) % SIZE, iy = ((Math.round(y) % SIZE) + SIZE) % SIZE; if (a > BAYER[iy & 3][ix & 3]) this.px(x, y, c); }
  // Soft halo baked as translucent colour: reads as glow over the dark dungeon.
  glow(x, y, r, c, k) {
    for (let dy = -Math.ceil(r); dy <= r; dy++) for (let dx = -Math.ceil(r); dx <= r; dx++) {
      const d = Math.hypot(dx, dy) / r;
      if (d < 1) this.blend(x + dx, y + dy, c, Math.round(k * (1 - d) ** 2 * 16) / 16);
    }
  }
}

// Each kind draws one particle; size 0..2 picks small, medium or large so layers read with depth.
const SHAPES = {
  flake(p, x, y, s, C) {
    if (s === 0) { p.blend(x, y, C[2], .7); return; }
    if (s === 1) { p.px(x, y, C[3]); p.px(x + 1, y, C[2]); p.px(x, y + 1, C[2]); p.px(x + 1, y + 1, C[1]); return; }
    // Large flake: a lit plus with a darker lower-right step, plus a faint halo.
    p.glow(x, y, 3, C[2], .3);
    p.px(x, y, C[3]); p.px(x - 1, y, C[2]); p.px(x, y - 1, C[3]); p.px(x + 1, y, C[1]); p.px(x, y + 1, C[1]);
  },
  ash(p, x, y, s, C) {
    p.px(x, y, C[s + 1]); if (s > 0) p.px(x + 1, y, C[s]); if (s > 1) p.px(x, y + 1, C[0]);
  },
  ember(p, x, y, s, C) {
    // Hot core on top, cooling tail below: embers rise, so the trail hangs under them.
    if (s > 0) p.glow(x, y, 2 + s * 1.5, C[3], .3 + s * .08);
    p.px(x, y, C[s > 0 ? 4 : 3]); if (s > 0) p.px(x, y + 1, C[2]); if (s > 1) { p.px(x + 1, y, C[3]); p.px(x, y + 2, C[1]); }
  },
  firefly(p, x, y, s, C) {
    p.glow(x, y, 3 + s * 1.5, C[1], .45 + s * .1);
    p.px(x, y, C[s > 0 ? 3 : 2]); if (s > 1) { p.px(x + 1, y, C[2]); p.px(x - 1, y, C[2]); p.px(x, y - 1, C[2]); p.px(x, y + 1, C[2]); }
  },
  leaf(p, x, y, s, C, r) {
    // A tilted two-tone leaf: light upper edge, dark lower edge.
    const dir = r.next() < .5 ? 1 : -1, len = 2 + s;
    for (let i = 0; i < len; i++) { p.px(x + i * dir, y + Math.floor(i / 2), C[2]); p.px(x + i * dir, y + Math.floor(i / 2) + 1, C[i === 0 ? 1 : 0]); }
  },
  mote(p, x, y, s, C) {
    p.glow(x, y, 2.5 + s * 1.5, C[2], .35 + s * .08);
    p.px(x, y, C[3]);
    if (s > 1) { p.dpx(x - 1, y, C[2], .8); p.dpx(x + 1, y, C[2], .8); p.dpx(x, y - 1, C[2], .8); p.dpx(x, y + 1, C[2], .8); }
  },
};

// Index matches RegionCatalog.Regions.
const REGIONS = [
  { name: "west-frontier", layers: [["leaf", MEADOW, 18], ["ash", MEADOW, 22]] },
  { name: "ash-wastes", layers: [["ash", ASH, 70], ["ember", EMBER, 4]] },
  { name: "moonshade-forest", layers: [["firefly", FIREFLY, 16], ["leaf", LEAF, 16]] },
  { name: "holy-kingdom", layers: [["mote", HOLY, 22]] },
  { name: "frostwind-snowfield", layers: [["flake", SNOW, 80]] },
  { name: "ember-volcano", layers: [["ember", EMBER, 26], ["ash", ASH, 34]] },
];

function render(region, seed) {
  const p = new Tile(), r = rng(seed);
  for (const [kind, C, count] of region.layers)
    for (let i = 0; i < count; i++) {
      // Mostly small particles, a few large ones: depth without clutter.
      const roll = r.next(), s = roll < .55 ? 0 : roll < .87 ? 1 : 2;
      SHAPES[kind](p, r.int(0, SIZE - 1), r.int(0, SIZE - 1), s, C, r);
    }
  const out = Buffer.alloc(SIZE * SIZE * 4);
  for (let i = 0; i < SIZE * SIZE; i++) {
    for (let k = 0; k < 3; k++) out[i * 4 + k] = Math.round(clamp(p.buf[i * 4 + k]) * 255);
    out[i * 4 + 3] = Math.round(clamp(p.buf[i * 4 + 3]) * 255);
  }
  return out;
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

// Preview: 2x2 tiles at 3x on a dark dungeon floor colour, so seams and readability can be judged.
function preview(dir, index, rgba) {
  const scale = 3, W = SIZE * 2 * scale, out = Buffer.alloc(W * W * 4), bg = [26, 18, 24];
  for (let y = 0; y < W; y++) for (let x = 0; x < W; x++) {
    const sx = Math.floor(x / scale) % SIZE, sy = Math.floor(y / scale) % SIZE, s = (sy * SIZE + sx) * 4, a = rgba[s + 3] / 255, o = (y * W + x) * 4;
    for (let k = 0; k < 3; k++) out[o + k] = Math.round(rgba[s + k] * a + bg[k] * (1 - a));
    out[o + 3] = 255;
  }
  writeFileSync(join(dir, `${index}-${REGIONS[index].name}.png`), png(W, W, out));
}

mkdirSync(OUT, { recursive: true });
const previewDir = process.argv[2];
if (previewDir) mkdirSync(previewDir, { recursive: true });
REGIONS.forEach((region, index) => {
  const rgba = render(region, 9100 + index * 31);
  writeFileSync(join(OUT, `${index}.png`), png(SIZE, SIZE, rgba));
  if (previewDir) preview(previewDir, index, rgba);
});
console.log(`Wrote ${REGIONS.length} region weather tiles to ${OUT}`);
