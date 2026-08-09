#!/usr/bin/env python3
"""Erzeugt die Charakterbilder fuer den Auswahlscreen.

Schreibt je eine SVG-Quelle nach Art/ und rastert sie nach
Assets/Art/Characters/<name>.png. Die beiden Figuren teilen sich dieselbe
Zeichnung und unterscheiden sich nur in Palette, Blickrichtung und Kamm.

    python3 Art/generate_characters.py

Gerastert wird ueber QuickLook (macOS-Bordmittel), damit keine zusaetzliche
Abhaengigkeit noetig ist. Zwei Eigenheiten von QuickLook sind eingepreist:

* Es skaliert die SVG nicht auf die Thumbnailgroesse, deshalb ist das Dokument
  bereits in Zielaufloesung angelegt.
* Es liefert immer ein deckendes Bild. Jede Figur wird daher zweimal gerendert,
  auf Weiss und auf Schwarz, und der Alphakanal daraus zurueckgerechnet.
"""
import os
import shutil
import subprocess
import sys
import tempfile

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import pngtools  # noqa: E402

SIZE = 512
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SVG_DIR = os.path.join(ROOT, "Art")
PNG_DIR = os.path.join(ROOT, "Assets", "Art", "Characters")

OUTLINE = "#0A0C12"
TONGUE = "#FF3B5C"

CHARACTERS = [
    {
        "file": "giu",
        # Neonblau
        "base": "#22E3FF",
        "dark": "#0C87AD",
        "light": "#BEF6FF",
        "crest": "spikes",
        "faces": "right",
    },
    {
        "file": "ju",
        # Neonpink
        "base": "#FF3FD0",
        "dark": "#A81287",
        "light": "#FFC2F0",
        "crest": "curl",
        "faces": "left",
    },
]


def crest(kind, c):
    """Kopfschmuck - das einzige Formmerkmal, das die beiden unterscheidet."""
    if kind == "spikes":
        spikes = [(196, 128, 214, 52), (256, 116, 256, 36), (316, 128, 298, 52)]
        parts = []
        for x0, y0, x1, y1 in spikes:
            parts.append(
                f'<path d="M {x0 - 26} {y0 + 14} L {x1} {y1} L {x0 + 26} {y0 + 14} Z" '
                f'fill="{c["base"]}" stroke="{OUTLINE}" stroke-width="12" stroke-linejoin="round"/>'
            )
        return "\n    ".join(parts)

    return (
        f'<path d="M 256 128 C 250 60 300 34 330 62 C 352 82 336 116 312 106" '
        f'fill="none" stroke="{OUTLINE}" stroke-width="34" stroke-linecap="round"/>\n    '
        f'<path d="M 256 128 C 250 60 300 34 330 62 C 352 82 336 116 312 106" '
        f'fill="none" stroke="{c["base"]}" stroke-width="20" stroke-linecap="round"/>'
    )


def svg(c, background=None):
    backdrop = f'  <rect width="{SIZE}" height="{SIZE}" fill="{background}"/>\n' if background else ""
    body = f"""  <g>
    <!-- Neonschein -->
    <circle cx="256" cy="268" r="232" fill="{c['base']}" opacity="0.13"/>
    <circle cx="256" cy="268" r="206" fill="{c['base']}" opacity="0.18"/>

    <!-- Schwanzspitze -->
    <path d="M 108 462 C 44 460 36 396 80 380 C 104 372 120 388 114 404"
          fill="none" stroke="{OUTLINE}" stroke-width="40" stroke-linecap="round"/>
    <path d="M 108 462 C 44 460 36 396 80 380 C 104 372 120 388 114 404"
          fill="none" stroke="{c['base']}" stroke-width="24" stroke-linecap="round"/>

    <!-- untere Windung -->
    <rect x="92" y="390" width="330" height="86" rx="43" fill="{OUTLINE}"/>
    <rect x="104" y="402" width="306" height="62" rx="31" fill="{c['base']}"/>
    <path d="M 118 446 q 138 30 276 0 q -138 22 -276 0 z" fill="{c['dark']}"/>

    <!-- obere Windung -->
    <rect x="128" y="322" width="256" height="84" rx="42" fill="{OUTLINE}"/>
    <rect x="140" y="334" width="232" height="60" rx="30" fill="{c['base']}"/>
    <path d="M 152 378 q 108 26 208 0 q -104 20 -208 0 z" fill="{c['dark']}"/>

    {crest(c['crest'], c)}

    <!-- Kopf -->
    <ellipse cx="256" cy="230" rx="134" ry="124" fill="{OUTLINE}"/>
    <ellipse cx="256" cy="230" rx="120" ry="110" fill="{c['base']}"/>
    <g clip-path="url(#head)">
      <ellipse cx="372" cy="336" rx="156" ry="146" fill="{c['dark']}" opacity="0.9"/>
      <ellipse cx="182" cy="146" rx="86" ry="66" fill="{c['light']}" opacity="0.5"/>
    </g>

    <!-- Brauen, bewusst innerhalb der Kopfsilhouette -->
    <path d="M 178 160 L 238 178" stroke="{OUTLINE}" stroke-width="17" stroke-linecap="round"/>
    <path d="M 274 178 L 334 160" stroke="{OUTLINE}" stroke-width="17" stroke-linecap="round"/>

    <!-- Augen -->
    <ellipse cx="206" cy="216" rx="41" ry="47" fill="#FFFFFF" stroke="{OUTLINE}" stroke-width="11"/>
    <ellipse cx="306" cy="216" rx="41" ry="47" fill="#FFFFFF" stroke="{OUTLINE}" stroke-width="11"/>
    <circle cx="219" cy="222" r="20" fill="{OUTLINE}"/>
    <circle cx="319" cy="222" r="20" fill="{OUTLINE}"/>
    <circle cx="211" cy="212" r="8" fill="#FFFFFF"/>
    <circle cx="311" cy="212" r="8" fill="#FFFFFF"/>

    <!-- Nasenloecher -->
    <circle cx="238" cy="266" r="7" fill="{OUTLINE}"/>
    <circle cx="274" cy="266" r="7" fill="{OUTLINE}"/>

    <!-- Grinsen und Zunge -->
    <path d="M 202 288 q 54 48 108 0" fill="none" stroke="{OUTLINE}"
          stroke-width="15" stroke-linecap="round"/>
    <path d="M 256 310 L 256 330" stroke="{TONGUE}" stroke-width="12" stroke-linecap="round"/>
    <path d="M 256 330 L 243 344" stroke="{TONGUE}" stroke-width="12" stroke-linecap="round"/>
    <path d="M 256 330 L 269 344" stroke="{TONGUE}" stroke-width="12" stroke-linecap="round"/>
  </g>"""

    # Nach links blickende Figur: gesamte Zeichnung spiegeln.
    if c["faces"] == "left":
        body = f'  <g transform="translate({SIZE},0) scale(-1,1)">\n{body}\n  </g>'

    return f"""<svg xmlns="http://www.w3.org/2000/svg" width="{SIZE}" height="{SIZE}" viewBox="0 0 {SIZE} {SIZE}">
  <defs>
    <clipPath id="head"><ellipse cx="256" cy="230" rx="120" ry="110"/></clipPath>
  </defs>
{backdrop}{body}
</svg>
"""


def rasterize(markup, work_dir, name):
    """Schreibt die SVG und gibt das von QuickLook erzeugte PNG als RGBA zurueck."""
    svg_path = os.path.join(work_dir, name + ".svg")
    with open(svg_path, "w", encoding="utf-8") as fh:
        fh.write(markup)

    subprocess.run(["qlmanage", "-t", "-s", str(SIZE), "-o", work_dir, svg_path],
                   stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, check=True)

    produced = svg_path + ".png"
    if not os.path.exists(produced):
        sys.exit(f"QuickLook hat kein PNG fuer {name} erzeugt.")
    return pngtools.read_rgba(produced)


def main():
    if not shutil.which("qlmanage"):
        sys.exit("qlmanage nicht gefunden - dieses Skript braucht macOS.")

    os.makedirs(PNG_DIR, exist_ok=True)
    for c in CHARACTERS:
        # Die Quelle ohne Hintergrund bleibt zum Weiterbearbeiten liegen.
        with open(os.path.join(SVG_DIR, c["file"] + ".svg"), "w", encoding="utf-8") as fh:
            fh.write(svg(c))

        with tempfile.TemporaryDirectory() as work:
            on_white = rasterize(svg(c, "#FFFFFF"), work, c["file"] + "_w")
            on_black = rasterize(svg(c, "#000000"), work, c["file"] + "_b")

        width, height, pixels = pngtools.recover_alpha(on_white, on_black)
        target = os.path.join(PNG_DIR, c["file"] + ".png")
        pngtools.write_rgba(target, width, height, pixels)

        opaque = sum(1 for i in range(3, len(pixels), 4) if pixels[i] > 250)
        print(f"{c['file']}.png geschrieben, {100 * opaque // (width * height)} % deckend")


if __name__ == "__main__":
    main()
