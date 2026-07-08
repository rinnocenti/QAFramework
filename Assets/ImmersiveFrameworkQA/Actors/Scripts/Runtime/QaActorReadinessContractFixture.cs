using System;
using Immersive.Framework.Actors;
using UnityEngine;

namespace ImmersiveFrameworkQA.Actors
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Actors/F49B Actor Readiness Contract Fixture")]
    public sealed class QaActorReadinessContractFixture : MonoBehaviour
    {
        private const string LogPrefix = "[F49B_ACTOR_READINESS_QA]";

        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool throwOnFailure;

        private bool _lastSmokePassed;

        public bool LastSmokePassed => _lastSmokePassed;

        private void Start()
        {
            if (runOnStart)
            {
                RunSmoke();
            }
        }

        [ContextMenu("Immersive Framework QA/Actors/Run F49B Actor Readiness Contract Smoke")]
        public void RunSmoke()
        {
            _lastSmokePassed = false;

            bool initialPassed = ValidateInitialState();
            bool viewPassed = ValidateReadyForView();
            bool controlPassed = ValidateReadyForControl();
            bool invalidControlPassed = ValidateInvalidControlWithoutViewFails();
            bool failedReasonPassed = ValidateFailedRequiresReason();
            bool releasePassed = ValidateReleaseBlocksReadinessChanges();
            bool newCyclePassed = ValidateBeginNewCycleAfterRelease();
            bool interfacePassed = ValidateInterfaceSnapshot();

            bool passed = initialPassed
                && viewPassed
                && controlPassed
                && invalidControlPassed
                && failedReasonPassed
                && releasePassed
                && newCyclePassed
                && interfacePassed;

            _lastSmokePassed = passed;

            string message = $"{LogPrefix} status='{(passed ? "Succeeded" : "Failed")}' " +
                $"initial='{initialPassed}' readyForView='{viewPassed}' readyForControl='{controlPassed}' " +
                $"invalidControl='{invalidControlPassed}' failedReason='{failedReasonPassed}' " +
                $"release='{releasePassed}' newCycle='{newCyclePassed}' interface='{interfacePassed}'.";

            if (passed)
            {
                Debug.Log(message, this);
                return;
            }

            Debug.LogError(message, this);
            if (throwOnFailure)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static bool ValidateInitialState()
        {
            ActorReadiness readiness = new ActorReadiness();
            ActorReadinessSnapshot snapshot = readiness.CreateSnapshot();
            bool passed = snapshot.State == ActorReadinessState.NotReady
                && !snapshot.IsReadyForView
                && !snapshot.IsReadyForControl
                && !snapshot.IsFailed
                && !snapshot.IsReleased;

            Debug.Log($"{LogPrefix} Initial state. passed='{passed}' snapshot=\"{snapshot}\".");
            return passed;
        }

        private static bool ValidateReadyForView()
        {
            ActorReadiness readiness = new ActorReadiness();
            readiness.MarkReadyForView("qa.actor-readiness.ready-for-view");
            ActorReadinessSnapshot snapshot = readiness.CreateSnapshot();
            bool passed = readiness.IsReadyForView
                && !readiness.IsReadyForControl
                && snapshot.State == ActorReadinessState.ReadyForView
                && string.Equals(snapshot.Reason, "qa.actor-readiness.ready-for-view", StringComparison.Ordinal);

            Debug.Log($"{LogPrefix} ReadyForView. passed='{passed}' snapshot=\"{snapshot}\".");
            return passed;
        }

        private static bool ValidateReadyForControl()
        {
            ActorReadiness readiness = new ActorReadiness();
            readiness.MarkReadyForControl("qa.actor-readiness.ready-for-control");
            ActorReadinessSnapshot snapshot = readiness.CreateSnapshot();
            bool passed = readiness.IsReadyForView
                && readiness.IsReadyForControl
                && snapshot.State == ActorReadinessState.ReadyForControl
                && snapshot.IsReadyForView
                && snapshot.IsReadyForControl
                && string.Equals(snapshot.Reason, "qa.actor-readiness.ready-for-control", StringComparison.Ordinal);

            Debug.Log($"{LogPrefix} ReadyForControl. passed='{passed}' snapshot=\"{snapshot}\".");
            return passed;
        }

        private static bool ValidateInvalidControlWithoutViewFails()
        {
            ActorReadiness readiness = new ActorReadiness();
            bool failedExplicitly = Throws<InvalidOperationException>(() => readiness.SetReadiness(false, true, "qa.invalid-control"));
            ActorReadinessSnapshot snapshot = readiness.CreateSnapshot();
            bool passed = failedExplicitly
                && snapshot.State == ActorReadinessState.NotReady
                && !snapshot.IsReadyForView
                && !snapshot.IsReadyForControl;

            Debug.Log($"{LogPrefix} Invalid ReadyForControl without ReadyForView. passed='{passed}' snapshot=\"{snapshot}\".");
            return passed;
        }

        private static bool ValidateFailedRequiresReason()
        {
            ActorReadiness readiness = new ActorReadiness();
            bool emptyReasonFailed = Throws<ArgumentException>(() => readiness.MarkFailed(string.Empty));

            readiness.MarkFailed("qa.actor-readiness.failed");
            ActorReadinessSnapshot snapshot = readiness.CreateSnapshot();
            bool passed = emptyReasonFailed
                && snapshot.State == ActorReadinessState.Failed
                && snapshot.IsFailed
                && string.Equals(snapshot.Reason, "qa.actor-readiness.failed", StringComparison.Ordinal);

            Debug.Log($"{LogPrefix} Failed requires reason. passed='{passed}' snapshot=\"{snapshot}\".");
            return passed;
        }

        private static bool ValidateReleaseBlocksReadinessChanges()
        {
            ActorReadiness readiness = new ActorReadiness();
            readiness.MarkReadyForControl("qa.actor-readiness.ready-before-release");
            readiness.Release("qa.actor-readiness.release");

            bool blocked = Throws<InvalidOperationException>(() => readiness.MarkReadyForView("qa.actor-readiness.invalid-after-release"));
            ActorReadinessSnapshot snapshot = readiness.CreateSnapshot();
            bool passed = blocked
                && snapshot.State == ActorReadinessState.Released
                && snapshot.IsReleased
                && string.Equals(snapshot.Reason, "qa.actor-readiness.release", StringComparison.Ordinal);

            Debug.Log($"{LogPrefix} Release blocks readiness changes. passed='{passed}' snapshot=\"{snapshot}\".");
            return passed;
        }

        private static bool ValidateBeginNewCycleAfterRelease()
        {
            ActorReadiness readiness = new ActorReadiness();
            readiness.Release("qa.actor-readiness.release-before-cycle");
            readiness.BeginNewCycle("qa.actor-readiness.new-cycle");
            readiness.MarkReadyForView("qa.actor-readiness.ready-after-cycle");

            ActorReadinessSnapshot snapshot = readiness.CreateSnapshot();
            bool passed = snapshot.State == ActorReadinessState.ReadyForView
                && snapshot.IsReadyForView
                && !snapshot.IsReadyForControl
                && string.Equals(snapshot.Reason, "qa.actor-readiness.ready-after-cycle", StringComparison.Ordinal);

            Debug.Log($"{LogPrefix} BeginNewCycle after release. passed='{passed}' snapshot=\"{snapshot}\".");
            return passed;
        }

        private static bool ValidateInterfaceSnapshot()
        {
            IActorReadiness readiness = new ActorReadiness(ActorReadinessState.ReadyForControl, "qa.actor-readiness.interface");
            ActorReadinessSnapshot snapshot = readiness.CreateSnapshot();
            bool passed = readiness.IsReadyForView
                && readiness.IsReadyForControl
                && snapshot.State == ActorReadinessState.ReadyForControl
                && string.Equals(snapshot.Reason, "qa.actor-readiness.interface", StringComparison.Ordinal);

            Debug.Log($"{LogPrefix} IActorReadiness snapshot. passed='{passed}' snapshot=\"{snapshot}\".");
            return passed;
        }

        private static bool Throws<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
                return false;
            }
            catch (TException)
            {
                return true;
            }
        }
    }
}
