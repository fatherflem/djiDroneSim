using System;
using System.Collections.Generic;

namespace DroneSim.Drone.Input.Calibration
{
    public enum ControllerCheckState
    {
        NotStarted,
        WaitingForController,
        CenterHold,
        MoveStick,
        ReturnToCenter,
        Review,
        Failed,
        Saved,
        Cancelled
    }

    public enum CheckIssueSeverity
    {
        Info,
        Warning,
        Blocking
    }

    public sealed class CheckIssue
    {
        public CheckIssue(CheckIssueSeverity severity, string code, string message)
        {
            Severity = severity;
            Code = code;
            Message = message;
        }

        public CheckIssueSeverity Severity { get; }
        public string Code { get; }
        public string Message { get; }

        public override string ToString() => $"{Severity}: {Message}";
    }

    /// <summary>One "push this stick that way" step. Direction is in pilot terms: +1 = right/up.</summary>
    public readonly struct StickPrompt
    {
        public StickPrompt(StickAxis axis, int direction, string instruction)
        {
            Axis = axis;
            Direction = direction;
            Instruction = instruction;
        }

        public StickAxis Axis { get; }
        public int Direction { get; }
        public string Instruction { get; }
    }

    public sealed class ControllerCheckSettings
    {
        /// <summary>Sticks must stay within this raw peak-to-peak band to count as "hands off".</summary>
        public float centerStillTolerance = 0.04f;
        public float centerHoldSeconds = 1.5f;
        /// <summary>How far (raw units from center) the prompted axis must move to count.</summary>
        public float moveThreshold = 0.5f;
        /// <summary>An unprompted axis moving this far while the prompted one hasn't means the mapping is wrong.</summary>
        public float wrongAxisThreshold = 0.5f;
        public float extremeStillTolerance = 0.03f;
        public float extremeHoldSeconds = 0.3f;
        public float returnTolerance = 0.1f;
        public float returnTimeoutSeconds = 2.5f;
        /// <summary>Less end-to-end travel than this (raw units from center) gets a warning.</summary>
        public float limitedTravelThreshold = 0.75f;
        public float centerOffsetNote = 0.1f;
        /// <summary>Center jitter above this fraction of the deadzone gets a warning.</summary>
        public float jitterWarningFractionOfDeadzone = 0.5f;
        public float stickDeadzone = 0.15f;
        /// <summary>Seconds the review screen shows before saving on its own. 0 or less waits for Accept().</summary>
        public float autoSaveSeconds = 4f;
        /// <summary>Seconds a failed check waits before starting over on its own. 0 or less waits for Redo().</summary>
        public float autoRetrySeconds = 8f;

        /// <summary>
        /// Raw sign the device should report when the pilot pushes each axis right/up, given how
        /// DroneInputReader post-processes the value (for example invertPitch flips pitch).
        /// </summary>
        public readonly int[] expectedRawSign = { 1, 1, 1, 1 };
    }

    /// <summary>
    /// Guided controller check and calibration: hands off (center), then each stick to each end,
    /// letting go between steps. Detects swapped or reversed channels, limited travel, jitter, and
    /// a throttle that doesn't spring back to center. Presentation-agnostic: desktop and VR UIs
    /// both read State/Instruction/Issues and feed raw axis values into Tick.
    /// </summary>
    public sealed class ControllerCheckFlow
    {
        private const int AxisCount = StickAxes.Count;

        private readonly ControllerCheckSettings settings;
        private readonly List<StickPrompt> prompts;
        private readonly List<CheckIssue> issues = new List<CheckIssue>();

        private readonly float[] lastRaw = new float[AxisCount];
        private readonly float[] center = new float[AxisCount];
        private readonly float[] centerNoise = new float[AxisCount];
        private readonly float[] windowMin = new float[AxisCount];
        private readonly float[] windowMax = new float[AxisCount];
        private readonly double[] windowSum = new double[AxisCount];
        private readonly float[] rawAtPositive = new float[AxisCount];
        private readonly float[] rawAtNegative = new float[AxisCount];
        private readonly bool[] capturedPositive = new bool[AxisCount];
        private readonly bool[] capturedNegative = new bool[AxisCount];
        private readonly bool[] reversedReported = new bool[AxisCount];
        private readonly bool[] notCentering = new bool[AxisCount];

        private int windowSamples;
        private float stateTimer;
        private float stillTimer;
        private int promptIndex;
        private float promptBestDelta;
        private float promptBestRaw;
        private float promptPreviousRaw;
        private string deviceName = string.Empty;
        private ControllerCalibrationProfile result;

        public ControllerCheckFlow(ControllerCheckSettings settings = null)
        {
            this.settings = settings ?? new ControllerCheckSettings();
            prompts = BuildMode2Prompts();
            State = ControllerCheckState.NotStarted;
        }

        public ControllerCheckState State { get; private set; }
        public IReadOnlyList<CheckIssue> Issues => issues;
        public IReadOnlyList<StickPrompt> Prompts => prompts;
        public int PromptIndex => promptIndex;
        public int PromptCount => prompts.Count;
        public StickPrompt? CurrentPrompt => promptIndex >= 0 && promptIndex < prompts.Count ? prompts[promptIndex] : (StickPrompt?)null;
        public ControllerCalibrationProfile Result => result;
        public ControllerCheckSettings Settings => settings;
        public bool IsActive => State == ControllerCheckState.WaitingForController || State == ControllerCheckState.CenterHold
            || State == ControllerCheckState.MoveStick || State == ControllerCheckState.ReturnToCenter
            || State == ControllerCheckState.Review || State == ControllerCheckState.Failed;
        public bool HasBlockingIssue => issues.Exists(i => i.Severity == CheckIssueSeverity.Blocking);

        /// <summary>0..1 progress through the current timed step (center hold, extreme hold, review countdown).</summary>
        public float StepProgress { get; private set; }

        /// <summary>Seconds until Review saves or Failed retries on its own; negative when not counting down.</summary>
        public float CountdownRemaining { get; private set; } = -1f;

        public float GetLastRaw(StickAxis axis) => lastRaw[(int)axis];
        public float GetCenter(StickAxis axis) => center[(int)axis];

        /// <summary>Live stick position relative to the measured center (raw units), for stick previews.</summary>
        public float GetLiveOffset(StickAxis axis) => lastRaw[(int)axis] - center[(int)axis];

        public string Instruction
        {
            get
            {
                switch (State)
                {
                    case ControllerCheckState.NotStarted:
                        return "Controller check not started.";
                    case ControllerCheckState.WaitingForController:
                        return "Connect your controller (USB) to begin the controller check.";
                    case ControllerCheckState.CenterHold:
                        return "Let go of both sticks and keep your hands off the controller.";
                    case ControllerCheckState.MoveStick:
                        return CurrentPrompt?.Instruction ?? string.Empty;
                    case ControllerCheckState.ReturnToCenter:
                        return "Let go of the stick.";
                    case ControllerCheckState.Review:
                        return issues.Exists(i => i.Severity == CheckIssueSeverity.Warning)
                            ? "Controller check finished with warnings. Review them below."
                            : "Controller check passed.";
                    case ControllerCheckState.Failed:
                        return "Controller check failed. Fix the problem below, then try again.";
                    case ControllerCheckState.Saved:
                        return "Controller calibrated and saved.";
                    default:
                        return "Controller check cancelled.";
                }
            }
        }

        public void Begin(string connectedDeviceName, bool controllerConnected)
        {
            deviceName = connectedDeviceName ?? string.Empty;
            issues.Clear();
            result = null;
            Array.Clear(capturedPositive, 0, AxisCount);
            Array.Clear(capturedNegative, 0, AxisCount);
            Array.Clear(reversedReported, 0, AxisCount);
            Array.Clear(notCentering, 0, AxisCount);
            promptIndex = 0;
            CountdownRemaining = -1f;
            Enter(controllerConnected ? ControllerCheckState.CenterHold : ControllerCheckState.WaitingForController);
        }

        public void Cancel()
        {
            if (State != ControllerCheckState.Saved)
            {
                Enter(ControllerCheckState.Cancelled);
            }
        }

        public void Redo() => Begin(deviceName, State != ControllerCheckState.WaitingForController);

        /// <summary>Save now from Review. Returns false if there's nothing valid to save.</summary>
        public bool Accept()
        {
            if (State != ControllerCheckState.Review || result == null || !result.IsUsable)
            {
                return false;
            }

            Enter(ControllerCheckState.Saved);
            return true;
        }

        /// <param name="rawAxes">Raw device values indexed by StickAxis, before deadzone or calibration.</param>
        public void Tick(float deltaTime, IReadOnlyList<float> rawAxes, bool controllerConnected, string connectedDeviceName = null)
        {
            if (!IsActive)
            {
                return;
            }

            if (!controllerConnected)
            {
                if (State != ControllerCheckState.WaitingForController && State != ControllerCheckState.Review && State != ControllerCheckState.Failed)
                {
                    Enter(ControllerCheckState.WaitingForController);
                }

                return;
            }

            if (!string.IsNullOrEmpty(connectedDeviceName))
            {
                deviceName = connectedDeviceName;
            }

            for (int i = 0; i < AxisCount; i++)
            {
                lastRaw[i] = rawAxes != null && i < rawAxes.Count ? rawAxes[i] : 0f;
            }

            float dt = Math.Max(0f, deltaTime);
            stateTimer += dt;

            switch (State)
            {
                case ControllerCheckState.WaitingForController:
                    Begin(deviceName, true);
                    break;
                case ControllerCheckState.CenterHold:
                    TickCenterHold(dt);
                    break;
                case ControllerCheckState.MoveStick:
                    TickMoveStick(dt);
                    break;
                case ControllerCheckState.ReturnToCenter:
                    TickReturnToCenter();
                    break;
                case ControllerCheckState.Review:
                    TickCountdown(settings.autoSaveSeconds, () => Accept());
                    break;
                case ControllerCheckState.Failed:
                    TickCountdown(settings.autoRetrySeconds, Redo);
                    break;
            }
        }

        // ------------------------------------------------------------------ steps

        private void TickCenterHold(float dt)
        {
            bool still = true;
            for (int i = 0; i < AxisCount; i++)
            {
                windowMin[i] = Math.Min(windowMin[i], lastRaw[i]);
                windowMax[i] = Math.Max(windowMax[i], lastRaw[i]);
                if (windowMax[i] - windowMin[i] > settings.centerStillTolerance)
                {
                    still = false;
                }
            }

            if (!still)
            {
                ResetCenterWindow();
                return;
            }

            for (int i = 0; i < AxisCount; i++)
            {
                windowSum[i] += lastRaw[i];
            }

            windowSamples++;
            stillTimer += dt;
            StepProgress = Math.Min(1f, stillTimer / Math.Max(0.01f, settings.centerHoldSeconds));

            if (stillTimer >= settings.centerHoldSeconds && windowSamples > 0)
            {
                for (int i = 0; i < AxisCount; i++)
                {
                    center[i] = (float)(windowSum[i] / windowSamples);
                    centerNoise[i] = windowMax[i] - windowMin[i];
                }

                promptIndex = 0;
                Enter(ControllerCheckState.MoveStick);
            }
        }

        private void TickMoveStick(float dt)
        {
            StickPrompt prompt = prompts[promptIndex];
            int axis = (int)prompt.Axis;

            // A stick that doesn't spring back (typically a hobby-radio throttle) can rest at the very
            // end this prompt asks for, so measure travel from the opposite end instead of the rest point.
            bool positiveEnd = IsPositiveEnd(prompt);
            bool fromOtherEnd = notCentering[axis] && (positiveEnd ? capturedNegative[axis] : capturedPositive[axis]);
            float reference = fromOtherEnd ? (positiveEnd ? rawAtNegative[axis] : rawAtPositive[axis]) : center[axis];
            float required = fromOtherEnd ? settings.moveThreshold * 2f : settings.moveThreshold;
            float delta = lastRaw[axis] - reference;
            float magnitude = Math.Abs(delta);

            if (magnitude < 0.25f)
            {
                for (int other = 0; other < AxisCount; other++)
                {
                    if (other != axis && Math.Abs(lastRaw[other] - center[other]) > settings.wrongAxisThreshold)
                    {
                        AddIssue(CheckIssueSeverity.Blocking, "mapping_mismatch",
                            $"Asked for {StickAxes.ShortName(prompt.Axis)} but {StickAxes.ShortName((StickAxis)other)} moved instead. " +
                            "Set the radio to Mode 2 and check its channel order (roll, pitch, throttle, yaw).");
                        Fail();
                        return;
                    }
                }
            }

            if (magnitude > promptBestDelta)
            {
                promptBestDelta = magnitude;
                promptBestRaw = lastRaw[axis];
            }

            bool farEnough = magnitude >= required;
            bool holdingStill = Math.Abs(lastRaw[axis] - promptPreviousRaw) <= settings.extremeStillTolerance;
            promptPreviousRaw = lastRaw[axis];

            if (!farEnough || !holdingStill)
            {
                stillTimer = 0f;
                StepProgress = farEnough ? 0f : magnitude / Math.Max(0.01f, required) * 0.5f;
                return;
            }

            stillTimer += dt;
            StepProgress = 0.5f + 0.5f * Math.Min(1f, stillTimer / Math.Max(0.01f, settings.extremeHoldSeconds));
            if (stillTimer < settings.extremeHoldSeconds)
            {
                return;
            }

            if (!RecordExtreme(prompt, promptBestRaw, reference, fromOtherEnd))
            {
                Fail();
                return;
            }

            Enter(ControllerCheckState.ReturnToCenter);
        }

        private bool RecordExtreme(StickPrompt prompt, float raw, float reference, bool fromOtherEnd)
        {
            int axis = (int)prompt.Axis;
            float movedSign = Math.Sign(raw - reference);
            int expectedSign = prompt.Direction * settings.expectedRawSign[axis];
            bool positiveEnd = IsPositiveEnd(prompt);

            // The other end of this axis, if already captured, must be on the opposite side of center.
            bool otherCaptured = positiveEnd ? capturedNegative[axis] : capturedPositive[axis];
            float otherRaw = positiveEnd ? rawAtNegative[axis] : rawAtPositive[axis];
            if (otherCaptured && !fromOtherEnd && Math.Sign(otherRaw - center[axis]) == movedSign)
            {
                AddIssue(CheckIssueSeverity.Blocking, "same_direction_twice",
                    $"{StickAxes.ShortName(prompt.Axis)} moved the same way for both directions. Follow each arrow, then try again.");
                return false;
            }

            if (movedSign != expectedSign && !reversedReported[axis])
            {
                reversedReported[axis] = true;
                AddIssue(CheckIssueSeverity.Warning, "axis_reversed",
                    $"{StickAxes.ShortName(prompt.Axis)} reads reversed on this controller. Calibration corrects it, " +
                    "as long as you pushed the stick the way the prompt asked. If not, choose Redo.");
            }

            if (positiveEnd)
            {
                rawAtPositive[axis] = raw;
                capturedPositive[axis] = true;
            }
            else
            {
                rawAtNegative[axis] = raw;
                capturedNegative[axis] = true;
            }

            return true;
        }

        /// <summary>
        /// Which calibration end a prompt measures. Calibration outputs values in DroneInputReader's raw
        /// convention (before invertPitch/invertThrottle), so "positive" is the end that should read
        /// +1 raw, not the end the pilot calls right/up.
        /// </summary>
        private bool IsPositiveEnd(StickPrompt prompt)
        {
            return prompt.Direction * settings.expectedRawSign[(int)prompt.Axis] > 0;
        }

        private void TickReturnToCenter()
        {
            StickPrompt prompt = prompts[promptIndex];
            int axis = (int)prompt.Axis;

            bool allCentered = true;
            for (int i = 0; i < AxisCount; i++)
            {
                if (notCentering[i])
                {
                    continue;
                }

                if (Math.Abs(lastRaw[i] - center[i]) > settings.returnTolerance)
                {
                    allCentered = false;
                    break;
                }
            }

            StepProgress = Math.Min(1f, stateTimer / Math.Max(0.01f, settings.returnTimeoutSeconds));

            if (!allCentered && stateTimer < settings.returnTimeoutSeconds)
            {
                return;
            }

            if (!allCentered && Math.Abs(lastRaw[axis] - center[axis]) > settings.returnTolerance && !notCentering[axis])
            {
                notCentering[axis] = true;
                if (prompt.Axis == StickAxis.Throttle)
                {
                    AddIssue(CheckIssueSeverity.Warning, "throttle_not_centering",
                        "The throttle stick doesn't spring back to center. DJI controllers center both sticks, " +
                        "so students learn \"let go to hover\". Switch this radio's throttle to self-centering.");
                }
                else
                {
                    AddIssue(CheckIssueSeverity.Warning, "axis_not_centering",
                        $"{StickAxes.ShortName(prompt.Axis)} doesn't return to center when released. Check the stick spring.");
                }
            }

            promptIndex++;
            if (promptIndex >= prompts.Count)
            {
                Finish();
            }
            else
            {
                Enter(ControllerCheckState.MoveStick);
            }
        }

        private void TickCountdown(float seconds, Action onElapsed)
        {
            if (seconds <= 0f)
            {
                CountdownRemaining = -1f;
                return;
            }

            CountdownRemaining = Math.Max(0f, seconds - stateTimer);
            StepProgress = Math.Min(1f, stateTimer / seconds);
            if (CountdownRemaining <= 0f)
            {
                onElapsed();
            }
        }

        private void Finish()
        {
            var profile = new ControllerCalibrationProfile
            {
                deviceName = deviceName,
                createdUtc = DateTime.UtcNow.ToString("o"),
            };

            for (int i = 0; i < AxisCount; i++)
            {
                StickAxis axis = (StickAxis)i;
                float axisCenter = center[i];
                if (notCentering[i])
                {
                    // A stick that rests anywhere has no meaningful rest point; use mid-travel.
                    axisCenter = (rawAtPositive[i] + rawAtNegative[i]) * 0.5f;
                }

                var calibration = new AxisCalibration
                {
                    center = axisCenter,
                    rawAtPositive = rawAtPositive[i],
                    rawAtNegative = rawAtNegative[i],
                };
                profile.Set(axis, calibration);

                if (!calibration.IsValid)
                {
                    AddIssue(CheckIssueSeverity.Blocking, "invalid_range",
                        $"{StickAxes.ShortName(axis)} didn't move far enough in both directions to calibrate.");
                    continue;
                }

                float travel = Math.Min(Math.Abs(rawAtPositive[i] - axisCenter), Math.Abs(rawAtNegative[i] - axisCenter));
                if (travel < settings.limitedTravelThreshold)
                {
                    AddIssue(CheckIssueSeverity.Warning, "limited_travel",
                        $"{StickAxes.ShortName(axis)} has limited travel. Check the radio's endpoints/weights are at 100%.");
                }

                if (!notCentering[i] && Math.Abs(center[i]) > settings.centerOffsetNote)
                {
                    AddIssue(CheckIssueSeverity.Info, "center_offset",
                        $"{StickAxes.ShortName(axis)} rests off center ({center[i]:+0.00;-0.00}). Calibration corrects it.");
                }

                if (!notCentering[i] && centerNoise[i] > settings.stickDeadzone * settings.jitterWarningFractionOfDeadzone)
                {
                    AddIssue(CheckIssueSeverity.Warning, "center_jitter",
                        $"{StickAxes.ShortName(axis)} jitters at rest. If the drone creeps, raise the stick deadzone.");
                }
            }

            profile.throttleSelfCentering = !notCentering[(int)StickAxis.Throttle];
            foreach (CheckIssue issue in issues)
            {
                profile.notes.Add(issue.ToString());
            }

            result = profile;
            if (HasBlockingIssue)
            {
                Fail();
            }
            else
            {
                Enter(ControllerCheckState.Review);
            }
        }

        private void Fail()
        {
            Enter(ControllerCheckState.Failed);
        }

        private void Enter(ControllerCheckState state)
        {
            State = state;
            stateTimer = 0f;
            stillTimer = 0f;
            StepProgress = 0f;
            CountdownRemaining = -1f;

            if (state == ControllerCheckState.CenterHold)
            {
                ResetCenterWindow();
            }
            else if (state == ControllerCheckState.MoveStick)
            {
                promptBestDelta = 0f;
                promptBestRaw = center[(int)prompts[promptIndex].Axis];
                promptPreviousRaw = float.NaN;
            }
        }

        private void ResetCenterWindow()
        {
            stillTimer = 0f;
            StepProgress = 0f;
            windowSamples = 0;
            for (int i = 0; i < AxisCount; i++)
            {
                windowMin[i] = lastRaw[i];
                windowMax[i] = lastRaw[i];
                windowSum[i] = 0d;
            }
        }

        private void AddIssue(CheckIssueSeverity severity, string code, string message)
        {
            issues.Add(new CheckIssue(severity, code, message));
        }

        private static List<StickPrompt> BuildMode2Prompts()
        {
            return new List<StickPrompt>
            {
                new StickPrompt(StickAxis.Roll, 1, "Push the RIGHT stick all the way RIGHT and hold it."),
                new StickPrompt(StickAxis.Roll, -1, "Push the RIGHT stick all the way LEFT and hold it."),
                new StickPrompt(StickAxis.Pitch, 1, "Push the RIGHT stick all the way UP (forward) and hold it."),
                new StickPrompt(StickAxis.Pitch, -1, "Pull the RIGHT stick all the way DOWN (back) and hold it."),
                new StickPrompt(StickAxis.Yaw, 1, "Push the LEFT stick all the way RIGHT and hold it."),
                new StickPrompt(StickAxis.Yaw, -1, "Push the LEFT stick all the way LEFT and hold it."),
                new StickPrompt(StickAxis.Throttle, 1, "Push the LEFT stick all the way UP and hold it."),
                new StickPrompt(StickAxis.Throttle, -1, "Pull the LEFT stick all the way DOWN and hold it."),
            };
        }
    }
}
