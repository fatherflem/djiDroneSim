using System;
using UnityEngine;

namespace DroneSim.Drone.Training
{
    /// <summary>
    /// Minimal common lifecycle for training drills. It owns timing and state transitions only;
    /// concrete drills retain their own evaluation, geometry, and scoring logic.
    /// </summary>
    public abstract class TrainingDrill : MonoBehaviour
    {
        [Header("Training Lifecycle")]
        [SerializeField] private TrainingDrillDefinition definition;
        [SerializeField] private bool beginOnStart = true;

        private float countdownRemaining;
        private float runElapsedSeconds;

        public event Action<DrillState, DrillState> StateChanged;
        public event Action<TrainingResult> ResultAvailable;

        public TrainingDrillDefinition Definition => definition;
        public DrillState State { get; private set; } = DrillState.NotStarted;
        public float CountdownRemaining => countdownRemaining;
        public float RunElapsedSeconds => runElapsedSeconds;
        public TrainingResult LatestResult { get; private set; }
        public bool IsTerminal => State == DrillState.Completed || State == DrillState.Failed || State == DrillState.Results;

        public string DisplayName => definition != null ? definition.displayName : GetType().Name;
        public string Instructions => definition != null ? definition.instructions : string.Empty;
        public bool CanRestart => definition == null || definition.allowRestart;

        protected virtual void Start()
        {
            if (beginOnStart)
            {
                BeginDrill();
            }
        }

        protected virtual void Update()
        {
            switch (State)
            {
                case DrillState.Countdown:
                    countdownRemaining = Mathf.Max(0f, countdownRemaining - Time.unscaledDeltaTime);
                    if (countdownRemaining <= 0f)
                    {
                        StartRun();
                    }
                    break;
                case DrillState.Running:
                    runElapsedSeconds += Time.deltaTime;
                    if (definition != null && definition.timeLimitSeconds > 0f && runElapsedSeconds >= definition.timeLimitSeconds)
                    {
                        FailDrill("Time limit reached.");
                        return;
                    }

                    UpdateRunning(Time.deltaTime);
                    break;
            }
        }

        public void Configure(TrainingDrillDefinition drillDefinition)
        {
            definition = drillDefinition;
        }

        public void BeginDrill()
        {
            if (State != DrillState.NotStarted)
            {
                return;
            }

            SetState(DrillState.Instructions);
        }

        public void ContinueFromInstructions()
        {
            if (State != DrillState.Instructions)
            {
                return;
            }

            countdownRemaining = definition != null ? definition.countdownSeconds : 0f;
            if (countdownRemaining > 0f)
            {
                SetState(DrillState.Countdown);
            }
            else
            {
                StartRun();
            }
        }

        public void RestartDrill()
        {
            if (!CanRestart)
            {
                return;
            }

            ResetCommonState();
            ResetDrill();
            SetState(DrillState.Instructions);
        }

        protected abstract void UpdateRunning(float deltaTime);

        protected virtual void ResetDrill()
        {
        }

        protected virtual TrainingResult BuildResult(bool succeeded, string summary)
        {
            return new TrainingResult
            {
                drillId = definition != null ? definition.drillId : GetType().Name,
                drillName = DisplayName,
                succeeded = succeeded,
                elapsedSeconds = runElapsedSeconds,
                score = succeeded ? 100f : 0f,
                summary = summary
            };
        }

        protected void CompleteDrill(string summary)
        {
            Finish(true, summary);
        }

        protected void FailDrill(string summary)
        {
            Finish(false, summary);
        }

        private void StartRun()
        {
            runElapsedSeconds = 0f;
            OnRunStarted();
            SetState(DrillState.Running);
        }

        protected virtual void OnRunStarted()
        {
        }

        private void Finish(bool succeeded, string summary)
        {
            if (State != DrillState.Running)
            {
                return;
            }

            LatestResult = BuildResult(succeeded, summary);
            SetState(succeeded ? DrillState.Completed : DrillState.Failed);
            ResultAvailable?.Invoke(LatestResult);
            SetState(DrillState.Results);
        }

        private void ResetCommonState()
        {
            countdownRemaining = 0f;
            runElapsedSeconds = 0f;
            LatestResult = null;
        }

        private void SetState(DrillState next)
        {
            if (State == next)
            {
                return;
            }

            DrillState previous = State;
            State = next;
            StateChanged?.Invoke(previous, next);
        }
    }
}
