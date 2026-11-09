"""Builds the minion variant sheets from BloodSlimeSheet.png: same 56 frames, so every animation keeps its timing.

Run from the project root: python Tools/Art/MinionVariants/generate.py [--preview out.png]
Then run Tools > Dark Lord > Apply Minion Variants in Unity to slice the sheets and wire up the prefabs.
"""
import struct
import sys
import zlib

SOURCE = "Assets/Sprite/Characters/Minions/BloodSlimeSheet.png"
OUTPUT = "Assets/Sprite/Characters/Minions/%sSheet.png"
CELL = 96


def read_png(path):
    data = open(path, "rb").read()
    pos, idat, width, height = 8, b"", 0, 0
    while pos < len(data):
        size, kind = struct.unpack(">I4s", data[pos:pos + 8])
        chunk = data[pos + 8:pos + 8 + size]
        if kind == b"IHDR":
            width, height, depth, color = struct.unpack(">IIBB", chunk[:10])
            assert depth == 8 and color == 6, "expected 8-bit RGBA"
        elif kind == b"IDAT":
            idat += chunk
        pos += size + 12
    raw, out, previous = zlib.decompress(idat), bytearray(), bytearray(width * 4)
    for y in range(height):
        start = y * (width * 4 + 1)
        kind, row = raw[start], bytearray(raw[start + 1:start + 1 + width * 4])
        for i in range(len(row)):
            a = row[i - 4] if i >= 4 else 0
            b = previous[i]
            c = previous[i - 4] if i >= 4 else 0
            if kind == 1:
                row[i] = (row[i] + a) & 255
            elif kind == 2:
                row[i] = (row[i] + b) & 255
            elif kind == 3:
                row[i] = (row[i] + (a + b) // 2) & 255
            elif kind == 4:
                p = a + b - c
                pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
                row[i] = (row[i] + (a if pa <= pb and pa <= pc else b if pb <= pc else c)) & 255
        out += row
        previous = row
    return width, height, out


def write_png(path, width, height, pixels):
    def chunk(kind, body):
        return struct.pack(">I", len(body)) + kind + body + struct.pack(">I", zlib.crc32(kind + body))
    raw = b"".join(b"\0" + bytes(pixels[y * width * 4:(y + 1) * width * 4]) for y in range(height))
    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)) +
                chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b""))


def rgb(text):
    return tuple(int(text[i:i + 2], 16) for i in (1, 3, 5))


# Each stamp is drawn with its bottom row one pixel above the top of the body.
SKINS = {
    "BoneSlime": {
        "ramp": ["#3b3128", "#7d6a55", "#b8a78c", "#ded0b5", "#fbf4e2"],
        "stamp": ["b..b..b", "b.bb.bb", "bbdbbdb"], "colors": {"b": "#f4ecd8", "d": "#6b5a48"},
    },
}


def is_body(r, g, b, a):
    return a > 0 and r > g + 22 and r > b + 22


def recolor(width, height, source, skin):
    ramp = [rgb(c) for c in skin["ramp"]]
    colors = {key: rgb(value) for key, value in skin["colors"].items()}
    out = bytearray(source)
    rows, columns = height // CELL, width // CELL
    for row in range(rows):
        for column in range(columns):
            left, top = column * CELL, row * CELL
            body = []
            for y in range(top, top + CELL):
                for x in range(left, left + CELL):
                    i = (y * width + x) * 4
                    if is_body(*source[i:i + 4]):
                        body.append((x, y, i))
            if not body:
                continue
            values = sorted(0.3 * source[i] + 0.59 * source[i + 1] + 0.11 * source[i + 2] for _, _, i in body)
            low, high = values[0], max(values[-1], values[0] + 1)
            for x, y, i in body:
                value = (0.3 * source[i] + 0.59 * source[i + 1] + 0.11 * source[i + 2] - low) / (high - low)
                out[i:i + 3] = bytes(ramp[min(len(ramp) - 1, int(value * len(ramp)))])
            ys = [p[1] for p in body]
            body_height = max(ys) - min(ys) + 1
            top_row = min(ys)
            top_xs = sorted(p[0] for p in body if p[1] == top_row)
            anchor = top_xs[len(top_xs) // 2]
            if body_height < 7:  # Flattened attack and death frames carry no accessory.
                continue
            stamp = skin["stamp"]
            for sy, line in enumerate(stamp):
                for sx, key in enumerate(line):
                    if key == ".":
                        continue
                    px = anchor - len(line) // 2 + sx
                    py = top_row - len(stamp) + sy + 1
                    i = (py * width + px) * 4
                    out[i:i + 4] = bytes(colors[key]) + b"\xff"
    return out


def preview(path, width, height, sheets, zoom=4):
    # Idle, attack and death samples for each sheet, cropped tight around the slime.
    samples = [(0, 0), (0, 3), (3, 2), (4, 3), (7, 0), (7, 3)]
    crop = 28
    pad = (CELL - crop) // 2
    out_w, out_h = len(samples) * crop * zoom, len(sheets) * crop * zoom
    out = bytearray(out_w * out_h * 4)
    for s, sheet in enumerate(sheets):
        for n, (row, column) in enumerate(samples):
            for y in range(crop):
                for x in range(crop):
                    i = ((row * CELL + pad + y) * width + column * CELL + pad + x) * 4
                    r, g, b, a = sheet[i:i + 4]
                    color = (40 + (r - 40) * a // 255, 38 + (g - 38) * a // 255, 44 + (b - 44) * a // 255, 255)
                    for zy in range(zoom):
                        start = ((s * crop + y) * zoom + zy) * out_w + (n * crop + x) * zoom
                        for zx in range(zoom):
                            out[(start + zx) * 4:(start + zx) * 4 + 4] = bytes(color)
    write_png(path, out_w, out_h, out)


def main():
    width, height, source = read_png(SOURCE)
    sheets = [source]
    for name, skin in SKINS.items():
        sheet = recolor(width, height, source, skin)
        write_png(OUTPUT % name, width, height, sheet)
        sheets.append(sheet)
    if "--preview" in sys.argv:
        preview(sys.argv[sys.argv.index("--preview") + 1], width, height, sheets)


if __name__ == "__main__":
    main()
