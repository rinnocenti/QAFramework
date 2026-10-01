using System;
using Immersive.Framework.Reset;
using Immersive.Framework.Reset.Unity;
using UnityEngine;

namespace Immersive.QaFramework.Reset035E
{
    public sealed class QaReset035ERestartStateParticipant : MonoBehaviour, IUnityResettable
    {
        [SerializeField] private int baselineValue = 3;
        [SerializeField] private int currentValue = 3;
        private bool _failNextReset;

        public event Action<int, int, bool> ResetExecuted;
        public string ResetParticipantId => "qa-reset-035-e.route-survivor";
        public int BaselineValue => baselineValue;
        public int CurrentValue { get => currentValue; set => currentValue = value; }
        public int ResetCount { get; private set; }
        public int LastValueBefore { get; private set; }
        public int LastValueAfter { get; private set; }
        public bool LastResetFailed { get; private set; }

        public void ArmFailure() => _failNextReset = true;

        public ResetParticipantResult Reset(ResetContext context)
        {
            bool failed = _failNextReset;
            _failNextReset = false;
            LastValueBefore = currentValue;
            if (!failed) currentValue = baselineValue;
            LastValueAfter = currentValue;
            LastResetFailed = failed;
            ResetCount++;
            ResetExecuted?.Invoke(LastValueBefore, LastValueAfter, failed);
            return failed
                ? ResetParticipantResult.CreateFailed(context.Participant, 1, context.Source, context.Reason,
                    "QA-RESET-035-E controlled Activity Restart Reset failure.")
                : ResetParticipantResult.CreateSucceeded(context.Participant, context.Source, context.Reason,
                    "QA-RESET-035-E surviving Route-owned state restored.");
        }
    }
}
