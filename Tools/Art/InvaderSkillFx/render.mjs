// Invader caster skill effects (orc shaman, elf mage, human cleric), drawn in code and written as sprite sheets.
// Standalone: no pixelfx checkout needed. `node Tools/Art/InvaderSkillFx/render.mjs [previewDir]`
// Sheets go to Assets/Sprite/Effects/PixelFx; sizes, columns and pivots must match PixelFxEffectsSetup.Sheets.
import { writeFileSync, mkdirSync, readFileSync } from "node:fs";
import { deflateSync, inflateSync } from "node:zlib";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const TAU = Math.PI * 2;
const OUT = join(dirname(fileURLToPath(import.meta.url)), "../../../Assets/Sprite/Effects/PixelFx");
const clamp = (v, a = 0, b = 1) => Math.min(b, Math.max(a, v));
const seg = (t, a, b) => clamp((t - a) / (b - a));
const outCubic = (k) => 1 - (1 - k) ** 3, inQuad = (k) => k * k;
const hex = (h) => [1, 3, 5].map((i) => parseInt(h.slice(i, i + 2), 16) / 255);
const ramp = (list) => list.map(hex);
const WHITE = hex("#ffffff");
const BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]].map((r) => r.map((v) => (v + .5) / 16));
function rng(seed) {
  let s = seed >>> 0;
  const next = () => { s = (s + 0x6d2b79f5) >>> 0; let t = s; t = Math.imul(t ^ (t >>> 15), t | 1); t ^= t + Math.imul(t ^ (t >>> 7), t | 61); return ((t ^ (t >>> 14)) >>> 0) / 4294967296; };
  return { next, range: (a, b) => a + (b - a) * next() };
}

// Spirit green for the orc shaman, moonlit violet-silver for the elf mage, the game's holy gold for the cleric.
const SPIRIT = ramp(["#06140c", "#0f3a1e", "#1d6b2c", "#3ea83c", "#7ee05a", "#c8ff8c", "#f4ffe2"]);
const MOON = ramp(["#0e0a24", "#2a1d5c", "#4b3a9a", "#7c6fd6", "#a9b8f0", "#d8e6ff", "#ffffff"]);
const HOLY = ramp(["#2a1a05", "#6a4a0c", "#b8860b", "#e8b923", "#ffd95a", "#fff0a8", "#fffbe6"]);
// Boss themes: Urgad's blood-rust rage, Hrim's glacier blue, Varkan's molten fire, plus rock and smoke for debris.
const RAGE = ramp(["#140806", "#3a120a", "#6e1e10", "#a8301a", "#e0482a", "#ff7a4a", "#ffd2a8"]);
const ICE = ramp(["#06101e", "#0f2a4a", "#1f5288", "#3f8ad0", "#7cc4f0", "#c4ecff", "#ffffff"]);
const EMBER = ramp(["#140404", "#3a0a04", "#7a1604", "#c83a06", "#ff7a12", "#ffc43a", "#fff6c0"]);
const STONE = ramp(["#1a1410", "#3a2e24", "#5e4c3a", "#8a7258"]);
const SMOKE = ramp(["#120e0e", "#2a2222", "#463a36", "#6a5a52"]);

class Painter {
  constructor(w, h, ox, oy) { Object.assign(this, { w, h, ox, oy }); this.buf = new Float32Array(w * h * 4); }
  blend(x, y, c, a) {
    const ix = Math.round(x + this.ox), iy = Math.round(y + this.oy);
    a = clamp(a);
    if (a <= 0 || ix < 0 || iy < 0 || ix >= this.w || iy >= this.h) return;
    const i = (iy * this.w + ix) * 4, da = this.buf[i + 3], oa = a + da * (1 - a);
    for (let k = 0; k < 3; k++) this.buf[i + k] = (c[k] * a + this.buf[i + k] * da * (1 - a)) / oa;
    this.buf[i + 3] = oa;
  }
  px(x, y, c) { this.blend(x, y, c, 1); }
  // Ordered dither: solid pixels that thin out as `a` falls, the pixel-art way to fade.
  dpx(x, y, c, a = 1) {
    const ix = Math.round(x + this.ox), iy = Math.round(y + this.oy);
    if (a > BAYER[iy & 3][ix & 3]) this.px(x, y, c);
  }
  disc(x, y, r, c, a = 1) {
    for (let dy = -Math.ceil(r); dy <= r; dy++) for (let dx = -Math.ceil(r); dx <= r; dx++)
      if (dx * dx + dy * dy <= r * r + .3) this.dpx(x + dx, y + dy, c, a);
  }
  // A floor circle seen from above at a slant: rx wide, ry tall.
  ellipse(x, y, rx, ry, c, a = 1, dash = 0, turn = 0) {
    const n = Math.max(12, Math.ceil(Math.max(rx, ry) * 7));
    for (let i = 0; i < n; i++) {
      const g = i / n * TAU + turn;
      if (dash && Math.sin(g * dash) < -.2) continue;
      this.dpx(x + Math.cos(g) * rx, y + Math.sin(g) * ry, c, a);
    }
  }
  glow(x, y, r, c, k) {
    for (let dy = -Math.ceil(r); dy <= r; dy++) for (let dx = -Math.ceil(r); dx <= r; dx++) {
      const d = Math.hypot(dx, dy) / r;
      if (d < 1) this.blend(x + dx, y + dy, c, Math.round(k * (1 - d) ** 2 * 16) / 16);
    }
  }
  line(x0, y0, x1, y1, c, a = 1) {
    const n = Math.max(1, Math.ceil(Math.hypot(x1 - x0, y1 - y0)));
    for (let i = 0; i <= n; i++) this.dpx(x0 + (x1 - x0) * i / n, y0 + (y1 - y0) * i / n, c, a);
  }
}

// Particles that rise from a floor ring: shared by the shaman's flames and the cleric's motes.
function risers(p, t, C, { seed, count, born, life, height, spread, sway, size }) {
  const r = rng(seed);
  for (let i = 0; i < count; i++) {
    const x0 = r.range(-spread, spread), y0 = r.range(-2, 2), b = r.range(0, born), l = r.range(...life), ph = r.range(0, TAU), h = r.range(.6, 1) * height;
    const q = (t - b) / l;
    if (q < 0 || q > 1) continue;
    const e = outCubic(q), x = x0 + Math.sin(ph + q * 7) * sway * q, y = y0 - e * h, s = size * (1 - q * .8);
    p.dpx(x, y + 2, C[2], 1 - q);
    p.dpx(x, y + 1, C[3], 1 - q * .6);
    if (s > 1.1) p.disc(x, y, s - 1, C[q < .5 ? 5 : 4], 1.2 - q);
    p.px(x, y, q < .35 ? C[6] : C[5]);
  }
}

const EFFECTS = {
  // At the end of the chant the shaman's staff throws up a gout of green spirit fire and a tribal ring flares on the floor.
  "orc-shaman-cast": { size: [48, 64], origin: [24, 48], duration: .5, fps: 30, columns: 5, draw(p, t) {
    const C = SPIRIT, k = seg(t, 0, .42);
    p.ellipse(0, 0, 4 + outCubic(k) * 12, 2 + outCubic(k) * 5, C[4], 1 - k, 4, k * 1.5);
    p.ellipse(0, 0, 2 + outCubic(k) * 8, 1 + outCubic(k) * 3.5, C[2], .9 - k);
    const f = 1 - seg(t, .02, .2);
    if (f > 0) { p.glow(0, -12, 14, C[4], .55 * f); p.disc(0, -12, 3 * f, C[6]); }
    risers(p, t, C, { seed: 11, count: 14, born: .14, life: [.2, .3], height: 34, spread: 7, sway: 3, size: 2.4 });
  } },
  // On the healed ally: fang marks circle the feet, spirit wisps spiral up into the body, then a green pulse.
  "orc-shaman-mend": { size: [56, 56], origin: [28, 34], duration: .8, fps: 30, columns: 6, draw(p, t) {
    const C = SPIRIT, grow = outCubic(seg(t, 0, .2)), fade = 1 - seg(t, .55, .74), turn = t * 2.5;
    p.ellipse(0, 2, 5 + 10 * grow, 2.5 + 5 * grow, C[3], fade);
    for (let i = 0; i < 4; i++) {
      const g = turn + i / 4 * TAU, x = Math.cos(g) * (5 + 10 * grow), y = 2 + Math.sin(g) * (2.5 + 5 * grow);
      p.dpx(x, y, C[5], fade); p.dpx(x, y - 1, C[5], fade); p.dpx(x, y - 2, C[6], fade);
    }
    const r = rng(23);
    for (let i = 0; i < 10; i++) {
      const g0 = r.range(0, TAU), b = r.range(.05, .2), l = r.range(.22, .3), q = (t - b) / l;
      if (q < 0 || q > 1) continue;
      const e = inQuad(q), g = g0 + e * 2.4, d = 15 * (1 - e);
      const at = (lag) => { const gg = g - lag, dd = 15 * (1 - inQuad(Math.max(0, q - lag * .25))) + lag * 2; return [Math.cos(gg) * dd, 2 + Math.sin(gg) * dd * .5 - e * 12 + lag * 3]; };
      for (const [lag, c] of [[.6, C[2]], [.4, C[3]], [.2, C[4]]]) { const [tx, ty] = at(lag); p.px(tx, ty, c); }
      const x = Math.cos(g) * d, y = 2 + Math.sin(g) * d * .5 - e * 12;
      p.disc(x, y, q < .7 ? 1 : 0, C[5]);
      p.px(x, y, C[6]);
    }
    const g = seg(t, .4, .48) * (1 - seg(t, .48, .66));
    if (g > 0) { p.glow(0, -10, 13, C[4], .5 * g); p.disc(0, -10, 1 + 2 * g, C[6]); }
    const k = seg(t, .46, .76);
    if (k > 0 && k < 1) p.ellipse(0, 2, 6 + outCubic(k) * 16, 3 + outCubic(k) * 8, k < .4 ? C[5] : C[3], 1 - k);
    if (t > .45) risers(p, t - .45, C, { seed: 31, count: 6, born: .06, life: [.15, .22], height: 18, spread: 6, sway: 2, size: 1 });
  } },
  // A four-point star of moonlight bursts from the elf's staff, a thin ring ripples out, sparkles drift and wink.
  "elf-mage-cast": { size: [48, 48], origin: [24, 24], duration: .45, fps: 30, columns: 5, draw(p, t) {
    const C = MOON, s = outCubic(seg(t, 0, .1)) * (1 - seg(t, .14, .36)), L = 11 * s;
    if (s > 0) {
      p.glow(0, 0, 12, C[4], .55 * s);
      for (let d = 0; d <= L; d++) {
        const c = d < L * .35 ? WHITE : d < L * .7 ? C[5] : C[4];
        p.px(d, 0, c); p.px(-d, 0, c); p.px(0, d, c); p.px(0, -d, c);
        if (d < L * .4) { p.px(d * .7, d * .7, C[4]); p.px(-d * .7, d * .7, C[4]); p.px(d * .7, -d * .7, C[4]); p.px(-d * .7, -d * .7, C[4]); }
      }
      p.disc(0, 0, 1.5 * s, WHITE);
    }
    const k = seg(t, .04, .4);
    if (k > 0 && k < 1) { const rad = 3 + outCubic(k) * 15; p.ellipse(0, 0, rad, rad, k < .3 ? C[5] : C[3], 1 - k); }
    const r = rng(41);
    for (let i = 0; i < 9; i++) {
      const g = r.range(0, TAU), b = r.range(.02, .12), l = r.range(.2, .3), sp = r.range(9, 17), q = (t - b) / l;
      if (q < 0 || q > 1) continue;
      const d = 3 + outCubic(q) * sp, x = Math.cos(g) * d, y = Math.sin(g) * d - q * 3;
      const twinkle = Math.sin(q * 18 + i) > 0;
      p.dpx(x, y, twinkle ? WHITE : C[4], 1.3 - q);
      if (twinkle && q < .6) { p.px(x + 1, y, C[3]); p.px(x - 1, y, C[3]); p.px(x, y + 1, C[3]); p.px(x, y - 1, C[3]); }
    }
  } },
  // Starlight motes fall around the healed ally into a rune circle that turns under the feet; a soft violet bloom.
  "elf-mage-mend": { size: [56, 56], origin: [28, 34], duration: .8, fps: 30, columns: 6, draw(p, t) {
    const C = MOON, grow = outCubic(seg(t, 0, .22)), fade = 1 - seg(t, .55, .74), turn = -t * 3;
    p.ellipse(0, 2, 14 * grow, 7 * grow, C[3], fade);
    p.ellipse(0, 2, 10 * grow, 5 * grow, C[2], fade * .8, 6, turn);
    for (let i = 0; i < 6; i++) {
      const g = turn + i / 6 * TAU, x = Math.cos(g) * 14 * grow, y = 2 + Math.sin(g) * 7 * grow;
      p.dpx(x, y - 1, C[5], fade); p.dpx(x - 1, y, C[4], fade); p.dpx(x + 1, y, C[4], fade); p.dpx(x, y + 1, C[4], fade);
    }
    const r = rng(53);
    for (let i = 0; i < 11; i++) {
      const x0 = r.range(-15, 15), b = r.range(0, .22), l = r.range(.2, .28), q = (t - b) / l;
      if (q < 0 || q > 1) continue;
      const e = inQuad(q), x = x0 * (1 - e * .8) + Math.sin(q * 9 + i) * 1.5, y = -26 + e * 20;
      p.dpx(x, y - 2, C[2], 1); p.dpx(x, y - 1, C[3], 1);
      p.px(x, y, Math.sin(q * 20 + i) > 0 ? WHITE : C[5]);
    }
    const g = seg(t, .4, .48) * (1 - seg(t, .48, .68));
    if (g > 0) {
      p.glow(0, -8, 14, C[4], .55 * g);
      for (let d = 0; d <= 5 * g; d++) { const c = d < 2 ? WHITE : C[5]; p.px(d, -8, c); p.px(-d, -8, c); p.px(0, -8 - d, c); p.px(0, -8 + d, c); }
    }
    const k = seg(t, .46, .74);
    if (k > 0 && k < 1) { const rad = 6 + outCubic(k) * 16; p.ellipse(0, 2, rad, rad * .5, k < .35 ? C[5] : C[3], 1 - k, 5, k); }
  } },
  // The cleric raises the holy symbol: a short pillar of gold light, a cross glint and a halo ring on the floor.
  "cleric-cast": { size: [48, 64], origin: [24, 48], duration: .5, fps: 30, columns: 5, draw(p, t) {
    const C = HOLY, on = outCubic(seg(t, 0, .06)), off = seg(t, .1, .36), w = 3.5 * on * (1 - off);
    if (w > .2) {
      p.glow(0, -18, 16, C[4], .45 * (1 - off));
      for (let y = -42; y <= 0; y++) {
        const taper = w * (.55 + .45 * (1 + y / 42)), a = 1.2 - off - Math.max(0, -y - 30) / 14;
        for (let x = -Math.ceil(taper); x <= taper; x++) {
          const k = Math.abs(x) / Math.max(.5, taper);
          p.dpx(x, y, k < .3 ? WHITE : k < .7 ? C[5] : C[3], a);
        }
      }
    }
    const c = outCubic(seg(t, .02, .1)) * (1 - seg(t, .16, .4)), L = 7 * c;
    for (let d = 0; c > 0 && d <= L; d++) {
      const col = d < L * .4 ? WHITE : C[5];
      p.px(d, -16, col); p.px(-d, -16, col); p.px(0, -16 - d, col); p.px(0, -16 + d * 1.4, col);
    }
    const k = seg(t, 0, .45);
    p.ellipse(0, 0, 3 + outCubic(k) * 13, 1.5 + outCubic(k) * 5.5, k < .3 ? C[5] : C[4], 1 - k);
    p.ellipse(0, 0, 2 + outCubic(k) * 8, 1 + outCubic(k) * 3.5, C[2], .8 - k);
    risers(p, t, C, { seed: 61, count: 10, born: .16, life: [.2, .28], height: 30, spread: 9, sway: 1.5, size: 1.2 });
  } },

  // Boss signature impacts. All 96 px with about a 40 px reach, played at the same scale as HolyPulse on each floor mark.
  // Urgad's axe splits the ground: jagged cracks glowing red, rock chunks thrown out, a ring of dust.
  "orc-quake": { size: [96, 96], origin: [48, 48], duration: .55, fps: 30, columns: 4, draw(p, t) {
    const C = RAGE, open = outCubic(seg(t, 0, .1)), fade = 1 - seg(t, .28, .52);
    const d = seg(t, 0, .42);
    if (d < 1) p.ellipse(0, 0, 10 + outCubic(d) * 30, 10 + outCubic(d) * 30, C[2], (1 - d) * .9);
    for (let i = 0; i < 7; i++) {
      const r = rng(71 + i), len = r.range(24, 38) * open;
      let a = i / 7 * TAU + r.range(-.3, .3), x = 0, y = 0;
      for (let s = 0; s < len; s += 1.5) {
        a += r.range(-.45, .45) * .5; x += Math.cos(a) * 1.5; y += Math.sin(a) * 1.5;
        const hot = s < len * (1 - seg(t, .1, .4));
        p.dpx(x + 1, y + 1, C[0], fade);
        p.dpx(x, y, hot ? C[5] : C[2], fade);
      }
    }
    const g = 1 - seg(t, 0, .16);
    if (g > 0) { p.glow(0, 0, 24, C[4], .6 * g); p.disc(0, 0, 7 * g, C[6]); }
    const r = rng(91);
    for (let i = 0; i < 12; i++) {
      const a = r.range(0, TAU), sp = r.range(16, 32), l = r.range(.3, .45), q = t / l, s = r.next() < .5 ? 2 : 1;
      if (q > 1) continue;
      const k = outCubic(q), x = Math.cos(a) * sp * k, y = Math.sin(a) * sp * k, h = 12 * Math.sin(Math.PI * q);
      p.dpx(x, y, C[0], .7);
      p.disc(x, y - h, s - .5, STONE[2]);
      p.px(x - 1, y - h - 1, STONE[3]);
    }
  } },
  // Serilia's judgement: a shaft of moonlight spears down, a star bursts and crescent blades whirl outward.
  "elf-moonfall": { size: [96, 96], origin: [48, 48], duration: .5, fps: 30, columns: 4, draw(p, t) {
    const C = MOON, beam = 1 - seg(t, .05, .14);
    if (beam > 0) for (let y = -48; y <= 0; y++) { p.dpx(0, y, WHITE, beam); p.dpx(-1, y, C[5], beam * .8); p.dpx(1, y, C[4], beam * .6); }
    const s = outCubic(seg(t, 0, .06)) * (1 - seg(t, .1, .3)), L = 18 * s;
    if (s > 0) {
      p.glow(0, 0, 26, C[4], .6 * s);
      for (let d = 0; d <= L; d++) { const c = d < L * .35 ? WHITE : C[5]; p.px(d, 0, c); p.px(-d, 0, c); p.px(0, d, c); p.px(0, -d, c); }
    }
    const k = seg(t, .03, .45);
    if (k < 1) for (let i = 0; i < 3; i++) {
      const rad = 8 + outCubic(k) * 30, base = i / 3 * TAU + k * 3;
      for (let g = 0; g < 1.1; g += .03) {
        const w = Math.sin(g / 1.1 * Math.PI) * 2.5 * (1 - k);
        for (let e = 0; e <= w; e += .7) p.dpx(Math.cos(base + g) * (rad - e), Math.sin(base + g) * (rad - e), e < 1 ? WHITE : C[4], 1.2 - k);
      }
    }
    const r = rng(101);
    for (let i = 0; i < 14; i++) {
      const a = r.range(0, TAU), sp = r.range(18, 36), q = (t - .04) / r.range(.25, .4);
      if (q < 0 || q > 1) continue;
      const dd = outCubic(q) * sp;
      p.dpx(Math.cos(a) * dd, Math.sin(a) * dd, Math.sin(q * 20 + i) > 0 ? WHITE : C[4], 1.2 - q);
    }
  } },
  // Hrim's ice spikes burst from the floor in a cluster, stand for a beat, then shatter into shards over a frost ring.
  "frost-spikes": { size: [96, 96], origin: [48, 48], duration: .6, fps: 30, columns: 6, draw(p, t) {
    const C = ICE, frost = 1 - seg(t, .38, .54);
    p.ellipse(0, 4, 26 * outCubic(seg(t, 0, .15)), 26 * outCubic(seg(t, 0, .15)), C[3], frost);
    const r = rng(111), spikes = [];
    for (let i = 0; i < 6; i++) spikes.push([Math.cos(i / 6 * TAU + .3) * 16, 4 + Math.sin(i / 6 * TAU + .3) * 13, r.range(12, 18), r.range(0, .06)]);
    spikes.push([-5, 2, 22, 0], [6, 6, 18, .02], [0, 10, 14, .04]);
    spikes.sort((a, b) => a[1] - b[1]);
    const shatter = seg(t, .32, .36);
    for (const [x, y, hh, delay] of spikes) {
      const h = hh * outCubic(seg(t, delay, delay + .08));
      if (h < 1 || shatter >= 1) continue;
      const half = 3 + hh * .08;
      for (let dy = 0; dy <= h; dy++) {
        const w = half * (1 - dy / h);
        for (let dx = -w; dx <= w; dx += 1) p.px(x + dx, y - dy, dy > h - 2 ? WHITE : dx < -w * .2 ? C[5] : dx < w * .4 ? C[4] : C[2]);
        p.px(x - w - 1, y - dy, C[1]); p.px(x + w + 1, y - dy, C[1]);
      }
    }
    const g = 1 - seg(t, 0, .14);
    if (g > 0) p.glow(0, 0, 26, C[5], .55 * g);
    if (t >= .34) {
      const r2 = rng(131);
      for (let i = 0; i < 22; i++) {
        const [x0, y0, hh] = spikes[i % spikes.length], a = r2.range(0, TAU), sp = r2.range(8, 20), l = r2.range(.15, .22), q = (t - .34) / l;
        if (q > 1) continue;
        const k = outCubic(q), x = x0 + Math.cos(a) * sp * k, y = y0 - hh * .5 * (1 - q) + Math.sin(a) * sp * k * .6 + 10 * q * q;
        p.px(x, y, q < .4 ? WHITE : C[4]); p.dpx(x + 1, y, C[3], 1 - q);
      }
    }
  } },
  // Varkan's meteor: a fireball streaks in from the upper left, bursts into a ball of flame, embers fly, scorch smoke rolls off.
  "ember-meteor": { size: [96, 96], origin: [48, 48], duration: .6, fps: 30, columns: 6, draw(p, t) {
    const C = EMBER, fall = seg(t, 0, .08);
    if (fall < 1) for (let i = 5; i >= 0; i--) {
      const k = Math.max(0, fall - i * .07), x = -34 * (1 - k), y = -44 * (1 - k);
      p.disc(x, y, 4.5 - i * .6, i === 0 ? C[6] : C[6 - Math.min(4, i)]);
    }
    const k = seg(t, .06, .4);
    if (k > 0 && k < 1) {
      const rad = 6 + outCubic(k) * 20, core = rad * (1 - k) * .9;
      p.glow(0, 0, rad + 12, C[4], .55 * (1 - k));
      for (let dy = -Math.ceil(rad); dy <= rad; dy++) for (let dx = -Math.ceil(rad); dx <= rad; dx++) {
        const d = Math.hypot(dx, dy * 1.1);
        if (d > rad) continue;
        const n = (Math.sin(dx * 12.9898 + dy * 78.233) * 43758.5453) % 1;
        if (Math.abs(n) < 1.2 - k * (1 + d / rad)) p.px(dx, dy, d < core * .5 ? WHITE : d < core ? C[5] : d < rad * .8 ? C[4] : C[3]);
      }
    }
    const sm = seg(t, .3, .58);
    if (sm > 0 && sm < 1) {
      const r = rng(141);
      for (let i = 0; i < 7; i++) {
        const a = r.range(0, TAU), d = 10 + sm * r.range(10, 20), x = Math.cos(a) * d, y = Math.sin(a) * d - sm * 8;
        p.disc(x, y, 3.5 * (1 - sm * .5), SMOKE[2], .9 - sm);
        p.disc(x - 1, y - 1, 2 * (1 - sm * .5), SMOKE[3], .9 - sm);
      }
    }
    const sc = seg(t, .08, .55);
    if (sc > 0 && sc < 1) p.ellipse(0, 0, 24 + sc * 6, 24 + sc * 6, C[2], 1 - sc, 7);
    const r = rng(151);
    for (let i = 0; i < 18; i++) {
      const a = r.range(0, TAU), sp = r.range(18, 40), q = (t - .07) / r.range(.25, .42);
      if (q < 0 || q > 1) continue;
      const dd = outCubic(q) * sp, x = Math.cos(a) * dd, y = Math.sin(a) * dd + 6 * q * q;
      p.px(x, y, q < .3 ? C[6] : q < .7 ? C[5] : C[3]);
    }
  } },
};

// Boss themes for the shared kit (cleave, charge, floor mark, impact): the holy sheets recoloured by brightness.
const RECOLOR = { orc: RAGE, elf: MOON, frost: ICE, ember: EMBER };
const RECOLOR_SOURCES = { "holy-cleave": "cleave", "holy-charge": "charge", "holy-mark": "mark", "holy-pulse": "pulse" };

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

const preview = process.argv[2];
for (const [name, fx] of Object.entries(EFFECTS)) {
  const [w, h] = fx.size, frames = Math.round(fx.duration * fx.fps), cols = fx.columns, rows = Math.ceil(frames / cols);
  const sheet = Buffer.alloc(w * cols * h * rows * 4);
  let lastEmpty = true;
  for (let f = 0; f < frames; f++) {
    const p = new Painter(w, h, ...fx.origin);
    fx.draw(p, f / fx.fps);
    lastEmpty = true;
    for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
      const i = (y * w + x) * 4, a = p.buf[i + 3];
      if (a <= 0) continue;
      lastEmpty = false;
      const gx = (f % cols) * w + x, gy = Math.floor(f / cols) * h + y, o = (gy * w * cols + gx) * 4;
      for (let k = 0; k < 3; k++) sheet[o + k] = Math.round(p.buf[i + k] * 255);
      sheet[o + 3] = Math.round(a * 255);
    }
  }
  if (!lastEmpty) console.warn(`${name}: last frame is not empty`);
  write(name, w * cols, h * rows, sheet, w, h);
  console.log(`${name}: ${w}x${h}, ${cols} columns, ${frames} frames, pivot ${fx.origin.join(",")}, ${fx.duration}s`);
}

function write(name, W, H, sheet, w, h) {
  writeFileSync(join(OUT, name + ".png"), png(W, H, sheet));
  if (!preview) return;
  // 3x over a dark floor so the frames can be judged by eye. Large sheets preview at 2x.
  const S = W > 400 ? 2 : 3, pw = W * S, prev = Buffer.alloc(pw * H * S * 4);
  for (let y = 0; y < H * S; y++) for (let x = 0; x < pw; x++) {
    const s = (Math.floor(y / S) * W + Math.floor(x / S)) * 4, o = (y * pw + x) * 4, a = sheet[s + 3] / 255;
    const bg = (Math.floor(x / S) % w === 0 || Math.floor(y / S) % h === 0) ? 60 : 22;
    for (let k = 0; k < 3; k++) prev[o + k] = Math.round(sheet[s + k] * a + bg * (1 - a));
    prev[o + 3] = 255;
  }
  mkdirSync(preview, { recursive: true });
  writeFileSync(join(preview, name + ".png"), png(pw, H * S, prev));
}

// Reads the 8-bit RGBA, non-interlaced PNGs pixelfx writes.
function readPng(file) {
  const b = readFileSync(file), W = b.readUInt32BE(16), H = b.readUInt32BE(20);
  if (b[24] !== 8 || b[25] !== 6 || b[28] !== 0) throw new Error(`${file}: expected 8-bit RGBA, non-interlaced`);
  const idat = [];
  for (let o = 8; o < b.length;) { const n = b.readUInt32BE(o), type = b.toString("ascii", o + 4, o + 8); if (type === "IDAT") idat.push(b.subarray(o + 8, o + 8 + n)); o += 12 + n; }
  const raw = inflateSync(Buffer.concat(idat)), out = Buffer.alloc(W * H * 4), row = W * 4;
  for (let y = 0; y < H; y++) {
    const f = raw[y * (row + 1)], src = raw.subarray(y * (row + 1) + 1, (y + 1) * (row + 1));
    for (let x = 0; x < row; x++) {
      const a = x >= 4 ? out[y * row + x - 4] : 0, up = y > 0 ? out[(y - 1) * row + x] : 0, c = x >= 4 && y > 0 ? out[(y - 1) * row + x - 4] : 0;
      const pa = Math.abs(up - c), pb = Math.abs(a - c), pc = Math.abs(a + up - 2 * c);
      const pred = f === 1 ? a : f === 2 ? up : f === 3 ? (a + up) >> 1 : f === 4 ? (pa <= pb && pa <= pc ? a : pb <= pc ? up : c) : 0;
      out[y * row + x] = (src[x] + pred) & 255;
    }
  }
  return { W, H, data: out };
}

for (const [source, slot] of Object.entries(RECOLOR_SOURCES)) {
  const { W, H, data } = readPng(join(OUT, source + ".png"));
  for (const [theme, C] of Object.entries(RECOLOR)) {
    const sheet = Buffer.from(data);
    for (let i = 0; i < sheet.length; i += 4) {
      if (!sheet[i + 3]) continue;
      // Brightness picks the ramp step, so the art keeps its shading and only the hue changes.
      const l = (sheet[i] * .3 + sheet[i + 1] * .55 + sheet[i + 2] * .15) / 255, c = C[Math.min(C.length - 1, Math.round(l ** 1.2 * (C.length - 1)))];
      for (let k = 0; k < 3; k++) sheet[i + k] = Math.round(c[k] * 255);
    }
    write(`${theme}-${slot}`, W, H, sheet, W, H);
  }
  console.log(`${source}: recoloured for ${Object.keys(RECOLOR).join(", ")}`);
}
