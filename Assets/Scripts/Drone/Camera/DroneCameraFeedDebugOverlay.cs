using DroneSim.Drone.UI;
using UnityEngine;

namespace DroneSim.Drone.Camera
{
    /// <summary>
    /// Purpose: Lightweight camera/feed diagnostics overlay for FPV/chase and RenderTexture validation.
    /// Does NOT: control camera switching or feed binding behavior.
    /// Fits in sim: quick visual check that onboard feed stays alive and reusable in all modes.
    /// Depends on: DroneCameraModeController, DroneVideoFeed, DroneGimbalCameraRig, and optional display surface.
    /// </summary>
    public class DroneCameraFeedDebugOverlay : MonoBehaviour
    {
        [Header("Visibility")]
        [Tooltip("Master toggle for this camera/feed diagnostics overlay.")]
        [SerializeField] private bool showOverlay = true;

        [Tooltip("Start with the diagnostics window collapsed.")]
        [SerializeField] private bool startCollapsed;

        [Tooltip("Default window rect used at startup and by layout reset.")]
        [SerializeField] private Rect defaultWindowRect = new Rect(1020f, 16f, 350f, 190f);

        [Header("References")]
        [Tooltip("Camera mode source (Chase/FPV). Auto-discovered if missing.")]
        [SerializeField] private DroneCameraModeController cameraModeController;

        [Tooltip("Onboard feed source. Auto-discovered if missing.")]
        [SerializeField] private DroneVideoFeed videoFeed;

        [Tooltip("Gimbal source for pitch/FOV display. Auto-discovered if missing.")]
        [SerializeField] private DroneGimbalCameraRig gimbalRig;

        [Tooltip("Optional display surface so you can confirm the feed is also bound to a world screen.")]
        [SerializeField] private DroneFeedDisplaySurface displaySurface;

        private const int WindowId = 1303;

        private GUIStyle panelStyle;
        private GUIStyle labelStyle;
        private Texture2D panelBackground;
        private Rect windowRect;
        private bool isCollapsed;

        public void Initialize(
            DroneCameraModeController modeController,
            DroneVideoFeed feed,
            DroneGimbalCameraRig rig,
            DroneFeedDisplaySurface surface)
        {
            cameraModeController = modeController;
            videoFeed = feed;
            gimbalRig = rig;
            displaySurface = surface;
        }

        public void ResetWindowLayout()
        {
            windowRect = defaultWindowRect;
            isCollapsed = startCollapsed;
        }

        private void Awake()
        {
            ResolveReferences();
            windowRect = defaultWindowRect;
            isCollapsed = startCollapsed;
        }

        private void OnGUI()
        {
            if (!showOverlay)
            {
                return;
            }

            ResolveReferences();
            EnsureStyles();

            windowRect = DebugWindowLayoutUtility.ClampToScreen(windowRect);
            float targetHeight = isCollapsed ? DebugWindowLayoutUtility.HeaderHeight + 6f : defaultWindowRect.height;
            windowRect.height = Mathf.Max(DebugWindowLayoutUtility.HeaderHeight + 6f, targetHeight);
            windowRect = GUI.Window(WindowId, windowRect, DrawWindow, GUIContent.none, panelStyle);
            windowRect = DebugWindowLayoutUtility.ClampToScreen(windowRect);
        }

        private void DrawWindow(int _)
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Camera / Feed Status", labelStyle, GUILayout.Height(DebugWindowLayoutUtility.HeaderHeight));
            string buttonLabel = isCollapsed ? "+" : "-";
            if (GUILayout.Button(buttonLabel, GUILayout.Width(26f), GUILayout.Height(20f)))
            {
                isCollapsed = !isCollapsed;
            }
            GUILayout.EndHorizontal();

            if (!isCollapsed)
            {
                GUILayout.Space(4f);

                string mode = cameraModeController != null ? cameraModeController.CurrentModeLabel : "(missing)";
                float gimbalPitch = gimbalRig != null ? gimbalRig.CurrentPitchDegrees : 0f;
                float fov = gimbalRig != null && gimbalRig.OnboardCamera != null ? gimbalRig.OnboardCamera.fieldOfView : 0f;

                bool feedObjectValid = videoFeed != null;
                bool feedTextureValid = feedObjectValid && videoFeed.FeedTexture != null && videoFeed.IsFeedTextureCreated;
                bool feedLive = feedObjectValid && videoFeed.IsActive && feedTextureValid;
                bool cameraBound = feedObjectValid && videoFeed.IsCameraBoundToFeed;

                string resolution = feedObjectValid
                    ? $"{videoFeed.FeedWidth} x {videoFeed.FeedHeight}"
                    : "(missing)";

                GUILayout.Label($"Mode: {mode}", labelStyle);
                GUILayout.Label($"Gimbal Pitch: {gimbalPitch,6:F1} deg", labelStyle);
                GUILayout.Label($"Onboard FOV: {fov,6:F1} deg", labelStyle);
                GUILayout.Space(2f);
                GUILayout.Label($"Feed Resolution: {resolution}", labelStyle);
                GUILayout.Label($"Feed Live: {feedLive}", labelStyle);
                GUILayout.Label($"Camera Bound -> RT: {cameraBound}", labelStyle);

                if (displaySurface != null)
                {
                    GUILayout.Label($"World Screen Bound: {displaySurface.IsBound}", labelStyle);
                }
            }

            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0f, 0f, windowRect.width - 34f, DebugWindowLayoutUtility.HeaderHeight));
        }

        private void ResolveReferences()
        {
            cameraModeController ??= FindFirstObjectByType<DroneCameraModeController>();
            videoFeed ??= FindFirstObjectByType<DroneVideoFeed>();
            gimbalRig ??= FindFirstObjectByType<DroneGimbalCameraRig>();
            displaySurface ??= FindFirstObjectByType<DroneFeedDisplaySurface>();
        }

        private void EnsureStyles()
        {
            if (panelStyle != null)
            {
                return;
            }

            panelStyle = new GUIStyle(GUI.skin.window)
            {
                padding = new RectOffset(10, 10, 6, 10)
            };
            panelBackground = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            panelBackground.SetPixel(0, 0, new Color(0.05f, 0.07f, 0.09f, 0.88f));
            panelBackground.Apply();
            panelStyle.normal.background = panelBackground;

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                richText = false
            };
            labelStyle.normal.textColor = new Color(0.95f, 0.97f, 0.99f);
        }

        private void OnDestroy()
        {
            if (panelBackground != null)
            {
                Destroy(panelBackground);
            }
        }
    }
}
