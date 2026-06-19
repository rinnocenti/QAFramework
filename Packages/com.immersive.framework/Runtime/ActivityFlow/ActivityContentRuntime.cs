using System.Collections.Generic;
using Immersive.Framework.Authoring;
using UnityEngine;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Minimal owner for applying Activity content visibility in loaded scenes.
    /// It does not load scenes, spawn actors, or own Activity identity.
    /// </summary>
    internal sealed class ActivityContentRuntime
    {
        private const int MaxObservedBindingsInMessage = 8;

        private ActivityContentApplyResult _lastApplyResult;
        private bool _hasLastApplyResult;

        internal bool HasLastApplyResult => _hasLastApplyResult;

        internal ActivityContentApplyResult LastApplyResult => _lastApplyResult;

        internal void ClearLastApplyResult()
        {
            _lastApplyResult = default;
            _hasLastApplyResult = false;
        }

        internal void HandleActivityEntered(ActivityEnteredEvent activityEnteredEvent)
        {
            if (activityEnteredEvent == null)
            {
                return;
            }

            StoreLastApplyResult(ApplyActiveActivity(activityEnteredEvent.Activity));
        }

        internal void HandleActivityExited(ActivityExitedEvent activityExitedEvent)
        {
            if (activityExitedEvent == null || activityExitedEvent.NextActivity != null)
            {
                return;
            }

            StoreLastApplyResult(ApplyActiveActivity(null));
        }

        internal ActivityContentApplyResult ApplyActiveActivity(ActivityAsset activeActivity)
        {
            var bindings = Object.FindObjectsByType<ActivityContentBinding>(FindObjectsInactive.Include);
            if (bindings == null || bindings.Length == 0)
            {
                return ActivityContentApplyResult.Empty(activeActivity);
            }

            var bindingCount = 0;
            var activatedCount = 0;
            var deactivatedCount = 0;
            var unchangedCount = 0;
            var missingActivityCount = 0;
            var observedBindings = new List<string>(MaxObservedBindingsInMessage);
            var warningBindings = new List<string>();
            var omittedObservationCount = 0;

            for (var i = 0; i < bindings.Length; i++)
            {
                var binding = bindings[i];
                if (binding == null || !binding.IsSceneBinding)
                {
                    continue;
                }

                bindingCount++;

                if (binding.Activity == null)
                {
                    missingActivityCount++;
                    AddWarning(warningBindings, binding, "MissingActivityReference");
                    AddObservation(
                        observedBindings,
                        ref omittedObservationCount,
                        binding,
                        "<missing>",
                        "Ignore",
                        "MissingActivityReference");
                    continue;
                }

                var shouldBeActive = activeActivity != null && ReferenceEquals(binding.Activity, activeActivity);
                var wasActive = binding.gameObject.activeSelf;
                var changed = binding.SetContentActive(shouldBeActive);
                var action = ResolveAction(shouldBeActive, wasActive, changed);
                var reason = shouldBeActive ? "MatchedActiveActivity" : "DifferentActivity";

                if (changed)
                {
                    if (shouldBeActive)
                    {
                        activatedCount++;
                    }
                    else
                    {
                        deactivatedCount++;
                    }
                }
                else
                {
                    unchangedCount++;
                }

                AddObservation(
                    observedBindings,
                    ref omittedObservationCount,
                    binding,
                    binding.Activity.ActivityName,
                    action,
                    reason);
            }

            return ActivityContentApplyResult.Applied(
                activeActivity,
                bindingCount,
                activatedCount,
                deactivatedCount,
                unchangedCount,
                missingActivityCount,
                BuildDetailMessage(activeActivity, observedBindings, omittedObservationCount),
                BuildWarningMessage(warningBindings));
        }

        private void StoreLastApplyResult(ActivityContentApplyResult applyResult)
        {
            _lastApplyResult = applyResult;
            _hasLastApplyResult = true;
        }

        private static string ResolveAction(bool shouldBeActive, bool wasActive, bool changed)
        {
            if (changed)
            {
                return shouldBeActive ? "Activate" : "Deactivate";
            }

            return wasActive ? "KeepActive" : "KeepInactive";
        }

        private static void AddObservation(
            List<string> observedBindings,
            ref int omittedObservationCount,
            ActivityContentBinding binding,
            string assignedActivity,
            string action,
            string reason)
        {
            if (observedBindings.Count >= MaxObservedBindingsInMessage)
            {
                omittedObservationCount++;
                return;
            }

            observedBindings.Add(
                $"object='{FormatValue(binding.ObjectName)}' scene='{FormatValue(binding.SceneName)}' assignedActivity='{FormatValue(assignedActivity)}' action='{FormatValue(action)}' reason='{FormatValue(reason)}'");
        }

        private static void AddWarning(List<string> warningBindings, ActivityContentBinding binding, string reason)
        {
            warningBindings.Add(
                $"object='{FormatValue(binding.ObjectName)}' scene='{FormatValue(binding.SceneName)}' reason='{FormatValue(reason)}'");
        }

        private static string BuildDetailMessage(ActivityAsset activeActivity, IReadOnlyList<string> observedBindings, int omittedObservationCount)
        {
            if (observedBindings == null || observedBindings.Count == 0)
            {
                return string.Empty;
            }

            var activeActivityName = activeActivity != null ? activeActivity.ActivityName : "<none>";
            var details = $"Activity Content Binding diagnostics. activeActivity='{FormatValue(activeActivityName)}' observations=[{string.Join("; ", observedBindings)}]";
            if (omittedObservationCount > 0)
            {
                details += $" omitted='{omittedObservationCount}'";
            }

            return details + ".";
        }

        private static string BuildWarningMessage(IReadOnlyList<string> warningBindings)
        {
            if (warningBindings == null || warningBindings.Count == 0)
            {
                return string.Empty;
            }

            return $"Activity Content Binding warning. warnings=[{string.Join("; ", warningBindings)}].";
        }

        private static string FormatValue(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "<empty>"
                : value.Replace("'", "\\'");
        }
    }
}
