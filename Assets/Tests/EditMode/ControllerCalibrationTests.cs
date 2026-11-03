using System.Collections.Generic;
using DroneSim.Drone.Input.Calibration;
using NUnit.Framework;

namespace DroneSim.Tests
{
    public class AxisCalibrationTests
    {
        [Test]
        public void Identity_PassesValuesThrough()
        {
            AxisCalibration c = AxisCalibration.Identity();
            Assert.That(c.IsValid, Is.True);
            Assert.That(c.Apply(0.5f), Is.EqualTo(0.5f).Within(1e-6));
            Assert.That(c.Apply(-0.25f), Is.EqualTo(-0.25f).Within(1e-6));
        }

        [Test]
        public void CorrectsCenterOffsetAndUnevenTravel()
        {
            var c = new AxisCalibration { center = 0.1f, rawAtPositive = 0.9f, rawAtNegative = -0.7f };
            Assert.That(c.Apply(0.1f), Is.EqualTo(0f).Within(1e-6));
            Assert.That(c.Apply(0.9f), Is.EqualTo(1f).Within(1e-6));
            Assert.That(c.Apply(-0.7f), Is.EqualTo(-1f).Within(1e-6));
            Assert.That(c.Apply(0.5f), Is.EqualTo(0.5f).Within(1e-6));
            Assert.That(c.Apply(-0.3f), Is.EqualTo(-0.5f).Within(1e-6));
        }

        [Test]
        public void CorrectsReversedChannel()
        {
            var c = new AxisCalibration { center = 0f, rawAtPositive = -1f, rawAtNegative = 1f };
            Assert.That(c.IsValid, Is.True);
            Assert.That(c.IsReversed, Is.True);
            Assert.That(c.Apply(-1f), Is.EqualTo(1f).Within(1e-6));
            Assert.That(c.Apply(0.5f), Is.EqualTo(-0.5f).Within(1e-6));
        }

        [Test]
        public void ClampsBeyondCalibratedEnds()
        {
            var c = new AxisCalibration { center = 0f, rawAtPositive = 0.8f, rawAtNegative = -0.8f };
            Assert.That(c.Apply(1f), Is.EqualTo(1f));
            Assert.That(c.Apply(-1f), Is.EqualTo(-1f));
        }

        [Test]
        public void InvalidCalibration_FallsBackToClampedRaw()
        {
            var tooSmall = new AxisCalibration { center = 0f, rawAtPositive = 0.05f, rawAtNegative = -1f };
            var sameSide = new AxisCalibration { center = 0f, rawAtPositive = 1f, rawAtNegative = 0.5f };
            Assert.That(tooSmall.IsValid, Is.False);
            Assert.That(sameSide.IsValid, Is.False);
            Assert.That(tooSmall.Apply(0.4f), Is.EqualTo(0.4f).Within(1e-6));
            Assert.That(sameSide.Apply(2f), Is.EqualTo(1f));
        }

        [Test]
        public void NonFiniteInput_ReadsAsZero()
        {
            AxisCalibration c = AxisCalibration.Identity();
            Assert.That(c.Apply(float.NaN), Is.EqualTo(0f));
        }

        [Test]
        public void Profile_DefaultsAreUsableIdentity()
        {
            var profile = new ControllerCalibrationProfile();
            Assert.That(profile.IsUsable, Is.True);
            Assert.That(profile.Apply(StickAxis.Throttle, 0.3f), Is.EqualTo(0.3f).Within(1e-6));
        }

        [Test]
        public void Profile_WrongVersionIsNotUsable()
        {
            var profile = new ControllerCalibrationProfile { version = 0 };
            Assert.That(profile.IsUsable, Is.False);
        }
    }

    public class ControllerCheckFlowTests
    {
        private const float Dt = 1f / 60f;

        /// <summary>Simulated radio: logical stick positions in, raw device values out.</summary>
        private sealed class FakeRadio
        {
            public readonly float[] Stick = new float[4]; // pilot terms: +1 right/up
            public readonly float[] RawCenter = new float[4];
            public readonly float[] RawScale = { 1f, 1f, 1f, 1f };
            public readonly int[] Sign = { 1, 1, 1, 1 };
            public bool ThrottleSelfCentering = true;
            public float ThrottleRest = 0f;

            public float[] Raw()
            {
                var raw = new float[4];
                for (int i = 0; i < 4; i++)
                {
                    raw[i] = RawCenter[i] + Sign[i] * Stick[i] * RawScale[i];
                }

                return raw;
            }

            public void Release(StickAxis axis)
            {
                int i = (int)axis;
                if (axis == StickAxis.Throttle && !ThrottleSelfCentering)
                {
                    return; // stays wherever it was left
                }

                Stick[i] = 0f;
            }
        }

        private static ControllerCheckFlow NewFlow(float autoSave = 0f, float autoRetry = 0f)
        {
            var settings = new ControllerCheckSettings { autoSaveSeconds = autoSave, autoRetrySeconds = autoRetry };
            return new ControllerCheckFlow(settings);
        }

        private static void Run(ControllerCheckFlow flow, FakeRadio radio, float seconds, bool connected = true)
        {
            int steps = (int)(seconds / Dt) + 1;
            for (int s = 0; s < steps; s++)
            {
                flow.Tick(Dt, radio.Raw(), connected, "Test Radio");
            }
        }

        /// <summary>Plays a pilot who follows every prompt correctly.</summary>
        private static void FollowPrompts(ControllerCheckFlow flow, FakeRadio radio, int maxSteps = 5000)
        {
            for (int s = 0; s < maxSteps && flow.IsActive && flow.State != ControllerCheckState.Review && flow.State != ControllerCheckState.Failed; s++)
            {
                if (flow.State == ControllerCheckState.MoveStick && flow.CurrentPrompt.HasValue)
                {
                    StickPrompt p = flow.CurrentPrompt.Value;
                    radio.Stick[(int)p.Axis] = p.Direction;
                }
                else if (flow.State == ControllerCheckState.ReturnToCenter && flow.CurrentPrompt.HasValue)
                {
                    radio.Release(flow.CurrentPrompt.Value.Axis);
                }

                flow.Tick(Dt, radio.Raw(), true, "Test Radio");
            }
        }

        [Test]
        public void HappyPath_ProducesUsableProfileAndSaves()
        {
            var radio = new FakeRadio();
            radio.RawCenter[0] = 0.04f;
            ControllerCheckFlow flow = NewFlow();
            flow.Begin("Test Radio", true);
            Assert.That(flow.State, Is.EqualTo(ControllerCheckState.CenterHold));

            FollowPrompts(flow, radio);

            Assert.That(flow.State, Is.EqualTo(ControllerCheckState.Review), string.Join("\n", flow.Issues));
            Assert.That(flow.HasBlockingIssue, Is.False);
            ControllerCalibrationProfile profile = flow.Result;
            Assert.That(profile.IsUsable, Is.True);
            Assert.That(profile.deviceName, Is.EqualTo("Test Radio"));
            Assert.That(profile.throttleSelfCentering, Is.True);
            Assert.That(profile.roll.center, Is.EqualTo(0.04f).Within(1e-4));
            Assert.That(profile.Apply(StickAxis.Roll, 0.04f), Is.EqualTo(0f).Within(1e-4));
            Assert.That(profile.Apply(StickAxis.Roll, 1.04f), Is.EqualTo(1f).Within(1e-4));

            Assert.That(flow.Accept(), Is.True);
            Assert.That(flow.State, Is.EqualTo(ControllerCheckState.Saved));
        }

        [Test]
        public void Review_AutoSavesAfterCountdown()
        {
            var radio = new FakeRadio();
            ControllerCheckFlow flow = NewFlow(autoSave: 1f);
            flow.Begin("Test Radio", true);
            FollowPrompts(flow, radio);
            Assert.That(flow.State, Is.EqualTo(ControllerCheckState.Review));

            Run(flow, radio, 1.2f);
            Assert.That(flow.State, Is.EqualTo(ControllerCheckState.Saved));
        }

        [Test]
        public void ReversedChannel_IsCorrectedWithWarning()
        {
            var radio = new FakeRadio();
            radio.Sign[(int)StickAxis.Pitch] = -1;
            ControllerCheckFlow flow = NewFlow();
            flow.Begin("Test Radio", true);
            FollowPrompts(flow, radio);

            Assert.That(flow.State, Is.EqualTo(ControllerCheckState.Review), string.Join("\n", flow.Issues));
            Assert.That(HasIssue(flow, "axis_reversed"), Is.True);
            float rawForward = -1f; // reversed radio reports forward as -1
            Assert.That(flow.Result.Apply(StickAxis.Pitch, rawForward), Is.EqualTo(1f).Within(1e-4));
        }

        [Test]
        public void ExpectedRawSign_MatchesInvertedPitchConfigWithoutWarning()
        {
            var radio = new FakeRadio();
            radio.Sign[(int)StickAxis.Pitch] = -1;
            var settings = new ControllerCheckSettings { autoSaveSeconds = 0f, autoRetrySeconds = 0f };
            settings.expectedRawSign[(int)StickAxis.Pitch] = -1; // e.g. DroneInputConfig.invertPitch
            var flow = new ControllerCheckFlow(settings);
            flow.Begin("Test Radio", true);
            FollowPrompts(flow, radio);

            Assert.That(flow.State, Is.EqualTo(ControllerCheckState.Review));
            Assert.That(HasIssue(flow, "axis_reversed"), Is.False);

            // Output stays in the reader's raw convention: forward reads -1 here and the reader's
            // invertPitch turns it into +1 (forward). Calibration must not flip it a second time.
            Assert.That(flow.Result.Apply(StickAxis.Pitch, -1f), Is.EqualTo(-1f).Within(1e-4));
            Assert.That(flow.Result.Apply(StickAxis.Pitch, 1f), Is.EqualTo(1f).Within(1e-4));
        }

        [Test]
        public void InvertedPitchConfig_WithReversedRadio_StillReadsForwardAsForward()
        {
            // Config expects forward = -1 raw, but this radio reports forward as +1.
            var radio = new FakeRadio();
            var settings = new ControllerCheckSettings { autoSaveSeconds = 0f, autoRetrySeconds = 0f };
            settings.expectedRawSign[(int)StickAxis.Pitch] = -1;
            var flow = new ControllerCheckFlow(settings);
            flow.Begin("Test Radio", true);
            FollowPrompts(flow, radio);

            Assert.That(flow.State, Is.EqualTo(ControllerCheckState.Review));
            Assert.That(HasIssue(flow, "axis_reversed"), Is.True);
            float readerInput = flow.Result.Apply(StickAxis.Pitch, 1f); // pilot pushes forward
            float afterInvertPitch = -readerInput;
            Assert.That(afterInvertPitch, Is.EqualTo(1f).Within(1e-4));
        }

        [Test]
        public void SwappedChannels_FailWithMappingMismatch()
        {
            ControllerCheckFlow flow = NewFlow();
            flow.Begin("Test Radio", true);
            var radio = new FakeRadio();
            Run(flow, radio, 2f);
            Assert.That(flow.State, Is.EqualTo(ControllerCheckState.MoveStick));

            // Prompt asks for roll; the radio sends it on the yaw channel.
            radio.Stick[(int)StickAxis.Yaw] = 1f;
            Run(flow, radio, 0.2f);

            Assert.That(flow.State, Is.EqualTo(ControllerCheckState.Failed));
            Assert.That(HasIssue(flow, "mapping_mismatch"), Is.True);
            Assert.That(flow.Result, Is.Null);
        }

        [Test]
        public void Failed_AutoRetriesWhenConfigured()
        {
            ControllerCheckFlow flow = NewFlow(autoRetry: 1f);
            flow.Begin("Test Radio", true);
            var radio = new FakeRadio();
            Run(flow, radio, 2f);
            radio.Stick[(int)StickAxis.Yaw] = 1f;
            Run(flow, radio, 0.2f);
            Assert.That(flow.State, Is.EqualTo(ControllerCheckState.Failed));

            radio.Stick[(int)StickAxis.Yaw] = 0f;
            Run(flow, radio, 1.2f);
            Assert.That(flow.State, Is.EqualTo(ControllerCheckState.CenterHold).Or.EqualTo(ControllerCheckState.MoveStick));
            Assert.That(flow.Issues, Is.Empty);
        }

        [Test]
        public void NonCenteringThrottleRestingLow_IsDetectedAndStillCalibrates()
        {
            var radio = new FakeRadio { ThrottleSelfCentering = false };
            radio.Stick[(int)StickAxis.Throttle] = -1f; // hobby radio throttle parked at the bottom
            ControllerCheckFlow flow = NewFlow();
            flow.Begin("Test Radio", true);
            FollowPrompts(flow, radio);

            Assert.That(flow.State, Is.EqualTo(ControllerCheckState.Review), string.Join("\n", flow.Issues));
            Assert.That(HasIssue(flow, "throttle_not_centering"), Is.True);
            Assert.That(HasIssue(flow, "axis_reversed"), Is.False);
            ControllerCalibrationProfile profile = flow.Result;
            Assert.That(profile.throttleSelfCentering, Is.False);
            Assert.That(profile.throttle.center, Is.EqualTo(0f).Within(1e-4));
            Assert.That(profile.Apply(StickAxis.Throttle, 1f), Is.EqualTo(1f).Within(1e-4));
            Assert.That(profile.Apply(StickAxis.Throttle, -1f), Is.EqualTo(-1f).Within(1e-4));
        }

        [Test]
        public void MovingDuringCenterHold_RestartsTheHold()
        {
            var radio = new FakeRadio();
            ControllerCheckFlow flow = NewFlow();
            flow.Begin("Test Radio", true);
            Run(flow, radio, 1f);
            radio.Stick[0] = 0.3f;
            Run(flow, radio, 0.1f);
            radio.Stick[0] = 0f;
            Run(flow, radio, 1f);
            Assert.That(flow.State, Is.EqualTo(ControllerCheckState.CenterHold), "1s of stillness after a bump should not be enough");
            Run(flow, radio, 0.7f);
            Assert.That(flow.State, Is.EqualTo(ControllerCheckState.MoveStick));
        }

        [Test]
        public void LimitedTravel_Warns()
        {
            var radio = new FakeRadio();
            radio.RawScale[(int)StickAxis.Yaw] = 0.6f;
            ControllerCheckFlow flow = NewFlow();
            flow.Begin("Test Radio", true);
            FollowPrompts(flow, radio);

            Assert.That(flow.State, Is.EqualTo(ControllerCheckState.Review));
            Assert.That(HasIssue(flow, "limited_travel"), Is.True);
            Assert.That(flow.Result.Apply(StickAxis.Yaw, 0.6f), Is.EqualTo(1f).Within(1e-4));
        }

        [Test]
        public void Disconnect_ReturnsToWaitingAndRestartsOnReconnect()
        {
            var radio = new FakeRadio();
            ControllerCheckFlow flow = NewFlow();
            flow.Begin("Test Radio", true);
            Run(flow, radio, 2f);
            Assert.That(flow.State, Is.EqualTo(ControllerCheckState.MoveStick));

            Run(flow, radio, 0.1f, connected: false);
            Assert.That(flow.State, Is.EqualTo(ControllerCheckState.WaitingForController));

            Run(flow, radio, 0.05f, connected: true);
            Assert.That(flow.State, Is.EqualTo(ControllerCheckState.CenterHold));
        }

        [Test]
        public void NotConnectedAtStart_WaitsForController()
        {
            ControllerCheckFlow flow = NewFlow();
            flow.Begin(null, false);
            Assert.That(flow.State, Is.EqualTo(ControllerCheckState.WaitingForController));
            Assert.That(flow.IsActive, Is.True);
        }

        [Test]
        public void Cancel_StopsTheFlow()
        {
            ControllerCheckFlow flow = NewFlow();
            flow.Begin("Test Radio", true);
            flow.Cancel();
            Assert.That(flow.State, Is.EqualTo(ControllerCheckState.Cancelled));
            Assert.That(flow.IsActive, Is.False);
            Assert.That(flow.Accept(), Is.False);
        }

        [Test]
        public void PromptsCoverEveryAxisInBothDirections()
        {
            var seen = new HashSet<string>();
            foreach (StickPrompt p in NewFlow().Prompts)
            {
                seen.Add($"{p.Axis}:{p.Direction}");
            }

            Assert.That(seen.Count, Is.EqualTo(8));
        }

        private static bool HasIssue(ControllerCheckFlow flow, string code)
        {
            foreach (CheckIssue issue in flow.Issues)
            {
                if (issue.Code == code)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
