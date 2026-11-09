"""Draws the vampire-court minions frame by frame on the slime sheet's layout, so they run on the same animator clips.

Everything faces right in profile and has no outline: form comes from shading, lit from the upper left, like the player.

Run from the project root: python Tools/Art/MinionVariants/creatures.py [--preview out.png [Name,Name]]
Then run Tools > Dark Lord > Apply Minion Variants in Unity.
"""
import math
import sys

from generate import CELL, OUTPUT, rgb, write_png

ROWS = [("idle", 6), ("walk", 8), ("idle", 6), ("block", 6), ("attack", 8), ("attack2", 8), ("hit", 4), ("die", 10)]
WIDTH, HEIGHT = 960, 768
FEET_X, FEET_Y = 48, 59
SHADOW = (24, 18, 30, 110)
TAU = math.pi * 2
LIGHT = (-.55, -.72)


def ramp(*colors):
    return [rgb(c) for c in colors]


WINE = ramp("#1c0a14", "#3a0f1e", "#5e1426", "#8e1a2e", "#bf2638", "#e04a52", "#f58f8a")
BLOOD = ramp("#2a0508", "#520a12", "#8a0f1c", "#c01a28", "#e83a3c", "#ff7a6a", "#ffc8b4")
DUSK = ramp("#0e0a14", "#1a1220", "#291c2d", "#3a2a3e", "#54405a", "#75607a")
BONE = ramp("#4a3e34", "#7d6a55", "#a8977e", "#cfc1a6", "#ece2cc", "#fbf6e8")
STEEL = ramp("#2c2e3c", "#4c5060", "#7d8494", "#a8aebc", "#d4d8e2", "#f4f6fa")
GOLD = ramp("#5c3a0a", "#96641a", "#d09a32", "#f0c85a", "#fff0a8")
SKIN = ramp("#6e4a5a", "#a87e88", "#d8b4b4", "#f2d8d4", "#fff2ec")
ROT = ramp("#2a332c", "#465248", "#6a7a66", "#93a288", "#bcc8a8", "#dfe6c8")
RUST = ramp("#2a0606", "#5a0c10", "#8e1416", "#c0241e", "#e8482c", "#ff8a5c", "#ffc69a")
SILVER = ramp("#6c6e84", "#9a9cb0", "#c8cad8", "#eceef4", "#ffffff")
EMBER = rgb("#ff2a4a")
HOT = rgb("#ffc2ca")
AMBER = rgb("#ffec9c")
WHITE = rgb("#ffffff")
# Light sources keep their color through the edge shading.
GLOW = {EMBER, HOT, AMBER, WHITE}


def pick(colors, value):
    return colors[max(0, min(len(colors) - 1, int(value * len(colors))))]


class Canvas:
    def __init__(self):
        self.pixels = {}

    def px(self, x, y, color):
        self.pixels[(round(x), round(y))] = color

    def ball(self, cx, cy, rx, ry, colors, boost=0):
        for y in range(int(cy - ry) - 1, int(cy + ry) + 2):
            for x in range(int(cx - rx) - 1, int(cx + rx) + 2):
                nx, ny = (x - cx) / max(rx, .5), (y - cy) / max(ry, .5)
                d = nx * nx + ny * ny
                if d <= 1:
                    self.px(x, y, pick(colors, (nx * LIGHT[0] + ny * LIGHT[1] + math.sqrt(1 - d) * .5 + .68) / 2.3 + boost))

    def limb(self, x0, y0, x1, y1, r0, r1, colors, boost=0):
        dx, dy = x1 - x0, y1 - y0
        length = max(.001, dx * dx + dy * dy)
        pad = max(r0, r1) + 1
        for y in range(int(min(y0, y1) - pad), int(max(y0, y1) + pad) + 1):
            for x in range(int(min(x0, x1) - pad), int(max(x0, x1) + pad) + 1):
                t = max(0, min(1, ((x - x0) * dx + (y - y0) * dy) / length))
                px, py = x0 + dx * t, y0 + dy * t
                r = max(.5, r0 + (r1 - r0) * t)
                ox, oy = (x - px) / r, (y - py) / r
                d = ox * ox + oy * oy
                if d <= 1:
                    self.px(x, y, pick(colors, (ox * LIGHT[0] + oy * LIGHT[1] + math.sqrt(1 - d) * .5 + .68) / 2.3 + boost))

    def poly(self, points, colors, boost=0, flat=False):
        xs, ys = [p[0] for p in points], [p[1] for p in points]
        span = max(1, max(xs) - min(xs) + max(ys) - min(ys))
        for y in range(int(min(ys)) - 1, int(max(ys)) + 2):
            for x in range(int(min(xs)) - 1, int(max(xs)) + 2):
                inside = False
                j = len(points) - 1
                for i in range(len(points)):
                    (ax, ay), (bx, by) = points[i], points[j]
                    if (ay > y) != (by > y) and x < (bx - ax) * (y - ay) / (by - ay) + ax:
                        inside = not inside
                    j = i
                if inside:
                    value = .82 - ((x - min(xs)) * -LIGHT[0] + (y - min(ys)) * -LIGHT[1]) / span * 1.05
                    self.px(x, y, pick(colors, .5 + boost if flat else value + boost))

    def line(self, x0, y0, x1, y1, color):
        steps = max(1, int(max(abs(x1 - x0), abs(y1 - y0))))
        for n in range(steps + 1):
            self.px(x0 + (x1 - x0) * n / steps, y0 + (y1 - y0) * n / steps, color)

    def dissolve(self, amount, seed=1):
        # Stable per pixel, so a dying body eats away instead of flickering.
        for (x, y) in list(self.pixels):
            if ((x * 73856093) ^ (y * 19349663) ^ (seed * 83492791)) % 100 < amount * 100:
                del self.pixels[(x, y)]

    def moved(self, dx, dy):
        out = Canvas()
        out.pixels = {(x + dx, y + dy): color for (x, y), color in self.pixels.items()}
        return out

    def tinted(self, amount):
        if amount > 0:
            for key, (r, g, b) in self.pixels.items():
                self.pixels[key] = (round(r + (255 - r) * amount), round(g + (255 - g) * amount), round(b + (255 - b) * amount))


def stamp(sheet, left, top, canvas, shadow_rx):
    def put(x, y, rgba):
        col, row = FEET_X + x, FEET_Y + y
        if 0 <= col < CELL and 0 <= row < CELL:
            i = ((top + row) * WIDTH + left + col) * 4
            sheet[i:i + 4] = bytes(rgba)
    for y in (0, 1):
        span = shadow_rx - y * 2
        for x in range(-span, span + 1):
            put(x, y, SHADOW)
    for (x, y), color in canvas.pixels.items():
        # With no outline, a darker underside is what lifts the shape off the floor.
        if color not in GLOW and (x, y + 1) not in canvas.pixels:
            color = tuple(round(v * .72) for v in color)
        put(x, y, color + (255,))


def phase(state, i, windup=3, strike=6):
    """Attack beat: 0 winding up, 1 striking, 2 recovering."""
    return 0 if i < windup else 1 if i < strike else 2


def vampire_bat(state, i, n):
    c = Canvas()
    t = i / n
    x, y, tint = 0, -14 + round(math.sin(t * TAU)), 0
    flap = math.sin(t * TAU * (2 if state == "walk" else 1))
    shadow = 5
    bite = False
    if state == "walk":
        x, y = 1, y - 1
    elif state == "block":
        flap = -.9
    elif state in ("attack", "attack2"):
        beat = phase(state, i)
        x, y, flap = ((-3, -17, 1), (8, -10, -.8 if state == "attack" else .3), (2, -13, .4))[beat]
        bite = beat == 1
    elif state == "hit":
        x, y, flap, tint = -3, -13, -.6, .5 if i < 2 else .18
    elif state == "die":
        k = min(1, i / 5)
        y, flap = round(-14 + k * 11), -.4 + k * .4
        shadow = 5 + round(k * 3)
        tint = .25 if i < 1 else 0
    down = state == "die" and i >= 5
    # Far wing reaches forward and sits in shade behind the body.
    far = [(x + 1, y - 2), (x + 6, y - 5 - flap * 6), (x + 12, y - 4 - flap * 9), (x + 9, y - flap * 3), (x + 5, y + 1)]
    c.poly(far, DUSK, .06)
    c.limb(far[0][0], far[0][1], far[1][0], far[1][1], .8, .6, DUSK, .1)
    c.limb(far[1][0], far[1][1], far[2][0], far[2][1], .7, .5, DUSK, .1)
    # Body in profile: a fur barrel with a small head pushed forward.
    c.ball(x, y, 6, 3 if down else 4.8, DUSK, .2)
    c.ball(x + 1, y + 2, 3, 2, WINE, .08)
    if not down:
        c.ball(x + 6, y - 2, 3.6, 3.4, DUSK, .26)
        c.poly([(x + 3, y - 4), (x + 3, y - 9), (x + 6, y - 4)], DUSK, .12)
        c.poly([(x + 4, y - 5), (x + 4, y - 8), (x + 5, y - 5)], WINE, .1, True)
        c.px(x + 8, y - 2, WINE[4])
        c.px(x + 8, y - 1, WINE[3])
        c.px(x + 6, y - 3, EMBER)
        c.px(x + 7, y, WHITE)
        if bite:
            c.px(x + 8, y, WINE[3])
            c.px(x + 7, y + 1, WHITE)
            c.px(x + 8, y + 1, WHITE)
        c.px(x - 1, y + 4, DUSK[1])
        c.px(x + 2, y + 4, DUSK[1])
    # Near wing: fingers over a wine membrane that scallops between them.
    near = [(x + 1, y - 2), (x - 5, y - 3 - flap * 5), (x - 13, y - 3 - flap * 8), (x - 10, y + 1 - flap * 4),
            (x - 8, y - 1 - flap * 2), (x - 5, y + 3 - flap), (x - 3, y), (x, y + 2)]
    c.poly(near, WINE, .02)
    c.limb(near[0][0], near[0][1], near[1][0], near[1][1], 1, .7, DUSK, .28)
    c.limb(near[1][0], near[1][1], near[2][0], near[2][1], .7, .5, DUSK, .28)
    c.line(near[1][0], near[1][1], near[3][0], near[3][1], DUSK[3])
    c.line(near[1][0], near[1][1], near[5][0], near[5][1], DUSK[3])
    if state == "die" and i >= 7:
        c.dissolve((i - 6) / 5, i)
    c.tinted(tint)
    return c, shadow


def blood_golem(state, i, n):
    c = Canvas()
    x, y, tint, front_step, back_step, lean = 0, 0, 0, 0, 0, 0
    hand = (9, -7)
    far_hand = (-7, -9)
    dust = False
    if state == "idle":
        y = 1 if i >= n // 2 else 0
    elif state == "walk":
        a = i % 4
        front_step, back_step = (3, -2) if a < 2 else (-2, 3)
        y = 1 if a in (1, 3) else 0
        hand = (9 - front_step, -7)
        far_hand = (-6 + front_step, -9)
    elif state == "block":
        hand, y = (9, -20), 1
    elif state == "attack":
        beat = phase(state, i, 4)
        if beat == 0:
            hand, lean = (-9, -31 + i), -3
        elif beat == 1:
            hand, lean, y, dust = (15, -3), 3, 2, True
        else:
            hand, lean = (11, -7), 1
    elif state == "attack2":
        beat = phase(state, i)
        if beat == 0:
            hand, lean = (-3, -16), -3
        elif beat == 1:
            hand, lean = (19, -16), 3
        else:
            hand = (11, -10)
    elif state == "hit":
        lean, tint = -2, .5 if i < 2 else .18
    elif state == "die":
        tint = .25 - i * .12
        if i >= 2:
            # The clot gives way and slumps into a heap, its bones left sitting on top.
            k = min(1, (i - 2) / 4)
            h = 22 - k * 17
            c.ball(0, -h / 2, 10 + k * 5, max(2.5, h / 2 + 1), BLOOD, -.02)
            c.ball(-3, -h * .62, 5 + k * 2, max(1.5, h / 4), BLOOD, .14)
            c.limb(-9 - k * 2, -h + 1, -5 - k * 2, -h + 2, 2, 2, BONE)
            c.limb(6 + k * 3, -3, 10 + k * 3, -2, 1.5, 1.5, BONE)
            for dx in (-12, -6, 7, 13):
                c.px(dx * (1 + k * .2), -1 - (dx % 3) * (1 - k), BLOOD[3])
            if i >= 8:
                c.dissolve((i - 7) / 5, i)
            return c, 12
    px = x + lean
    # Far limbs in shade, then a hunched body with the head thrust forward, then the near arm.
    c.limb(px - 3, -21 + y, far_hand[0] + px, far_hand[1] + y, 3.2, 3.6, BLOOD, -.28)
    c.limb(x - 3, -9 + y, x - 3 + back_step, -1, 3.4, 3, BLOOD, -.28)
    c.limb(x - 3 + back_step, -1, x + back_step, -1, 2, 2, BLOOD, -.34)
    c.ball(px - 1, -15 + y, 9, 9.5, BLOOD)
    c.ball(px - 3, -20 + y, 6, 4, BLOOD, .1)
    for ax, ay, bx, by in ((-6, -11, -3, -16), (2, -9, 0, -13), (-1, -22, 2, -18)):
        c.line(px + ax, ay + y, px + bx, by + y, BLOOD[1])
        c.line(px + ax + 1, ay + y, px + bx + 1, by + y, BLOOD[5])
    # Bone plates grown through the back and the skull-like head.
    for bx, by, h in ((-9, -20, 4), (-6, -24, 4), (-2, -26, 3)):
        c.poly([(px + bx - 2, by + 2 + y), (px + bx, by - h + y), (px + bx + 2, by + 2 + y)], BONE)
    c.ball(px + 8, -22 + y, 4.2, 3.8, BONE, .12)
    c.px(px + 10, -23 + y, EMBER)
    c.px(px + 10, -22 + y, BLOOD[1])
    c.line(px + 8, -19 + y, px + 11, -19 + y, BONE[0])
    pulse = HOT if i % 3 != 2 else BLOOD[5]
    c.ball(px + 3, -14 + y, 2.2, 2.2, [BLOOD[3], BLOOD[5]])
    c.px(px + 3, -14 + y, pulse)
    c.limb(x + 3, -9 + y, x + 3 + front_step, -1, 3.8, 3.2, BLOOD, .06)
    c.limb(x + 3 + front_step, -1, x + 7 + front_step, -1, 2, 2, BLOOD, -.08)
    fx, fy = hand[0] + px, hand[1] + y
    c.limb(px + 1, -19 + y, fx, fy, 4, 3.4, BLOOD, .12)
    c.ball(fx, fy, 5.2, 4.6, BLOOD, .06)
    c.ball(fx - 1, fy - 1, 3, 2, BLOOD, .16)
    for kx, ky in ((3, -2), (4, 0), (3, 2)):
        c.px(fx + kx, fy + ky, BONE[4])
        c.px(fx + kx + 1, fy + ky, BONE[3])
    c.ball(px, -21 + y, 3.4, 2.6, BONE, -.04)
    if dust:
        for dx, dy in ((12, 0), (19, 0), (17, -2), (22, -1), (14, -2)):
            c.px(dx, dy, BONE[1])
    c.tinted(max(0, tint))
    return c, 11


def ghoul(state, i, n):
    c = Canvas()
    x, hunch, tint, step = 0, 0, 0, 0
    hand = (9, -5)
    swipe = None
    if state == "idle":
        hunch = 1 if i >= n // 2 else 0
    elif state == "walk":
        step = 3 if i % 4 < 2 else -3
        hunch = 1 if i % 4 in (1, 3) else 0
        x = 1 if i % 4 < 2 else 0
        hand = (9 - step // 2, -5)
    elif state == "block":
        hand, hunch = (7, -15), 2
    elif state in ("attack", "attack2"):
        beat = phase(state, i)
        if beat == 0:
            hand, x = ((-1, -22) if state == "attack" else (-3, -8)), -2
        elif beat == 1:
            hand, x, swipe = ((12, -7) if state == "attack" else (14, -13)), 3, i
        else:
            hand, x = (10, -7), 1
    elif state == "hit":
        x, tint, hunch = -2, .5 if i < 2 else .18, -1
    elif state == "die":
        tint = .25 - i * .12
        if i >= 2:
            k = min(1, (i - 2) / 4)
            drop = k * 11
            c.limb(-8 + k * -2, -2, 2, -2, 1.6, 1.6, ROT, -.14)
            c.limb(-1, -9 + drop * .7, 3 + k * 5, -13 + drop, 3.5, 3, ROT)
            c.poly([(-8, 0), (5, 0), (3, -7 + drop * .5), (-4, -7 + drop * .5)], WINE, -.1)
            c.ball(6 + k * 5, -15 + drop, 3, 2.8, ROT, .12)
            if i < 6:
                c.px(7 + k * 5, -16 + drop, EMBER)
            if i >= 8:
                c.dissolve((i - 7) / 5, i)
            return c, 8
    c.limb(x - 1, -7, x - 2 - step // 2, -1, 1.5, 1.2, ROT, -.26)
    c.limb(x - 2 - step // 2, 0, x - step // 2, 0, 1, 1, ROT, -.3)
    c.limb(x + 1, -14 + hunch, -2 + x + hand[0] // 2, -8 + hunch, 1.3, 1.1, ROT, -.24)
    c.limb(x - 2, -8 + hunch, x + 2, -15 + hunch, 3.2, 3.8, ROT)
    for rx in (-2, 0, 2):
        c.px(x + rx - 1, -13 + hunch + abs(rx), ROT[1])
    c.limb(x + 2, -15 + hunch, x + 6, -17 + hunch, 1.8, 1.6, ROT, .02)
    c.ball(x + 7, -18 + hunch, 3.2, 3, ROT, .14)
    c.px(x + 8, -19 + hunch, EMBER)
    c.line(x + 7, -16 + hunch, x + 10, -16 + hunch, ROT[0])
    c.px(x + 8, -15 + hunch, WHITE)
    c.px(x + 10, -15 + hunch, WHITE)
    c.px(x + 5, -21 + hunch, ROT[0])
    c.px(x + 7, -21 + hunch, ROT[1])
    c.limb(x + 1, -7, x + 2 + step // 2, -1, 1.6, 1.3, ROT, .02)
    c.limb(x + 2 + step // 2, 0, x + 5 + step // 2, 0, 1, 1, ROT, -.06)
    # Rotted wine cloth tied at the waist, torn at the hem.
    c.poly([(x - 5, -9 + hunch), (x + 4, -8 + hunch), (x + 5, -4), (x + 3, -3), (x + 1, -5), (x - 1, -2), (x - 3, -5), (x - 5, -3)], WINE)
    c.limb(x + 2, -14 + hunch, hand[0] + x, hand[1] + hunch, 1.6, 1.2, ROT, .14)
    hx, hy = hand[0] + x, hand[1] + hunch
    for dx in (-1, 1, 3):
        c.px(hx + dx, hy + 1, BONE[4])
        c.px(hx + dx + 1, hy + 2, BONE[3])
    if swipe is not None:
        reach = swipe - 3
        for s in range(3):
            c.line(x + 9 + s * 2, -21 + s * 3 + reach, x + 16 + s, -7 + s + reach, HOT if swipe == 3 else BLOOD[3])
    c.tinted(max(0, tint))
    return c, 7


def blood_wraith(state, i, n):
    c = Canvas()
    t = i / n
    wave = t * TAU * (2 if state == "walk" else 1)
    y = -3 + round(math.sin(t * TAU))
    x, tint, orb = 0, 0, 0
    hand = (8, -13)
    if state == "walk":
        x = 1
    elif state == "block":
        hand = (6, -18)
    elif state in ("attack", "attack2"):
        beat = phase(state, i, 4)
        if beat == 0:
            orb, hand, x = i + 1, ((7, -20) if state == "attack" else (6, -15)), -1
        elif beat == 1:
            orb, hand, x = 6 - i, (12, -14), 2
        else:
            hand = (9, -13)
    elif state == "hit":
        x, tint = -2, .5 if i < 2 else .18
    # A robe seen from the side: straight in front, trailing back into a rippling tail.
    tail = round(math.sin(wave) * 2)
    robe = [(x + 1, -23 + y), (x - 4, -17 + y), (x - 7, -8 + y), (x - 13 + tail, -1 + y), (x - 8, 0 + y + (1 if tail > 0 else 0)),
            (x - 4, -1 + y), (x - 1, 1 + y), (x + 2, -1 + y), (x + 5, 0 + y), (x + 6, -12 + y), (x + 6, -20 + y)]
    c.poly(robe, DUSK, .16)
    for fx, sway in ((-2, 1), (2, -1)):
        c.line(x + fx, -16 + y, x + fx + sway * 2 - 1, 0 + y, WINE[2])
        c.line(x + fx + 1, -14 + y, x + fx + sway * 2, -1 + y, DUSK[1])
    c.line(x - 8, -1 + y, x + 5, -1 + y, WINE[3])
    # Hood in profile with a dark cavity and one burning eye.
    c.ball(x + 1, -20 + y, 5.5, 5.5, DUSK, .3)
    c.line(x - 3, -23 + y, x - 2, -18 + y, WINE[2])
    c.ball(x + 4, -19 + y, 3, 3.4, [DUSK[0]])
    c.px(x + 5, -20 + y, EMBER)
    c.px(x + 6, -20 + y, HOT)
    c.limb(x + 2, -14 + y, hand[0] + x, hand[1] + y, 1.8, 1.4, DUSK, .26)
    c.px(hand[0] + x + 1, hand[1] + y, SKIN[3])
    c.px(hand[0] + x + 2, hand[1] + y - 1, SKIN[3])
    if orb:
        ox, oy, r = hand[0] + x + 4, hand[1] + y - 3, min(4, orb)
        c.ball(ox, oy, r, r, BLOOD, .15)
        c.px(ox - 1, oy - 1, WHITE)
        c.px(ox + r + 1, oy, EMBER)
        c.px(ox, oy - r - 1, EMBER)
    if state == "die":
        k = i / (n - 1)
        out = c.moved(0, -round(k * 7))
        out.dissolve(k ** .8, i)
        return out, max(0, 7 - i)
    c.tinted(tint)
    return c, 7


def blood_bird(state, i, n):
    # A carrion bird of living blood: it perches at rest and flaps low while hauling, shedding drops as it goes.
    c = Canvas()
    t = i / n
    y, x, tint, flap = 0, 0, 0, 0
    wing_open, peck = False, False
    if state == "idle":
        y = 1 if i >= n // 2 else 0
    elif state == "walk":
        wing_open, flap = True, math.sin(t * TAU * 2)
        y, x = -4 + round(flap), 1
    elif state == "block":
        y, x = 1, -1
    elif state in ("attack", "attack2"):
        beat = phase(state, i)
        x, peck = (-1, 3, 1)[beat], beat == 1
        y = 1 if peck else 0
    elif state == "hit":
        x, tint, wing_open, flap = -2, .5 if i < 2 else .18, True, .8
    elif state == "die":
        tint = .25 - i * .12
        if i >= 2:
            # It loses its shape and is only blood again: a falling splash that spreads and sinks.
            k = min(1, (i - 2) / 4)
            h = 11 - k * 9
            c.ball(0, -h / 2, 5 + k * 7, max(1.5, h / 2 + .5), BLOOD)
            for s, (dx, dy) in enumerate(((-9, -3), (8, -4), (-5, -6), (10, -1))):
                if i < 6:
                    c.px(dx + math.copysign(i - 2, dx), dy + (i - 2) + s % 2, BLOOD[4])
            if i >= 8:
                c.dissolve((i - 7) / 5, i)
            return c, 6 + round(k * 5)
    if y >= 0:
        for leg, shade in ((-1, -.28), (2, -.1)):
            c.limb(x + leg, -4 + y, x + leg, 0, .8, .7, BONE, shade - .2)
            c.px(x + leg + 1, 0, BONE[1])
            c.px(x + leg + 2, 0, BONE[1])
    else:
        for leg in (-1, 2):
            c.limb(x + leg, -4 + y, x + leg - 2, -1 + y, .8, .6, BONE, -.3)
            c.px(x + leg - 3, -1 + y, BONE[1])
    if wing_open:
        tip = -14 - flap * 6 + y
        c.poly([(x, -9 + y), (x + 5, tip - 1), (x + 5, -9 + y)], BLOOD, -.3)
    c.poly([(x - 3, -8 + y), (x - 13, -5 + y), (x - 11, -3 + y), (x - 12, -1 + y), (x - 8, -2 + y), (x - 3, -4 + y)], BLOOD, -.12)
    c.line(x - 4, -6 + y, x - 11, -4 + y, BLOOD[1])
    c.ball(x, -7 + y, 6, 4.6, BLOOD)
    peck_x, peck_y = (6, 5) if peck else (4, 0)
    head_x, head_y = x + peck_x, -13 + y + peck_y
    c.limb(x + 3, -9 + y, head_x, head_y, 2.5, 2.2, BLOOD, .08)
    c.ball(head_x, head_y, 3.2, 3, BLOOD, .14)
    c.poly([(head_x + 2, head_y - 2), (head_x + 8, head_y + 1), (head_x + 2, head_y + 2)], BONE)
    c.line(head_x + 2, head_y + 1, head_x + 6, head_y + 1, BONE[0])
    c.px(head_x + 1, head_y - 1, AMBER)
    if wing_open:
        tip = -15 - flap * 7 + y
        wing = [(x + 2, -9 + y), (x - 7, tip), (x - 10, tip + 5), (x - 8, -6 + y - flap * 2), (x - 3, -4 + y)]
        c.poly(wing, BLOOD, .06)
        c.line(x + 1, -9 + y, x - 7, tip, BLOOD[5])
        for fx in (-6, -3):
            c.line(x + fx, -6 + y - flap, x + fx - 3, tip + 5, BLOOD[1])
    else:
        c.poly([(x + 3, -10 + y), (x - 5, -9 + y), (x - 10, -5 + y), (x - 4, -4 + y), (x + 2, -6 + y)], BLOOD, -.06)
        c.line(x + 2, -9 + y, x - 8, -5 + y, BLOOD[5])
        c.line(x - 1, -6 + y, x - 7, -5 + y, BLOOD[1])
    c.px(x - 2, -10 + y, BLOOD[6])
    # Drops peel off and fall, offset per frame so the bird always looks wet.
    if state in ("walk", "idle"):
        for offset, start in ((-7, 0), (-2, 3), (-10, 5)):
            d = (i + start) % 6
            if y < 0 or start == 0:
                c.px(x + offset, -3 + y + d * (2 if y < 0 else 1), BLOOD[4] if d < 4 else BLOOD[2])
    c.tinted(max(0, tint))
    return c, 6


def red_scorpion(state, i, n):
    c = Canvas()
    x, tint = 0, 0
    tip = (-2, -18)
    claw = 2 if i % 4 < 2 else 1
    gait = i % 2 if state == "walk" else 0
    glint = False
    if state == "walk":
        x = 1 if i % 4 < 2 else 0
    elif state == "block":
        tip, claw = (-5, -15), 0
    elif state in ("attack", "attack2"):
        beat = phase(state, i)
        if beat == 0:
            tip, x = (-8, -20), -1
        elif beat == 1:
            tip, x, glint = (11, -13), 2, True
        else:
            tip = (2, -17)
    elif state == "hit":
        x, tint = -2, .5 if i < 2 else .18
    elif state == "die":
        tint = .25 - i * .12
        if i >= 2:
            # Flips onto its back with its legs curled up.
            k = min(1, (i - 2) / 3)
            c.ball(0, -3 + k, 8, 3.2 - k, RUST, -.06)
            for lx in (-5, -2, 1, 4):
                c.limb(lx, -4, lx + 1, -6 - k * 2, .7, .5, RUST, -.2)
            c.limb(-8, -2, -13, -2, 1.8, 1.2, RUST)
            c.px(-15, -2, BONE[4])
            if i >= 7:
                c.dissolve((i - 6) / 5, i)
            return c, 9
    # Far legs, then the body in overlapping plates, near legs, the tail arcing over, and the pincer.
    for s, lx in enumerate((-5, -2, 1, 4)):
        lift = (gait if s % 2 else 1 - gait) if state == "walk" else 0
        c.limb(x + lx + 1, -4, x + lx + 3, -1 - lift, .6, .5, RUST, -.34)
    for s, (px, r) in enumerate(((-6, 2.4), (-3, 3), (0, 3.3), (3, 3.2))):
        c.ball(x + px, -5, r + .4, r, RUST, .1 + s * .02)
        c.px(x + px - 1, -5 - round(r) + 1, RUST[6])
    c.ball(x + 7, -5, 3.2, 2.6, RUST, .12)
    c.px(x + 8, -6, AMBER)
    c.px(x + 9, -5, AMBER)
    for s, lx in enumerate((-5, -2, 1, 4)):
        lift = (1 - gait if s % 2 else gait) if state == "walk" else 0
        c.limb(x + lx, -3, x + lx - 2, 0 - lift, .8, .6, RUST, -.12)
    # Tail segments follow a curve from the rear to the stinger, shrinking as they go.
    start, control = (x - 8, -6), (x - 15, -19)
    end = (x + tip[0], tip[1])
    last = start
    for s in range(1, 13):
        u = s / 12
        px = (1 - u) ** 2 * start[0] + 2 * u * (1 - u) * control[0] + u * u * end[0]
        py = (1 - u) ** 2 * start[1] + 2 * u * (1 - u) * control[1] + u * u * end[1]
        c.ball(px, py, 2.6 - u * 1.1, 2.6 - u * 1.1, RUST, .1 if s % 2 else -.02)
        last = (px, py)
    c.poly([(last[0] - 1, last[1] - 2), (last[0] + 5, last[1] + 2), (last[0] - 1, last[1] + 2)], BONE)
    c.px(last[0] + 5, last[1] + 2, BLOOD[4])
    # Pincer on a jointed arm.
    c.limb(x + 8, -4, x + 11, -6, 1.3, 1.1, RUST, .06)
    c.ball(x + 13, -6, 2.6, 2.2, RUST, .12)
    c.poly([(x + 14, -8), (x + 19, -8 - claw), (x + 15, -6)], RUST, .16)
    c.poly([(x + 14, -5), (x + 19, -4 + claw), (x + 15, -6)], RUST, -.02)
    if glint:
        c.px(last[0] + 6, last[1] + 2, WHITE)
    c.tinted(max(0, tint))
    return c, 9


def web_spider(state, i, n):
    c = Canvas()
    x, y, tint, raised, web = 0, 0, 0, 0, 0
    gait = i % 2 if state == "walk" else 0
    if state == "idle":
        y = 1 if i >= n // 2 else 0
    elif state == "block":
        raised, y = 3, 1
    elif state in ("attack", "attack2"):
        beat = phase(state, i)
        if beat == 0:
            raised, x = 5, -1
        elif beat == 1:
            raised, x, web = 2, 1, i - 2
        else:
            raised = 2
    elif state == "hit":
        x, tint = -2, .5 if i < 2 else .18
    elif state == "die":
        tint = .25 - i * .12
        if i >= 2:
            # Legs curl in under the body as it sinks.
            k = min(1, (i - 2) / 4)
            cy = -7 + k * 4
            for s, reach in enumerate((10, 7, -7, -10)):
                r = reach * (1 - k * .55)
                c.limb(x, cy, x + r, cy - 4 * (1 - k), .7, .5, DUSK, .14)
                c.limb(x + r, cy - 4 * (1 - k), x + r * .7, cy + 3 - k * 3, .6, .4, DUSK, .14)
            c.ball(x - 2, cy, 6, max(2, 5 - k * 2), DUSK, .16)
            c.px(x - 2, cy, BLOOD[3])
            if i >= 7:
                c.dissolve((i - 6) / 5, i)
            return c, 9

    def leg(hip, knee, foot, boost):
        c.limb(hip[0], hip[1], knee[0], knee[1], 1, .8, DUSK, boost)
        c.limb(knee[0], knee[1], foot[0], foot[1], .8, .5, DUSK, boost)

    # Profile legs fan out from the head end: two reach back, two reach forward; the far set sits in shade.
    plan = [(0, -7, -14, -14), (1, -3, -10, -15), (2, 7, 9, -15), (3, 11, 15, -14)]
    for s, knee_x, foot_x, knee_y in plan:
        lift = (gait if s % 2 else 1 - gait) if state == "walk" else 0
        leg((x + 1 + s, -8 + y), (x + knee_x + 2, knee_y + 1 + y - lift), (x + foot_x + 3, -1 - lift), .18)
    c.ball(x - 5, -9 + y, 7, 5.6, DUSK, .28)
    c.ball(x - 7, -11 + y, 3.5, 2, WINE, -.04)
    # Red hourglass on the back, the mark of a venomous court pet.
    c.poly([(x - 8, -12 + y), (x - 3, -12 + y), (x - 5, -9 + y)], BLOOD, .1, True)
    c.poly([(x - 8, -6 + y), (x - 3, -6 + y), (x - 5, -9 + y)], BLOOD, .1, True)
    c.ball(x + 3, -8 + y, 3.8, 3.2, DUSK, .32)
    for ex, ey in ((5, -9), (6, -8), (4, -10)):
        c.px(x + ex, ey + y, EMBER)
    c.px(x + 6, -6 + y, BONE[3])
    c.px(x + 7, -5 + y, BONE[3])
    for s, knee_x, foot_x, knee_y in plan:
        lift = (1 - gait if s % 2 else gait) if state == "walk" else 0
        hip, knee, foot = (x + s - 1, -7 + y), (x + knee_x, knee_y + y - lift), (x + foot_x, -lift)
        if s == 3 and raised:
            knee, foot = (x + 8, -14 + y - raised), (x + 13, -16 + y - raised)
        leg(hip, knee, foot, .4)
    if web:
        for d in range(web * 5):
            c.px(x + 8 + d, -6 + y + (d % 4 == 1), SILVER[3] if d % 2 else SILVER[1])
        for fan in (-1, 1):
            c.line(x + 8 + web * 5, -6 + y, x + 11 + web * 5, -6 + y + fan * 2, SILVER[2])
    c.tinted(max(0, tint))
    return c, 9


def blood_knight(state, i, n):
    c = Canvas()
    x, y, tint, step = 0, 0, 0, 0
    hand, tip = (8, -13), (15, 0)
    arc = None
    if state == "idle":
        y = 1 if i >= n // 2 else 0
    elif state == "walk":
        step = 3 if i % 4 < 2 else -3
        y = 1 if i % 4 in (1, 3) else 0
        hand, tip = (8, -13), (17, -2)
    elif state == "block":
        hand, tip, y = (9, -14), (10, -36), 1
    elif state == "attack":
        beat = phase(state, i)
        if beat == 0:
            hand, tip, x = (-2, -27), (-15, -38), -1
        elif beat == 1:
            hand, tip, x, y, arc = (11, -15), (28, -3), 3, 1, i
        else:
            hand, tip = (9, -13), (20, -2)
    elif state == "attack2":
        beat = phase(state, i)
        if beat == 0:
            hand, tip, x = (-5, -16), (-22, -14), -2
        elif beat == 1:
            hand, tip, x = (13, -18), (32, -19), 4
        else:
            hand, tip = (9, -14), (20, -7)
    elif state == "hit":
        x, tint = -2, .5 if i < 2 else .18
    elif state == "die":
        tint = .25 - i * .12
        if i >= 2:
            # Sinks to a knee, then the armor settles in a heap beside the fallen blade.
            k = min(1, (i - 2) / 4)
            sink = k * 14
            c.poly([(-14, 0), (8, 0), (-2, -4 - (1 - k) * 12)], WINE, -.08)
            c.ball(-1, -9 + sink * .5, 7, max(2.5, 8 - sink * .45), WINE, .1)
            c.ball(-3, -11 + sink * .5, 4, max(1.5, 4 - sink * .2), STEEL, -.1)
            c.ball(3, -18 + sink, 4, 3.6, STEEL, -.12)
            c.px(5, -18 + sink, EMBER if i < 5 else STEEL[0])
            c.limb(8, -1, 24, 0, 1.2, .7, STEEL, .05)
            c.limb(8, -2, 8, 1, .9, .9, GOLD, 0)
            if i >= 8:
                c.dissolve((i - 7) / 5, i)
            return c, 12
    # Cape, far leg, far arm, torso, head, near leg, then the sword arm in front of everything.
    sway = round(math.sin(i / n * TAU) * 1.5)
    flare = 3 if state == "walk" else 0
    c.poly([(x - 2, -27 + y), (x - 8, -24 + y), (x - 12 - flare + sway, -8), (x - 15 - flare + sway, 0), (x - 8 + sway, -1), (x - 3, 0), (x + 1, -14 + y)], WINE, -.04)
    c.line(x - 5, -23 + y, x - 11 - flare + sway, -1, WINE[1])
    c.line(x - 3, -22 + y, x - 7 + sway, -1, WINE[1])
    c.limb(x - 1, -11 + y, x - 2 - step, -2, 2.6, 2.2, STEEL, -.3)
    c.limb(x - 2 - step, -1, x + 1 - step, -1, 1.6, 1.4, STEEL, -.34)
    c.limb(x + 1, -21 + y, x - 5, -13 + y, 2, 1.8, STEEL, -.28)
    c.limb(x, -14 + y, x + 1, -22 + y, 4.8, 5.4, WINE, .08)
    c.ball(x + 2, -21 + y, 3.2, 3.4, STEEL, -.06)
    c.line(x - 4, -12 + y, x + 5, -12 + y, GOLD[3])
    c.line(x - 4, -11 + y, x + 5, -11 + y, GOLD[1])
    c.poly([(x + 1, -12 + y), (x + 7, -10 + y), (x + 5, -4 + y), (x - 1, -5 + y)], WINE, .02)
    c.limb(x + 2, -11 + y, x + 3 + step, -2, 2.8, 2.3, STEEL, .02)
    c.limb(x + 3 + step, -1, x + 8 + step, -1, 1.7, 1.4, STEEL, -.04)
    c.px(x + 3 + step // 2, -6 + y, GOLD[3])
    # Great helm in profile: a jutting visor with a burning slit and one swept horn.
    c.limb(x + 1, -25 + y, x + 2, -28 + y, 2.6, 2.6, STEEL, -.1)
    c.ball(x + 2, -30 + y, 4.6, 4.4, STEEL, .02)
    c.poly([(x + 3, -31 + y), (x + 9, -29 + y), (x + 7, -26 + y), (x + 2, -26 + y)], STEEL, -.02)
    c.line(x + 3, -30 + y, x + 7, -30 + y, STEEL[0])
    c.px(x + 5, -30 + y, EMBER)
    c.px(x + 6, -30 + y, HOT)
    c.poly([(x + 3, -35 + y), (x - 3, -38 + y), (x - 11, -34 + y), (x - 14, -27 + y), (x - 9, -30 + y), (x - 3, -30 + y), (x + 1, -32 + y)], WINE, .12)
    c.line(x + 2, -35 + y, x - 11, -32 + y, WINE[5])
    c.line(x - 2, -32 + y, x - 11, -29 + y, WINE[1])
    c.line(x - 2, -29 + y, x + 5, -33 + y, GOLD[2])
    hx, hy = hand[0] + x, hand[1] + y
    tx, ty = tip[0] + x, tip[1] + y
    c.limb(hx, hy, tx, ty, 1.7, .6, STEEL, .02)
    c.line(hx + (tx - hx) * .1, hy + (ty - hy) * .1 - 1, tx, ty - 1 if abs(tx - hx) > abs(ty - hy) else ty, STEEL[5])
    length = max(1, math.hypot(tx - hx, ty - hy))
    gx, gy = -(ty - hy) / length * 3, (tx - hx) / length * 3
    c.limb(hx + (tx - hx) * .08 + gx, hy + (ty - hy) * .08 + gy, hx + (tx - hx) * .08 - gx, hy + (ty - hy) * .08 - gy, .9, .9, GOLD, .05)
    c.limb(x + 3, -22 + y, hx, hy, 2.4, 2, STEEL, .06)
    c.ball(hx, hy, 2, 2, STEEL, .02)
    c.ball(x + 2, -23 + y, 3.8, 3.4, WINE, .16)
    c.line(x - 1, -25 + y, x + 5, -25 + y, GOLD[3])
    if arc is not None:
        for a in range(0, 14):
            angle = -1.35 + a * .12
            radius = 29 - (arc - 3)
            c.px(x + math.cos(angle) * radius, -9 + math.sin(angle) * radius, HOT if arc == 3 else BLOOD[3])
    c.tinted(max(0, tint))
    return c, 11


def vampire_healer(state, i, n):
    c = Canvas()
    x, y, tint, step, flare = 0, 0, 0, 0, 0
    hand = (7, -10)
    glow = 0
    if state == "idle":
        y = 1 if i >= n // 2 else 0
    elif state == "walk":
        step = 2 if i % 4 < 2 else -2
        flare = 3
    elif state == "block":
        hand, flare = (6, -17), 4
    elif state in ("attack", "attack2"):
        hand = (6, -24) if state == "attack" else (11, -16)
        glow = min(4, i + 1) if i < 6 else 0
        if i >= 6:
            hand = (7, -12)
        flare = 2
    elif state == "hit":
        x, tint = -2, .5 if i < 2 else .18
    elif state == "die":
        tint = .25 - i * .12
    sway = round(math.sin(i / n * TAU))
    # Cape behind, wine lining catching light where it flares.
    c.poly([(x - 1, -20 + y), (x - 6, -17 + y), (x - 10 - flare + sway, -7), (x - 13 - flare + sway, 0), (x - 6 + sway, -1), (x - 1, 0), (x + 1, -10 + y)], DUSK, .1)
    c.poly([(x - 3, -16 + y), (x - 9 - flare + sway, -2), (x - 4 + sway, -2), (x - 1, -9 + y)], WINE, .02)
    c.limb(x - 1, -8 + y, x - 1 - step, -1, 1.5, 1.2, DUSK, .02)
    c.limb(x - 1 - step, 0, x + 1 - step, 0, 1, 1, DUSK, -.04)
    c.limb(x, -8 + y, x + 1, -16 + y, 3.2, 3.5, DUSK, .32)
    c.line(x + 3, -16 + y, x + 3, -9 + y, BLOOD[3])
    c.px(x + 2, -15 + y, SILVER[3])
    c.px(x + 3, -14 + y, SILVER[3])
    c.line(x - 2, -9 + y, x + 3, -9 + y, GOLD[3])
    c.limb(x + 1, -8 + y, x + 1 + step, -1, 1.6, 1.3, DUSK, .3)
    c.limb(x + 1 + step, 0, x + 4 + step, 0, 1, .9, DUSK, .1)
    # High collar stands up behind a pale profile: brow, nose, swept silver hair, red eye.
    c.poly([(x - 2, -24 + y), (x - 4, -18 + y), (x + 2, -16 + y), (x + 1, -19 + y)], WINE, .16)
    c.limb(x + 1, -17 + y, x + 1, -19 + y, 1.3, 1.3, SKIN, -.1)
    c.ball(x + 1, -22 + y, 3.4, 3.4, SKIN, .06)
    c.px(x + 5, -22 + y, SKIN[3])
    c.px(x + 4, -20 + y, SKIN[1])
    c.px(x + 3, -23 + y, EMBER)
    c.px(x + 4, -21 + y, SKIN[0])
    c.poly([(x + 3, -26 + y), (x - 1, -27 + y), (x - 5, -25 + y), (x - 6, -20 + y), (x - 2, -21 + y), (x, -23 + y), (x + 3, -24 + y)], SILVER)
    c.line(x - 4, -24 + y, x - 5, -21 + y, SILVER[0])
    hx, hy = hand[0] + x, hand[1] + y
    c.limb(x + 2, -15 + y, hx, hy, 1.7, 1.2, DUSK, .42)
    c.px(hx + 1, hy, SKIN[3])
    c.px(hx + 2, hy - 1, SKIN[3])
    if glow:
        ox, oy = hx + 2, hy - 4
        c.ball(ox, oy, glow, glow, BLOOD, .2)
        c.px(ox - 1, oy - 1, WHITE)
        for a in range(4):
            angle = a * TAU / 4 + i * .9
            c.px(ox + math.cos(angle) * (glow + 2), oy + math.sin(angle) * (glow + 2), EMBER)
    if state == "die" and i >= 2:
        # Comes apart upward into a swarm of small bats.
        k = (i - 2) / (n - 3)
        out = c.moved(0, -round(k * 4))
        out.dissolve(k ** .7, i)
        for b, (bx, by) in enumerate(((-9, -16), (7, -22), (-4, -27), (10, -13), (-11, -8))):
            ax, ay = bx + round(math.copysign(k * 7, bx)), by - round(k * 9)
            f = (i + b) % 2
            out.px(ax, ay, DUSK[3])
            for side in (-1, 1):
                out.px(ax + side, ay - f, DUSK[2])
                out.px(ax + side * 2, ay + 1 - f * 2, DUSK[1])
        return out, max(0, 7 - i)
    c.tinted(max(0, tint))
    return c, 7


CREATURES = {
    "VampireBat": vampire_bat, "BloodGolem": blood_golem, "Ghoul": ghoul, "BloodWraith": blood_wraith, "BloodBird": blood_bird,
    "RedScorpion": red_scorpion, "WebSpider": web_spider, "BloodKnight": blood_knight, "VampireHealer": vampire_healer,
}


def draw_sheet(creature):
    sheet = bytearray(WIDTH * HEIGHT * 4)
    for row, (state, count) in enumerate(ROWS):
        for i in range(count):
            canvas, shadow = creature(state, i, count)
            stamp(sheet, i * CELL, row * CELL, canvas, shadow)
    return sheet


def preview(path, sheets, zoom=4):
    # One row per creature: idle, walking, windup, strike and death samples on a floor-toned background.
    samples = [(0, 0), (1, 1), (1, 3), (4, 1), (4, 3), (5, 3), (6, 0), (7, 3), (7, 5), (7, 7)]
    crop_w, crop_h = 62, 50
    left_pad, top_pad = (CELL - crop_w) // 2, FEET_Y - crop_h + 6
    out_w, out_h = len(samples) * crop_w * zoom, len(sheets) * crop_h * zoom
    out = bytearray(out_w * out_h * 4)
    for s, sheet in enumerate(sheets):
        for n, (row, i) in enumerate(samples):
            for y in range(crop_h):
                for x in range(crop_w):
                    p = ((row * CELL + top_pad + y) * WIDTH + i * CELL + left_pad + x) * 4
                    r, g, b, a = sheet[p:p + 4]
                    color = bytes((92 + (r - 92) * a // 255, 84 + (g - 84) * a // 255, 90 + (b - 90) * a // 255, 255))
                    for zy in range(zoom):
                        start = ((s * crop_h + y) * zoom + zy) * out_w + (n * crop_w + x) * zoom
                        for zx in range(zoom):
                            out[(start + zx) * 4:(start + zx) * 4 + 4] = color
    write_png(path, out_w, out_h, out)


def main():
    arg = sys.argv.index("--preview") if "--preview" in sys.argv else -1
    only = sys.argv[arg + 2].split(",") if arg >= 0 and len(sys.argv) > arg + 2 else list(CREATURES)
    shown = []
    for name, creature in CREATURES.items():
        sheet = draw_sheet(creature)
        write_png(OUTPUT % name, WIDTH, HEIGHT, sheet)
        if name in only:
            shown.append(sheet)
    if arg >= 0:
        preview(sys.argv[arg + 1], shown)


if __name__ == "__main__":
    main()
