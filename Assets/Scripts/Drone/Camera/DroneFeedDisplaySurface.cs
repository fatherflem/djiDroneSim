using UnityEngine;
using UnityEngine.UI;

namespace DroneSim.Drone.Camera
{
    /// <summary>
    /// Purpose: Displays the drone video feed on an in-world surface (mesh renderer or UI RawImage).
    /// Does NOT: manage the feed texture itself or control camera modes.
    /// Fits in sim: drop this on any GameObject with a Renderer or RawImage to show the live drone camera feed.
    /// Depends on: DroneVideoFeed providing the RenderTexture.
    ///
    /// --- VR CONTROLLER SCREEN USAGE ---
    /// To show the drone feed on a VR controller screen:
    ///   1. Create a Quad or plane mesh on the controller model where the screen should be.
    ///   2. Add this component to that mesh GameObject.
    ///   3. Assign the DroneVideoFeed reference (or let it auto-find).
    ///   4. The component will automatically assign the live feed texture to the mesh material.
    ///   5. For UI-based screens, add a RawImage component instead and this will bind to that.
    ///
    /// The feed updates every frame automatically. No polling or manual refresh needed.
    /// </summary>
    public class DroneFeedDisplaySurface : MonoBehaviour
    {
        [Header("Feed source")]
        [Tooltip("The drone video feed to display. Auto-discovered if not assigned.")]
        [SerializeField] private DroneVideoFeed videoFeed;

        [Header("Display target")]
        [Tooltip("If set, the feed texture is applied to this renderer's main material. " +
                 "If null, the component looks for a Renderer on this GameObject.")]
        [SerializeField] private Renderer targetRenderer;

        [Tooltip("If set, the feed texture is applied to this RawImage. " +
                 "Useful for UI-based screens (Canvas world-space).")]
        [SerializeField] private RawImage targetRawImage;

        [Tooltip("Material property name to set the texture on. Usually '_MainTex'.")]
        [SerializeField] private string texturePropertyName = "_MainTex";

        private MaterialPropertyBlock propertyBlock;
        private int texturePropertyId;
        private bool isBound;
        private RenderTexture lastBoundTexture;

        private void Awake()
        {
            videoFeed ??= FindFirstObjectByType<DroneVideoFeed>();
            targetRenderer ??= GetComponent<Renderer>();
            targetRawImage ??= GetComponent<RawImage>();
            texturePropertyId = Shader.PropertyToID(texturePropertyName);
            propertyBlock = new MaterialPropertyBlock();
        }

        private void LateUpdate()
        {
            if (videoFeed == null || videoFeed.FeedTexture == null)
            {
                isBound = false;
                lastBoundTexture = null;
                return;
            }

            RenderTexture feed = videoFeed.FeedTexture;
            lastBoundTexture = feed;

            // Bind to mesh renderer using property block (avoids material instance creation).
            if (targetRenderer != null)
            {
                targetRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetTexture(texturePropertyId, feed);
                targetRenderer.SetPropertyBlock(propertyBlock);
                isBound = true;
            }

            // Bind to UI RawImage.
            if (targetRawImage != null)
            {
                targetRawImage.texture = feed;
                isBound = true;
            }
        }

        /// <summary>Whether the feed is currently bound to a display target.</summary>
        public bool IsBound => isBound;
        public bool HasDisplayTarget => targetRenderer != null || targetRawImage != null;
        public RenderTexture LastBoundTexture => lastBoundTexture;

        /// <summary>
        /// Manually set the video feed source at runtime.
        /// </summary>
        public void SetVideoFeed(DroneVideoFeed feed)
        {
            videoFeed = feed;
            isBound = false;
        }
    }
}
