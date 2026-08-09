using System;
using System.Threading.Tasks;
using Immersive.Framework.GameFlow;
using ImmersiveFrameworkQA.Hub;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    /// <summary>
    /// Shared helpers for resolving the authored public navigation fixture and
    /// awaiting ActivityRequestTrigger outcomes without privileged binding.
    /// </summary>
    internal static class QaPlayerSurfacePublicNavigationSupport
    {
        internal static bool TryResolveAuthoredFixture(
            out QaPlayerSurfacePublicNavigationFixture fixture,
            out string diagnostic)
        {
            fixture = null;
            for (int sceneIndex = 0;
                 sceneIndex < SceneManager.sceneCount;
                 sceneIndex++)
            {
                Scene scene = SceneManager.GetSceneAt(sceneIndex);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                GameObject[] roots = scene.GetRootGameObjects();
                for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                {
                    GameObject root = roots[rootIndex];
                    if (root == null ||
                        !string.Equals(
                            root.name,
                            QaPlayerSurfacePublicNavigationFixture.RootObjectName,
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    fixture = root.GetComponent<
                        QaPlayerSurfacePublicNavigationFixture>();
                    if (fixture == null)
                    {
                        diagnostic =
                            "Authored public navigation root exists but runtime fixture component is missing. " +
                            "Re-run Prepare Player Surface Public Navigation Fixture.";
                        return false;
                    }

                    if (!fixture.TryValidateAuthoredSurface(out string issue))
                    {
                        diagnostic = issue;
                        fixture = null;
                        return false;
                    }

                    diagnostic =
                        $"fixture='resolved' scene='{scene.name}' activity='{fixture.TargetActivity.ActivityName}'.";
                    return true;
                }
            }

            diagnostic =
                "Authored public navigation fixture was not found in loaded scenes. " +
                "Run Prepare Player Surface Public Navigation Fixture in Edit Mode.";
            return false;
        }

        internal static async Task RequireCompositionBoundAsync(
            ActivityRequestTrigger trigger,
            int frameBudget)
        {
            Require(trigger != null, "ActivityRequestTrigger is required.");
            for (int frame = 0; frame < frameBudget; frame++)
            {
                if (trigger.HasActivityRuntimeBinding)
                {
                    return;
                }

                await Awaitable.NextFrameAsync();
            }

            throw new TimeoutException(
                "Authored ActivityRequestTrigger was not composition-bound by Framework. " +
                $"status='{trigger.ActivityRuntimeBindingStatus}' " +
                $"diagnostic='{trigger.ActivityRuntimeBindingDiagnostic}'.");
        }

        internal static void RequestActivityPublic(ActivityRequestTrigger trigger)
        {
            Require(trigger != null, "Public Activity request requires a trigger.");
            Require(
                trigger.HasActivityRuntimeBinding,
                "Public Activity request rejected: ActivityRequestTrigger is not composition-bound. " +
                trigger.ActivityRuntimeBindingDiagnostic);
            Require(
                trigger.TargetActivity != null,
                "Public Activity request rejected: Target Activity is missing.");
            trigger.RequestActivity();
        }

        internal static void ClearActivityPublic(ActivityRequestTrigger trigger)
        {
            Require(trigger != null, "Public Activity clear requires a trigger.");
            Require(
                trigger.HasActivityRuntimeBinding,
                "Public Activity clear rejected: ActivityRequestTrigger is not composition-bound. " +
                trigger.ActivityRuntimeBindingDiagnostic);
            trigger.ClearActivity();
        }

        internal static async Task AwaitTriggerInFlightAsync(
            ActivityRequestTrigger trigger,
            int frameBudget,
            string failure)
        {
            for (int frame = 0; frame < frameBudget; frame++)
            {
                if (trigger.IsRequestInFlight)
                {
                    return;
                }

                if (trigger.LastRequestFailed)
                {
                    throw new InvalidOperationException(
                        $"{failure} Trigger failed before in-flight. message='{trigger.LastMessage}'.");
                }

                await Awaitable.NextFrameAsync();
            }

            throw new TimeoutException(
                $"{failure} Trigger never entered in-flight. " +
                $"phase='{trigger.LastEventPhase}' outcome='{trigger.LastOutcome}' " +
                $"message='{trigger.LastMessage}'.");
        }

        internal static async Task AwaitTriggerTerminalSuccessAsync(
            ActivityRequestTrigger trigger,
            int frameBudget,
            string failure)
        {
            for (int frame = 0; frame < frameBudget; frame++)
            {
                if (!trigger.IsRequestInFlight)
                {
                    if (trigger.LastRequestSucceeded)
                    {
                        return;
                    }

                    if (trigger.LastRequestFailed || trigger.LastRequestIgnored)
                    {
                        throw new InvalidOperationException(
                            $"{failure} " +
                            $"phase='{trigger.LastEventPhase}' outcome='{trigger.LastOutcome}' " +
                            $"message='{trigger.LastMessage}'.");
                    }
                }

                await Awaitable.NextFrameAsync();
            }

            throw new TimeoutException(
                $"{failure} Trigger did not reach terminal success within '{frameBudget}' frames. " +
                $"inFlight='{trigger.IsRequestInFlight}' message='{trigger.LastMessage}'.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}

