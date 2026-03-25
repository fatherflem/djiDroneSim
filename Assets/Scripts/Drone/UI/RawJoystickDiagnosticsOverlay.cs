using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace DroneSim.Drone.UI
{
    /// <summary>
    /// Purpose: Runtime-only overlay for inspecting raw joystick controls reported by Unity Input System.
    /// Does NOT: modify flight controls, mappings, or simulator behavior.
    /// Fits in sim: temporary diagnostics utility for identifying stick/axis names and control paths.
    /// Depends on: UnityEngine.InputSystem joystick devices.
    /// </summary>
    public class RawJoystickDiagnosticsOverlay : MonoBehaviour
    {
        [Header("Visibility")]
        [Tooltip("Overlay starts visible on play. Toggle at runtime using the keys below.")]
        [SerializeField] private bool startVisible = true;

        [Tooltip("Keyboard key to toggle the diagnostics overlay.")]
        [SerializeField] private Key toggleKeyPrimary = Key.Backquote;

        [Tooltip("Secondary keyboard key to toggle the diagnostics overlay.")]
        [SerializeField] private Key toggleKeySecondary = Key.F1;

        [Header("Filtering")]
        [Tooltip("Minimum absolute value required to highlight a control as active.")]
        [Range(0f, 1f)]
        [SerializeField] private float activityThreshold = 0.08f;

        [Tooltip("Only show controls that are currently above the activity threshold.")]
        [SerializeField] private bool showOnlyChangingControls;

        [Header("Logging")]
        [Tooltip("Logs active axis controls to Console while moving (throttled to avoid spam).")]
        [SerializeField] private bool logMovingAxes;

        [Tooltip("Seconds between repeated logs for the same control.")]
        [SerializeField] private float axisLogCooldown = 0.25f;

        private readonly Dictionary<string, float> nextLogTimeByPath = new Dictionary<string, float>();
        private readonly List<AxisControl> axisBuffer = new List<AxisControl>();
        private readonly List<ButtonControl> buttonBuffer = new List<ButtonControl>();

        private Joystick activeJoystick;
        private GUIStyle panelStyle;
        private GUIStyle labelStyle;
        private Texture2D panelBackground;
        private bool isVisible;

        private void Awake()
        {
            isVisible = startVisible;
        }

        private void Update()
        {
            HandleToggleInput();
            SelectActiveJoystick();
            if (logMovingAxes)
            {
                LogMovingAxes();
            }
        }

        private void HandleToggleInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            bool toggledPrimary = keyboard[toggleKeyPrimary].wasPressedThisFrame;
            bool toggledSecondary = keyboard[toggleKeySecondary].wasPressedThisFrame;
            if (toggledPrimary || toggledSecondary)
            {
                isVisible = !isVisible;
            }
        }

        private void SelectActiveJoystick()
        {
            if (Joystick.all.Count == 0)
            {
                activeJoystick = null;
                return;
            }

            float bestActivity = activityThreshold;
            Joystick best = activeJoystick != null ? activeJoystick : Joystick.current ?? Joystick.all[0];

            foreach (Joystick joystick in Joystick.all)
            {
                float axisActivity = 0f;
                foreach (AxisControl axis in joystick.allControls.OfType<AxisControl>())
                {
                    axisActivity = Mathf.Max(axisActivity, Mathf.Abs(axis.ReadValue()));
                }

                bool buttonPressed = joystick.allControls.OfType<ButtonControl>().Any(button => button.isPressed);
                float totalActivity = buttonPressed ? Mathf.Max(axisActivity, activityThreshold + 0.01f) : axisActivity;
                if (totalActivity > bestActivity)
                {
                    bestActivity = totalActivity;
                    best = joystick;
                }
            }

            activeJoystick = best;
        }

        private void OnGUI()
        {
            if (!isVisible)
            {
                return;
            }

            EnsureStyles();
            Rect panelRect = new Rect(410f, 16f, 620f, Screen.height - 32f);
            GUILayout.BeginArea(panelRect, GUIContent.none, panelStyle);
            GUILayout.Label("Raw Joystick Diagnostics", labelStyle);
            GUILayout.Label($"Toggle: {toggleKeyPrimary} / {toggleKeySecondary}", labelStyle);

            if (Joystick.all.Count == 0)
            {
                GUILayout.Space(6f);
                GUILayout.Label("No joystick detected by Unity Input System.", labelStyle);
                GUILayout.EndArea();
                return;
            }

            if (activeJoystick == null)
            {
                activeJoystick = Joystick.current ?? Joystick.all[0];
            }

            GUILayout.Space(6f);
            GUILayout.Label($"Active Joystick: {activeJoystick.displayName}", labelStyle);
            GUILayout.Label($"Product: {activeJoystick.description.product}", labelStyle);
            GUILayout.Label($"Interface: {activeJoystick.description.interfaceName}", labelStyle);
            GUILayout.Label($"Show only changing: {showOnlyChangingControls}", labelStyle);
            GUILayout.Label($"Activity threshold: {activityThreshold:F2}", labelStyle);

            axisBuffer.Clear();
            axisBuffer.AddRange(activeJoystick.allControls.OfType<AxisControl>());
            GUILayout.Space(8f);
            GUILayout.Label($"Axes ({axisBuffer.Count})", labelStyle);
            DrawAxisList();

            buttonBuffer.Clear();
            buttonBuffer.AddRange(activeJoystick.allControls.OfType<ButtonControl>());
            GUILayout.Space(8f);
            GUILayout.Label($"Buttons ({buttonBuffer.Count})", labelStyle);
            DrawButtonList();

            GUILayout.EndArea();
        }

        private void DrawAxisList()
        {
            foreach (AxisControl axis in axisBuffer)
            {
                float value = axis.ReadValue();
                bool isActive = Mathf.Abs(value) >= activityThreshold;
                if (showOnlyChangingControls && !isActive)
                {
                    continue;
                }

                string color = isActive ? "#7CFF7C" : "#D8E0E8";
                string label = $"<color={color}>{axis.name,-18} {value,7:F3}  {axis.path}</color>";
                GUILayout.Label(label, labelStyle);
            }
        }

        private void DrawButtonList()
        {
            foreach (ButtonControl button in buttonBuffer)
            {
                bool isPressed = button.isPressed;
                if (showOnlyChangingControls && !isPressed)
                {
                    continue;
                }

                string color = isPressed ? "#FFD166" : "#D8E0E8";
                string state = isPressed ? "Pressed" : "Idle";
                string label = $"<color={color}>{button.name,-18} {state,-7}  {button.path}</color>";
                GUILayout.Label(label, labelStyle);
            }
        }

        private void LogMovingAxes()
        {
            if (activeJoystick == null)
            {
                return;
            }

            foreach (AxisControl axis in activeJoystick.allControls.OfType<AxisControl>())
            {
                float value = axis.ReadValue();
                if (Mathf.Abs(value) < activityThreshold)
                {
                    continue;
                }

                string path = axis.path;
                float now = Time.unscaledTime;
                if (nextLogTimeByPath.TryGetValue(path, out float allowedTime) && now < allowedTime)
                {
                    continue;
                }

                nextLogTimeByPath[path] = now + Mathf.Max(0.05f, axisLogCooldown);
                Debug.Log($"[JoystickDiag] {activeJoystick.displayName} axis {axis.name} path={path} value={value:F3}");
            }
        }

        private void EnsureStyles()
        {
            if (panelStyle != null)
            {
                return;
            }

            panelStyle = new GUIStyle(GUI.skin.box);
            panelBackground = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            panelBackground.SetPixel(0, 0, new Color(0.04f, 0.06f, 0.08f, 0.93f));
            panelBackground.Apply();
            panelStyle.normal.background = panelBackground;

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                richText = true
            };
            labelStyle.normal.textColor = new Color(0.9f, 0.95f, 1f);
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
