using UnityEngine;

namespace Farm
{
    /// <summary>Orthographic camera following the farmer, with drag-pan and pinch zoom.</summary>
    public class CameraController : MonoBehaviour
    {
        public static CameraController I { get; private set; }
        public Camera Cam { get; private set; }

        public const float MinSize = 3.2f, MaxSize = 8f;
        private Vector3 panOffset;
        private float targetSize;
        private Bounds bounds;

        public float Zoom => targetSize;

        public void Init(Camera cam, float zoom)
        {
            I = this;
            Cam = cam;
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.36f, 0.62f, 0.78f);
            cam.nearClipPlane = -50f;
            cam.farClipPlane = 50f;
            cam.transparencySortMode = TransparencySortMode.Orthographic;
            targetSize = Mathf.Clamp(zoom <= 0 ? 6f : zoom, MinSize, MaxSize);
            cam.orthographicSize = targetSize;
            bounds = WorldMap.I.UnlockedBounds;
            WorldMap.I.ZonesChanged += () => bounds = WorldMap.I.UnlockedBounds;
            Vector3 f = FarmerController.I.transform.position;
            transform.position = new Vector3(f.x, f.y, -10f);
        }

        public Vector2 ScreenToWorld(Vector2 screen)
        {
            Vector3 w = Cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10f));
            return new Vector2(w.x, w.y);
        }

        public Vector2 WorldToScreen(Vector3 world) => Cam.WorldToScreenPoint(world);

        public void Pan(Vector2 worldDelta)
        {
            panOffset -= (Vector3)worldDelta;
            transform.position -= (Vector3)worldDelta; // immediate response
            ClampOffset();
        }

        public void ZoomBy(float factor)
        {
            targetSize = Mathf.Clamp(targetSize * factor, MinSize, MaxSize);
        }

        public void FocusOn(Vector3 world)
        {
            Vector3 f = FarmerController.I.transform.position;
            panOffset = new Vector3(world.x - f.x, world.y - f.y, 0);
            ClampOffset();
        }

        public void ResetPan() => panOffset = Vector3.zero;

        private void ClampOffset()
        {
            Vector3 f = FarmerController.I.transform.position;
            Vector3 c = f + panOffset;
            // keep the view inside unlocked land (+ a strip of the wilderness around it)
            float halfH = Cam.orthographicSize, halfW = halfH * Cam.aspect;
            c.x = ClampAxis(c.x, bounds.min.x + halfW * 0.6f, bounds.max.x - halfW * 0.6f);
            c.y = ClampAxis(c.y, bounds.min.y + halfH * 0.6f, bounds.max.y - halfH * 0.6f);
            panOffset = new Vector3(c.x - f.x, c.y - f.y, 0);
        }

        private static float ClampAxis(float v, float min, float max)
        {
            return min > max ? (min + max) * 0.5f : Mathf.Clamp(v, min, max);
        }

        private void LateUpdate()
        {
            if (FarmerController.I == null) return;
            if (Joystick.Value.sqrMagnitude > 0.02f)
                panOffset = Vector3.Lerp(panOffset, Vector3.zero, Time.deltaTime * 4f);
            ClampOffset();

            Vector3 f = FarmerController.I.transform.position + panOffset;
            Vector3 desired = new Vector3(f.x, f.y, -10f);
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-Time.deltaTime * 8f));
            Cam.orthographicSize = Mathf.Lerp(Cam.orthographicSize, targetSize, 1f - Mathf.Exp(-Time.deltaTime * 10f));
        }
    }
}
