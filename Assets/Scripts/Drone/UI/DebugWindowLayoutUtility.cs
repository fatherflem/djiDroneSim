using UnityEngine;

namespace DroneSim.Drone.UI
{
    /// <summary>
    /// Shared helpers for runtime IMGUI debug windows (layout clamping + header helpers).
    /// </summary>
    public static class DebugWindowLayoutUtility
    {
        public const float HeaderHeight = 24f;

        public static Rect ClampToScreen(Rect rect)
        {
            float width = Mathf.Min(rect.width, Mathf.Max(140f, Screen.width));
            float height = Mathf.Min(rect.height, Mathf.Max(HeaderHeight + 4f, Screen.height));

            float maxX = Mathf.Max(0f, Screen.width - width);
            float maxY = Mathf.Max(0f, Screen.height - HeaderHeight);

            rect.width = width;
            rect.height = height;
            rect.x = Mathf.Clamp(rect.x, 0f, maxX);
            rect.y = Mathf.Clamp(rect.y, 0f, maxY);
            return rect;
        }
    }
}
