"""Minimaler PNG-Leser und -Schreiber (nur RGBA, 8 bit) ohne Fremdbibliotheken."""
import struct
import zlib


def read_rgba(path):
    """Liest ein 8-bit-PNG (RGB oder RGBA) und gibt (breite, hoehe, RGBA-bytearray) zurueck.

    RGB-Quellen (Farbtyp 2, z. B. von sips aus WebP konvertiert) werden mit
    Alpha 255 aufgefuellt, damit der Rest der Pipeline immer mit RGBA arbeitet.
    """
    data = open(path, "rb").read()
    if data[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError(f"{path}: kein PNG")

    pos, idat, width, height, color_type = 8, bytearray(), None, None, None
    while pos < len(data):
        length = struct.unpack(">I", data[pos:pos + 4])[0]
        chunk = data[pos + 4:pos + 8]
        body = data[pos + 8:pos + 8 + length]
        pos += 12 + length
        if chunk == b"IHDR":
            width, height, depth, color_type, _, _, interlace = struct.unpack(">IIBBBBB", body)
            if depth != 8 or color_type not in (2, 6) or interlace != 0:
                raise ValueError(f"{path}: erwartet 8-bit RGB oder RGBA ohne Interlace")
        elif chunk == b"IDAT":
            idat += body
        elif chunk == b"IEND":
            break

    channels = 3 if color_type == 2 else 4
    raw = zlib.decompress(bytes(idat))
    stride = width * channels
    out = bytearray(stride * height)
    previous = bytearray(stride)
    pos = 0

    for y in range(height):
        filter_type = raw[pos]
        pos += 1
        line = bytearray(raw[pos:pos + stride])
        pos += stride

        if filter_type == 1:
            for x in range(channels, stride):
                line[x] = (line[x] + line[x - channels]) & 255
        elif filter_type == 2:
            for x in range(stride):
                line[x] = (line[x] + previous[x]) & 255
        elif filter_type == 3:
            for x in range(stride):
                left = line[x - channels] if x >= channels else 0
                line[x] = (line[x] + ((left + previous[x]) >> 1)) & 255
        elif filter_type == 4:
            for x in range(stride):
                a = line[x - channels] if x >= channels else 0
                b = previous[x]
                c = previous[x - channels] if x >= channels else 0
                p = a + b - c
                pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
                predictor = a if (pa <= pb and pa <= pc) else (b if pb <= pc else c)
                line[x] = (line[x] + predictor) & 255
        elif filter_type != 0:
            raise ValueError(f"{path}: unbekannter Zeilenfilter {filter_type}")

        out[y * stride:(y + 1) * stride] = line
        previous = line

    if channels == 4:
        return width, height, out

    rgba = bytearray(width * height * 4)
    for i in range(width * height):
        rgba[i * 4:i * 4 + 3] = out[i * 3:i * 3 + 3]
        rgba[i * 4 + 3] = 255
    return width, height, rgba


def write_rgba(path, width, height, pixels):
    """Schreibt ein 8-bit-RGBA-PNG, unkomprimierte Zeilenfilter."""
    stride = width * 4
    raw = bytearray()
    for y in range(height):
        raw.append(0)
        raw += pixels[y * stride:(y + 1) * stride]

    def chunk(tag, body):
        return (struct.pack(">I", len(body)) + tag + body
                + struct.pack(">I", zlib.crc32(tag + body) & 0xFFFFFFFF))

    with open(path, "wb") as fh:
        fh.write(b"\x89PNG\r\n\x1a\n")
        fh.write(chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)))
        fh.write(chunk(b"IDAT", zlib.compress(bytes(raw), 9)))
        fh.write(chunk(b"IEND", b""))


def recover_alpha(on_white, on_black):
    """Rechnet Deckung und Farbe aus zwei Renderings zurueck.

    Beim Komponieren gilt fuer jeden Kanal
        weiss  = farbe * a + 1 * (1 - a)
        schwarz = farbe * a
    also a = 1 - (weiss - schwarz) und farbe = schwarz / a. Das ist exakt und
    behandelt weiche Kanten korrekt - anders als ein Aussschneiden nach Farbe,
    das die weissen Augen mit durchlöchern wuerde.
    """
    width, height, w = on_white
    _, _, b = on_black
    out = bytearray(len(w))

    for i in range(0, len(w), 4):
        alpha = 0
        for c in range(3):
            alpha += 255 - (w[i + c] - b[i + c])
        alpha = max(0, min(255, alpha // 3))

        if alpha == 0:
            out[i:i + 4] = b"\x00\x00\x00\x00"
            continue

        for c in range(3):
            value = b[i + c] * 255 // alpha
            out[i + c] = min(255, value)
        out[i + 3] = alpha

    return width, height, out
