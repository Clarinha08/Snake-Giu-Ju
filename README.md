# SnakeGiuJu

Ein Snake in der Bauart von *Achtung, die Kurve!*: Die gefahrene Linie bleibt für immer
liegen, statt hinten kürzer zu werden. Wer den Rand oder eine Linie berührt, ist raus.
Gezählt werden Punkte – ein Punkt je gefahrenem Meter, im Power-up-Modus zeitweise mehr.
Gelenkt wird nur nach links und rechts, und zwar mit begrenzter Winkelgeschwindigkeit –
dadurch entsteht ein Mindestkurvenradius, scharfe Knicke sind nicht möglich.

Gebaut für WebGL, primär fürs Handy optimiert, spielbar aber auch im Desktop-Browser.
Die Spieloberfläche ist komplett auf Englisch (Zielgruppe: mobile Spieler); diese
Doku bleibt Deutsch, da sie sich an dich als Entwickler richtet.

## Steuerung

| Gerät | Links | Rechts | Start |
| --- | --- | --- | --- |
| Tastatur | `←` oder `A` | `→` oder `D` | Leertaste oder Enter |
| Touch | linke Bildschirmhälfte halten | rechte Bildschirmhälfte halten | START-Button tippen |
| Maus | links klicken und halten | rechts klicken und halten | START-Button klicken |

## Charakterauswahl

Vor jeder Runde stehen zwei Figuren zur Wahl: **Giu** in Neonpink links und **Ju** in
Neonblau rechts. Die Farbe der Figur färbt die Linie im Spiel, den Akzent im HUD und den
Power-up-Schalter.

Auswählen und Starten sind zwei getrennte Schritte, genau in dieser Reihenfolge auf dem
Bildschirm: Titel, „Pick a player“, Avatare, Power-up-Schalter, Steuerhinweis, START-Button.
Gewählt wird mit derselben Geste wie gelenkt (links/rechts antippen bzw. die Pfeiltasten),
das startet aber nichts von selbst mehr – erst ein Tipp auf **START** (oder Leertaste/Enter)
beginnt die Runde. Der Grund für die Trennung: ein einzelner Tipp auf den Power-up-Schalter
darf nicht gleichzeitig auch die Charakterauswahl treffen oder gar die Runde lostreten.

Der Auswahlscreen erscheint auch nach dem Aus, sodass sich Figur und Modus zwischen zwei
Runden wechseln lassen.

## Power-up-Modus

Ein Schalter unter der Charakterauswahl schaltet den Modus an und aus; auf der Tastatur
tut das auch die Taste `P`. Die Einstellung überlebt einen Neustart des Browsers.

Ist der Modus an, erscheint alle 5 bis 10 Sekunden ein Ring an einer freien Stelle der
Arena. Mehrere Ringe können gleichzeitig liegen, nach 20 Sekunden verschwindet einer
wieder – die letzten vier Sekunden blinkt er als Vorwarnung. Eingesammelt wird durch
Überfahren, der Ring verschwindet dabei.

| Power-up | Farbe | Wirkung |
| --- | --- | --- |
| THICK | Orange, dicker Ring | Linie wird doppelt so breit, gilt bis zum Rundenende |
| THIN | Violett, dünner Ring | Linie wird halb so breit, gilt bis zum Rundenende |
| SPEED | Gelb | 3 Sekunden lang 25 % schneller und 50 % mehr Punkte |

THICK und THIN multiplizieren sich, heben sich also gegenseitig auf, und sind auf das
0,35- bis 3-fache der Grundbreite begrenzt – ohne Grenze liesse sich die Linie bis zur
Unspielbarkeit aufblasen. Ein zweites SPEED verlängert den Schub, statt sich zu stapeln.
Die Anzeigenamen sind Englisch, intern heißen die Enum-Werte weiterhin `Fett`/`Duenn` in
[PowerUps.cs](Assets/Scripts/PowerUps.cs) – reines Codeinterna, nicht sichtbar im Spiel.

Beim Temposchub zieht die Winkelgeschwindigkeit mit dem Tempo mit. Der Mindestkurvenradius
bleibt dadurch derselbe – er ist eine feste Regel des Spiels und soll sich nicht heimlich
ändern, nur weil man schneller unterwegs ist.

Die Zahlen stehen als Konstanten in `PowerUpRules` in
[Assets/Scripts/PowerUps.cs](Assets/Scripts/PowerUps.cs).

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
    CharacterDefinition.cs Name, Neonfarbe und Bild einer wählbaren Figur
    PowerUps.cs            Regeln der Power-ups, Erscheinen, Ablaufen, Aufsammeln
    Hud.cs                 Texte, Auswahlscreen, Schalter und Start-Button über IMGUI
    HudLayout.cs           Flächen, die HUD und Spiellogik gemeinsam brauchen
    UITextures.cs          Abgerundete Formen zur Laufzeit erzeugt (kein Bild nötig)
    MeshShapes.cs          Kreisscheibe für den Kopf, Ring für die Power-ups
  Art/Characters/*.png     Charakterbilder für den Auswahlscreen
  Shaders/CurveLine.shader Unlit mit Vertexfarbe, URP-tauglich
  Shaders/HeadPortrait.shader   Unlit mit Textur, für das Charakterfoto am Kopf
  Editor/WebGLBuilder.cs   Buildeinstellungen + Build für Menü und CI
  WebGLTemplates/SnakeGiuJu/index.html   Vollbild-Canvas, mobiltauglich
Art/generate_characters.py Quelle der Charakterbilder (SVG + Rasterung)
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

Die Liste `characters` am selben Objekt hält Name, Farbe und Bild der wählbaren Figuren.
Sie ist nicht auf zwei begrenzt, allerdings wählt die Links/Rechts-Geste nur die erste
und die letzte – für mehr als zwei Figuren bräuchte die Auswahl eine andere Bedienung.

Die Bilder unter `Assets/Art/Characters/` sind echte Fotos von Giu und Ju, per Hand als
Kreis mit transparenten Ecken zugeschnitten – dafür sorgt
`Art/prepare_avatar_photos.py`:

```bash
python3 Art/prepare_avatar_photos.py eingabe.png Assets/Art/Characters/giu.png
```

Das Skript findet den Bildinhalt selbst (funktioniert mit Fotos auf annähernd weißem
Grund, egal wie viel Rand drumherum ist), schneidet quadratisch zu, skaliert auf 512×512
und schneidet kreisförmig mit weicher Kante frei.

`Art/generate_characters.py` erzeugt stattdessen gezeichnete Platzhalterfiguren im
Comicstil – Quelle der ursprünglichen Illustrationen, bevor echte Fotos an ihre Stelle
traten. Läuft es erneut, überschreibt es `giu.png`/`ju.png` wieder mit der Illustration;
zum Anpassen der echten Fotos also `prepare_avatar_photos.py` verwenden, nicht dieses
Skript.

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
