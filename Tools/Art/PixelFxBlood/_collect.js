// Shared by the four pickup effects. The pop (a white flash, a cross of light, a ring snapping out) follows
// pixelfx's ui/coin-collect and pickups/collect-burst; each drop then throws out debris of its own kind.
import { ease, rng, WHITE } from "../../core/index.js";

// Centred on the spot where the orb was absorbed; small, because the orbs are small.
export const COLLECT = { size: [32, 32], origin: [16, 18], duration: 0.6, loop: false, fps: 30, bg: "dark" };

// The flash, the cross and the ring, all gone by about 0.3 s.
export function pop(p, t, C) {
  if (t < 0.05) p.circle(0, 0, 3.5 * (1 - t / 0.05) + 1, WHITE);
  if (t < 0.12) {
    const k = 1 - t / 0.12, L = 9 * ease.outCubic(Math.min(1, t / 0.03 + 0.3)) * k;
    for (const [dx, dy, l] of [[1, 0, 1], [-1, 0, 1], [0, 1, 0.6], [0, -1, 0.6]])
      p.line(0, 0, dx * L * l, dy * L * l, (u) => (u < 0.35 ? WHITE : C[6]), (u) => (u < 0.5 ? 1 : (1 - u) * 2));
    p.glow(0, 0, 10 * k + 3, C[5], 0.6 * k);
  }
  // the ring takes the drop's own colour after its first frame, and is gone before the debris settles
  const u = t / 0.2;
  if (u < 1) {
    const r = 2 + 8 * ease.outExpo(u);
    p.ring(0, 0, r, 1, u < 0.2 ? WHITE : u < 0.5 ? C[6] : C[5], u < 0.5 ? 1 : 2 * (1 - u));
    p.glowRing(0, 0, r, 3, C[5], 0.3 * (1 - u));
  }
}

// n pieces thrown up and out from the centre, braking with drag and falling to a floor just below; `fn` draws
// each one from (x, y, age, life, landed, r). Every piece ends before the effect does.
export function burst(t, n, seed, { up = 90, out = 45, g = 260, drag = 3, delay = 0.02, life = [0.3, 0.5] } = {}, fn) {
  for (let i = 0; i < n; i++) {
    const r = rng(seed, i), a = ((i + r.range(-0.3, 0.3)) / n) * Math.PI * 2;
    const vx = Math.cos(a) * out * r.range(0.6, 1.1), vy = -up * r.range(0.6, 1.1) + Math.sin(a) * out * 0.4;
    const d = delay + r.range(0, 0.04), L = r.range(life[0], life[1]), age = t - d, floor = r.range(2, 5);
    if (age < 0 || age > L) continue;
    const e = (1 - Math.exp(-drag * age)) / drag;
    let x = vx * e, y = vy * e + 0.5 * g * age * age, landed = false;
    if (y > floor) { y = floor; landed = true; }
    fn(x, y, age, L, landed, r, i);
  }
}
