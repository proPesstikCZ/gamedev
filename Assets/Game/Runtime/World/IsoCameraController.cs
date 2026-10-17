using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Runtime
{
    /// <summary>
    /// Orthographic camera over the studio: pan with WASD / arrows or by dragging with the right or
    /// middle mouse button, zoom with the mouse wheel towards the cursor. Stays inside the bounds.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class IsoCameraController : MonoBehaviour
    {
        [Tooltip("World area the camera centre may move in.")]
        [SerializeField] private Rect bounds = new Rect(-3f, -1f, 6f, 5f);
        [SerializeField, Min(0.1f)] private float minZoom = 1.2f;
        [SerializeField, Min(0.1f)] private float maxZoom = 4f;
        [Tooltip("Screen heights per second with the keyboard.")]
        [SerializeField, Min(0f)] private float keyPanSpeed = 0.8f;
        [Tooltip("Zoom change per wheel step (0.1 = 10 %).")]
        [SerializeField, Range(0.01f, 0.5f)] private float zoomStep = 0.1f;

        private Camera _camera;
        private bool _dragging;

        /// <summary>True when the pointer is over UI; the wheel and drag then belong to the UI.</summary>
        public Func<Vector2, bool> PointerBlocked { get; set; }
        /// <summary>True when the keyboard belongs to the UI (open dialog, text field).</summary>
        public Func<bool> KeyboardBlocked { get; set; }

        public Rect Bounds => bounds;
        public float MinZoom => minZoom;
        public float MaxZoom => maxZoom;
        public Camera Camera => _camera != null ? _camera : _camera = GetComponent<Camera>();

        private void Awake()
        {
            Camera.orthographic = true;
            Camera.orthographicSize = Mathf.Clamp(Camera.orthographicSize, minZoom, maxZoom);
            transform.position = Clamp(transform.position);
        }

        private void Update()
        {
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;

            if (keyboard != null && !(KeyboardBlocked?.Invoke() ?? false))
            {
                var dir = Vector2.zero;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) dir.x -= 1;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) dir.x += 1;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) dir.y -= 1;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) dir.y += 1;
                // Unscaled time: the camera moves even while the game is paused.
                if (dir != Vector2.zero)
                    Pan(dir.normalized * (keyPanSpeed * 2f * Camera.orthographicSize * Time.unscaledDeltaTime));
            }

            if (mouse == null) return;
            Vector2 pointer = mouse.position.ReadValue();
            bool overUi = PointerBlocked?.Invoke(pointer) ?? false;

            bool dragButton = mouse.rightButton.isPressed || mouse.middleButton.isPressed;
            if (!dragButton) _dragging = false;
            else if (!_dragging && !overUi) _dragging = true;
            if (_dragging)
            {
                Vector2 delta = mouse.delta.ReadValue();
                Pan(-delta * WorldUnitsPerPixel());
            }

            float wheel = mouse.scroll.ReadValue().y;
            if (!overUi && Mathf.Abs(wheel) > 0.01f) ZoomAt(pointer, Mathf.Sign(wheel));
        }

        /// <summary>Moves the camera by a world offset, kept inside the bounds.</summary>
        public void Pan(Vector2 worldOffset)
        {
            transform.position = Clamp(transform.position + (Vector3)worldOffset);
        }

        /// <summary>One zoom step at a screen point: positive steps zoom in. The point under the cursor stays put.</summary>
        public void ZoomAt(Vector2 screenPoint, float steps)
        {
            float oldSize = Camera.orthographicSize;
            float newSize = ZoomedSize(oldSize, steps, zoomStep, minZoom, maxZoom);
            if (Mathf.Approximately(oldSize, newSize)) return;
            Vector3 before = Camera.ScreenToWorldPoint(screenPoint);
            Camera.orthographicSize = newSize;
            Vector3 after = Camera.ScreenToWorldPoint(screenPoint);
            transform.position = Clamp(transform.position + (before - after));
        }

        /// <summary>Puts the camera centre at a world point (inside the bounds).</summary>
        public void Focus(Vector2 worldPoint)
        {
            var p = transform.position;
            transform.position = Clamp(new Vector3(worldPoint.x, worldPoint.y, p.z));
        }

        public void Configure(Rect worldBounds, float min, float max)
        {
            bounds = worldBounds;
            minZoom = min;
            maxZoom = Mathf.Max(min, max);
        }

        public static float ZoomedSize(float size, float steps, float step, float min, float max) =>
            Mathf.Clamp(size * Mathf.Pow(1f - step, steps), min, max);

        public static Vector3 ClampToBounds(Vector3 position, Rect area) =>
            new Vector3(Mathf.Clamp(position.x, area.xMin, area.xMax), Mathf.Clamp(position.y, area.yMin, area.yMax), position.z);

        private Vector3 Clamp(Vector3 position) => ClampToBounds(position, bounds);

        private float WorldUnitsPerPixel() => 2f * Camera.orthographicSize / Mathf.Max(1, Screen.height);
    }
}
