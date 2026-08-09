# SnakeGiuJu

Ein Snake in der Bauart von *Achtung, die Kurve!*: Die gefahrene Linie bleibt für immer
liegen, statt hinten kürzer zu werden. Wer den Rand oder eine Linie berührt, ist raus.
Gelenkt wird nur nach links und rechts, und zwar mit begrenzter Winkelgeschwindigkeit –
dadurch entsteht ein Mindestkurvenradius, scharfe Knicke sind nicht möglich.

Gebaut für WebGL, spielbar im Browser am Rechner und auf dem Handy.

## Steuerung

| Gerät | Links | Rechts | Start / Neustart |
| --- | --- | --- | --- |
| Tastatur | `←` oder `A` | `→` oder `D` | Leertaste oder Enter |
| Touch | linke Bildschirmhälfte halten | rechte Bildschirmhälfte halten | tippen |
| Maus | links klicken und halten | rechts klicken und halten | klicken |

## Aufbau

```
Assets/
  Scenes/Game.unity        Kamera + GameManager, sonst leer – alles Weitere entsteht zur Laufzeit
  Scripts/
    GameManager.cs         Spielzustände, Arena, Kamera, feste Simulationsschrittweite
    CurvePlayer.cs         Kopfbewegung, Mindestradius, Kollisionsprüfung
    ArenaGrid.cs           Belegungsraster: Linien einstempeln, Kreis-Abfrage
    TrailPainter.cs        Linie als LineRenderer-Abschnitte à 128 Punkte
    SteeringInput.cs       Tastatur / Touch / Maus über das Input System
    Hud.cs                 Texte über IMGUI (braucht keine Fonts oder Prefabs)
    MeshShapes.cs          Kreisscheibe für den Kopf
  Shaders/CurveLine.shader Unlit mit Vertexfarbe, URP-tauglich
  Editor/WebGLBuilder.cs   Buildeinstellungen + Build für Menü und CI
  WebGLTemplates/SnakeGiuJu/index.html   Vollbild-Canvas, mobiltauglich
.github/workflows/deploy-pages.yml       Build und Veröffentlichung
```

Die Kollision läuft nicht über die Linienpunkte, sondern über ein Belegungsraster
(0,05 Weltmeter pro Zelle). Die Kosten pro Frame bleiben dadurch konstant, egal wie lang
die Linie schon ist. Das Stück direkt hinter dem Kopf wird bewusst noch nicht eingestempelt,
sonst würde der Kopf sofort mit seinem eigenen Körper kollidieren.

## Spielgefühl anpassen

Alle Stellschrauben hängen am `Game`-Objekt in `Assets/Scenes/Game.unity`:

| Feld | Standard | Wirkung |
| --- | --- | --- |
| `moveSpeed` | 7.2 | Tempo in Weltmetern pro Sekunde |
| `minTurnRadius` | 1.6 | kleinster fahrbarer Kurvenradius |
| `lineWidth` | 0.36 | Strichbreite und zugleich Kollisionsdurchmesser |
| `arenaHeight` | 20 | Höhe des Spielfelds, die Breite folgt dem Seitenverhältnis |
| `cellSize` | 0.05 | Auflösung des Kollisionsrasters |

Die Winkelgeschwindigkeit ergibt sich aus `moveSpeed / minTurnRadius` – wer schneller
fährt, fährt automatisch weitere Kurven.

## Lokal bauen

Menü **SnakeGiuJu → WebGL bauen**, Ergebnis liegt in `Builds/WebGL`. Zum Testen braucht es
einen Webserver, `file://` funktioniert nicht:

```bash
python3 -m http.server 8080 --directory Builds/WebGL
```

## Auf GitHub Pages veröffentlichen

1. Repository anlegen und pushen (Branch `main`).
2. Unter **Settings → Pages** als Source **GitHub Actions** wählen.
3. Unter **Settings → Secrets and variables → Actions** drei Secrets anlegen:
   - `UNITY_EMAIL`, `UNITY_PASSWORD` – die Zugangsdaten des Unity-Kontos
   - `UNITY_LICENSE` – der komplette Inhalt der `Unity_lic.ulf`. Unter macOS liegt sie in
     `/Library/Application Support/Unity/Unity_lic.ulf`. Wer eine Pro-Lizenz nutzt, setzt
     stattdessen `UNITY_SERIAL`.
4. Push auf `main` startet den Workflow; danach liegt das Spiel unter
   `https://<benutzer>.github.io/<repository>/`.

Der Workflow nutzt das Docker-Image von game-ci für die im Projekt eingetragene
Editorversion (6000.5.7f1). Falls für eine Version noch kein Image existiert, schlägt der
Schritt „Build“ mit einem Pull-Fehler fehl – dann in `deploy-pages.yml` eine vorhandene
Version bei `unityVersion` eintragen.

## Warum diese Buildeinstellungen

- **Gzip mit Dekomprimierungs-Fallback**: GitHub Pages liefert keinen
  `Content-Encoding`-Header, der Fallback packt die Dateien deshalb in JavaScript aus.
- **Keine Threads**: dafür wären `COOP`/`COEP`-Header nötig, die GitHub Pages nicht setzt.
- **`.nojekyll`**: sonst schluckt Jekyll Dateien und Ordner mit führendem Unterstrich.
