using UnityEngine;

namespace DroneSim.Drone.Environment
{
    [CreateAssetMenu(fileName = "FieldDefinition", menuName = "DroneSim/Field Definition")]
    public class FieldDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Human-readable name of the field, shown in UI and logs.")]
        public string fieldName = "Placeholder Field";

        [Tooltip("Where this field is located (school name, address, GPS coords, etc.).")]
        [TextArea(2, 4)]
        public string captureLocation = "Placeholder — flat ground, no real capture yet.";

        [Tooltip("Date the field was scanned/captured. Leave empty for placeholder.")]
        public string captureDate = "";

        [Header("Visual Content")]
        [Tooltip("Prefab containing the field geometry, lighting probes, decorations, and colliders. Instantiated at Vector3.zero when the field loads.")]
        public GameObject fieldPrefab;

        [Header("Spatial Layout")]
        [Tooltip("World-space Y coordinate of the ground at the field's origin point. Drone spawn and waypoints are placed relative to this.")]
        public float groundY = 0f;

        [Tooltip("Recommended hover altitude for training drills above this field, in meters.")]
        public float recommendedDrillAltitude = 2f;

        [Tooltip("Horizontal X/Z extent of safe operating area in meters. Used to size the drill safety envelope.")]
        public Vector2 operatingAreaSize = new Vector2(12f, 12f);

        [Tooltip("Maximum safe altitude above groundY for training drills, in meters.")]
        public float maxAltitude = 8f;

        [Header("Notes")]
        [Tooltip("Free-text notes about the field — known hazards, seasonal differences, etc.")]
        [TextArea(3, 8)]
        public string notes = "";
    }
}
