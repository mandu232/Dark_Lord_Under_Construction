import { lerp, TAU, cycle, fit } from "../../core/index.js";
import { BLOOD, GOLD, GEM_Y, RAD, drawDais, drawGem } from "./_core.js";

export const meta = {
  name: "Dungeon core",
  desc: "An enchanted ruby turns over a rune-carved stone dais, shedding warm sparks as light catches its facets.",
  tags: ["blood", "core", "gem", "magic", "loop"],
  size: [96, 112],
  origin: [48, 100],
  duration: 3,
  loop: true,
  fps: 20,
  options: { palette: BLOOD, seed: 11 },
};

export function draw(p, t, o) {
  const C = fit(o.palette, 8), ph = t / meta.duration;
  const cy = GEM_Y + 2 * Math.sin(TAU * ph);
  drawDais(p, ph, C);
  const pulse = 0.85 + 0.15 * Math.sin(TAU * ph);
  p.glow(0, cy + 6, RAD * 2.1, C[3], 0.29 * pulse);
  // Loose embers drift from the setting and fade into the air.
  cycle(t, meta.duration, 12, o.seed, (r) => r.range(0.9, 1.7), (age, life, r) => {
    const k = age / life;
    const x = r.range(-23, 23) + Math.sin(k * Math.PI) * r.range(-7, 7);
    const y = lerp(-14, r.range(cy - 21, cy + 5), k);
    const alpha = Math.sin(k * Math.PI);
    p.dot(x, y, r.chance(0.35) ? GOLD[3] : GOLD[1], alpha);
    p.glow(x, y, 3, GOLD[2], alpha * 0.22);
  });
  drawGem(p, TAU * ph, C, { cy });
}
