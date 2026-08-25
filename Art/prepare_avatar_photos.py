#!/usr/bin/env python3
"""Bereitet ein Foto als Charakter-Avatar auf: auf den Bildinhalt zuschneiden,
quadratisch skalieren, kreisförmig freistellen.

Erwartet ein Foto, das bereits als Kreis auf nahezu weißem Grund vorliegt (so wie
von Bildgeneratoren typischerweise geliefert). Die weißen Ränder sind je nach Bild
unterschiedlich breit, deshalb wird zuerst der tatsächliche Bildinhalt gesucht,
statt eine feste Zuschneidegröße anzunehmen.

    python3 Art/prepare_avatar_photos.py input.png output.png
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import pngtools  # noqa: E402

SIZE = 512
BACKGROUND_THRESHOLD = 245  # ab diesem Wert in allen drei Kanälen gilt ein Pixel als Rand
FEATHER = 2.0  # weiche Kreiskante in Pixeln, gegen Treppenstufen


def find_content_bounds(width, height, pixels):
    """Kleinstes Rechteck, das alle nicht-weißen Pixel enthaelt."""
    min_x, min_y, max_x, max_y = width, height, -1, -1

    for y in range(height):
        row = y * width * 4
        for x in range(width):
            i = row + x * 4
            if pixels[i] < BACKGROUND_THRESHOLD or pixels[i + 1] < BACKGROUND_THRESHOLD \
                    or pixels[i + 2] < BACKGROUND_THRESHOLD:
                if x < min_x: min_x = x
                if x > max_x: max_x = x
                if y < min_y: min_y = y
                if y > max_y: max_y = y

    if max_x < 0:
        raise ValueError("kein Bildinhalt gefunden - nur weiße Pixel")
    return min_x, min_y, max_x, max_y


def resize_bilinear(width, height, pixels, target):
    """Einfache bilineare Skalierung, ausreichend fuer ein einmaliges Vorverarbeiten."""
    out = bytearray(target * target * 4)
    scale_x = width / target
    scale_y = height / target

    for ty in range(target):
        sy = (ty + 0.5) * scale_y - 0.5
        y0 = max(0, min(height - 1, int(sy)))
        y1 = min(height - 1, y0 + 1)
        fy = min(1.0, max(0.0, sy - y0))

        for tx in range(target):
            sx = (tx + 0.5) * scale_x - 0.5
            x0 = max(0, min(width - 1, int(sx)))
            x1 = min(width - 1, x0 + 1)
            fx = min(1.0, max(0.0, sx - x0))

            i00 = (y0 * width + x0) * 4
            i10 = (y0 * width + x1) * 4
            i01 = (y1 * width + x0) * 4
            i11 = (y1 * width + x1) * 4
            oi = (ty * target + tx) * 4

            for c in range(4):
                top = pixels[i00 + c] * (1 - fx) + pixels[i10 + c] * fx
                bottom = pixels[i01 + c] * (1 - fx) + pixels[i11 + c] * fx
                out[oi + c] = round(top * (1 - fy) + bottom * fy)

    return out


def apply_circular_mask(size, pixels):
    """Schneidet kreisförmig frei, mit weicher Kante gegen Treppenstufen."""
    radius = size / 2.0
    cx = cy = radius

    for y in range(size):
        dy = y + 0.5 - cy
        for x in range(size):
            dx = x + 0.5 - cx
            distance = (dx * dx + dy * dy) ** 0.5
            i = (y * size + x) * 4

            if distance <= radius - FEATHER:
                continue
            if distance >= radius:
                pixels[i:i + 4] = b"\x00\x00\x00\x00"
                continue

            coverage = (radius - distance) / FEATHER
            pixels[i + 3] = round(pixels[i + 3] * coverage)


def main():
    if len(sys.argv) != 3:
        sys.exit("Aufruf: prepare_avatar_photos.py <eingabe.png> <ausgabe.png>")

    src_path, dst_path = sys.argv[1], sys.argv[2]
    width, height, pixels = pngtools.read_rgba(src_path)

    min_x, min_y, max_x, max_y = find_content_bounds(width, height, pixels)
    content_w = max_x - min_x + 1
    content_h = max_y - min_y + 1

    # Auf ein Quadrat um den gefundenen Inhalt herum ausdehnen, zentriert -
    # das Foto ist als Kreis angelegt, ein Quadrat um ihn herum reicht als Zuschnitt.
    side = max(content_w, content_h)
    cx = min_x + content_w / 2.0
    cy = min_y + content_h / 2.0
    left = round(cx - side / 2.0)
    top = round(cy - side / 2.0)
    left = max(0, min(width - side, left))
    top = max(0, min(height - side, top))

    cropped = bytearray(side * side * 4)
    for y in range(side):
        src_row = (top + y) * width * 4 + left * 4
        cropped[y * side * 4:(y + 1) * side * 4] = pixels[src_row:src_row + side * 4]

    resized = resize_bilinear(side, side, cropped, SIZE)
    apply_circular_mask(SIZE, resized)

    pngtools.write_rgba(dst_path, SIZE, SIZE, resized)
    print(f"{dst_path} geschrieben ({side}x{side} zugeschnitten -> {SIZE}x{SIZE})")


if __name__ == "__main__":
    main()
