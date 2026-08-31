using System.Threading.Tasks;
using Immersive.Framework.Actors;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.Player
{
    public static class QaPlayerSessionQaSupport
    {
        public static bool TryGetSupportedSlot(
            GameApplicationAsset application,
            int configuredIndex,
            out PlayerSlotProfile playerSlot)
        {
            playerSlot = null;
            if (!TryResolveProfile(application, out PlayerSessionProfile profile, out _) ||
                configuredIndex < 0 ||
                configuredIndex >= profile.SupportedSlots.Count)
            {
                return false;
            }

            playerSlot = profile.SupportedSlots[configuredIndex];
            return playerSlot != null;
        }

        public static bool TryResolveProfile(
            GameApplicationAsset application,
            out PlayerSessionProfile profile,
            out string issue)
        {
            profile = application != null
                ? application.DefaultPlayerSessionProfile
                : null;
            if (profile == null)
            {
                issue = "Active Game Application has no PlayerSessionProfile.";
                return false;
            }

            if (!application.PlayerSessionEnabled)
            {
                issue = "Active Game Application has Player Session disabled.";
                profile = null;
                return false;
            }

            if (!profile.TryValidate(out issue))
            {
                return false;
            }

            issue = string.Empty;
            return true;
        }

        public static bool TryValidateManagerBridge(
            PlayerSessionProfile profile,
            PlayerInputManager manager,
            out string issue)
        {
            if (profile == null)
            {
                issue = "Manager-Provisioned fixture has no PlayerSessionProfile.";
                return false;
            }

            if (!profile.TryValidate(out issue))
            {
                return false;
            }

            if (manager == null)
            {
                issue = "Manager-Provisioned fixture has no PlayerInputManager.";
                return false;
            }

            if (manager.maxPlayerCount != profile.SupportedSlotCount)
            {
                issue =
                    $"PlayerInputManager '{manager.name}' limit '{manager.maxPlayerCount}' " +
                    $"does not match PlayerSessionProfile Supported Slots '{profile.SupportedSlotCount}'.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        public static void ConfigureManagerBridge(
            PlayerSessionProfile profile,
            PlayerInputManager manager)
        {
            string profileIssue = string.Empty;
            if (profile == null || !profile.TryValidate(out profileIssue))
            {
                throw new System.InvalidOperationException(
                    "PlayerInputManager bridge requires a valid PlayerSessionProfile. " +
                    profileIssue);
            }

            if (manager == null)
            {
                throw new System.ArgumentNullException(nameof(manager));
            }

            var serializedManager = new SerializedObject(manager);
            SerializedProperty limit = serializedManager.FindProperty("m_MaxPlayerCount");
            if (limit == null)
            {
                throw new System.InvalidOperationException(
                    "PlayerInputManager serialized max-player-count field was not found.");
            }

            limit.intValue = profile.SupportedSlotCount;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

namespace ImmersiveFrameworkQA.Player.Internal.Editor
{
    /// <summary>
    /// Narrow observation seam for Game Flow participation regressions. It
    /// converts already-composed host internals into public immutable contracts.
    /// </summary>
    public static class QaPlayerRuntimeObservationBridge
    {
        public static bool TryGetParticipationSnapshot(
            Component hostComponent,
            out PlayerParticipationSnapshot snapshot)
        {
            snapshot = default;
            if (hostComponent is not FrameworkRuntimeHost host ||
                !host.TryGetPlayerParticipationRuntime(
                    out PlayerParticipationRuntimeContext context))
            {
                return false;
            }

            snapshot = context.CreateSnapshot();
            return true;
        }

        public static bool TryGetLocalPlayerProvisioningAuthoring(
            Component hostComponent,
            out LocalPlayerProvisioningAuthoring authoring,
            out string diagnostic)
        {
            authoring = null;
            diagnostic = string.Empty;
            if (hostComponent is not FrameworkRuntimeHost host ||
                host.GetComponent<LocalPlayerProvisioningRuntimeHostModule>() is not
                    LocalPlayerProvisioningRuntimeHostModule provisioning)
            {
                diagnostic = "Framework Local Player provisioning runtime is unavailable.";
                return false;
            }

            authoring = provisioning.Authoring;
            if (authoring == null)
            {
                diagnostic = string.IsNullOrWhiteSpace(provisioning.Diagnostic)
                    ? "Framework Local Player provisioning authoring is unavailable."
                    : provisioning.Diagnostic;
                return false;
            }

            return true;
        }

        public static bool TryGetScenePreparation(
            Component hostComponent,
            PlayerSlotId slotId,
            out PlayerActorPreparationSummary summary)
        {
            summary = default;
            return hostComponent is FrameworkRuntimeHost host &&
                host.GetComponent<PlayerActorPreparationRuntimeHostModule>() is
                    PlayerActorPreparationRuntimeHostModule preparation &&
                preparation.TryGetScenePlayerActorPreparationSummary(slotId, out summary);
        }

        public static bool TryGetSceneAdoption(
            Component hostComponent,
            PlayerSlotId slotId,
            out ScenePlayerActorAdoptionToken token)
        {
            token = default;
            return hostComponent is FrameworkRuntimeHost host &&
                host.GetComponent<PlayerActorPreparationRuntimeHostModule>() is
                    PlayerActorPreparationRuntimeHostModule preparation &&
                preparation.TryGetScenePlayerActorAdoption(slotId, out token);
        }

        public static bool TryGetPreparedPhysicalEvidence(
            Component hostComponent,
            PlayerSlotId slotId,
            PlayerActorPreparationToken expectedPreparation,
            out LocalPlayerHostAuthoring host,
            out PlayerActorDeclaration actorDeclaration,
            out string diagnostic)
        {
            host = null;
            actorDeclaration = null;
            diagnostic = string.Empty;
            if (hostComponent is not FrameworkRuntimeHost runtimeHost ||
                runtimeHost.GetComponent<PlayerActorPreparationRuntimeHostModule>() is not
                    PlayerActorPreparationRuntimeHostModule preparation)
            {
                diagnostic = "Player Actor preparation runtime is unavailable.";
                return false;
            }

            return preparation.TryGetPreparedPhysicalEvidence(
                slotId,
                expectedPreparation,
                out host,
                out _,
                out actorDeclaration,
                out _,
                out diagnostic);
        }

        public static int GetActiveSceneAdmissionCount(Component hostComponent)
        {
            return hostComponent is FrameworkRuntimeHost host &&
                host.GetComponent<SceneLocalPlayerAdmissionRuntimeHostModule>() is
                    SceneLocalPlayerAdmissionRuntimeHostModule admissions
                ? admissions.ActiveAdmissionCount
                : 0;
        }

        public static QaAutomaticSceneAdmissionResolution ResolveAutomaticSceneAdmission(
            Component hostComponent,
            ActivityAsset activity)
        {
            if (hostComponent is not FrameworkRuntimeHost host ||
                host.GetComponent<SceneLocalPlayerAdmissionRuntimeHostModule>() is not
                    SceneLocalPlayerAdmissionRuntimeHostModule admissions)
            {
                return new QaAutomaticSceneAdmissionResolution(
                    false,
                    0,
                    "Scene Local Player admission runtime is unavailable.");
            }

            bool succeeded = admissions.TryResolveAutomaticActivityAuthoring(
                activity,
                out System.Collections.Generic.IReadOnlyList<
                    SceneProvidedLocalPlayerAuthoring> authoring,
                out string issue);
            return new QaAutomaticSceneAdmissionResolution(
                succeeded,
                authoring?.Count ?? 0,
                issue);
        }

        public static int CountRuntimeRoots(
            Component hostComponent,
            RuntimeContentOwner owner)
        {
            if (hostComponent is not FrameworkRuntimeHost host ||
                !owner.IsValid ||
                host.RuntimeContentRuntime == null)
            {
                return 0;
            }

            int count = 0;
            RuntimeScopeRoot[] roots = host.RuntimeContentRuntime.SnapshotRoots();
            for (int index = 0; index < roots.Length; index++)
            {
                if (roots[index].Owner == owner)
                {
                    count++;
                }
            }

            return count;
        }

        public static RouteAsset GetCurrentRoute(Component hostComponent) =>
            hostComponent is FrameworkRuntimeHost host ? host.State.CurrentRoute : null;

        public static ActivityAsset GetCurrentActivity(Component hostComponent) =>
            hostComponent is FrameworkRuntimeHost host ? host.State.CurrentActivity : null;

        public static bool IsReady(Component hostComponent) =>
            hostComponent is FrameworkRuntimeHost host &&
            host.State.GameFlowStarted &&
            host.State.CurrentRoute != null &&
            host.State.CurrentActivity != null &&
            host.State.IsActivityReady;

        public static async Task<QaRouteRequestObservation> RequestRouteAsync(
            Component hostComponent,
            RouteAsset route,
            string source,
            string reason)
        {
            if (hostComponent is not FrameworkRuntimeHost host)
            {
                throw new System.InvalidOperationException(
                    "Runtime host is unavailable for Route request plumbing.");
            }

            FrameworkRouteRequestResult result = await host.RequestRouteAsync(
                route, source, reason);
            ActivityContentExecutionLifecycleResult activityContentExecution =
                result.RouteLifecycleResult.ActivityFlowResult
                    .ActivityContentExecutionResult;
            string activityContentDiagnostic =
                activityContentExecution.ToDiagnosticString();
            string activityContentEnterDiagnostic =
                activityContentExecution.EnterResult.ToDiagnosticString();
            string message = string.IsNullOrWhiteSpace(result.Message)
                ? $"activityContentExecution=({activityContentDiagnostic}) " +
                  $"activityContentEnter=({activityContentEnterDiagnostic})"
                : $"{result.Message} " +
                  $"activityContentExecution=({activityContentDiagnostic}) " +
                  $"activityContentEnter=({activityContentEnterDiagnostic})";
            return new QaRouteRequestObservation(
                result.Succeeded,
                result.RouteLifecycleResult.ActivityFlowResult.IsActivityReady,
                message);
        }
    }

    public readonly struct QaRouteRequestObservation
    {
        public QaRouteRequestObservation(bool succeeded, bool activityReady, string message)
        {
            Succeeded = succeeded;
            ActivityReady = activityReady;
            Message = message ?? string.Empty;
        }

        public bool Succeeded { get; }
        public bool ActivityReady { get; }
        public string Message { get; }
    }

    public readonly struct QaAutomaticSceneAdmissionResolution
    {
        public QaAutomaticSceneAdmissionResolution(bool succeeded, int count, string issue)
        {
            Succeeded = succeeded;
            Count = count;
            Issue = issue ?? string.Empty;
        }

        public bool Succeeded { get; }
        public int Count { get; }
        public string Issue { get; }
    }
}
