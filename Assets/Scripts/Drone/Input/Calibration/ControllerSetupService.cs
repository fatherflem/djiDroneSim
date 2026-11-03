using System;
using System.Collections.Generic;
using DroneSim.Drone.Benchmark;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DroneSim.Drone.Input.Calibration
{
    /// <summary>
    /// Purpose: owns the physical controller's lifecycle — connect/disconnect, loading and applying its
    /// saved calibration, and running the guided controller check.
    /// Does NOT: draw anything. ControllerSetupDesktopUI and ControllerSetupVRUI present the same state.
    /// Fits in sim: sits beside DroneInputReader; while a check runs it holds the drone at neutral input.
    /// </summary>
    public class ControllerSetupService : MonoBehaviour
    {
        [SerializeField] private DroneInputReader inputReader;

        [Tooltip("Start the controller check by itself when a controller with no saved calibration is connected.")]
        [SerializeField] private bool autoStartWhenUncalibrated = true;

        [Tooltip("Key that starts or cancels the controller check.")]
        [SerializeField] private Key toggleCheckKey = Key.F4;

        [Tooltip("Seconds the results show before saving on their own. 0 waits for Save (desktop only).")]
        [SerializeField, Min(0f)] private float autoSaveSeconds = 4f;

        [Tooltip("Seconds a failed check waits before starting over on its own. 0 waits for Redo (desktop only).")]
        [SerializeField, Min(0f)] private float autoRetrySeconds = 8f;

        private readonly float[] rawAxes = new float[StickAxes.Count];
        private readonly InputAction[] rawActions = new InputAction[StickAxes.Count];

        private ControllerCheckFlow flow;
        private Joystick controller;
        private ControllerCalibrationProfile activeProfile;
        private bool ownsExternalInput;
        private BenchmarkRunner benchmarkRunner;
        private float savedMessageUntil;

        /// <summary>True while any controller check is running. Training input checks this to ignore Start/Retry.</summary>
        public static bool IsCheckRunning { get; private set; }

        public event Action StatusChanged;

        public ControllerCheckFlow Flow => flow;
        public bool IsCheckActive => flow != null && flow.IsActive;
        public bool IsControllerConnected => controller != null && controller.added;
        public string ControllerName => controller != null ? DeviceName(controller) : string.Empty;
        public ControllerCalibrationProfile ActiveProfile => activeProfile;
        public bool HasCalibration => activeProfile != null;
        public bool ControllerWasLost { get; private set; }
        public bool ShowSavedConfirmation => Time.unscaledTime < savedMessageUntil;
        public Key ToggleCheckKey => toggleCheckKey;

        /// <summary>Finds or creates the scene's service and points it at the reader.</summary>
        public static ControllerSetupService Ensure(DroneInputReader reader)
        {
            ControllerSetupService service = FindFirstObjectByType<ControllerSetupService>();
            if (service == null)
            {
                service = new GameObject("ControllerSetup").AddComponent<ControllerSetupService>();
            }

            service.SetInputReader(reader);
            return service;
        }

        public void SetInputReader(DroneInputReader reader)
        {
            inputReader = reader;
            RebuildRawActions();
            ApplyProfileToReader();
        }

        public void BeginCheck()
        {
            if (inputReader == null)
            {
                Debug.LogWarning("Controller check needs a DroneInputReader.", this);
                return;
            }

            if (IsBenchmarkRunning() || (inputReader.UseExternalInput && !ownsExternalInput))
            {
                Debug.LogWarning("Controller check can't start while a benchmark is driving the drone.", this);
                return;
            }

            flow = new ControllerCheckFlow(BuildSettings());
            flow.Begin(ControllerName, IsControllerConnected);
            HoldDroneNeutral(true);
            SetRunning(true);
        }

        public void CancelCheck()
        {
            if (flow == null)
            {
                return;
            }

            flow.Cancel();
            EndCheck();
        }

        public void AcceptCheck()
        {
            if (flow != null && flow.Accept())
            {
                CompleteSavedCheck();
            }
        }

        public void RedoCheck()
        {
            flow?.Redo();
            NotifyChanged();
        }

        public void ClearCalibration()
        {
            if (controller != null)
            {
                ControllerCalibrationStore.Delete(ControllerName);
            }

            activeProfile = null;
            ApplyProfileToReader();
            NotifyChanged();
        }

        private void Awake()
        {
            inputReader ??= FindFirstObjectByType<DroneInputReader>();
        }

        private void OnEnable()
        {
            InputSystem.onDeviceChange += OnDeviceChange;
            RebuildRawActions();
            AttachController(FindConnectedJoystick());
        }

        private void OnDisable()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
            if (IsCheckActive)
            {
                flow.Cancel();
                EndCheck();
            }

            DisposeRawActions();
        }

        private void Start()
        {
            MaybeAutoStart();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current[toggleCheckKey].wasPressedThisFrame)
            {
                if (IsCheckActive)
                {
                    CancelCheck();
                }
                else
                {
                    BeginCheck();
                }
            }

            if (!IsCheckActive)
            {
                return;
            }

            if (IsBenchmarkRunning())
            {
                // The benchmark took over the reader; give it the drone and stop the check.
                ownsExternalInput = false;
                CancelCheck();
                return;
            }

            for (int i = 0; i < rawAxes.Length; i++)
            {
                rawAxes[i] = rawActions[i] != null ? rawActions[i].ReadValue<float>() : 0f;
            }

            ControllerCheckState before = flow.State;
            flow.Tick(Time.unscaledDeltaTime, rawAxes, IsControllerConnected, ControllerName);

            if (flow.State == ControllerCheckState.Saved)
            {
                CompleteSavedCheck();
            }
            else if (flow.State == ControllerCheckState.Cancelled)
            {
                EndCheck();
            }
            else if (flow.State != before)
            {
                NotifyChanged();
            }
        }

        private void CompleteSavedCheck()
        {
            ControllerCalibrationProfile profile = flow.Result;
            if (profile != null && profile.IsUsable)
            {
                activeProfile = profile;
                ControllerCalibrationStore.Save(profile);
                ApplyProfileToReader();
                savedMessageUntil = Time.unscaledTime + 3f;
            }

            EndCheck();
        }

        private void EndCheck()
        {
            HoldDroneNeutral(false);
            SetRunning(false);
            NotifyChanged();
        }

        // ------------------------------------------------------------------ device handling

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (!(device is Joystick joystick))
            {
                return;
            }

            switch (change)
            {
                case InputDeviceChange.Added:
                case InputDeviceChange.Reconnected:
                case InputDeviceChange.Enabled:
                    if (controller == null || !controller.added)
                    {
                        AttachController(joystick);
                        MaybeAutoStart();
                    }
                    break;

                case InputDeviceChange.Removed:
                case InputDeviceChange.Disconnected:
                case InputDeviceChange.Disabled:
                    if (joystick == controller)
                    {
                        // Sticks read zero once the device is gone, so the drone holds position.
                        controller = null;
                        ControllerWasLost = true;
                        AttachController(FindConnectedJoystick());
                        NotifyChanged();
                    }
                    break;
            }
        }

        private void AttachController(Joystick joystick)
        {
            controller = joystick;
            if (controller != null)
            {
                ControllerWasLost = false;
                activeProfile = ControllerCalibrationStore.Load(ControllerName);
                ApplyProfileToReader();
            }

            NotifyChanged();
        }

        private void MaybeAutoStart()
        {
            if (autoStartWhenUncalibrated && !Application.isBatchMode && isActiveAndEnabled
                && IsControllerConnected && !HasCalibration && !IsCheckActive && inputReader != null)
            {
                BeginCheck();
            }
        }

        private static Joystick FindConnectedJoystick()
        {
            foreach (Joystick joystick in Joystick.all)
            {
                if (joystick != null && joystick.added && joystick.enabled)
                {
                    return joystick;
                }
            }

            return null;
        }

        private static string DeviceName(InputDevice device)
        {
            string product = device.description.product;
            return string.IsNullOrWhiteSpace(product) ? device.displayName : product;
        }

        // ------------------------------------------------------------------ reader integration

        private void ApplyProfileToReader()
        {
            inputReader?.SetCalibration(IsControllerConnected ? activeProfile : null);
        }

        private void HoldDroneNeutral(bool hold)
        {
            if (inputReader == null)
            {
                return;
            }

            if (hold && !inputReader.UseExternalInput)
            {
                DroneInputFrame neutral = new DroneInputFrame { RequestedMode = inputReader.CurrentInput.RequestedMode };
                inputReader.SetExternalInputEnabled(true);
                inputReader.SetExternalInputFrame(neutral);
                ownsExternalInput = true;
            }
            else if (!hold && ownsExternalInput)
            {
                inputReader.SetExternalInputEnabled(false);
                ownsExternalInput = false;
            }
        }

        private bool IsBenchmarkRunning()
        {
            if (benchmarkRunner == null)
            {
                benchmarkRunner = FindFirstObjectByType<BenchmarkRunner>();
            }

            return benchmarkRunner != null && benchmarkRunner.IsRunning;
        }

        private ControllerCheckSettings BuildSettings()
        {
            var settings = new ControllerCheckSettings
            {
                autoSaveSeconds = autoSaveSeconds,
                autoRetrySeconds = autoRetrySeconds,
            };

            DroneInputConfig config = inputReader != null ? inputReader.Config : null;
            if (config != null)
            {
                settings.stickDeadzone = config.stickDeadzone;
                settings.expectedRawSign[(int)StickAxis.Pitch] = config.invertPitch ? -1 : 1;
                settings.expectedRawSign[(int)StickAxis.Throttle] = config.invertThrottle ? -1 : 1;
            }

            return settings;
        }

        private void RebuildRawActions()
        {
            DisposeRawActions();
            DroneInputConfig config = inputReader != null ? inputReader.Config : null;
            if (config == null)
            {
                return;
            }

            rawActions[(int)StickAxis.Roll] = CreateRawAction("RawRoll", config.rollBinding);
            rawActions[(int)StickAxis.Pitch] = CreateRawAction("RawPitch", config.pitchBinding);
            rawActions[(int)StickAxis.Yaw] = CreateRawAction("RawYaw", config.yawBinding);
            rawActions[(int)StickAxis.Throttle] = CreateRawAction("RawThrottle", config.throttleBinding);
        }

        private static InputAction CreateRawAction(string name, string joystickBinding)
        {
            // Joystick bindings only: no keyboard/gamepad fallbacks, no deadzone, no calibration.
            // Value (not PassThrough) so a stick resting off center reads correctly before it first moves.
            var action = new InputAction(name, InputActionType.Value);
            foreach (string binding in DroneInputReader.ExpandJoystickAxisBinding(joystickBinding))
            {
                action.AddBinding(binding);
            }

            action.Enable();
            return action;
        }

        private void DisposeRawActions()
        {
            for (int i = 0; i < rawActions.Length; i++)
            {
                rawActions[i]?.Dispose();
                rawActions[i] = null;
            }
        }

        private void SetRunning(bool running)
        {
            IsCheckRunning = running;
        }

        private void NotifyChanged()
        {
            StatusChanged?.Invoke();
        }

        /// <summary>Calibrated or live stick values for previews, in pilot terms (+1 = right/up).</summary>
        public Vector2 GetStickPreview(bool leftStick)
        {
            StickAxis xAxis = leftStick ? StickAxis.Yaw : StickAxis.Roll;
            StickAxis yAxis = leftStick ? StickAxis.Throttle : StickAxis.Pitch;
            return new Vector2(PilotValue(xAxis), PilotValue(yAxis));
        }

        private float PilotValue(StickAxis axis)
        {
            float raw = rawActions[(int)axis] != null ? rawActions[(int)axis].ReadValue<float>() : 0f;
            float value;
            if (IsCheckActive && flow.State != ControllerCheckState.WaitingForController)
            {
                value = raw - flow.GetCenter(axis);
            }
            else
            {
                value = activeProfile != null ? activeProfile.Apply(axis, raw) : raw;
            }

            DroneInputConfig config = inputReader != null ? inputReader.Config : null;
            if (config != null && axis == StickAxis.Pitch && config.invertPitch) value = -value;
            if (config != null && axis == StickAxis.Throttle && config.invertThrottle) value = -value;
            return Mathf.Clamp(value, -1f, 1f);
        }

        /// <summary>Issues from the current or last check, most severe first.</summary>
        public IEnumerable<CheckIssue> IssuesBySeverity()
        {
            if (flow == null)
            {
                yield break;
            }

            for (int severity = (int)CheckIssueSeverity.Blocking; severity >= 0; severity--)
            {
                foreach (CheckIssue issue in flow.Issues)
                {
                    if ((int)issue.Severity == severity)
                    {
                        yield return issue;
                    }
                }
            }
        }
    }
}
