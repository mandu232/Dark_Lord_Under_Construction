import { TAU, clamp, ease, fit, lerp, rand, WHITE } from "../../core/index.js";
import { BLOOD, GEM_Y, RAD, STONE, drawDais, drawGem } from "./_core.js";

export const meta = {
  name: "Dungeon core collapse",
  desc: "The ruby shudders and cracks with trapped magic before bursting into red shards above a cracked dais.",
  tags: ["blood", "core", "gem", "death", "explosion", "once"],
  size: [176, 160],
  origin: [88, 148],
  duration: 3,
  fps: 20,
  options: { palette: BLOOD, seed: 5 },
};

export const BURST = 1.3;
const SHARDS = 34;

export function draw(p, t, o) {
  const C = fit(o.palette, 8), S = o.seed, frame = Math.floor(t * 20);
  if (t < BURST) {
    // Trapped magic heats the stone as it shudders and cracks.
    const k = t / BURST, spin = TAU * (k * 1.2 + k * k * k * 3.5);
    const shake = k * k * 2.5, cx = (rand(S, frame) - 0.5) * shake * 2, cy = GEM_Y + (rand(S, frame, 1) - 0.5) * shake;
    drawDais(p, t * 2, C, 1 - k * 0.2);
    p.glow(cx, cy, RAD * (2.1 + k * 1.5), C[4], 0.26 + k * 0.43);
    drawGem(p, spin, C, { cx, cy, heat: k * k * 0.45 });
    // Jagged fissures spread through the stone before the burst.
    for (let i = 0; i < 7; i++) {
      if (k < 0.3 + i * 0.08) continue;
      const a = rand(S, i, 3) * TAU, grown = clamp((k - 0.3 - i * 0.08) / 0.3) * RAD * 0.84;
      const x = cx + Math.cos(a + 0.25) * grown * 0.48, y = cy + Math.sin(a + 0.25) * grown * 0.56;
      p.line(cx, cy, x, y, C[6]);
      p.line(x, y, cx + Math.cos(a) * grown, cy + Math.sin(a) * grown * 1.2, C[6]);
    }
    p.glow(cx, cy, RAD * (0.6 + k * 0.5), C[6], k * k * 0.4);
    return;
  }

  const a = t - BURST, end = meta.duration - BURST, u = a / end;
  drawDais(p, 0, C, 0.15 * (1 - u), 1);
  // The floor shockwave.
  if (a < 0.45) {
    const k = ease.outQuart(a / 0.45), R = lerp(12, 80, k);
    for (let i = 0; i < 120; i++) {
      const b = (i / 120) * TAU;
      p.dot(Math.cos(b) * R, -5 + Math.sin(b) * R * 0.32, k < 0.5 ? WHITE : C[5], 1 - k);
    }
    p.glowRing(0, -5, R, 6, C[4], 0.4 * (1 - k));
  }
  // Shards flung out, falling under gravity to the floor, where they lie and go dark.
  for (let i = 0; i < SHARDS; i++) {
    const b = rand(S, i, 4) * TAU, v = lerp(36, 112, rand(S, i, 5)), lift = rand(S, i, 6) * 60;
    const vx = Math.cos(b) * v, vy = Math.sin(b) * v * 0.7 - lift, g = 190;
    const floor = -2 - rand(S, i, 7) * 12, y0 = GEM_Y;
    const land = (-vy + Math.sqrt(vy * vy + 2 * g * Math.max(0, floor - y0))) / g, age = Math.min(a, land);
    const x = vx * age, y = y0 + vy * age + 0.5 * g * age * age;
    const size = 1 + Math.floor(rand(S, i, 8) * 3), cool = clamp(a / (land + 0.5));
    const c = cool > 0.85 ? C[1] : cool > 0.55 ? C[3] : cool > 0.25 ? C[5] : C[6];
    const fade = clamp((end - a) / 0.5);
    if (fade <= 0) continue;
    const turn = (rand(S, i, 9) + (a < land ? a * 3 : land * 3)) % 1 < 0.5;
    for (let n = 0; n < size; n++) p.dot(x + (turn ? n : 0), y + (turn ? 0 : n), c, fade);
    if (cool < 0.5) p.glow(x, y, 4, C[5], 0.3 * (1 - cool * 2));
  }
  // Smoke that rises from the wreck and thins away.
  for (let i = 0; i < 12; i++) {
    const delay = rand(S, i, 10) * 0.5, age = a - delay;
    if (age <= 0) continue;
    const life = lerp(0.9, 1.6, rand(S, i, 11)), k = age / life;
    if (k >= 1) continue;
    const x = (rand(S, i, 12) - 0.5) * 40 + (rand(S, i, 13) - 0.5) * 22 * k, y = -14 - ease.outQuad(k) * lerp(36, 64, rand(S, i, 14));
    p.disc(x, y, lerp(4, 12, k), fit(STONE, 6)[2], 0.42 * (1 - k));
  }
  // The flash itself: two white frames, then red dying away.
  if (a < 0.1) p.disc(0, GEM_Y, 46, WHITE, 1);
  p.glow(0, GEM_Y, lerp(70, 30, u), C[4], Math.max(0, 0.95 * (1 - a / 0.8)));
  p.glow(0, -6, 26, C[3], 0.3 * (1 - u));
}
