using System.Text;
using DroneSim.Drone.Input.Calibration;
using UnityEngine;
using UnityEngine.UI;

namespace DroneSim.VR
{
    /// <summary>
    /// VR presentation of ControllerSetupService: a small world-space panel above the virtual RC.
    /// The check is driven entirely by the sticks (it saves and retries on its own), so no
    /// pointer or button interaction is needed in the headset.
    /// </summary>
    public class ControllerSetupVRUI : MonoBehaviour
    {
        // 1 canvas pixel = 1 mm, so the panel is 32 cm x 20 cm in the world.
        private const float PixelsToMeters = 0.001f;
        private static readonly Vector2 PanelSize = new Vector2(320f, 200f);

        [SerializeField] private ControllerSetupService service;
        [SerializeField] private VirtualRCControllerRig rcRig;
        [SerializeField] private Vector3 panelLocalPosition = new Vector3(0f, 0.16f, 0.06f);
        [SerializeField] private Vector3 panelLocalEuler = new Vector3(25f, 0f, 0f);

        private GameObject panel;
        private Text header;
        private Text body;
        private Text status;

        public static ControllerSetupVRUI Ensure(ControllerSetupService setupService, VirtualRCControllerRig rig)
        {
            ControllerSetupVRUI ui = FindFirstObjectByType<ControllerSetupVRUI>();
            if (ui == null)
            {
                ui = new GameObject("ControllerSetupVRUI").AddComponent<ControllerSetupVRUI>();
            }

            ui.service = setupService;
            ui.rcRig = rig;
            return ui;
        }

        private void Update()
        {
            service ??= FindFirstObjectByType<ControllerSetupService>();
            if (rcRig == null)
            {
                rcRig = FindFirstObjectByType<VirtualRCControllerRig>();
            }

            if (service == null || rcRig == null)
            {
                return;
            }

            if (panel == null)
            {
                Build();
            }

            string statusLine = StatusLine();
            bool checking = service.IsCheckActive;
            panel.SetActive(checking || statusLine != null);
            header.text = checking ? "CONTROLLER CHECK" : "CONTROLLER";
            body.text = checking ? BuildCheckText(service.Flow) : string.Empty;
            status.text = statusLine ?? string.Empty;
        }

        private string StatusLine()
        {
            if (!service.IsCheckActive && service.ShowSavedConfirmation)
            {
                return "<color=#5dff8a>Calibrated and saved.</color>";
            }

            if (!service.IsControllerConnected && service.ControllerWasLost)
            {
                return "<color=#ff6b5e>Controller disconnected. The drone is holding position. Reconnect the USB cable.</color>";
            }

            if (!service.IsCheckActive && service.IsControllerConnected && !service.HasCalibration)
            {
                return "<color=#ffc247>Controller not calibrated. Ask your instructor to run the controller check.</color>";
            }

            return null;
        }

        private static string BuildCheckText(ControllerCheckFlow flow)
        {
            var builder = new StringBuilder();
            builder.Append("<b>").Append(flow.Instruction).Append("</b>\n");

            switch (flow.State)
            {
                case ControllerCheckState.CenterHold:
                case ControllerCheckState.MoveStick:
                case ControllerCheckState.ReturnToCenter:
                    int stepNumber = flow.State == ControllerCheckState.CenterHold ? 1 : flow.PromptIndex + 2;
                    builder.Append("Step ").Append(stepNumber).Append(" of ").Append(flow.PromptCount + 1).Append("  ")
                        .Append(ProgressBar(flow.StepProgress)).Append('\n');
                    break;
                case ControllerCheckState.Review:
                    if (flow.CountdownRemaining >= 0f)
                    {
                        builder.Append("Saving in ").Append(Mathf.CeilToInt(flow.CountdownRemaining)).Append("...\n");
                    }
                    break;
                case ControllerCheckState.Failed:
                    if (flow.CountdownRemaining >= 0f)
                    {
                        builder.Append("Starting over in ").Append(Mathf.CeilToInt(flow.CountdownRemaining)).Append("...\n");
                    }
                    break;
            }

            // The headset panel is small: show only the most important issue.
            CheckIssue top = null;
            foreach (CheckIssue issue in flow.Issues)
            {
                if (top == null || issue.Severity > top.Severity)
                {
                    top = issue;
                }
            }

            if (top != null && top.Severity != CheckIssueSeverity.Info)
            {
                string color = top.Severity == CheckIssueSeverity.Blocking ? "#ff6b5e" : "#ffc247";
                builder.Append("<color=").Append(color).Append('>').Append(top.Message).Append("</color>");
            }

            return builder.ToString();
        }

        private static string ProgressBar(float value)
        {
            const int Cells = 10;
            int filled = Mathf.Clamp(Mathf.RoundToInt(value * Cells), 0, Cells);
            return "[" + new string('#', filled) + new string('-', Cells - filled) + "]";
        }

        private void Build()
        {
            Canvas canvas = new GameObject("ControllerSetupVRPanel").AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.transform.SetParent(rcRig.transform, false);
            canvas.transform.localPosition = panelLocalPosition;
            canvas.transform.localRotation = Quaternion.Euler(panelLocalEuler);
            canvas.transform.localScale = Vector3.one * PixelsToMeters;
            RectTransform rect = canvas.GetComponent<RectTransform>();
            rect.sizeDelta = PanelSize;
            panel = canvas.gameObject;

            Image background = new GameObject("Background").AddComponent<Image>();
            background.transform.SetParent(canvas.transform, false);
            background.color = new Color(0.02f, 0.03f, 0.05f, 0.9f);
            RectTransform bg = background.rectTransform;
            bg.anchorMin = Vector2.zero;
            bg.anchorMax = Vector2.one;
            bg.sizeDelta = Vector2.zero;

            header = CreateText(canvas.transform, new Vector2(12f, -10f), new Vector2(296f, 24f), 16, FontStyle.Bold);
            body = CreateText(canvas.transform, new Vector2(12f, -38f), new Vector2(296f, 120f), 13, FontStyle.Normal);
            status = CreateText(canvas.transform, new Vector2(12f, -160f), new Vector2(296f, 34f), 12, FontStyle.Bold);
        }

        private static Text CreateText(Transform parent, Vector2 position, Vector2 size, int fontSize, FontStyle style)
        {
            Text text = new GameObject("Text").AddComponent<Text>();
            text.transform.SetParent(parent, false);
            RectTransform rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = Color.white;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }
    }
}
