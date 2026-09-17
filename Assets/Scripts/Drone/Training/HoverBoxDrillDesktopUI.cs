using UnityEngine;
using UnityEngine.UI;

namespace DroneSim.Drone.Training
{
    public class HoverBoxDrillDesktopUI : MonoBehaviour
    {
        [SerializeField] private HoverBoxDrill drill;
        [SerializeField] private Text status;
        [SerializeField] private Slider holdBar;
        [SerializeField] private Text stability;
        [SerializeField] private Text completion;
        [SerializeField] private Text outOfBounds;
        [SerializeField] private Button restart;
        private Text actionButtonLabel;

        private void Awake()
        {
            drill ??= FindFirstObjectByType<HoverBoxDrill>();
            EnsureCanvas();
        }

        private void Update()
        {
            if (drill == null) return;
            status.text = GetStatusText();
            holdBar.maxValue = drill.RequiredHoldSeconds;
            holdBar.value = drill.HoldTimer;
            stability.text = $"H:{drill.HorizontalSpeed:F2}  V:{drill.VerticalSpeed:F2}  Y:{drill.YawRateDegPerSec:F1}";
            stability.color = drill.IsStable ? Color.green : Color.red;
            completion.text = GetProgressText();
            outOfBounds.gameObject.SetActive(drill.State == DrillState.Running && drill.IsOutOfBounds);
            UpdateActionButton();
        }

        private void EnsureCanvas()
        {
            if (status != null) return;
            Canvas c = new GameObject("HoverBoxCanvas").AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.gameObject.AddComponent<CanvasScaler>();
            c.gameObject.AddComponent<GraphicRaycaster>();
            Transform root = c.transform;
            status = CreateText(root, new Vector2(170, -30), "Waypoint: A");
            holdBar = new GameObject("HoldBar").AddComponent<Slider>();
            holdBar.transform.SetParent(root, false);
            RectTransform hb = holdBar.GetComponent<RectTransform>(); hb.anchorMin = hb.anchorMax = new Vector2(0,1); hb.pivot=new Vector2(0,1); hb.sizeDelta = new Vector2(220,20); hb.anchoredPosition = new Vector2(20,-60);
            stability = CreateText(root, new Vector2(170, -90), "");
            completion = CreateText(root, new Vector2(170, -120), "Waypoints: 0/5");
            outOfBounds = CreateText(root, new Vector2(220, -150), "Out of bounds — return to course");
            outOfBounds.color = Color.red;
            restart = CreateRestartButton(root);
            restart.onClick.AddListener(HandleAction);
            actionButtonLabel = restart.GetComponentInChildren<Text>();
            restart.gameObject.SetActive(true);
        }

        private string GetStatusText()
        {
            return drill.State switch
            {
                DrillState.Instructions => string.IsNullOrWhiteSpace(drill.Instructions) ? "Ready to begin" : drill.Instructions,
                DrillState.Countdown => $"Starting in {Mathf.CeilToInt(drill.CountdownRemaining)}",
                DrillState.Completed => "Drill complete",
                DrillState.Failed => "Drill failed",
                DrillState.Results => drill.LatestResult?.summary ?? "Results",
                DrillState.Running => $"Waypoint: {drill.ActiveWaypointLetter}",
                _ => drill.DisplayName
            };
        }

        private string GetProgressText()
        {
            if (drill.LatestResult != null)
            {
                return $"Score: {drill.LatestResult.score:F0}  Time: {drill.LatestResult.elapsedSeconds:F1}s";
            }

            return $"Waypoints: {drill.CompletedWaypoints}/5  Time: {drill.RunElapsedSeconds:F1}s";
        }

        private void UpdateActionButton()
        {
            bool visible = drill.State == DrillState.Instructions || (drill.IsTerminal && drill.CanRestart);
            restart.gameObject.SetActive(visible);
            if (actionButtonLabel != null)
            {
                actionButtonLabel.text = drill.State == DrillState.Instructions ? "Start Drill" : "Retry Drill";
            }
        }

        private void HandleAction()
        {
            if (drill.State == DrillState.Instructions)
            {
                drill.ContinueFromInstructions();
            }
            else if (drill.IsTerminal)
            {
                drill.RestartDrill();
            }
        }

        private Text CreateText(Transform parent, Vector2 pos, string value)
        {
            Text t = new GameObject("Text").AddComponent<Text>();
            t.transform.SetParent(parent, false);
            RectTransform rt = t.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0,1); rt.pivot = new Vector2(0,1); rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(360,28);
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = value;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private Button CreateRestartButton(Transform parent)
        {
            Image img = new GameObject("RestartButton").AddComponent<Image>();
            img.transform.SetParent(parent, false);
            RectTransform rt = img.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 40f);
            rt.sizeDelta = new Vector2(200f, 50f);
            img.color = new Color(0.2f, 0.6f, 0.3f, 0.9f);

            Button b = img.gameObject.AddComponent<Button>();
            Text text = new GameObject("Text").AddComponent<Text>();
            text.transform.SetParent(img.transform, false);
            RectTransform textRt = text.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = "Restart Drill";
            text.color = Color.white;
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleCenter;
            actionButtonLabel = text;
            return b;
        }
    }
}
