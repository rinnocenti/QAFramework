using System;
using System.Collections;
using System.Collections.Generic;
using Immersive.Foundation.Events;
using Immersive.Framework.ActivityRestart;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.Identity;
using Immersive.Framework.Reset;
using Immersive.Framework.Reset.Unity;
using Immersive.Framework.RuntimeContent;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.QaFramework.Reset035E
{
    public enum QaReset035ECase
    {
        ActivityRestartSuccess = 0,
        ResetFailureBlocksRestart = 1
    }

    [DisallowMultipleComponent]
    public sealed class QaReset035EActivityRestartScenario : MonoBehaviour
    {
        [SerializeField] private QaReset035ECase scenarioCase;
        [SerializeField] private ActivityAsset activityA;
        [SerializeField] private RouteAsset route;
        [SerializeField] private ActivityRestartTrigger restartTrigger;
        [SerializeField] private QaReset035EActivityLifecycleProbe lifecycleProbeA;
        [SerializeField] private Resettable survivingResettable;
        [SerializeField] private QaReset035ERestartStateParticipant survivingState;
        [SerializeField] private float baselineTimeoutSeconds = 30f;
        [SerializeField] private float terminalTimeoutSeconds = 15f;

        private IEventBinding _requestBinding;
        private bool _running;
        private bool _verdictEmitted;
        private ActivityRestartTriggerEvent _requestTerminal;
        private bool _requestSubmitted;

        private void Start()
        {
            if (Application.isPlaying && !_running)
                StartCoroutine(Run());
        }

        private void OnDisable()
        {
            _requestBinding?.Dispose();
            _requestBinding = null;
            if (_running && !_verdictEmitted)
                Emit(false, "Interrupted", "The independent restart scenario was disabled before terminal evidence.", "FreshBootRequired");
            _running = false;
        }

        private IEnumerator Run()
        {
            _running = true;
            double baselineDeadline = Time.realtimeSinceStartupAsDouble + baselineTimeoutSeconds;
            while (!TryValidateBaseline(out _, out _) && Time.realtimeSinceStartupAsDouble < baselineDeadline)
                yield return null;

            if (!TryValidateBaseline(out QaReset035EActivityLifecycleProbe probe, out string baselineIssue))
            {
                Emit(false, "Baseline", baselineIssue, "BaselineUnchanged");
                yield break;
            }

            Resettable activityResettableBefore = ResolveActivityResettable(out string activityIssue);
            if (activityResettableBefore == null || !activityResettableBefore.IsRegistered)
            {
                Emit(false, "Baseline", "Activity A Resettable registration is unavailable. " + activityIssue, "BaselineUnchanged");
                yield break;
            }

            RuntimeContentOwner routeOwnerBefore = survivingResettable.Owner;
            RuntimeContentOwner activityOwnerBefore = activityResettableBefore.Owner;
            ResetSubjectId activitySubjectBefore = activityResettableBefore.RuntimeSubjectId;
            int routeValueBefore = survivingState.CurrentValue;
            int enterBefore = probe.EnterCount;
            int exitBefore = probe.ExitCount;

            if (scenarioCase == QaReset035ECase.ActivityRestartSuccess)
                yield return RunSuccessCase(probe, activityResettableBefore, routeOwnerBefore,
                    activityOwnerBefore, activitySubjectBefore, routeValueBefore, enterBefore, exitBefore);
            else
                yield return RunFailureCase(probe, activityResettableBefore, routeOwnerBefore,
                    activityOwnerBefore, activitySubjectBefore, routeValueBefore, enterBefore, exitBefore);

            _running = false;
        }

        private IEnumerator RunSuccessCase(
            QaReset035EActivityLifecycleProbe probe,
            Resettable activityResettableBefore,
            RuntimeContentOwner routeOwnerBefore,
            RuntimeContentOwner activityOwnerBefore,
            ResetSubjectId activitySubjectBefore,
            int routeValueBefore,
            int enterBefore,
            int exitBefore)
        {
            int mutatedValue = routeValueBefore + 17;
            survivingState.CurrentValue = mutatedValue;
            var sequence = new List<string>(3);
            int resetCountBefore = survivingState.ResetCount;
            int exitCountBefore = probe.ExitCount;
            int enterCountBefore = probe.EnterCount;
            Action<int, int, bool> resetObserver = (_, __, ___) => sequence.Add("Reset");
            Action<QaReset035EActivityLifecycleProbe, ActivityContentLifecycleContext> exitObserver = (_, context) =>
            {
                if (ReferenceEquals(context.Activity, activityA)) sequence.Add("Clear");
            };
            Action<QaReset035EActivityLifecycleProbe, ActivityContentLifecycleContext> enterObserver = (_, context) =>
            {
                if (ReferenceEquals(context.Activity, activityA)) sequence.Add("Reenter");
            };
            survivingState.ResetExecuted += resetObserver;
            probe.Exited += exitObserver;
            probe.Entered += enterObserver;
            using (Subscribe())
            {
                restartTrigger.RequestActivityRestart();
                yield return AwaitTerminal(() => _requestTerminal != null);
                }
            yield return AwaitRequestSettlement();
            survivingState.ResetExecuted -= resetObserver;
            probe.Exited -= exitObserver;
            probe.Entered -= enterObserver;

            Resettable activityResettableAfter = ResolveActivityResettable(out string activityIssue);
            ActivityRestartResult result = _requestTerminal != null ? _requestTerminal.Result : null;
            bool passed = _requestSubmitted && result != null && result.Succeeded
                && result.ResetStatus == ResetExecutionStatus.Succeeded.ToString()
                && result.ClearStatus == "Succeeded" && result.ReenterStatus == "Succeeded"
                && _requestTerminal.Outcome == FlowRequestOutcome.Succeeded
                && ReferenceEquals(_requestTerminal.Trigger, restartTrigger)
                && result.ResetSubjectCount == 1 && result.ResetParticipantCount == 1
                && survivingState.ResetCount == resetCountBefore + 1
                && survivingState.LastValueBefore == mutatedValue
                && survivingState.LastValueAfter == routeValueBefore
                && !survivingState.LastResetFailed
                && sequence.Count == 3 && sequence[0] == "Reset" && sequence[1] == "Clear" && sequence[2] == "Reenter"
                && probe.ExitCount == exitCountBefore + 1 && probe.EnterCount == enterCountBefore + 1
                && probe.IsActivityContentActive && ReferenceEquals(probe.ActiveActivity, activityA)
                && survivingResettable != null && survivingResettable.IsRegistered
                && survivingResettable.Owner.Equals(routeOwnerBefore)
                && routeOwnerBefore.Scope == RuntimeContentScope.Route
                && routeOwnerBefore.OwnerIdentity == FrameworkIdentityKey.From(route.RouteId)
                && survivingResettable.Membership == ResetMembership.Activity
                && survivingState.CurrentValue == routeValueBefore
                && activityResettableAfter != null && activityResettableAfter.IsRegistered
                && !ReferenceEquals(activityResettableAfter, activityResettableBefore)
                && activityResettableAfter.Owner.Equals(activityOwnerBefore)
                && activityResettableAfter.RuntimeSubjectId != activitySubjectBefore;

            string cleanup = RestoreBaselineAndValidate(probe) ? "BaselineRestored" : "FreshBootRequired";
            Emit(passed && cleanup == "BaselineRestored", "ActivityRestartSuccess",
                passed ? result.ToDiagnosticString() :
                    $"E1 contract diverged. submitted='{_requestSubmitted}' sequence='{string.Join("<", sequence)}' terminal='{(result != null ? result.ToDiagnosticString() : "<missing>")}' state='{survivingState.CurrentValue}' routeOwner='{survivingResettable?.Owner.StableText}' activityIssue='{activityIssue}'.",
                cleanup);
        }

        private IEnumerator RunFailureCase(
            QaReset035EActivityLifecycleProbe probe,
            Resettable activityResettableBefore,
            RuntimeContentOwner routeOwnerBefore,
            RuntimeContentOwner activityOwnerBefore,
            ResetSubjectId activitySubjectBefore,
            int routeValueBefore,
            int enterBefore,
            int exitBefore)
        {
            int resetCountBefore = survivingState.ResetCount;
            int exitCountBefore = probe.ExitCount;
            int enterCountBefore = probe.EnterCount;
            survivingState.ArmFailure();
            using (Subscribe())
            {
                restartTrigger.RequestActivityRestart();
                yield return AwaitTerminal(() => _requestTerminal != null);
                }
            yield return AwaitRequestSettlement();

            Resettable activityResettableAfter = ResolveActivityResettable(out string activityIssue);
            ActivityRestartResult result = _requestTerminal != null ? _requestTerminal.Result : null;
            bool passed = _requestSubmitted && result != null
                && result.Status == ActivityRestartResultStatus.ResetExecutionFailed
                && result.Failed && result.ResetParticipantFailedCount == 1
                && _requestTerminal.Outcome == FlowRequestOutcome.Failed
                && ReferenceEquals(_requestTerminal.Trigger, restartTrigger)
                && string.IsNullOrEmpty(result.ClearStatus) && string.IsNullOrEmpty(result.ReenterStatus)
                && survivingState.ResetCount == resetCountBefore + 1 && survivingState.LastResetFailed
                && probe.ExitCount == exitCountBefore && probe.EnterCount == enterCountBefore
                && probe.IsActivityContentActive && ReferenceEquals(probe.ActiveActivity, activityA)
                && ReferenceEquals(activityResettableAfter, activityResettableBefore)
                && activityResettableAfter.IsRegistered
                && activityResettableAfter.Owner.Equals(activityOwnerBefore)
                && activityResettableAfter.RuntimeSubjectId == activitySubjectBefore
                && survivingResettable.IsRegistered && survivingResettable.Owner.Equals(routeOwnerBefore)
                && routeOwnerBefore.Scope == RuntimeContentScope.Route
                && routeOwnerBefore.OwnerIdentity == FrameworkIdentityKey.From(route.RouteId)
                && survivingState.CurrentValue == routeValueBefore;

            string cleanup = RestoreBaselineAndValidate(probe) ? "BaselineRestored" : "FreshBootRequired";
            Emit(passed && cleanup == "BaselineRestored", "ResetFailureBlocksRestart",
                passed ? result.ToDiagnosticString() :
                    $"E2 contract diverged. submitted='{_requestSubmitted}' terminal='{(result != null ? result.ToDiagnosticString() : "<missing>")}' activitySameInstance='{ReferenceEquals(activityResettableAfter, activityResettableBefore)}' routeOwner='{survivingResettable?.Owner.StableText}' state='{survivingState.CurrentValue}' activityIssue='{activityIssue}'.",
                cleanup);
        }

        private IDisposable Subscribe()
        {
            _requestTerminal = null;
            _requestSubmitted = false;
            _requestBinding = restartTrigger.SubscribeRequestEvents(e =>
            {
                if (e == null) return;
                if (e.Phase == FlowRequestEventPhase.Submitted) _requestSubmitted = true;
                if (e.Phase == FlowRequestEventPhase.Completed) _requestTerminal = e;
            });
            return new BindingLease(this);
        }

        private sealed class BindingLease : IDisposable
        {
            private QaReset035EActivityRestartScenario _owner;
            public BindingLease(QaReset035EActivityRestartScenario owner) => _owner = owner;
            public void Dispose()
            {
                if (_owner == null) return;
                _owner._requestBinding?.Dispose();
                _owner._requestBinding = null;
                _owner = null;
            }
        }

        private IEnumerator AwaitTerminal(Func<bool> completed)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + terminalTimeoutSeconds;
            while (!completed() && Time.realtimeSinceStartupAsDouble < deadline)
                yield return null;
        }

        private IEnumerator AwaitRequestSettlement()
        {
            double deadline = Time.realtimeSinceStartupAsDouble + terminalTimeoutSeconds;
            while (restartTrigger != null && restartTrigger.IsRequestInFlight
                   && Time.realtimeSinceStartupAsDouble < deadline)
                yield return null;
        }

        private bool TryValidateBaseline(out QaReset035EActivityLifecycleProbe probe, out string issue)
        {
            probe = lifecycleProbeA;
            if (activityA == null || route == null || restartTrigger == null || probe == null
                || survivingResettable == null || survivingState == null)
            {
                issue = "Authored fixture references are incomplete.";
                return false;
            }
            if (!restartTrigger.HasActivityRestartRuntimeBinding || restartTrigger.IsRequestInFlight
                || restartTrigger.ResetTarget.Kind != ResetTargetKind.CurrentActivity)
            {
                issue = "Activity Restart trigger must be bound, idle, and target CurrentActivity.";
                return false;
            }
            if (!probe.IsActivityContentActive || !ReferenceEquals(probe.ActiveActivity, activityA))
            {
                issue = "Dedicated boot has not reached active Activity A.";
                return false;
            }
            RuntimeContentOwner owner = survivingResettable.Owner;
            if (!survivingResettable.IsRegistered || owner.Scope != RuntimeContentScope.Route
                || owner.OwnerIdentity != FrameworkIdentityKey.From(route.RouteId)
                || survivingResettable.Membership != ResetMembership.Activity
                || survivingState.CurrentValue != survivingState.BaselineValue)
            {
                issue = $"Route survivor baseline is invalid. owner='{owner.StableText}' membership='{survivingResettable.Membership}' state='{survivingState.CurrentValue}' baseline='{survivingState.BaselineValue}'.";
                return false;
            }
            Resettable activityResettable = ResolveActivityResettable(out string activityIssue);
            if (activityResettable == null || !activityResettable.IsRegistered
                || activityResettable.Owner.Scope != RuntimeContentScope.Activity
                || activityResettable.Owner.OwnerIdentity != FrameworkIdentityKey.From(activityA.ActivityId))
            {
                issue = "Activity A Resettable baseline is unavailable. " + activityIssue;
                return false;
            }
            issue = string.Empty;
            return true;
        }

        private Resettable ResolveActivityResettable(out string issue)
        {
            if (activityA?.ActivityContentProfile == null || activityA.ActivityContentProfile.SceneCount != 1)
            {
                issue = "Activity A must have exactly one Activity Content scene.";
                return null;
            }
            string path = activityA.ActivityContentProfile.Scenes[0].ScenePath;
            Scene scene = SceneManager.GetSceneByPath(path);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                issue = $"Activity A content scene '{path}' is not loaded.";
                return null;
            }
            var found = new List<Resettable>();
            foreach (GameObject root in scene.GetRootGameObjects())
                found.AddRange(root.GetComponentsInChildren<Resettable>(true));
            issue = found.Count == 1 ? string.Empty : $"Expected one Activity A Resettable, found '{found.Count}'.";
            return found.Count == 1 ? found[0] : null;
        }

        private bool RestoreBaselineAndValidate(QaReset035EActivityLifecycleProbe probe)
        {
            if (restartTrigger != null && restartTrigger.IsRequestInFlight) return false;
            if (survivingState != null) survivingState.CurrentValue = survivingState.BaselineValue;
            return survivingState != null && survivingState.CurrentValue == survivingState.BaselineValue
                && TryValidateBaseline(out QaReset035EActivityLifecycleProbe currentProbe, out _)
                && ReferenceEquals(currentProbe, probe);
        }

        private void Emit(bool passed, string phase, string evidence, string cleanup)
        {
            if (_verdictEmitted) return;
            _verdictEmitted = true;
            _running = false;
            string status = passed ? "PASS" : "FAIL";
            string line = $"[QA-RESET-035-E] case='{scenarioCase}' status='{status}' phase='{phase}' evidence='{Sanitize(evidence)}' cleanup='{cleanup}'.";
            if (passed) Debug.Log(line, this); else Debug.LogError(line, this);
        }

        private static string Sanitize(string value) => (value ?? string.Empty)
            .Replace("'", "\\'").Replace("\r", "\\r").Replace("\n", "\\n");
    }
}


