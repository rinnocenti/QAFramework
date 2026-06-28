using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Side-effect-free Activity operation plan introduced by F25E.
    /// This plan owns the combined decision language for Activity visual policy, scene load/release side-effects and loading requirement.
    /// It does not execute transition, loading, scene load, scene release or Activity lifecycle changes.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F25E Activity operation plan baseline; no runtime execution wiring.")]
    internal readonly struct ActivityOperationPlan
    {
        private readonly ActivityOperationPlanSceneEntry[] _scenes;
        private readonly ActivityOperationIssue[] _issues;

        public ActivityOperationPlan(
            ActivityOperationKind operationKind,
            ActivityAsset previousActivity,
            ActivityAsset targetActivity,
            ActivityVisualTransitionMode visualMode,
            IReadOnlyList<ActivityOperationPlanSceneEntry> scenes,
            IReadOnlyList<ActivityOperationIssue> issues,
            string source,
            string reason)
        {
            OperationKind = operationKind;
            PreviousActivity = previousActivity;
            TargetActivity = targetActivity;
            VisualMode = visualMode;
            Source = Normalize(source);
            Reason = Normalize(reason);

            _scenes = CopyScenes(scenes);
            _issues = BuildIssues(operationKind, targetActivity, visualMode, _scenes, issues);
            Status = DetermineStatus(operationKind, _issues);
        }

        public ActivityOperationKind OperationKind { get; }

        public ActivityAsset PreviousActivity { get; }

        public ActivityAsset TargetActivity { get; }

        public ActivityVisualTransitionMode VisualMode { get; }

        public ActivityOperationPlanStatus Status { get; }

        public IReadOnlyList<ActivityOperationPlanSceneEntry> Scenes => _scenes ?? Array.Empty<ActivityOperationPlanSceneEntry>();

        public IReadOnlyList<ActivityOperationIssue> Issues => _issues ?? Array.Empty<ActivityOperationIssue>();

        public string Source { get; }

        public string Reason { get; }

        public bool HasPreviousActivity => PreviousActivity != null;

        public bool HasTargetActivity => TargetActivity != null;

        public string PreviousActivityName => PreviousActivity != null ? PreviousActivity.ActivityName : string.Empty;

        public string TargetActivityName => TargetActivity != null ? TargetActivity.ActivityName : string.Empty;

        public int SceneCount => Scenes.Count;

        public int ScenesToLoadCount => CountScenes(ActivityOperationSceneAction.Load);

        public int ScenesToReleaseCount => CountScenes(ActivityOperationSceneAction.Release);

        public int SceneSideEffectCount => CountSceneSideEffects();

        public bool HasSceneSideEffects => SceneSideEffectCount > 0;

        public bool RequiresLoadingSurface => VisualMode == ActivityVisualTransitionMode.FadeWithLoading && HasSceneSideEffects;

        public bool RequiresVisualOcclusion => VisualMode == ActivityVisualTransitionMode.Fade
            || VisualMode == ActivityVisualTransitionMode.FadeWithLoading;

        public int BlockingIssueCount => CountIssues(ActivityOperationIssueSeverity.Blocking);

        public int WarningIssueCount => CountIssues(ActivityOperationIssueSeverity.Warning);

        public bool HasBlockingIssues => BlockingIssueCount > 0;

        public bool IsValid => !HasBlockingIssues
            && (Status == ActivityOperationPlanStatus.Planned
                || Status == ActivityOperationPlanStatus.PlannedWithIssues);

        public static ActivityOperationPlan NotRequested(string source, string reason)
        {
            return new ActivityOperationPlan(
                ActivityOperationKind.Unknown,
                null,
                null,
                ActivityVisualTransitionMode.Seamless,
                Array.Empty<ActivityOperationPlanSceneEntry>(),
                Array.Empty<ActivityOperationIssue>(),
                source,
                reason);
        }

        public string ToDiagnosticString()
        {
            var previous = !string.IsNullOrWhiteSpace(PreviousActivityName) ? PreviousActivityName : "<none>";
            var target = !string.IsNullOrWhiteSpace(TargetActivityName) ? TargetActivityName : "<none>";
            var builder = new StringBuilder();
            builder.Append($"Activity Operation Plan kind='{OperationKind}' status='{Status}' previous='{previous}' target='{target}' visualMode='{VisualMode}' scenes='{SceneCount}' load='{ScenesToLoadCount}' release='{ScenesToReleaseCount}' sceneSideEffects='{SceneSideEffectCount}' requiresVisualOcclusion='{RequiresVisualOcclusion}' requiresLoadingSurface='{RequiresLoadingSurface}' valid='{IsValid}' blockingIssues='{BlockingIssueCount}' warnings='{WarningIssueCount}' source='{Source}' reason='{Reason}' scenes=[");

            for (var i = 0; i < Scenes.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(" | ");
                }

                builder.Append(Scenes[i].ToDiagnosticString());
            }

            builder.Append("] issues=[");
            for (var i = 0; i < Issues.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(" | ");
                }

                builder.Append(Issues[i].ToDiagnosticString());
            }

            builder.Append("]");
            return builder.ToString();
        }

        private int CountScenes(ActivityOperationSceneAction action)
        {
            var count = 0;
            for (var i = 0; i < Scenes.Count; i++)
            {
                if (Scenes[i].Action == action)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountSceneSideEffects()
        {
            var count = 0;
            for (var i = 0; i < Scenes.Count; i++)
            {
                if (Scenes[i].IsSceneSideEffect)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountIssues(ActivityOperationIssueSeverity severity)
        {
            var count = 0;
            for (var i = 0; i < Issues.Count; i++)
            {
                if (Issues[i].Severity == severity)
                {
                    count++;
                }
            }

            return count;
        }

        private static ActivityOperationPlanSceneEntry[] CopyScenes(IReadOnlyList<ActivityOperationPlanSceneEntry> scenes)
        {
            if (scenes == null || scenes.Count == 0)
            {
                return Array.Empty<ActivityOperationPlanSceneEntry>();
            }

            var copy = new ActivityOperationPlanSceneEntry[scenes.Count];
            for (var i = 0; i < scenes.Count; i++)
            {
                copy[i] = scenes[i];
            }

            return copy;
        }

        private static ActivityOperationIssue[] BuildIssues(
            ActivityOperationKind operationKind,
            ActivityAsset targetActivity,
            ActivityVisualTransitionMode visualMode,
            IReadOnlyList<ActivityOperationPlanSceneEntry> scenes,
            IReadOnlyList<ActivityOperationIssue> callerIssues)
        {
            var issues = new List<ActivityOperationIssue>();
            if (callerIssues != null)
            {
                for (var i = 0; i < callerIssues.Count; i++)
                {
                    if (callerIssues[i].HasIssue)
                    {
                        issues.Add(callerIssues[i]);
                    }
                }
            }

            if (RequiresTargetActivity(operationKind) && targetActivity == null)
            {
                issues.Add(ActivityOperationIssue.Blocking(
                    ActivityOperationIssueKind.MissingTargetActivity,
                    "Activity operation target Activity is required for this operation kind."));
            }

            var sceneSideEffects = CountSceneSideEffects(scenes);
            if (sceneSideEffects > 0)
            {
                if (visualMode == ActivityVisualTransitionMode.Seamless)
                {
                    issues.Add(ActivityOperationIssue.Blocking(
                        ActivityOperationIssueKind.SeamlessSceneSideEffects,
                        "Seamless Activity operation cannot contain Activity scene load/release side-effects."));
                }
                else if (visualMode == ActivityVisualTransitionMode.Fade)
                {
                    issues.Add(ActivityOperationIssue.Blocking(
                        ActivityOperationIssueKind.FadeSceneSideEffectsRequireFadeWithLoading,
                        "Activity scene load/release side-effects require explicit FadeWithLoading authoring."));
                }
            }
            else if (visualMode == ActivityVisualTransitionMode.FadeWithLoading)
            {
                issues.Add(ActivityOperationIssue.Warning(
                    ActivityOperationIssueKind.FadeWithLoadingWithoutSceneSideEffects,
                    "FadeWithLoading has no Activity scene side-effect in this plan; LoadingSurface is not required."));
            }

            if (scenes != null)
            {
                for (var i = 0; i < scenes.Count; i++)
                {
                    if (scenes[i].HasBlockingDeclarationIssue)
                    {
                        issues.Add(ActivityOperationIssue.Blocking(
                            ActivityOperationIssueKind.InvalidSceneEntry,
                            $"Activity operation scene entry at index '{i}' is missing scene or content identity."));
                    }
                }
            }

            if (issues.Count == 0)
            {
                return Array.Empty<ActivityOperationIssue>();
            }

            return issues.ToArray();
        }

        private static ActivityOperationPlanStatus DetermineStatus(
            ActivityOperationKind operationKind,
            IReadOnlyList<ActivityOperationIssue> issues)
        {
            if (operationKind == ActivityOperationKind.Unknown)
            {
                return ActivityOperationPlanStatus.NotRequested;
            }

            if (HasIssue(issues, ActivityOperationIssueSeverity.Blocking))
            {
                return ActivityOperationPlanStatus.Blocked;
            }

            if (HasIssue(issues, ActivityOperationIssueSeverity.Warning))
            {
                return ActivityOperationPlanStatus.PlannedWithIssues;
            }

            return ActivityOperationPlanStatus.Planned;
        }

        private static bool HasIssue(
            IReadOnlyList<ActivityOperationIssue> issues,
            ActivityOperationIssueSeverity severity)
        {
            if (issues == null)
            {
                return false;
            }

            for (var i = 0; i < issues.Count; i++)
            {
                if (issues[i].Severity == severity)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountSceneSideEffects(IReadOnlyList<ActivityOperationPlanSceneEntry> scenes)
        {
            if (scenes == null)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].IsSceneSideEffect)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool RequiresTargetActivity(ActivityOperationKind operationKind)
        {
            return operationKind == ActivityOperationKind.Start
                || operationKind == ActivityOperationKind.Switch
                || operationKind == ActivityOperationKind.RouteStartup;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
