using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    public static class QaH2212PlayerActorSelectionRuntimeBindingSmoke
    {
        private const string LogPrefix =
            "[H2212_PLAYER_ACTOR_SELECTION_RUNTIME_BINDING_SMOKE]";
        private const string Source =
            nameof(QaH2212PlayerActorSelectionRuntimeBindingSmoke);
        private const string MenuPath =
            "Immersive Framework/QA/Game Flow/H2.2.12 Run Player Actor Selection Runtime Binding Smoke";

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        public static async void Run()
        {
            await RunInternalAsync();
        }

        public static async Task RunInternalAsync()
        {
            var completed = new List<string>();
            var objects = new List<UnityObject>();
            LocalPlayerProvisioningAuthoring provisioning = null;
            LocalPlayerActorSelectionRequestAuthoring productAuthoring = null;
            bool productAuthoringCreated = false;
            bool joiningOpened = false;

            try
            {
                Require(
                    EditorApplication.isPlaying,
                    "H2.2.12 vertical smoke requires Play Mode.");
                Require(
                    global::ImmersiveFrameworkQA.GameFlow.Internal.Editor.ImmersiveFrameworkQA.GameFlow.InternalEditor.QaH2FrameworkReadiness.TryResolveUniqueHost(
                        out FrameworkRuntimeHost host) &&
                    host != null,
                    "H2.2.12 vertical smoke requires FrameworkRuntimeHost.");

                IPlayerActorSelectionRuntimePort hostRuntime = host;
                string hostRuntimeIssue = string.Empty;
                bool hostRuntimeReady =
                    hostRuntime != null &&
                    hostRuntime.TryValidatePlayerActorSelectionRuntime(
                        out hostRuntimeIssue);
                Require(
                    hostRuntimeReady,
                    string.IsNullOrWhiteSpace(hostRuntimeIssue)
                        ? "FrameworkRuntimeHost did not expose a ready Player Actor selection runtime port."
                        : hostRuntimeIssue);
                completed.Add("runtime-port-available-and-ready");

                RunBindingCompositionCases(
                    hostRuntime,
                    objects,
                    completed);

                provisioning = await AwaitProvisioningAsync();
                Require(
                    provisioning.RuntimeReady,
                    "Local Player provisioning runtime is not ready. " +
                    provisioning.RuntimeDiagnostic);
                object playerInputManager =
                    GetRequiredPropertyValue(
                        provisioning,
                        "PlayerInputManager",
                        "Local Player provisioning has no PlayerInputManager.");
                int playerCount =
                    GetRequiredIntPropertyValue(
                        playerInputManager,
                        "playerCount",
                        "PlayerInputManager has no readable playerCount property.");
                Require(
                    playerCount == 0,
                    "H2.2.12 smoke is one-shot. Re-enter Play Mode before running it again.");

                productAuthoring =
                    provisioning.GetComponent<
                        LocalPlayerActorSelectionRequestAuthoring>();
                if (productAuthoring == null)
                {
                    productAuthoring =
                        provisioning.gameObject.AddComponent<
                            LocalPlayerActorSelectionRequestAuthoring>();
                    productAuthoringCreated = true;
                }

                productAuthoring.ProvisioningAuthoring = provisioning;
                Require(
                    productAuthoring.TryValidateConfiguration(
                        out string configurationIssue),
                    configurationIssue);
                Require(
                    !productAuthoring.HasPlayerActorSelectionRuntimeBinding,
                    "H2.2.12 smoke requires an initially unbound product authoring. Re-enter Play Mode.");

                PlayerParticipationSnapshot beforeUnbound =
                    provisioning.RuntimeSnapshot;
                Require(
                    beforeUnbound != null &&
                    beforeUnbound.IsInitialized &&
                    beforeUnbound.ConfiguredSlotCount > 0,
                    "Local Player participation snapshot has no configured Player Slots.");
                PlayerSlotRuntimeSnapshot unboundSlot =
                    beforeUnbound.Slots[0];

                PlayerActorSelectionResult unboundResult =
                    productAuthoring.RequestDefaultActorSelection(
                        unboundSlot.PlayerSlotId,
                        unboundSlot.SelectionRevision,
                        Source,
                        "unbound-default-selection");
                PlayerParticipationSnapshot afterUnbound =
                    provisioning.RuntimeSnapshot;
                Require(
                    unboundResult != null &&
                    unboundResult.Status ==
                        PlayerActorSelectionStatus.RejectedRuntimeUnavailable &&
                    unboundResult.Message.Contains("not bound") &&
                    !productAuthoring.HasPlayerActorSelectionRuntimeBinding &&
                    productAuthoring.RequestCount == 1 &&
                    ReferenceEquals(
                        productAuthoring.LastResult,
                        unboundResult) &&
                    afterUnbound.Revision == beforeUnbound.Revision &&
                    afterUnbound.JoinedCount == beforeUnbound.JoinedCount &&
                    afterUnbound.SelectedActorCount ==
                        beforeUnbound.SelectedActorCount,
                    BuildProductDiagnostic(
                        productAuthoring,
                        unboundResult,
                        afterUnbound));
                completed.Add(
                    "unbound-authoring-does-not-fallback-to-current-host");

                LocalPlayerActorSelectionRequestAuthoringBindingResult productBinding =
                    LocalPlayerActorSelectionRequestAuthoringBinding.TryBind(
                        new[]
                        {
                            provisioning.gameObject,
                            provisioning.gameObject
                        },
                        hostRuntime);
                Require(
                    productBinding.Succeeded &&
                    productBinding.Status == "Bound" &&
                    productBinding.RootCount == 1 &&
                    productBinding.AuthoringCount == 1 &&
                    productBinding.BoundCount == 1 &&
                    productBinding.IdempotentCount == 0 &&
                    productBinding.RejectedCount == 0 &&
                    productAuthoring.HasPlayerActorSelectionRuntimeBinding,
                    productBinding.Message);
                completed.Add("product-authoring-bound-explicitly");

                Require(
                    productAuthoring.RuntimeReady,
                    "Product authoring did not become ready after provisioning and Player Actor selection runtime binding. " +
                    productAuthoring.PlayerActorSelectionRuntimeBindingDiagnostic);
                completed.Add(
                    "runtime-ready-requires-provisioning-and-selection-binding");

                PlayerParticipationOperationResult opened =
                    provisioning.OpenJoining(
                        Source,
                        "h2.2.12-open-joining");
                Require(
                    opened != null &&
                    opened.Completed &&
                    opened.Snapshot != null &&
                    opened.Snapshot.JoiningOpen,
                    opened != null
                        ? opened.ToDiagnosticString()
                        : "Opening joining returned no result.");
                joiningOpened = true;

                LocalPlayerJoinResult joined =
                    provisioning.RequestJoin(
                        Source,
                        "h2.2.12-public-join");
                Require(
                    joined != null &&
                    joined.Succeeded &&
                    joined.LocalPlayerHost != null &&
                    joined.Slot.IsJoined,
                    joined != null
                        ? joined.ToDiagnosticString()
                        : "Public local Player join returned no result.");

                object joinedPlayerInput =
                    GetRequiredPropertyValue(
                        joined,
                        "PlayerInput",
                        "Successful Local Player join has no PlayerInput evidence.");
                object hostPlayerInput =
                    GetRequiredPropertyValue(
                        joined.LocalPlayerHost,
                        "PlayerInput",
                        "Joined Local Player Host has no PlayerInput evidence.");
                Require(
                    joinedPlayerInput != null &&
                    ReferenceEquals(
                        hostPlayerInput,
                        joinedPlayerInput),
                    "Stable Local Player Host does not own the PlayerInput returned by provisioning.");
                completed.Add("public-join-succeeded");

                Require(
                    joined.Slot.Profile != null &&
                    joined.Slot.Profile.DefaultActorProfile != null,
                    "Joined Player Slot has no default ActorProfile intent.");

                const string SelectionReason =
                    "h2.2.12-select-default-after-join";
                PlayerActorSelectionResult selected =
                    productAuthoring.RequestDefaultActorSelection(
                        joined.Slot.PlayerSlotId,
                        joined.Slot.SelectionRevision,
                        Source,
                        SelectionReason);
                Require(
                    selected != null &&
                    selected.Succeeded &&
                    selected.Slot.IsJoined &&
                    selected.Slot.HasSelectedActor &&
                    ReferenceEquals(
                        joined.Slot.Profile.DefaultActorProfile,
                        selected.SelectedActorProfile) &&
                    selected.SelectionRevision >
                        joined.Slot.SelectionRevision &&
                    selected.Source == Source &&
                    selected.Reason == SelectionReason,
                    BuildProductDiagnostic(
                        productAuthoring,
                        selected,
                        provisioning.RuntimeSnapshot));
                completed.Add(
                    "default-actor-selection-forwarded-to-runtime-port");

                PlayerParticipationSnapshot selectedSnapshot =
                    provisioning.RuntimeSnapshot;
                PlayerSlotRuntimeSnapshot current =
                    FindSlot(
                        selectedSnapshot,
                        joined.Slot.PlayerSlotId);
                Require(
                    current.IsJoined &&
                    current.HasSelectedActor &&
                    ReferenceEquals(
                        selected.SelectedActorProfile,
                        current.SelectedActorProfile) &&
                    current.SelectionRevision ==
                        selected.SelectionRevision,
                    "Authoritative Player participation snapshot did not retain the selected default Actor.");
                completed.Add(
                    "authoritative-selection-snapshot-updated");

                var divergentRuntime =
                    new RejectingPlayerActorSelectionRuntimePort();
                LocalPlayerActorSelectionRequestAuthoringBindingResult divergentBinding =
                    LocalPlayerActorSelectionRequestAuthoringBinding.TryBind(
                        new[] { provisioning.gameObject },
                        divergentRuntime);
                Require(
                    !divergentBinding.Succeeded &&
                    divergentBinding.Status ==
                        "RejectedAuthoringBinding" &&
                    divergentBinding.AuthoringCount == 1 &&
                    divergentBinding.BoundCount == 0 &&
                    divergentBinding.IdempotentCount == 0 &&
                    divergentBinding.RejectedCount == 1 &&
                    divergentRuntime.SelectionCallCount == 0,
                    divergentBinding.Message);

                PlayerActorSelectionResult repeated =
                    productAuthoring.RequestDefaultActorSelection(
                        current.PlayerSlotId,
                        current.SelectionRevision,
                        Source,
                        "h2.2.12-repeat-default-selection");
                Require(
                    repeated != null &&
                    repeated.Succeeded &&
                    repeated.SelectionRevision ==
                        current.SelectionRevision &&
                    ReferenceEquals(
                        repeated.SelectedActorProfile,
                        selected.SelectedActorProfile) &&
                    divergentRuntime.SelectionCallCount == 0,
                    BuildProductDiagnostic(
                        productAuthoring,
                        repeated,
                        provisioning.RuntimeSnapshot));
                completed.Add(
                    "divergent-rebind-cannot-redirect-selection-authority");

                Require(
                    productAuthoring.RequestCount == 3 &&
                    ReferenceEquals(
                        productAuthoring.LastResult,
                        repeated) &&
                    productAuthoring.LastDiagnostic ==
                        repeated.ToDiagnosticString(),
                    BuildProductDiagnostic(
                        productAuthoring,
                        repeated,
                        provisioning.RuntimeSnapshot));
                completed.Add("endpoint-diagnostics-retained");

                PlayerParticipationOperationResult closed =
                    provisioning.CloseJoining(
                        Source,
                        "h2.2.12-complete");
                Require(
                    closed != null &&
                    closed.Completed &&
                    closed.Snapshot != null &&
                    !closed.Snapshot.JoiningOpen,
                    closed != null
                        ? closed.ToDiagnosticString()
                        : "Closing joining returned no result.");
                joiningOpened = false;
                completed.Add("joining-closed");

                Require(
                    completed.Count == 12,
                    $"Unexpected H2.2.12 case count. actual='{completed.Count}'.");

                Debug.Log(
                    $"{LogPrefix} status='Passed' cases='12' " +
                    $"slot='{joined.Slot.PlayerSlotId.StableText}' " +
                    $"actor='{selected.SelectedActorProfileId.StableText}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{LogPrefix} status='Failed' " +
                    $"exception='{exception.GetType().Name}' " +
                    $"message='{exception.Message}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
            }
            finally
            {
                if (joiningOpened &&
                    provisioning != null &&
                    provisioning.RuntimeReady)
                {
                    try
                    {
                        provisioning.CloseJoining(
                            Source,
                            "h2.2.12-finally-close");
                    }
                    catch
                    {
                        // Preserve the primary smoke failure.
                    }
                }

                if (productAuthoringCreated &&
                    productAuthoring != null)
                {
                    UnityObject.Destroy(productAuthoring);
                }

                for (int index = objects.Count - 1;
                     index >= 0;
                     index--)
                {
                    if (objects[index] != null)
                    {
                        UnityObject.Destroy(objects[index]);
                    }
                }
            }
        }

        private static void RunBindingCompositionCases(
            IPlayerActorSelectionRuntimePort hostRuntime,
            ICollection<UnityObject> objects,
            ICollection<string> completed)
        {
            GameObject emptyRoot =
                CreateInactiveRoot(
                    "H2212 Binding Empty Root",
                    objects);

            LocalPlayerActorSelectionRequestAuthoringBindingResult optionalAbsent =
                LocalPlayerActorSelectionRequestAuthoringBinding.TryBind(
                    new[] { emptyRoot, emptyRoot },
                    hostRuntime);
            Require(
                optionalAbsent.Succeeded &&
                optionalAbsent.Status == "OptionalAbsent" &&
                optionalAbsent.RootCount == 1 &&
                optionalAbsent.AuthoringCount == 0,
                optionalAbsent.Message);

            GameObject authoredRoot =
                CreateInactiveRoot(
                    "H2212 Binding Authored Root",
                    objects);
            var child =
                new GameObject(
                    "H2212 Binding Actor Selection Authoring");
            child.transform.SetParent(
                authoredRoot.transform,
                false);
            LocalPlayerActorSelectionRequestAuthoring authoring =
                child.AddComponent<
                    LocalPlayerActorSelectionRequestAuthoring>();

            LocalPlayerActorSelectionRequestAuthoringBindingResult missingRuntime =
                LocalPlayerActorSelectionRequestAuthoringBinding.TryBind(
                    new[] { authoredRoot },
                    null);
            Require(
                !missingRuntime.Succeeded &&
                missingRuntime.Status ==
                    "RejectedMissingPlayerActorSelectionRuntime" &&
                missingRuntime.RootCount == 1 &&
                missingRuntime.AuthoringCount == 0 &&
                !authoring.HasPlayerActorSelectionRuntimeBinding,
                missingRuntime.Message);

            LocalPlayerActorSelectionRequestAuthoringBindingResult bound =
                LocalPlayerActorSelectionRequestAuthoringBinding.TryBind(
                    new[]
                    {
                        authoredRoot,
                        child,
                        authoredRoot,
                        null
                    },
                    hostRuntime);
            Require(
                bound.Succeeded &&
                bound.Status == "Bound" &&
                bound.RootCount == 2 &&
                bound.AuthoringCount == 1 &&
                bound.BoundCount == 1 &&
                bound.IdempotentCount == 0 &&
                bound.RejectedCount == 0 &&
                authoring.HasPlayerActorSelectionRuntimeBinding,
                bound.Message);

            LocalPlayerActorSelectionRequestAuthoringBindingResult idempotent =
                LocalPlayerActorSelectionRequestAuthoringBinding.TryBind(
                    new[] { authoredRoot, child },
                    hostRuntime);
            Require(
                idempotent.Succeeded &&
                idempotent.Status == "Bound" &&
                idempotent.RootCount == 2 &&
                idempotent.AuthoringCount == 1 &&
                idempotent.BoundCount == 0 &&
                idempotent.IdempotentCount == 1 &&
                idempotent.RejectedCount == 0,
                idempotent.Message);

            var divergentRuntime =
                new RejectingPlayerActorSelectionRuntimePort();
            LocalPlayerActorSelectionRequestAuthoringBindingResult divergent =
                LocalPlayerActorSelectionRequestAuthoringBinding.TryBind(
                    new[] { authoredRoot },
                    divergentRuntime);
            Require(
                !divergent.Succeeded &&
                divergent.Status == "RejectedAuthoringBinding" &&
                divergent.RootCount == 1 &&
                divergent.AuthoringCount == 1 &&
                divergent.BoundCount == 0 &&
                divergent.IdempotentCount == 0 &&
                divergent.RejectedCount == 1 &&
                divergentRuntime.SelectionCallCount == 0,
                divergent.Message);

            completed.Add(
                "explicit-root-binding-missing-optional-idempotent-and-divergent-cases");

            Require(
                authoring.HasPlayerActorSelectionRuntimeBinding &&
                !authoring.RuntimeReady &&
                !authoring.HasProvisioningAuthoring,
                "A bound Player Actor selection authoring without provisioning incorrectly reported runtime readiness.");
            completed.Add(
                "binding-is-distinct-from-runtime-readiness");
        }

        private static async Task<
            LocalPlayerProvisioningAuthoring>
            AwaitProvisioningAsync()
        {
            const int MaxFrames = 600;
            for (int frame = 0;
                 frame < MaxFrames;
                 frame++)
            {
                LocalPlayerProvisioningAuthoring[] candidates =
                    Resources.FindObjectsOfTypeAll<
                        LocalPlayerProvisioningAuthoring>();
                for (int index = 0;
                     index < candidates.Length;
                     index++)
                {
                    LocalPlayerProvisioningAuthoring candidate =
                        candidates[index];
                    if (candidate != null &&
                        candidate.gameObject.scene.IsValid() &&
                        candidate.gameObject.scene.isLoaded &&
                        candidate.RuntimeReady)
                    {
                        return candidate;
                    }
                }

                await Awaitable.NextFrameAsync();
            }

            throw new InvalidOperationException(
                "Timed out waiting for LocalPlayerProvisioningAuthoring RuntimeReady.");
        }

        private static PlayerSlotRuntimeSnapshot FindSlot(
            PlayerParticipationSnapshot snapshot,
            PlayerSlotId playerSlotId)
        {
            Require(
                snapshot != null,
                "Player participation snapshot is missing.");

            for (int index = 0;
                 index < snapshot.Slots.Count;
                 index++)
            {
                if (snapshot.Slots[index].PlayerSlotId ==
                    playerSlotId)
                {
                    return snapshot.Slots[index];
                }
            }

            throw new InvalidOperationException(
                $"Player Slot '{playerSlotId.StableText}' is missing from RuntimeSnapshot.");
        }

        private static GameObject CreateInactiveRoot(
            string name,
            ICollection<UnityObject> objects)
        {
            var root = new GameObject(name);
            root.SetActive(false);
            objects.Add(root);
            return root;
        }

        private static string BuildProductDiagnostic(
            LocalPlayerActorSelectionRequestAuthoring authoring,
            PlayerActorSelectionResult result,
            PlayerParticipationSnapshot snapshot)
        {
            return
                $"binding='{authoring?.PlayerActorSelectionRuntimeBindingStatus}' " +
                $"bindingDiagnostic='{authoring?.PlayerActorSelectionRuntimeBindingDiagnostic}' " +
                $"runtimeReady='{authoring?.RuntimeReady}' " +
                $"requestCount='{authoring?.RequestCount}' " +
                $"result='{result?.ToDiagnosticString()}' " +
                $"snapshotRevision='{snapshot?.Revision}' " +
                $"joined='{snapshot?.JoinedCount}' " +
                $"selected='{snapshot?.SelectedActorCount}'.";
        }

        private static object GetRequiredPropertyValue(
            object target,
            string propertyName,
            string issue)
        {
            Require(
                target != null,
                issue);

            System.Reflection.PropertyInfo property =
                target.GetType().GetProperty(
                    propertyName,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public);
            Require(
                property != null &&
                property.CanRead,
                issue);

            object value = property.GetValue(target);
            Require(
                value != null,
                issue);
            return value;
        }

        private static int GetRequiredIntPropertyValue(
            object target,
            string propertyName,
            string issue)
        {
            object value =
                GetRequiredPropertyValue(
                    target,
                    propertyName,
                    issue);
            Require(
                value is int,
                issue);
            return (int)value;
        }

        private static void Require(
            bool condition,
            string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private sealed class RejectingPlayerActorSelectionRuntimePort :
            IPlayerActorSelectionRuntimePort
        {
            internal int SelectionCallCount { get; private set; }

            public bool TryValidatePlayerActorSelectionRuntime(
                out string issue)
            {
                issue =
                    "Rejecting QA Player Actor selection runtime is intentionally unavailable.";
                return false;
            }

            public PlayerActorSelectionResult TrySelectDefaultActor(
                PlayerSlotId playerSlotId,
                int expectedSelectionRevision,
                string source,
                string reason)
            {
                SelectionCallCount++;
                throw new InvalidOperationException(
                    "Divergent QA Player Actor selection runtime must never receive a selection request.");
            }
        }
    }
}
