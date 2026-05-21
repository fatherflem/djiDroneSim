using DroneSim.VR;
using UnityEngine;
using UnityEngine.UI;

namespace DroneSim.Drone.Training
{
    public class HoverBoxDrillVRUI : MonoBehaviour
    {
        [SerializeField] private HoverBoxDrill drill;
        [SerializeField] private VirtualRCControllerRig rcRig;
        [SerializeField] private Text waypointText;
        [SerializeField] private Text completionText;
        [SerializeField] private Text stabilityText;
        [SerializeField] private TextMesh worldWarningText;
        [SerializeField] private Button restart;

        private void Awake()
        {
            drill ??= FindFirstObjectByType<HoverBoxDrill>();
            rcRig ??= FindFirstObjectByType<VirtualRCControllerRig>();
            BuildPanel();
            BuildWorldWarning();
        }

        private void Update()
        {
            if (drill == null) return;
            waypointText.text = drill.IsComplete ? "Drill complete" : $"WP: {drill.ActiveWaypointLetter}";
            completionText.text = $"{drill.CompletedWaypoints}/5";
            restart.gameObject.SetActive(drill.IsComplete);
            if (drill.IsOutOfBounds)
            {
                stabilityText.text = "Out of bounds";
                stabilityText.color = Color.red;
                worldWarningText.gameObject.SetActive(true);
                worldWarningText.transform.position = drill.LastOutOfBoundsPosition + Vector3.up * 1.5f;
            }
            else
            {
                bool stable = drill.HorizontalSpeed <= 0.3f && drill.VerticalSpeed <= 0.3f && drill.YawRateDegPerSec <= 10f;
                stabilityText.text = stable ? "Stable" : "Drifting";
                stabilityText.color = stable ? Color.green : Color.yellow;
                worldWarningText.gameObject.SetActive(false);
            }
        }

        private void BuildPanel()
        {
            if (waypointText != null || rcRig == null) return;
            Canvas canvas = new GameObject("RCStatusPanel").AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.transform.SetParent(rcRig.transform, false);
            canvas.transform.localPosition = new Vector3(0.06f, 0.04f, -0.02f);
            canvas.transform.localRotation = Quaternion.Euler(80f, 0f, 0f);
            RectTransform rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(180, 80);
            waypointText = CreateText(canvas.transform, new Vector2(10,-10), "WP: A");
            completionText = CreateText(canvas.transform, new Vector2(10,-30), "0/5");
            stabilityText = CreateText(canvas.transform, new Vector2(10,-50), "Stable");
            restart = CreateRestartButton(canvas.transform);
            restart.onClick.AddListener(() => drill.RestartDrill());
            restart.gameObject.SetActive(false);
        }

        private void BuildWorldWarning()
        {
            if (worldWarningText != null) return;
            worldWarningText = new GameObject("OutOfBoundsWorldWarning").AddComponent<TextMesh>();
            worldWarningText.text = "Out of bounds — return to course";
            worldWarningText.color = new Color(1f, 0f, 0f, 0.8f);
            worldWarningText.anchor = TextAnchor.MiddleCenter;
            worldWarningText.characterSize = 0.08f;
            worldWarningText.gameObject.SetActive(false);
        }

        private Text CreateText(Transform parent, Vector2 pos, string value)
        {
            Text t = new GameObject("Text").AddComponent<Text>();
            t.transform.SetParent(parent, false);
            RectTransform rt = t.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(160, 18);
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = value;
            t.color = Color.white;
            t.fontSize = 14;
            return t;
        }

        private Button CreateRestartButton(Transform parent)
        {
            Image img = new GameObject("RestartButton").AddComponent<Image>();
            img.transform.SetParent(parent, false);
            RectTransform rt = img.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(10f, -70f);
            rt.sizeDelta = new Vector2(160f, 22f);
            img.color = new Color(0.2f, 0.7f, 0.3f, 0.95f);

            Button button = img.gameObject.AddComponent<Button>();
            Text text = new GameObject("Text").AddComponent<Text>();
            text.transform.SetParent(img.transform, false);
            RectTransform textRt = text.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = "Restart Drill";
            text.fontSize = 14;
            text.color = Color.black;
            text.alignment = TextAnchor.MiddleCenter;
            return button;
        }
    }
}
