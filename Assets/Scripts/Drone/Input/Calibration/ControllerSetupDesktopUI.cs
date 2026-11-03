using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DroneSim.Drone.Input.Calibration
{
    /// <summary>
    /// Desktop presentation of ControllerSetupService: a centered controller-check panel with live stick
    /// previews, plus a small status banner (controller disconnected / not calibrated / saved).
    /// All logic lives in the service and ControllerCheckFlow; this only draws it.
    /// </summary>
    public class ControllerSetupDesktopUI : MonoBehaviour
    {
        private const float StickBoxSize = 200f;
        private const float DotSize = 22f;

        [SerializeField] private ControllerSetupService service;

        private Font font;
        private GameObject panel;
        private Text device;
        private Text instruction;
        private Text step;
        private Slider progress;
        private Text issues;
        private RectTransform leftDot;
        private RectTransform rightDot;
        private RectTransform leftTarget;
        private RectTransform rightTarget;
        private Button saveButton;
        private Button redoButton;
        private Button closeButton;
        private Text closeLabel;
        private GameObject banner;
        private Image bannerBackground;
        private Text bannerText;

        public static ControllerSetupDesktopUI Ensure(ControllerSetupService setupService)
        {
            ControllerSetupDesktopUI ui = FindFirstObjectByType<ControllerSetupDesktopUI>();
            if (ui == null)
            {
                ui = new GameObject("ControllerSetupDesktopUI").AddComponent<ControllerSetupDesktopUI>();
            }

            ui.service = setupService;
            EnsureEventSystem();
            return ui;
        }

        private void Awake()
        {
            service ??= FindFirstObjectByType<ControllerSetupService>();
            Build();
        }

        private void Update()
        {
            if (service == null)
            {
                service = FindFirstObjectByType<ControllerSetupService>();
                if (service == null) return;
            }

            bool checking = service.IsCheckActive;
            panel.SetActive(checking);
            UpdateBanner(checking);
            if (!checking) return;

            ControllerCheckFlow flow = service.Flow;
            ControllerCheckState state = flow.State;
            device.text = service.IsControllerConnected ? service.ControllerName : "No controller connected";
            instruction.text = flow.Instruction;

            bool prompting = state == ControllerCheckState.MoveStick || state == ControllerCheckState.ReturnToCenter;
            step.text = state switch
            {
                ControllerCheckState.CenterHold => "Step 1: hands off",
                ControllerCheckState.MoveStick or ControllerCheckState.ReturnToCenter => $"Step {flow.PromptIndex + 2} of {flow.PromptCount + 1}",
                ControllerCheckState.Review => flow.CountdownRemaining >= 0f ? $"Saving in {Mathf.CeilToInt(flow.CountdownRemaining)}..." : "Review",
                ControllerCheckState.Failed => flow.CountdownRemaining >= 0f ? $"Restarting in {Mathf.CeilToInt(flow.CountdownRemaining)}..." : string.Empty,
                _ => string.Empty,
            };
            progress.value = flow.StepProgress;
            progress.gameObject.SetActive(state == ControllerCheckState.CenterHold || prompting || flow.CountdownRemaining >= 0f);

            SetDot(leftDot, service.GetStickPreview(true));
            SetDot(rightDot, service.GetStickPreview(false));
            UpdateTargets(flow, state);

            issues.text = BuildIssuesText();
            saveButton.gameObject.SetActive(state == ControllerCheckState.Review);
            redoButton.gameObject.SetActive(state == ControllerCheckState.Review || state == ControllerCheckState.Failed);
            closeLabel.text = state == ControllerCheckState.Review ? "DISCARD" : "CANCEL  (" + service.ToggleCheckKey + ")";
        }

        private void UpdateBanner(bool checking)
        {
            string message = null;
            Color color = default;
            if (!checking && service.ShowSavedConfirmation)
            {
                message = "Controller calibrated and saved.";
                color = new Color(0.08f, 0.45f, 0.2f, 0.92f);
            }
            else if (!service.IsControllerConnected && service.ControllerWasLost)
            {
                message = "CONTROLLER DISCONNECTED: the drone is holding position. Reconnect the USB cable to keep flying.";
                color = new Color(0.6f, 0.08f, 0.06f, 0.95f);
            }
            else if (!checking && service.IsControllerConnected && !service.HasCalibration)
            {
                message = $"Controller not calibrated. Press {service.ToggleCheckKey} to run the controller check.";
                color = new Color(0.55f, 0.38f, 0.02f, 0.92f);
            }

            banner.SetActive(message != null);
            if (message != null)
            {
                bannerText.text = message;
                bannerBackground.color = color;
            }
        }

        private void UpdateTargets(ControllerCheckFlow flow, ControllerCheckState state)
        {
            leftTarget.gameObject.SetActive(false);
            rightTarget.gameObject.SetActive(false);
            if (state != ControllerCheckState.MoveStick || !flow.CurrentPrompt.HasValue) return;

            StickPrompt prompt = flow.CurrentPrompt.Value;
            bool left = prompt.Axis == StickAxis.Yaw || prompt.Axis == StickAxis.Throttle;
            bool horizontal = prompt.Axis == StickAxis.Roll || prompt.Axis == StickAxis.Yaw;
            RectTransform target = left ? leftTarget : rightTarget;
            target.gameObject.SetActive(true);
            SetDot(target, horizontal ? new Vector2(prompt.Direction, 0f) : new Vector2(0f, prompt.Direction));
        }

        private string BuildIssuesText()
        {
            var builder = new StringBuilder();
            foreach (CheckIssue issue in service.IssuesBySeverity())
            {
                string color = issue.Severity switch
                {
                    CheckIssueSeverity.Blocking => "#ff6b5e",
                    CheckIssueSeverity.Warning => "#ffc247",
                    _ => "#9fd4ff",
                };
                string label = issue.Severity switch
                {
                    CheckIssueSeverity.Blocking => "PROBLEM",
                    CheckIssueSeverity.Warning => "WARNING",
                    _ => "NOTE",
                };
                builder.Append("<color=").Append(color).Append("><b>").Append(label).Append("</b></color>  ")
                    .Append(issue.Message).Append('\n');
            }

            return builder.ToString();
        }

        private static void SetDot(RectTransform dot, Vector2 value)
        {
            float half = (StickBoxSize - DotSize) * 0.5f;
            dot.anchoredPosition = new Vector2(Mathf.Clamp(value.x, -1f, 1f) * half, Mathf.Clamp(value.y, -1f, 1f) * half);
        }

        // ------------------------------------------------------------------ construction

        private void Build()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Canvas canvas = new GameObject("ControllerSetupCanvas").AddComponent<Canvas>();
            canvas.transform.SetParent(transform, false);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200; // above the training HUD
            CanvasScaler scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvas.gameObject.AddComponent<GraphicRaycaster>();

            Image panelImage = CreateImage(canvas.transform, "ControllerCheckPanel", new Color(0.025f, 0.035f, 0.055f, 0.96f));
            RectTransform panelRect = panelImage.rectTransform;
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(820f, 700f);
            panel = panelImage.gameObject;
            Transform root = panel.transform;

            CreateText(root, new Vector2(30f, -22f), new Vector2(760f, 44f), "Controller Check", 32, FontStyle.Bold);
            device = CreateText(root, new Vector2(30f, -66f), new Vector2(760f, 28f), string.Empty, 18);
            device.color = new Color(0.7f, 0.78f, 0.88f);
            instruction = CreateText(root, new Vector2(30f, -104f), new Vector2(760f, 80f), string.Empty, 28, FontStyle.Bold);
            step = CreateText(root, new Vector2(30f, -188f), new Vector2(760f, 28f), string.Empty, 18);
            progress = CreateProgressBar(root, new Vector2(30f, -220f), new Vector2(760f, 16f));

            CreateStickBox(root, new Vector2(150f, -262f), "LEFT STICK  (throttle / yaw)", out leftDot, out leftTarget);
            CreateStickBox(root, new Vector2(470f, -262f), "RIGHT STICK  (pitch / roll)", out rightDot, out rightTarget);

            issues = CreateText(root, new Vector2(30f, -506f), new Vector2(760f, 112f), string.Empty, 17);
            issues.supportRichText = true;
            issues.verticalOverflow = VerticalWrapMode.Truncate;

            saveButton = CreateButton(root, new Vector2(30f, -628f), new Vector2(240f, 50f), "SAVE NOW", new Color(0.08f, 0.55f, 0.25f), out _);
            redoButton = CreateButton(root, new Vector2(290f, -628f), new Vector2(240f, 50f), "REDO", new Color(0.18f, 0.32f, 0.6f), out _);
            closeButton = CreateButton(root, new Vector2(550f, -628f), new Vector2(240f, 50f), "CANCEL", new Color(0.35f, 0.12f, 0.12f), out closeLabel);
            saveButton.onClick.AddListener(() => service?.AcceptCheck());
            redoButton.onClick.AddListener(() => service?.RedoCheck());
            closeButton.onClick.AddListener(() => service?.CancelCheck());

            bannerBackground = CreateImage(canvas.transform, "ControllerStatusBanner", Color.black);
            RectTransform bannerRect = bannerBackground.rectTransform;
            bannerRect.anchorMin = bannerRect.anchorMax = bannerRect.pivot = new Vector2(0.5f, 1f);
            bannerRect.anchoredPosition = new Vector2(0f, -16f);
            bannerRect.sizeDelta = new Vector2(1100f, 52f);
            banner = bannerBackground.gameObject;
            bannerText = CreateText(banner.transform, Vector2.zero, Vector2.zero, string.Empty, 20, FontStyle.Bold);
            Stretch(bannerText.rectTransform);
            bannerText.alignment = TextAnchor.MiddleCenter;

            panel.SetActive(false);
            banner.SetActive(false);
        }

        private void CreateStickBox(Transform parent, Vector2 position, string label, out RectTransform dot, out RectTransform target)
        {
            Text caption = CreateText(parent, position, new Vector2(StickBoxSize + 40f, 26f), label, 16, FontStyle.Bold);
            caption.rectTransform.anchoredPosition = position + new Vector2(-20f, 0f);
            caption.alignment = TextAnchor.MiddleCenter;

            Image box = CreateImage(parent, "StickBox", new Color(1f, 1f, 1f, 0.08f));
            RectTransform boxRect = box.rectTransform;
            boxRect.anchorMin = boxRect.anchorMax = boxRect.pivot = new Vector2(0f, 1f);
            boxRect.anchoredPosition = position + new Vector2(0f, -32f);
            boxRect.sizeDelta = new Vector2(StickBoxSize, StickBoxSize);

            Image crossH = CreateImage(box.transform, "CrossH", new Color(1f, 1f, 1f, 0.15f));
            CenterIn(crossH.rectTransform, new Vector2(StickBoxSize, 2f));
            Image crossV = CreateImage(box.transform, "CrossV", new Color(1f, 1f, 1f, 0.15f));
            CenterIn(crossV.rectTransform, new Vector2(2f, StickBoxSize));

            Image targetImage = CreateImage(box.transform, "Target", new Color(1f, 0.85f, 0.1f, 0.55f));
            CenterIn(targetImage.rectTransform, new Vector2(DotSize + 14f, DotSize + 14f));
            target = targetImage.rectTransform;

            Image dotImage = CreateImage(box.transform, "Dot", new Color(0.35f, 0.9f, 1f, 1f));
            CenterIn(dotImage.rectTransform, new Vector2(DotSize, DotSize));
            dot = dotImage.rectTransform;
        }

        private Slider CreateProgressBar(Transform parent, Vector2 position, Vector2 size)
        {
            Image background = CreateImage(parent, "Progress", new Color(1f, 1f, 1f, 0.18f));
            RectTransform rect = background.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Slider slider = background.gameObject.AddComponent<Slider>();
            slider.interactable = false;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            Image fill = CreateImage(background.transform, "Fill", new Color(1f, 0.85f, 0.1f, 1f));
            Stretch(fill.rectTransform);
            fill.rectTransform.offsetMin = new Vector2(2f, 2f);
            fill.rectTransform.offsetMax = new Vector2(-2f, -2f);
            slider.fillRect = fill.rectTransform;
            slider.targetGraphic = background;
            return slider;
        }

        private Button CreateButton(Transform parent, Vector2 position, Vector2 size, string label, Color color, out Text labelText)
        {
            Image image = CreateImage(parent, "Button", color);
            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Button button = image.gameObject.AddComponent<Button>();
            labelText = CreateText(image.transform, Vector2.zero, Vector2.zero, label, 20, FontStyle.Bold);
            Stretch(labelText.rectTransform);
            labelText.alignment = TextAnchor.MiddleCenter;
            return button;
        }

        private Text CreateText(Transform parent, Vector2 position, Vector2 size, string value, int fontSize, FontStyle style = FontStyle.Normal)
        {
            Text text = new GameObject("Text").AddComponent<Text>();
            text.transform.SetParent(parent, false);
            RectTransform rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.text = value;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            Image image = new GameObject(name).AddComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            return image;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static void CenterIn(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
            }

            if (eventSystem.GetComponent<BaseInputModule>() == null)
            {
                InputSystemUIInputModule module = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                module.AssignDefaultActions();
            }
        }
    }
}
