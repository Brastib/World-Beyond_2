using UnityEngine;

namespace WorldBeyond.Presentation
{
    /// <summary>Schräg-isometrische Strategiekamera: WASD/Pfeile schwenken, Q/E oder rechte Maustaste drehen, Mausrad zoomen, mittlere Maustaste ziehen.</summary>
    public sealed class CameraRig : MonoBehaviour
    {
        public Vector3 Focus;
        public float Yaw = 45f, Pitch = 50f, Distance = 40f;
        public float MinDistance = 6f, MaxDistance = 140f;
        public Vector2 Bounds = new Vector2(128, 128);
        public Camera Cam;

        void Awake()
        {
            Cam = GetComponent<Camera>();
            if (Cam == null) Cam = gameObject.AddComponent<Camera>();
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.backgroundColor = new Color(0.08f, 0.1f, 0.14f);
            Cam.farClipPlane = 600f;
        }

        public void LookAt(Vector3 p) { Focus = p; }

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            var fwd = Quaternion.Euler(0, Yaw, 0) * Vector3.forward;
            var right = Quaternion.Euler(0, Yaw, 0) * Vector3.right;
            float pan = Distance * 0.9f * dt;
            Vector3 move = Vector3.zero;
            if (InputAdapter.KeyHeld(KeyCode.W) || InputAdapter.KeyHeld(KeyCode.UpArrow)) move += fwd;
            if (InputAdapter.KeyHeld(KeyCode.S) || InputAdapter.KeyHeld(KeyCode.DownArrow)) move -= fwd;
            if (InputAdapter.KeyHeld(KeyCode.D) || InputAdapter.KeyHeld(KeyCode.RightArrow)) move += right;
            if (InputAdapter.KeyHeld(KeyCode.A) || InputAdapter.KeyHeld(KeyCode.LeftArrow)) move -= right;
            Focus += move * pan;
            if (InputAdapter.KeyHeld(KeyCode.Q)) Yaw -= 90f * dt;
            if (InputAdapter.KeyHeld(KeyCode.E)) Yaw += 90f * dt;
            if (!HudGui.PointerOverUi)
            {
                var d = InputAdapter.MouseDelta;
                if (InputAdapter.MouseHeld(InputAdapter.Btn.Right)) { Yaw += d.x * 0.25f; Pitch = Mathf.Clamp(Pitch - d.y * 0.2f, 25f, 85f); }
                if (InputAdapter.MouseHeld(InputAdapter.Btn.Middle)) Focus -= (right * d.x + fwd * d.y) * Distance * 0.002f;
                float s = InputAdapter.Scroll;
                if (Mathf.Abs(s) > 0.01f) Distance = Mathf.Clamp(Distance * (1f - s * 0.1f), MinDistance, MaxDistance);
            }
            Focus.x = Mathf.Clamp(Focus.x, 0, Bounds.x); Focus.z = Mathf.Clamp(Focus.z, 0, Bounds.y); Focus.y = 0;
            var rot = Quaternion.Euler(Pitch, Yaw, 0);
            transform.position = Focus - rot * Vector3.forward * Distance;
            transform.rotation = rot;
        }
    }
}
