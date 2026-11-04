using UnityEngine;
#if WB_HAS_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace WorldBeyond.Presentation
{
    /// <summary>Kapselt Maus/Tastatur, damit der Code mit neuem Input System und altem Input Manager läuft.</summary>
    public static class InputAdapter
    {
        public enum Btn { Left, Right, Middle }

#if WB_HAS_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
        public static Vector2 MousePosition => Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        public static float Scroll => Mouse.current != null ? Mouse.current.scroll.ReadValue().y / 120f : 0f;
        public static Vector2 MouseDelta => Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
        static UnityEngine.InputSystem.Controls.ButtonControl B(Btn b) =>
            Mouse.current == null ? null : b == Btn.Left ? Mouse.current.leftButton : b == Btn.Right ? Mouse.current.rightButton : Mouse.current.middleButton;
        public static bool MouseDown(Btn b) { var c = B(b); return c != null && c.wasPressedThisFrame; }
        public static bool MouseHeld(Btn b) { var c = B(b); return c != null && c.isPressed; }
        public static bool KeyDown(KeyCode k) { var c = Map(k); return c != null && c.wasPressedThisFrame; }
        public static bool KeyHeld(KeyCode k) { var c = Map(k); return c != null && c.isPressed; }

        static UnityEngine.InputSystem.Controls.KeyControl Map(KeyCode k)
        {
            var kb = Keyboard.current;
            if (kb == null) return null;
            switch (k)
            {
                case KeyCode.W: return kb.wKey; case KeyCode.A: return kb.aKey; case KeyCode.S: return kb.sKey; case KeyCode.D: return kb.dKey;
                case KeyCode.Q: return kb.qKey; case KeyCode.E: return kb.eKey; case KeyCode.B: return kb.bKey; case KeyCode.R: return kb.rKey;
                case KeyCode.L: return kb.lKey; case KeyCode.UpArrow: return kb.upArrowKey; case KeyCode.DownArrow: return kb.downArrowKey;
                case KeyCode.LeftArrow: return kb.leftArrowKey; case KeyCode.RightArrow: return kb.rightArrowKey;
                case KeyCode.Space: return kb.spaceKey; case KeyCode.Escape: return kb.escapeKey;
                case KeyCode.Alpha1: return kb.digit1Key; case KeyCode.Alpha2: return kb.digit2Key;
                case KeyCode.Alpha3: return kb.digit3Key; case KeyCode.Alpha4: return kb.digit4Key;
                case KeyCode.F3: return kb.f3Key; case KeyCode.F5: return kb.f5Key; case KeyCode.F9: return kb.f9Key;
                case KeyCode.F12: return kb.f12Key;
                default: return null;
            }
        }
#else
        public static Vector2 MousePosition => Input.mousePosition;
        public static float Scroll => Input.mouseScrollDelta.y;
        public static Vector2 MouseDelta => new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 10f;
        public static bool MouseDown(Btn b) => Input.GetMouseButtonDown((int)b);
        public static bool MouseHeld(Btn b) => Input.GetMouseButton((int)b);
        public static bool KeyDown(KeyCode k) => Input.GetKeyDown(k);
        public static bool KeyHeld(KeyCode k) => Input.GetKey(k);
#endif
    }
}
