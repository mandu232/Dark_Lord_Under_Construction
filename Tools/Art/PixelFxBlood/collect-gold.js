import { fit, sparkle, WHITE } from "../../core/index.js";
import { COLLECT, pop, burst } from "./_collect.js";

export const meta = {
  ...COLLECT,
  name: "Collect: gold",
  desc: "A gold marble pops on pickup like a purse: a gold flash and ring, a fountain of tiny spinning coins that bounce once with a glint and wink out.",
  tags: ["pickup", "collect", "gold", "coins", "one-shot"],
  options: { palette: ["#2a1405", "#5c3008", "#9a5a0e", "#d48e18", "#f5b92c", "#ffde5e", "#fff4bc", "#ffffff"], seed: 4 },
};

// a coin 3 px tall, its face narrowing to the rim and back as it spins
function coin(p, C, x, y, spin) {
  const w = Math.abs(Math.cos(spin));
  if (w > 0.55) {
    p.px(x - 1, y, C[4]); p.px(x + 1, y, C[3]);
    p.px(x, y - 1, C[6]); p.px(x, y + 1, C[3]); p.px(x, y, C[5]);
  } else {
    p.px(x, y - 1, C[6]); p.px(x, y, w > 0.2 ? C[5] : C[4]); p.px(x, y + 1, C[3]);
  }
}

export function draw(p, t, o) {
  const C = fit(o.palette, 8);
  pop(p, t, C);
  burst(t, 8, o.seed, { up: 110, out: 40, g: 340, drag: 2.5, life: [0.42, 0.55] }, (x, y, age, L, landed, r) => {
    const k = 1 - age / L;
    if (k < 0.2 && Math.floor(age * 30) % 2) return;     // wink out
    coin(p, C, x, y, age * r.range(18, 26) + r.range(0, 3));
    p.glow(x, y, 3, C[5], 0.3 * k);
    if (landed && k > 0.45 && k < 0.6) sparkle(p, x, y - 2, 2, WHITE, C[6], 0.5);
  });
}
