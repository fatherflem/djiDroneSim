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

        [Tooltip("Screen-space X offset from the top-right corner.")]
        [Min(0f)]
        [SerializeField] private float marginX = 16f;

        [Tooltip("Screen-space Y offset from the top edge.")]
        [Min(0f)]
        [SerializeField] private float marginY = 16f;

        [Header("References")]
        [Tooltip("Camera mode source (Chase/FPV). Auto-discovered if missing.")]
        [SerializeField] private DroneCameraModeController cameraModeController;

        [Tooltip("Onboard feed source. Auto-discovered if missing.")]
        [SerializeField] private DroneVideoFeed videoFeed;

        [Tooltip("Gimbal source for pitch/FOV display. Auto-discovered if missing.")]
        [SerializeField] private DroneGimbalCameraRig gimbalRig;

        [Tooltip("Optional display surface so you can confirm the feed is also bound to a world screen.")]
        [SerializeField] private DroneFeedDisplaySurface displaySurface;

        private GUIStyle panelStyle;
        private GUIStyle labelStyle;
        private Texture2D panelBackground;

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

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnGUI()
        {
            if (!showOverlay)
            {
                return;
            }

            ResolveReferences();
            EnsureStyles();

            Rect panelRect = new Rect(Screen.width - 360f - marginX, marginY, 360f, 182f);
            GUILayout.BeginArea(panelRect, GUIContent.none, panelStyle);
            GUILayout.Label("Camera / Feed Status", labelStyle);
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

            GUILayout.EndArea();
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

            panelStyle = new GUIStyle(GUI.skin.box);
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
