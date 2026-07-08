using System;
using Immersive.Framework.Actors;
using UnityEngine;

namespace ImmersiveFrameworkQA.Actors
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Actors/F49C Actor Readiness Behaviour Fixture")]
    public sealed class QaActorReadinessBehaviourFixture : MonoBehaviour
    {
        private const string LogPrefix = "[F49C_ACTOR_READINESS_BEHAVIOUR_QA]";

        [SerializeField] private ActorReadinessBehaviour actorReadiness;
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

        [ContextMenu("Immersive Framework QA/Actors/Run F49C Actor Readiness Behaviour Smoke")]
        public void RunSmoke()
        {
            _lastSmokePassed = false;

            bool componentPassed = ValidateComponentExists();
            bool interfacePassed = componentPassed && ValidateInterfaceExposure();
            bool readyForViewPassed = componentPassed && ValidateReadyForView();
            bool readyForControlPassed = componentPassed && ValidateReadyForControl();
            bool invalidControlPassed = componentPassed && ValidateInvalidControlWithoutViewFails();
            bool failedReasonPassed = componentPassed && ValidateFailedRequiresReason();
            bool releasePassed = componentPassed && ValidateReleaseBlocksReadinessChanges();
            bool newCyclePassed = componentPassed && ValidateBeginNewCycleAfterRelease();

            bool passed = componentPassed
                && interfacePassed
                && readyForViewPassed
                && readyForControlPassed
                && invalidControlPassed
                && failedReasonPassed
                && releasePassed
                && newCyclePassed;

            _lastSmokePassed = passed;

            string message = $"{LogPrefix} status='{(passed ? "Succeeded" : "Failed")}' " +
                $"component='{componentPassed}' interface='{interfacePassed}' readyForView='{readyForViewPassed}' " +
                $"readyForControl='{readyForControlPassed}' invalidControl='{invalidControlPassed}' " +
                $"failedReason='{failedReasonPassed}' release='{releasePassed}' newCycle='{newCyclePassed}'.";

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

        private bool ValidateComponentExists()
        {
            bool passed = actorReadiness != null;
            Debug.Log($"{LogPrefix} Component exists. passed='{passed}' component='{FormatObject(actorReadiness)}'.");
            return passed;
        }

        private bool ValidateInterfaceExposure()
        {
            ResetToNotReady("qa.actor-readiness-behaviour.interface-reset");
            IActorReadiness readiness = actorReadiness;
            actorReadiness.MarkReadyForControl("qa.actor-readiness-behaviour.interface");
            ActorReadinessSnapshot snapshot = readiness.CreateSnapshot();
            bool passed = readiness.IsReadyForView
                && readiness.IsReadyForControl
                && snapshot.State == ActorReadinessState.ReadyForControl
                && string.Equals(snapshot.Reason, "qa.actor-readiness-behaviour.interface", StringComparison.Ordinal);

            Debug.Log($"{LogPrefix} IActorReadiness exposure. passed='{passed}' snapshot=\"{snapshot}\".");
            return passed;
        }

        private bool ValidateReadyForView()
        {
            ResetToNotReady("qa.actor-readiness-behaviour.ready-for-view-reset");
            actorReadiness.MarkReadyForView("qa.actor-readiness-behaviour.ready-for-view");
            ActorReadinessSnapshot snapshot = actorReadiness.CreateSnapshot();
            bool passed = actorReadiness.IsReadyForView
                && !actorReadiness.IsReadyForControl
                && snapshot.State == ActorReadinessState.ReadyForView
                && string.Equals(snapshot.Reason, "qa.actor-readiness-behaviour.ready-for-view", StringComparison.Ordinal);

            Debug.Log($"{LogPrefix} ReadyForView. passed='{passed}' snapshot=\"{snapshot}\".");
            return passed;
        }

        private bool ValidateReadyForControl()
        {
            ResetToNotReady("qa.actor-readiness-behaviour.ready-for-control-reset");
            actorReadiness.MarkReadyForControl("qa.actor-readiness-behaviour.ready-for-control");
            ActorReadinessSnapshot snapshot = actorReadiness.CreateSnapshot();
            bool passed = actorReadiness.IsReadyForView
                && actorReadiness.IsReadyForControl
                && snapshot.State == ActorReadinessState.ReadyForControl
                && snapshot.IsReadyForView
                && snapshot.IsReadyForControl
                && string.Equals(snapshot.Reason, "qa.actor-readiness-behaviour.ready-for-control", StringComparison.Ordinal);

            Debug.Log($"{LogPrefix} ReadyForControl. passed='{passed}' snapshot=\"{snapshot}\".");
            return passed;
        }

        private bool ValidateInvalidControlWithoutViewFails()
        {
            ResetToNotReady("qa.actor-readiness-behaviour.invalid-control-reset");
            bool failedExplicitly = Throws<InvalidOperationException>(() => actorReadiness.SetReadiness(false, true, "qa.actor-readiness-behaviour.invalid-control"));
            ActorReadinessSnapshot snapshot = actorReadiness.CreateSnapshot();
            bool passed = failedExplicitly
                && snapshot.State == ActorReadinessState.NotReady
                && !snapshot.IsReadyForView
                && !snapshot.IsReadyForControl;

            Debug.Log($"{LogPrefix} Invalid ReadyForControl without ReadyForView. passed='{passed}' snapshot=\"{snapshot}\".");
            return passed;
        }

        private bool ValidateFailedRequiresReason()
        {
            ResetToNotReady("qa.actor-readiness-behaviour.failed-reset");
            bool emptyReasonFailed = Throws<ArgumentException>(() => actorReadiness.MarkFailed(string.Empty));
            actorReadiness.MarkFailed("qa.actor-readiness-behaviour.failed");
            ActorReadinessSnapshot snapshot = actorReadiness.CreateSnapshot();
            bool passed = emptyReasonFailed
                && snapshot.State == ActorReadinessState.Failed
                && snapshot.IsFailed
                && string.Equals(snapshot.Reason, "qa.actor-readiness-behaviour.failed", StringComparison.Ordinal);

            Debug.Log($"{LogPrefix} Failed requires reason. passed='{passed}' snapshot=\"{snapshot}\".");
            return passed;
        }

        private bool ValidateReleaseBlocksReadinessChanges()
        {
            ResetToNotReady("qa.actor-readiness-behaviour.release-reset");
            actorReadiness.MarkReadyForControl("qa.actor-readiness-behaviour.ready-before-release");
            actorReadiness.Release("qa.actor-readiness-behaviour.release");
            bool blocked = Throws<InvalidOperationException>(() => actorReadiness.MarkReadyForView("qa.actor-readiness-behaviour.invalid-after-release"));
            ActorReadinessSnapshot snapshot = actorReadiness.CreateSnapshot();
            bool passed = blocked
                && snapshot.State == ActorReadinessState.Released
                && snapshot.IsReleased
                && string.Equals(snapshot.Reason, "qa.actor-readiness-behaviour.release", StringComparison.Ordinal);

            Debug.Log($"{LogPrefix} Release blocks readiness changes. passed='{passed}' snapshot=\"{snapshot}\".");
            return passed;
        }

        private bool ValidateBeginNewCycleAfterRelease()
        {
            actorReadiness.BeginNewCycle("qa.actor-readiness-behaviour.new-cycle");
            actorReadiness.MarkReadyForView("qa.actor-readiness-behaviour.ready-after-cycle");
            ActorReadinessSnapshot snapshot = actorReadiness.CreateSnapshot();
            bool passed = snapshot.State == ActorReadinessState.ReadyForView
                && snapshot.IsReadyForView
                && !snapshot.IsReadyForControl
                && string.Equals(snapshot.Reason, "qa.actor-readiness-behaviour.ready-after-cycle", StringComparison.Ordinal);

            Debug.Log($"{LogPrefix} BeginNewCycle after release. passed='{passed}' snapshot=\"{snapshot}\".");
            return passed;
        }

        private void ResetToNotReady(string reason)
        {
            if (actorReadiness.IsReleased)
            {
                actorReadiness.BeginNewCycle(reason + ".new-cycle");
            }

            actorReadiness.ClearReadiness(reason);
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

        private static string FormatObject(UnityEngine.Object target)
        {
            return target != null ? target.name : "<none>";
        }
    }
}
