using System;
using Immersive.Framework.GameFlow;
using Immersive.Framework.Transition;
using Immersive.Framework.TransitionEffects;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    /// <summary>
    /// Focused ADR-006 supporting evidence for the presentation-policy boundary.
    ///
    /// This regression intentionally proves only ADR006-QA-06 and ADR006-QA-07.
    /// It must not be interpreted as the ADR-006 Stage A certification runner:
    /// behavioral Before/After/Superseded and runtime Loading/Gate evidence remain
    /// owned by their dedicated GameFlow regressions.
    /// </summary>
    public static class QaAdr006PresentationPolicyRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Game Flow/Run ADR-006 Presentation Policy Regression";
        private const string Prefix = "[ADR006_PRESENTATION_POLICY]";
        private const int ExpectedCaseCount = 5;

        private static readonly string[] ExpectedCases =
        {
            "edit-mode-required",
            "required-presentation-missing-fails",
            "required-failure-is-not-accepted",
            "optional-explicit-noop-succeeds",
            "optional-noop-retains-no-lifecycle-authority"
        };

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => !EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var cases = new QaCaseRegistry(ExpectedCases, ExpectedCaseCount);
            try
            {
                Require(!EditorApplication.isPlaying,
                    "ADR-006 presentation policy regression requires Edit Mode.");
                cases.Complete("edit-mode-required");

                ProveRequiredPresentationMissing(cases);
                ProveExplicitOptionalNoOp(cases);

                cases.RequireComplete();
                Debug.Log(
                    $"{Prefix} status='Passed' cases='{cases.Count}' " +
                    $"completed='{cases.DescribeCompleted()}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{Prefix} status='Failed' completed='{cases.DescribeCompleted()}' " +
                    $"missing='{cases.DescribeMissing()}' " +
                    $"exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}'.");
                throw;
            }
        }

        private static void ProveRequiredPresentationMissing(QaCaseRegistry cases)
        {
            var orchestrator = new TransitionEffectOrchestrator(
                Array.Empty<ITransitionEffectAdapter>(),
                "ADR-006 required presentation");
            TransitionRequest request = TransitionRequest.Before(
                TransitionOperationId.From("qa.adr006.required-missing"),
                TransitionScope.Route,
                nameof(QaAdr006PresentationPolicyRegression),
                "required-presentation-missing",
                null,
                null,
                null,
                null);

            TransitionResult result = orchestrator.Execute(request);

            Require(result.IsValid && result.Failed,
                "Required presentation without an adapter must return a valid Failed result.");
            Require(result.Status == TransitionStatus.Failed,
                $"Required presentation missing returned status '{result.Status}'.");
            Require(result.EffectStatus == TransitionEffectStatus.MissingAdapter,
                $"Required presentation missing returned effect status '{result.EffectStatus}'.");
            Require(result.EffectAdapterCount == 0,
                "Required presentation missing unexpectedly resolved an adapter.");
            Require(result.EffectBlockingIssueCount > 0 && result.HasIssues,
                "Required presentation missing did not retain a blocking diagnostic cause.");
            Require(string.Equals(
                    result.VisualText,
                    "RequiredSurfaceMissing",
                    StringComparison.Ordinal),
                $"Required presentation missing visual diagnostic diverged. visual='{result.VisualText}'.");
            cases.Complete("required-presentation-missing-fails");

            Require(!GameFlowRuntime.IsAcceptedTransitionPhase(result),
                "Required presentation failure must not be accepted by GameFlow as a successful/optional phase.");
            Require(!GameFlowRuntime.TryAcceptTransitionPhase(
                    result,
                    "Before",
                    out string issue) &&
                !string.IsNullOrWhiteSpace(issue),
                "Required presentation failure must produce an explicit Before rejection diagnostic.");
            cases.Complete("required-failure-is-not-accepted");
        }

        private static void ProveExplicitOptionalNoOp(QaCaseRegistry cases)
        {
            TransitionRequest request = TransitionRequest.Before(
                TransitionOperationId.From("qa.adr006.explicit-noop"),
                TransitionScope.Route,
                nameof(QaAdr006PresentationPolicyRegression),
                "explicit-optional-noop",
                null,
                null,
                null,
                null);

            TransitionResult result = NoOpTransitionOrchestrator.Instance.Execute(request);

            Require(result.IsValid && result.Succeeded,
                "Explicit NoOp presentation must produce a valid successful Transition result.");
            Require(result.EffectStatus == TransitionEffectStatus.Skipped,
                $"Explicit NoOp effect status diverged. actual='{result.EffectStatus}'.");
            Require(result.EffectAdapterCount == 0,
                "Explicit NoOp unexpectedly created or resolved a presentation adapter.");
            Require(result.EffectBlockingIssueCount == 0 && !result.HasIssues,
                "Explicit NoOp unexpectedly produced blocking presentation issues.");
            Require(string.Equals(
                    result.VisualText,
                    "NoneConfigured",
                    StringComparison.Ordinal),
                $"Explicit NoOp visual diagnostic diverged. visual='{result.VisualText}'.");
            Require(GameFlowRuntime.IsAcceptedTransitionPhase(result),
                "Explicit NoOp must be accepted as the configured no-visual Transition policy.");
            cases.Complete("optional-explicit-noop-succeeds");

            Require(result.OperationId.Equals(request.OperationId) &&
                result.Kind == request.Kind &&
                string.Equals(result.Source, request.Source, StringComparison.Ordinal) &&
                string.Equals(result.Reason, request.Reason, StringComparison.Ordinal),
                "Explicit NoOp must preserve operation identity and intent without taking lifecycle authority.");
            Require(result.ObservedStepCount == 1 &&
                result.ObservedSteps[0].Phase == request.Phase,
                "Explicit NoOp did not retain the requested Transition phase as diagnostic evidence.");
            cases.Complete("optional-noop-retains-no-lifecycle-authority");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "\\'")
                    .Replace("\r", " ")
                    .Replace("\n", " ");
        }
    }
}
