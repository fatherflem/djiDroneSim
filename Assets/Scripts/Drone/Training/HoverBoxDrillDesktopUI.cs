using UnityEngine;
using UnityEngine.UI;

namespace DroneSim.Drone.Training
{
    public class HoverBoxDrillDesktopUI : MonoBehaviour
    {
        [SerializeField] private HoverBoxDrill drill;
        [SerializeField] private Text title;
        [SerializeField] private Text status;
        [SerializeField] private Text instructions;
        [SerializeField] private Slider holdBar;
        [SerializeField] private Text stability;
        [SerializeField] private Text completion;
        [SerializeField] private Text outOfBounds;
        [SerializeField] private Button restart;
        private Text actionButtonLabel;
        private Font runtimeFont;

        private void Awake()
        {
            drill ??= FindFirstObjectByType<HoverBoxDrill>();
            EnsureCanvas();
        }

        private void Update()
        {
            if (drill == null) return;
            title.text = drill.DisplayName;
            status.text = GetStatusText();
            instructions.text = drill.State == DrillState.Instructions ? drill.Instructions : string.Empty;
            instructions.gameObject.SetActive(drill.State == DrillState.Instructions);
            holdBar.maxValue = drill.RequiredHoldSeconds;
            holdBar.value = drill.HoldTimer;
            bool running = drill.State == DrillState.Running;
            stability.gameObject.SetActive(running);
            holdBar.gameObject.SetActive(running);
            stability.text = drill.IsStable
                ? $"STABLE  •  H {drill.HorizontalSpeed:F2}  V {drill.VerticalSpeed:F2}  Y {drill.YawRateDegPerSec:F1}"
                : $"DRIFTING  •  H {drill.HorizontalSpeed:F2}  V {drill.VerticalSpeed:F2}  Y {drill.YawRateDegPerSec:F1}";
            stability.color = drill.IsStable ? new Color(0.35f, 1f, 0.45f) : new Color(1f, 0.65f, 0.2f);
            completion.text = GetProgressText();
            outOfBounds.gameObject.SetActive(running && drill.IsOutOfBounds);
            UpdateActionButton();
        }

        private void EnsureCanvas()
        {
            if (status != null) return;

            runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Canvas canvas = new GameObject("HoverBoxCanvas").AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvas.gameObject.AddComponent<GraphicRaycaster>();

            Image panel = new GameObject("TrainingPanel").AddComponent<Image>();
            panel.transform.SetParent(canvas.transform, false);
            panel.color = new Color(0.025f, 0.035f, 0.055f, 0.9f);
            RectTransform panelRect = panel.rectTransform;
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(28f, -28f);
            panelRect.sizeDelta = new Vector2(650f, 430f);

            Transform root = panel.transform;
            title = CreateText(root, new Vector2(24f, -18f), "Hover Box", new Vector2(602f, 42f), 30, FontStyle.Bold);
            status = CreateText(root, new Vector2(24f, -66f), "Instructions", new Vector2(602f, 40f), 26, FontStyle.Bold);
            instructions = CreateText(root, new Vector2(24f, -112f), string.Empty, new Vector2(602f, 105f), 22);
            completion = CreateText(root, new Vector2(24f, -230f), "Waypoints: 0/5", new Vector2(602f, 34f), 21, FontStyle.Bold);
            stability = CreateText(root, new Vector2(24f, -274f), string.Empty, new Vector2(602f, 34f), 20, FontStyle.Bold);
            holdBar = CreateHoldBar(root, new Vector2(24f, -316f));
            outOfBounds = CreateText(root, new Vector2(24f, -354f), "RETURN TO THE COURSE BOUNDARY", new Vector2(602f, 34f), 23, FontStyle.Bold);
            outOfBounds.color = new Color(1f, 0.3f, 0.25f);
            outOfBounds.alignment = TextAnchor.MiddleCenter;
            restart = CreateRestartButton(root);
            restart.onClick.AddListener(HandleAction);
            actionButtonLabel = restart.GetComponentInChildren<Text>();
        }

        private Slider CreateHoldBar(Transform parent, Vector2 position)
        {
            Image background = new GameObject("HoldProgress").AddComponent<Image>();
            background.transform.SetParent(parent, false);
            background.color = new Color(1f, 1f, 1f, 0.25f);
            RectTransform rect = background.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(602f, 22f);

            Slider slider = background.gameObject.AddComponent<Slider>();
            slider.interactable = false;
            Image fill = new GameObject("Fill").AddComponent<Image>();
            fill.transform.SetParent(background.transform, false);
            fill.color = new Color(1f, 0.85f, 0.1f, 1f);
            RectTransform fillRect = fill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(3f, 3f);
            fillRect.offsetMax = new Vector2(-3f, -3f);
            slider.fillRect = fillRect;
            slider.targetGraphic = background;
            return slider;
        }

        private string GetStatusText()
        {
            return drill.State switch
            {
                DrillState.Instructions => "Instructions",
                DrillState.Countdown => $"STARTING IN {Mathf.CeilToInt(drill.CountdownRemaining)}  •  CONTROLS LOCKED",
                DrillState.Completed => "DRILL COMPLETE",
                DrillState.Failed => "DRILL FAILED",
                DrillState.Results => drill.LatestResult?.summary ?? "Results",
                DrillState.Running => $"ACTIVE WAYPOINT: {drill.ActiveWaypointLetter}",
                _ => drill.DisplayName
            };
        }

        private string GetProgressText()
        {
            if (drill.LatestResult != null)
                return $"Score: {drill.LatestResult.score:F0}  •  Time: {drill.LatestResult.elapsedSeconds:F1}s";

            return drill.State == DrillState.Running
                ? $"Waypoints: {drill.CompletedWaypoints}/5  •  Time: {drill.RunElapsedSeconds:F1}s"
                : "Press Start Drill when ready";
        }

        private void UpdateActionButton()
        {
            bool visible = drill.State == DrillState.Instructions || (drill.IsTerminal && drill.CanRestart);
            restart.gameObject.SetActive(visible);
            if (actionButtonLabel != null)
                actionButtonLabel.text = drill.State == DrillState.Instructions ? "START DRILL  (ENTER / SPACE)" : "RETRY DRILL  (R)";
        }

        private void HandleAction()
        {
            if (drill.State == DrillState.Instructions) drill.ContinueFromInstructions();
            else if (drill.IsTerminal) drill.RestartDrill();
        }

        private Text CreateText(Transform parent, Vector2 position, string value, Vector2 size, int fontSize, FontStyle style = FontStyle.Normal)
        {
            Text text = new GameObject("Text").AddComponent<Text>();
            text.transform.SetParent(parent, false);
            RectTransform rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            text.font = runtimeFont;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.text = value;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private Button CreateRestartButton(Transform parent)
        {
            Image image = new GameObject("ActionButton").AddComponent<Image>();
            image.transform.SetParent(parent, false);
            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24f, -354f);
            rect.sizeDelta = new Vector2(602f, 56f);
            image.color = new Color(0.08f, 0.55f, 0.25f, 1f);

            Button button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.12f, 0.72f, 0.32f, 1f);
            colors.pressedColor = new Color(0.05f, 0.4f, 0.18f, 1f);
            button.colors = colors;

            Text text = CreateText(image.transform, Vector2.zero, "START DRILL  (ENTER / SPACE)", Vector2.zero, 22, FontStyle.Bold);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = Vector2.zero;
            text.alignment = TextAnchor.MiddleCenter;
            actionButtonLabel = text;
            return button;
        }
    }
}
