using UnityEngine;

namespace SnakeGiuJu
{
    /// <summary>
    /// Bildschirmtexte und Charakterauswahl über IMGUI – braucht keine Fonts oder
    /// Prefabs und skaliert dadurch ohne Zutun auf Handy- wie Desktop-Auflösungen.
    /// </summary>
    public sealed class Hud : MonoBehaviour
    {
        static readonly Color Muted = new Color(0.72f, 0.77f, 0.85f, 1f);
        // Bewusst unabhaengig vom gewaehlten Charakter: Steuerhinweis und Start-Button
        // sollen nicht bei jeder Auswahl zwischen Pink und Blau umspringen. Dunkles,
        // saturiertes Gruen (~5.5:1 Kontrast zu weisser Schrift, WCAG AA fuer
        // Fliesstext braucht 4.5:1) statt des vorherigen hellen Mint/Tuerkis
        // (~2.1:1) - das ging zu sehr Richtung Blau und war mit weissem "START"
        // kaum lesbar.
        static readonly Color StartGreen = new Color(0.125f, 0.470f, 0.196f, 1f);
        // Dieselbe Farbe wie der Kamera-Hintergrund (siehe GameManager), als Karte
        // hinter dem Game-Over-Screen - deckt die zu Ende gefahrene Linie ab, statt
        // sie mit den Ergebnis-Texten ueberlappen zu lassen.
        static readonly Color PanelBackground = new Color(0.043f, 0.055f, 0.078f, 0.94f);

        GameManager game;
        GUIStyle score;
        GUIStyle best;
        GUIStyle title;
        GUIStyle subtitle;
        GUIStyle pickHeading;
        GUIStyle call;
        GUIStyle characterName;
        GUIStyle switchLabel;
        GUIStyle switchState;
        GUIStyle startLabel;
        GUIStyle resultReason;
        GUIStyle resultScore;
        GUIStyle resultBest;
        GUIStyle backLink;
        GUIStyle pickup;
        GUIStyle zoneLeft;
        GUIStyle zoneRight;

        // Abgerundete Formen für Schalter, CTA-Button und Game-Over-Karten, prozedural
        // erzeugt statt als Bilddatei mitgeliefert - siehe UITextures. Bei jeder
        // Größenänderung neu gebaut, zusammen mit den GUIStyles unten.
        Texture2D switchTrackTex;
        Texture2D switchKnobTex;
        Texture2D ctaButtonTex;
        Texture2D panelTex;
        Texture2D statCardTex;

        int builtForHeight = -1;
        int builtForWidth = -1;

        public void Bind(GameManager gameManager) => game = gameManager;

        void OnGUI()
        {
            if (game == null) return;

            BuildStyles();
            float w = Screen.width;
            float h = Screen.height;
            float margin = Mathf.Min(w, h) * 0.04f;
            var bar = new Rect(margin, margin, w - margin * 2f, h * 0.06f);

            // Der Akzent folgt dem gewählten Charakter - nur für Punktestand und
            // Auswahlrahmen. Steuerhinweis und Start-Button bleiben bewusst neutral
            // (weiß / grün), sonst würde die halbe Oberfläche bei jeder Auswahl
            // zwischen Pink und Blau umspringen.
            Color accent = game.Selected.color;
            score.normal.textColor = accent;

            // Auf dem Game-Over-Screen steht Score/Best schon groß in der eigenen
            // Karte - die kleine Kopfzeile würde sich nur doppeln.
            if (game.State != GameState.GameOver)
            {
                if (game.State == GameState.Playing) GUI.Label(bar, $"Score  {game.Score:0}", score);
                GUI.Label(bar, $"Best  {game.BestScore:0}", best);
            }

            switch (game.State)
            {
                case GameState.CharacterSelect:
                    GUI.Label(TextRect(w, h, 0.05f, 0.10f), "SNAKE GIU JU", title);
                    GUI.Label(TextRect(w, h, 0.17f, 0.07f), "Pick a player", pickHeading);
                    DrawPicker(w, h, 0.44f);
                    DrawPowerUpSwitch(w, h, accent);
                    GUI.Label(TextRect(w, h, 0.78f, 0.06f), SteeringHint(), call);
                    DrawButton(HudLayout.StartButton(w, h), StartGreen, "START", startLabel);
                    break;

                case GameState.Playing:
                    DrawBoost(w, h);
                    DrawPickup(w, h);
                    DrawZoneHints();
                    break;

                case GameState.GameOver:
                    DrawGameOverScreen(w, h, accent);
                    break;
            }
        }

        /// <summary>
        /// Eigener Screen statt der Charakterauswahl darunter: keine Avatare mehr (die
        /// gehören jetzt ausschließlich zum Start-Screen), dafür eine deckende Karte,
        /// die die zu Ende gefahrene Linie im Hintergrund abdeckt statt sie mit den
        /// Ergebnis-Texten überlappen zu lassen.
        /// </summary>
        void DrawGameOverScreen(float w, float h, Color accent)
        {
            DrawTexture(new Rect(w * 0.06f, h * 0.06f, w * 0.88f, h * 0.84f), panelTex, PanelBackground);

            GUI.Label(TextRect(w, h, 0.10f, 0.13f), "Ooops", title);

            string reason = game.Cause == DeathCause.Wall ? "You hit the wall" : "You hit the trail";
            GUI.Label(TextRect(w, h, 0.235f, 0.05f), reason, resultReason);

            DrawResultCard(w, h, accent);

            DrawButton(HudLayout.PlayAgainButton(w, h), StartGreen, "PLAY AGAIN", startLabel);

            Rect backRect = HudLayout.BackToStartLink(w, h);
            GUI.Label(backRect, "Back to start", backLink);
        }

        /// <summary>Porträt der gespielten Figur plus Score/Best, groß und für den Anlass etwas spielerisch.</summary>
        void DrawResultCard(float w, float h, Color accent)
        {
            var card = new Rect(w * 0.14f, h * 0.32f, w * 0.72f, h * 0.20f);
            DrawTexture(card, statCardTex, new Color(1f, 1f, 1f, 0.07f));

            float pad = card.height * 0.12f;
            float portraitSize = card.height - pad * 2f;
            var portraitRect = new Rect(card.x + pad, card.y + pad, portraitSize, portraitSize);

            if (game.Selected.portrait != null)
            {
                GUI.DrawTexture(portraitRect, game.Selected.portrait, ScaleMode.ScaleToFit);
            }
            else
            {
                DrawRect(portraitRect, accent);
            }

            float textX = portraitRect.xMax + pad;
            var scoreRect = new Rect(textX, card.y + card.height * 0.12f, card.xMax - textX - pad, card.height * 0.5f);
            var bestRect = new Rect(textX, card.y + card.height * 0.62f, card.xMax - textX - pad, card.height * 0.3f);

            resultScore.normal.textColor = accent;
            GUI.Label(scoreRect, $"Score {game.Score:0}", resultScore);
            GUI.Label(bestRect, $"Best {game.BestScore:0}", resultBest);
        }

        /// <summary>Beide Charaktere nebeneinander, der gewählte hervorgehoben.</summary>
        void DrawPicker(float w, float h, float centerY)
        {
            var characters = game.Characters;
            if (characters == null || characters.Count == 0) return;

            float box = Mathf.Min(w * 0.36f, h * 0.30f);
            float gap = box * 0.18f;
            float totalWidth = characters.Count * box + (characters.Count - 1) * gap;
            float x = (w - totalWidth) * 0.5f;
            float y = h * centerY - box * 0.5f;
            float border = Mathf.Max(2f, box * 0.02f);

            for (int i = 0; i < characters.Count; i++)
            {
                CharacterDefinition character = characters[i];
                var frame = new Rect(x + i * (box + gap), y, box, box);
                bool selected = i == game.SelectedIndex;

                Color previous = GUI.color;
                if (selected)
                {
                    DrawRect(frame, new Color(character.color.r, character.color.g, character.color.b, 0.12f));
                    DrawFrame(frame, character.color, border);
                }
                else
                {
                    // Nicht gewählte Figur bewusst zurücknehmen statt ausgrauen –
                    // die Neonfarbe bleibt so als Vorschau lesbar.
                    GUI.color = new Color(1f, 1f, 1f, 0.45f);
                }

                if (character.portrait != null)
                {
                    float pad = box * 0.06f;
                    var inner = new Rect(frame.x + pad, frame.y + pad, frame.width - pad * 2f, frame.height - pad * 2f);
                    GUI.DrawTexture(inner, character.portrait, ScaleMode.ScaleToFit);
                }
                else
                {
                    DrawRect(frame, character.color);
                }

                GUI.color = previous;

                characterName.normal.textColor = selected ? character.color : Muted;
                GUI.Label(new Rect(frame.x, frame.yMax + box * 0.04f, frame.width, box * 0.22f),
                    character.displayName, characterName);
            }
        }

        /// <summary>
        /// Schalter für den Power-up-Modus. Die Fläche kommt aus <see cref="HudLayout"/>,
        /// weil die Spiellogik dieselbe braucht, um den Tipp nicht als Auswahl zu werten.
        /// </summary>
        void DrawPowerUpSwitch(float w, float h, Color accent)
        {
            Rect rect = HudLayout.PowerUpSwitch(w, h);
            bool on = game.PowerUpsEnabled;

            float trackWidth = rect.height * 1.9f;
            float trackHeight = rect.height * 0.62f;
            var track = new Rect(rect.xMax - trackWidth, rect.y + (rect.height - trackHeight) * 0.5f,
                trackWidth, trackHeight);

            float pad = trackHeight * 0.14f;
            float knob = trackHeight - pad * 2f;
            var knobRect = new Rect(on ? track.xMax - pad - knob : track.x + pad,
                track.y + pad, knob, knob);

            Color previous = GUI.color;
            GUI.color = on ? new Color(accent.r, accent.g, accent.b, 1f) : new Color(1f, 1f, 1f, 0.16f);
            GUI.DrawTexture(track, switchTrackTex);
            GUI.color = on ? Color.white : Muted;
            GUI.DrawTexture(knobRect, switchKnobTex);
            GUI.color = previous;

            // Wie eine Einstellungszeile: Beschriftung links, Zustand und Schaltbahn rechts.
            float gap = rect.height * 0.3f;
            float stateWidth = rect.height * 1.8f;
            var stateRect = new Rect(track.x - gap - stateWidth, rect.y, stateWidth, rect.height);
            var labelRect = new Rect(rect.x, rect.y, stateRect.x - rect.x - gap, rect.height);

            switchState.normal.textColor = on ? accent : Muted;
            GUI.Label(labelRect, SteeringInput.HasTouchscreen ? "Power-ups" : "Power-ups (P)", switchLabel);
            GUI.Label(stateRect, on ? "ON" : "OFF", switchState);
        }

        /// <summary>Gefüllter, abgerundeter Button - für Start, Play again & Co.</summary>
        void DrawButton(Rect rect, Color color, string label, GUIStyle style)
        {
            DrawTexture(rect, ctaButtonTex, color);
            GUI.Label(rect, label, style);
        }

        /// <summary>Restlaufzeit des Temposchubs, damit der Punkteschub nachvollziehbar ist.</summary>
        void DrawBoost(float w, float h)
        {
            if (game.BoostRemaining <= 0f) return;

            Color color = PowerUpRules.ColorOf(PowerUpKind.Speed);
            pickup.normal.textColor = color;
            GUI.Label(TextRect(w, h, 0.115f, 0.07f),
                $"SPEED  x{PowerUpRules.SpeedScoreFactor:0.0} points  ·  {game.BoostRemaining:0.0}s", pickup);
        }

        /// <summary>Kurze Einblendung, was gerade eingesammelt wurde.</summary>
        void DrawPickup(float w, float h)
        {
            const float Duration = 1.2f;
            float age = Time.time - game.LastPickupAt;
            if (age < 0f || age > Duration) return;

            Color color = PowerUpRules.ColorOf(game.LastPickup);
            color.a = 1f - age / Duration;
            title.normal.textColor = color;
            GUI.Label(TextRect(w, h, 0.24f, 0.11f), PowerUpRules.LabelOf(game.LastPickup), title);
            title.normal.textColor = Color.white;
        }

        void DrawZoneHints()
        {
            if (!SteeringInput.HasTouchscreen) return;

            float w = Screen.width;
            float h = Screen.height;
            // Nur ASCII: der eingebaute IMGUI-Font hat keine Pfeil-Glyphen.
            GUI.Label(new Rect(0f, h * 0.90f, w * 0.5f, h * 0.08f), "<<  left", zoneLeft);
            GUI.Label(new Rect(w * 0.5f, h * 0.90f, w * 0.5f, h * 0.08f), "right  >>", zoneRight);
        }

        static string SteeringHint()
        {
            return SteeringInput.HasTouchscreen
                ? "Hold the left or right half of the screen to steer"
                : "Hold the left or right arrow key to steer";
        }

        static Rect TextRect(float w, float h, float top, float height)
        {
            float margin = w * 0.06f;
            return new Rect(margin, h * top, w - margin * 2f, h * height);
        }

        static void DrawRect(Rect rect, Color color)
        {
            DrawTexture(rect, Texture2D.whiteTexture, color);
        }

        static void DrawTexture(Rect rect, Texture texture, Color tint)
        {
            Color previous = GUI.color;
            GUI.color = tint;
            GUI.DrawTexture(rect, texture);
            GUI.color = previous;
        }

        static void DrawFrame(Rect rect, Color color, float thickness)
        {
            DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        void BuildStyles()
        {
            if (builtForHeight == Screen.height && builtForWidth == Screen.width && score != null) return;
            builtForHeight = Screen.height;
            builtForWidth = Screen.width;

            // An der kürzeren Kante ausrichten, sonst wird die Schrift im Hochformat
            // eines Handys so groß, dass Überschrift und Punktestand kollidieren.
            int baseSize = Mathf.Max(11, Mathf.RoundToInt(Mathf.Min(Screen.width, Screen.height * 0.75f) * 0.04f));

            // Ein einziger Schriftschnitt für das ganze HUD (siehe Kommentar oben):
            // fontStyle bleibt überall auf dem Default (Normal), Hierarchie kommt
            // allein aus Größe und Farbe.
            score = new GUIStyle(GUI.skin.label)
            {
                fontSize = baseSize,
                alignment = TextAnchor.UpperLeft
            };

            best = new GUIStyle(score) { alignment = TextAnchor.UpperRight };
            best.normal.textColor = Muted;

            title = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(baseSize * 2.2f),
                alignment = TextAnchor.MiddleCenter
            };
            title.normal.textColor = Color.white;

            subtitle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(baseSize * 0.95f),
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            subtitle.normal.textColor = Muted;

            pickHeading = new GUIStyle(subtitle)
            {
                fontSize = Mathf.RoundToInt(baseSize * 1.1f)
            };
            pickHeading.normal.textColor = Color.white;

            call = new GUIStyle(subtitle);
            call.normal.textColor = Color.white;

            characterName = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(baseSize * 1.2f),
                alignment = TextAnchor.UpperCenter
            };

            switchLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(baseSize * 0.9f),
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false,
                clipping = TextClipping.Clip
            };
            switchLabel.normal.textColor = Muted;

            switchState = new GUIStyle(switchLabel)
            {
                alignment = TextAnchor.MiddleRight
            };

            startLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(baseSize * 1.3f),
                alignment = TextAnchor.MiddleCenter
            };
            startLabel.normal.textColor = Color.white;

            resultReason = new GUIStyle(subtitle);

            resultScore = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(baseSize * 1.5f),
                alignment = TextAnchor.MiddleLeft
            };

            resultBest = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(baseSize * 0.95f),
                alignment = TextAnchor.MiddleLeft
            };
            resultBest.normal.textColor = Muted;

            backLink = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(baseSize * 1.0f),
                alignment = TextAnchor.MiddleCenter
            };
            backLink.normal.textColor = Muted;

            pickup = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(baseSize * 0.95f),
                alignment = TextAnchor.MiddleLeft
            };

            zoneLeft = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(baseSize * 0.85f),
                alignment = TextAnchor.MiddleCenter
            };
            zoneLeft.normal.textColor = new Color(1f, 1f, 1f, 0.35f);
            zoneRight = new GUIStyle(zoneLeft);

            BuildRoundedTextures();
        }

        /// <summary>
        /// Abgerundete Formen in der tatsächlichen Zielgröße neu erzeugen, damit die
        /// Ecken bei jedem Seitenverhältnis sauber rund bleiben statt beim Strecken
        /// einer festen Textur zu verzerren.
        /// </summary>
        void BuildRoundedTextures()
        {
            float w = Screen.width;
            float h = Screen.height;

            Rect switchRect = HudLayout.PowerUpSwitch(w, h);
            float trackHeight = switchRect.height * 0.62f;
            float trackWidth = switchRect.height * 1.9f;
            switchTrackTex = UITextures.RoundedRect(
                Mathf.RoundToInt(trackWidth), Mathf.RoundToInt(trackHeight), trackHeight * 0.5f);

            float pad = trackHeight * 0.14f;
            float knobSize = trackHeight - pad * 2f;
            int knobPx = Mathf.Max(2, Mathf.RoundToInt(knobSize));
            switchKnobTex = UITextures.RoundedRect(knobPx, knobPx, knobPx * 0.5f);

            // Start- und Play-again-Button haben dieselbe Größenformel (siehe
            // HudLayout) und teilen sich deshalb eine Textur.
            Rect ctaRect = HudLayout.StartButton(w, h);
            ctaButtonTex = UITextures.RoundedRect(
                Mathf.RoundToInt(ctaRect.width), Mathf.RoundToInt(ctaRect.height), ctaRect.height * 0.32f);

            var panelRect = new Rect(0, 0, w * 0.88f, h * 0.84f);
            panelTex = UITextures.RoundedRect(
                Mathf.RoundToInt(panelRect.width), Mathf.RoundToInt(panelRect.height),
                Mathf.Min(panelRect.width, panelRect.height) * 0.05f);

            var cardRect = new Rect(0, 0, w * 0.72f, h * 0.20f);
            statCardTex = UITextures.RoundedRect(
                Mathf.RoundToInt(cardRect.width), Mathf.RoundToInt(cardRect.height), cardRect.height * 0.2f);
        }
    }
}
