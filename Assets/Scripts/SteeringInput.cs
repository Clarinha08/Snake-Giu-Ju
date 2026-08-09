using UnityEngine;
using UnityEngine.InputSystem;

namespace SnakeGiuJu
{
    /// <summary>
    /// Steuerung: Pfeiltasten (bzw. A/D) auf der Tastatur, auf dem Touchgerät die
    /// linke bzw. rechte Bildschirmhälfte gedrückt halten. Maus verhält sich wie Touch,
    /// damit sich das Spiel auch im Editor und auf Notebooks ohne Touch testen lässt.
    /// </summary>
    public static class SteeringInput
    {
        /// <summary>-1 = links, +1 = rechts, 0 = geradeaus.</summary>
        public static int ReadSteering()
        {
            int steering = 0;

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed) steering--;
                if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed) steering++;
            }

            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                foreach (var touch in touchscreen.touches)
                {
                    if (!touch.press.isPressed) continue;
                    steering += SideOf(touch.position.ReadValue().x);
                }
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
            {
                steering += SideOf(mouse.position.ReadValue().x);
            }

            return Mathf.Clamp(steering, -1, 1);
        }

        /// <summary>Start bzw. Neustart: Leertaste, Enter oder ein Tippen/Klick.</summary>
        public static bool ConfirmPressed()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame ||
                                     keyboard.enterKey.wasPressedThisFrame ||
                                     keyboard.numpadEnterKey.wasPressedThisFrame))
            {
                return true;
            }

            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                foreach (var touch in touchscreen.touches)
                {
                    if (touch.press.wasPressedThisFrame) return true;
                }
            }

            Mouse mouse = Mouse.current;
            return mouse != null && mouse.leftButton.wasPressedThisFrame;
        }

        /// <summary>
        /// Position des Tipps oder Klicks aus diesem Frame, in Bildschirmpixeln mit
        /// Ursprung unten links. Nötig, um Flächen wie den Power-up-Schalter von der
        /// Start-Geste auszunehmen.
        /// </summary>
        public static bool TryGetPressPosition(out Vector2 position)
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                foreach (var touch in touchscreen.touches)
                {
                    if (!touch.press.wasPressedThisFrame) continue;
                    position = touch.position.ReadValue();
                    return true;
                }
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                position = mouse.position.ReadValue();
                return true;
            }

            position = default;
            return false;
        }

        /// <summary>Tastaturkürzel für den Power-up-Schalter.</summary>
        public static bool TogglePressed()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.pKey.wasPressedThisFrame;
        }

        public static bool HasTouchscreen => Touchscreen.current != null;

        static int SideOf(float screenX) => screenX < Screen.width * 0.5f ? -1 : 1;
    }
}
