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
        [SerializeField] private Text actionPrompt;

        private void Awake()
        {
            drill ??= FindFirstObjectByType<HoverBoxDrill>();
            rcRig ??= FindFirstObjectByType<VirtualRCControllerRig>();
            BuildPanel();
            BuildWorldWarning();
            if (waypointText == null || completionText == null || stabilityText == null || actionPrompt == null)
            {
                Debug.LogError("Hover Box VR presentation requires an initialized virtual RC rig.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (drill == null) return;
            waypointText.text = GetStatusText();
            completionText.text = drill.LatestResult != null
                ? $"Score {drill.LatestResult.score:F0} | {drill.LatestResult.elapsedSeconds:F1}s"
                : drill.State == DrillState.Running ? $"{drill.CompletedWaypoints}/5 | {drill.RunElapsedSeconds:F1}s" : string.Empty;
            UpdateActionPrompt();
            if (drill.State == DrillState.Running && drill.IsOutOfBounds)
            {
                stabilityText.text = "Out of bounds";
                stabilityText.color = Color.red;
                worldWarningText.gameObject.SetActive(true);
                worldWarningText.transform.position = drill.LastOutOfBoundsPosition + Vector3.up * 1.5f;
            }
            else
            {
                stabilityText.text = drill.State == DrillState.Running ? (drill.IsStable ? "Stable" : "Drifting") : string.Empty;
                stabilityText.color = drill.IsStable ? Color.green : Color.yellow;
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
            rt.sizeDelta = new Vector2(240, 130);
            waypointText = CreateText(canvas.transform, new Vector2(10,-10), "WP: A", new Vector2(220, 55));
            completionText = CreateText(canvas.transform, new Vector2(10,-70), "0/5");
            stabilityText = CreateText(canvas.transform, new Vector2(10,-90), "");
            actionPrompt = CreateText(canvas.transform, new Vector2(10,-110), "Press controller action to start");
        }

        private string GetStatusText()
        {
            return drill.State switch
            {
                DrillState.Instructions => $"{drill.DisplayName}\n{drill.Instructions}",
                DrillState.Countdown => $"Start in {Mathf.CeilToInt(drill.CountdownRemaining)} — controls locked",
                DrillState.Completed => "Drill complete",
                DrillState.Failed => "Drill failed",
                DrillState.Results => drill.LatestResult?.summary ?? "Results",
                DrillState.Running => $"WP: {drill.ActiveWaypointLetter}",
                _ => drill.DisplayName
            };
        }

        private void UpdateActionPrompt()
        {
            bool visible = drill.State == DrillState.Instructions || (drill.IsTerminal && drill.CanRestart);
            actionPrompt.gameObject.SetActive(visible);
            actionPrompt.text = drill.State == DrillState.Instructions
                ? "Press controller action to start"
                : "Press controller action to retry";
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

        private Text CreateText(Transform parent, Vector2 pos, string value, Vector2? size = null)
        {
            Text t = new GameObject("Text").AddComponent<Text>();
            t.transform.SetParent(parent, false);
            RectTransform rt = t.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size ?? new Vector2(220, 18);
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = value;
            t.color = Color.white;
            t.fontSize = 14;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

    }
}
