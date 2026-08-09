using System;
using System.Threading.Tasks;
using Immersive.Framework.GameFlow;
using Immersive.Framework.PlayerParticipation;
using ImmersiveFrameworkQA.Hub;
using ImmersiveFrameworkQA.UnityBuildSurface;
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

        internal static bool TryResolveGlobalUiFixture(
            out QaPlayerSurfaceGlobalUiFixture fixture,
            out string diagnostic)
        {
            fixture = null;
            var candidates = new System.Collections.Generic.List<
                QaPlayerSurfaceGlobalUiFixture>();
            QaPlayerSurfaceGlobalUiFixture[] discovered =
                UnityEngine.Object.FindObjectsByType<
                    QaPlayerSurfaceGlobalUiFixture>(
                    FindObjectsInactive.Include);
            for (int index = 0; index < discovered.Length; index++)
            {
                QaPlayerSurfaceGlobalUiFixture candidate = discovered[index];
                if (candidate == null ||
                    candidate.gameObject == null ||
                    !candidate.gameObject.scene.IsValid() ||
                    !candidate.gameObject.scene.isLoaded)
                {
                    continue;
                }

                candidates.Add(candidate);
            }

            int count = candidates.Count;

            if (count != 1)
            {
                diagnostic =
                    "Player Surface certification requires exactly one live " +
                    $"QaPlayerSurfaceGlobalUiFixture; found '{count}'. " +
                    "The fixture is QA-owned evidence and must be retained by the " +
                    $"normal UIGlobal composition. {DescribeGlobalUiFixtures(candidates.ToArray())}";
                return false;
            }

            fixture = candidates[0];
            if (!fixture.TryValidateAuthoredSurface(out string issue))
            {
                diagnostic =
                    $"UIGlobal QA fixture validation failed. {DescribeGlobalUiFixtures(candidates.ToArray())} " +
                    $"issue='{issue}'.";
                fixture = null;
                return false;
            }

            diagnostic =
                DescribeGlobalUiFixtures(candidates.ToArray());
            return true;
        }

        internal static async Task<LocalPlayerActorSelectionRequestAuthoring>
            RequireActorSelectionRuntimeReadyAsync(
                QaPlayerSurfaceGlobalUiFixture fixture,
                int frameBudget)
        {
            Require(fixture != null,
                "UIGlobal QA fixture is required for Actor Selection readiness.");
            Require(
                fixture.TryValidateAuthoredSurface(out string fixtureIssue),
                fixtureIssue);

            LocalPlayerActorSelectionRequestAuthoring authoring =
                fixture.ActorSelectionRequestAuthoring;
            Require(authoring != null,
                "UIGlobal QA fixture has no Actor Selection authoring.");

            for (int frame = 0; frame < frameBudget; frame++)
            {
                if (authoring.HasPlayerActorSelectionRuntimeBinding &&
                    authoring.RuntimeReady)
                {
                    return authoring;
                }

                await Awaitable.NextFrameAsync();
            }

            LocalPlayerProvisioningAuthoring provisioning =
                authoring.ProvisioningAuthoring;
            throw new TimeoutException(
                "Public Actor Selection did not become runtime-ready after Framework boot. " +
                $"authoringId='{authoring.GetEntityId()}' " +
                $"object='{authoring.gameObject.name}' " +
                $"scene='{authoring.gameObject.scene.name}' " +
                $"binding='{authoring.PlayerActorSelectionRuntimeBindingStatus}' " +
                $"bindingDiagnostic='{authoring.PlayerActorSelectionRuntimeBindingDiagnostic}' " +
                $"provisioningId='{(provisioning != null ? provisioning.GetEntityId().ToString() : "missing")}' " +
                $"provisioningReady='{(provisioning != null && provisioning.RuntimeReady)}' " +
                $"provisioningDiagnostic='{(provisioning != null ? provisioning.RuntimeDiagnostic : "missing")}' " +
                $"fixtureEvidence=\"fixtureCount='1' {DescribeGlobalUiFixture(fixture)}\".");
        }

        internal static async Task<LocalPlayerProvisioningAuthoring>
            RequireProvisioningRuntimeReadyAsync(
                QaPlayerSurfaceGlobalUiFixture fixture,
                int frameBudget)
        {
            Require(fixture != null,
                "UIGlobal QA fixture is required for provisioning readiness.");
            Require(
                fixture.TryValidateAuthoredSurface(out string fixtureIssue),
                fixtureIssue);

            LocalPlayerProvisioningAuthoring provisioning = fixture
                .ActorSelectionRequestAuthoring.ProvisioningAuthoring;
            Require(provisioning != null,
                "UIGlobal QA fixture has no Local Player provisioning authoring.");

            for (int frame = 0; frame < frameBudget; frame++)
            {
                if (provisioning.RuntimeReady)
                {
                    return provisioning;
                }

                await Awaitable.NextFrameAsync();
            }

            throw new TimeoutException(
                "Canonical Local Player provisioning did not become RuntimeReady after Framework boot. " +
                $"object='{provisioning.gameObject.name}' " +
                $"scene='{provisioning.gameObject.scene.name}' " +
                $"diagnostic='{provisioning.RuntimeDiagnostic}'.");
        }

        private static string DescribeGlobalUiFixtures(
            QaPlayerSurfaceGlobalUiFixture[] fixtures)
        {
            if (fixtures == null || fixtures.Length == 0)
            {
                return "fixtureCount='0'";
            }

            string result = $"fixtureCount='{fixtures.Length}'";
            for (int index = 0; index < fixtures.Length; index++)
            {
                result += $" fixture[{index}]=\"{DescribeGlobalUiFixture(fixtures[index])}\"";
            }

            return result;
        }

        private static string DescribeGlobalUiFixture(
            QaPlayerSurfaceGlobalUiFixture fixture)
        {
            if (fixture == null)
            {
                return "fixture='missing'";
            }

            LocalPlayerActorSelectionRequestAuthoring actorSelection =
                fixture.ActorSelectionRequestAuthoring;
            LocalPlayerProvisioningAuthoring provisioning =
                actorSelection != null
                    ? actorSelection.ProvisioningAuthoring
                    : null;
            return
                $"fixture='resolved' fixtureId='{fixture.GetEntityId()}' " +
                $"object='{fixture.gameObject.name}' " +
                $"scene='{fixture.gameObject.scene.name}' " +
                $"actorSelectionId='{(actorSelection != null ? actorSelection.GetEntityId().ToString() : "missing")}' " +
                $"actorSelectionReady='{(actorSelection != null && actorSelection.RuntimeReady)}' " +
                $"actorSelectionBinding='{(actorSelection != null ? actorSelection.PlayerActorSelectionRuntimeBindingStatus : "missing")}' " +
                $"provisioningId='{(provisioning != null ? provisioning.GetEntityId().ToString() : "missing")}' " +
                $"provisioningReady='{(provisioning != null && provisioning.RuntimeReady)}'.";
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

