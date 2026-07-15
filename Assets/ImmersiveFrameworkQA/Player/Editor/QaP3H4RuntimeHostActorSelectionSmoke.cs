using System;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Play Mode integration smoke proving GameApplication policy composition, real local join,
    /// and explicit Actor-selection operations through the Session-scoped runtime-host module.
    /// </summary>
    public static class QaP3H4RuntimeHostActorSelectionSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3H.4 Run Runtime Host Actor Selection Smoke";
        private const string RuntimeAssemblyName = "Immersive.Framework.Runtime";
        private const string RuntimeHostTypeName =
            "Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost";
        private const string RuntimeModuleTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerParticipationRuntimeHostModule";
        private const string AlternateActorPath =
            "Assets/ImmersiveFrameworkQA/Player/P3H4/P3H4_AlternateActor.asset";

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var completed = new List<string>();

            try
            {
                AssertTrue(EditorApplication.isPlaying,
                    "P3H.4 runtime-host smoke must run in Play Mode.");

                LocalPlayerProvisioningAuthoring provisioning = ResolveProvisioningAuthoring();
                AssertTrue(provisioning.RuntimeReady,
                    "Local Player provisioning runtime is not ready. " + provisioning.RuntimeDiagnostic);
                completed.Add("provisioning-runtime-ready");

                Type hostType = ResolveRuntimeType(RuntimeHostTypeName);
                Type moduleType = ResolveRuntimeType(RuntimeModuleTypeName);
                Component runtimeHost = ResolveCurrentRuntimeHost(hostType);
                AssertNotNull(runtimeHost, "FrameworkRuntimeHost was not resolved.");
                Component module = ResolveSingleModule(runtimeHost, moduleType);
                completed.Add("runtime-host-module-resolved");

                GameApplicationAsset gameApplication = ResolveGameApplication(hostType, runtimeHost);
                AssertNotNull(gameApplication, "Runtime host GameApplication was not resolved.");
                AssertNotNull(gameApplication.PlayerActorSelectionPolicyProfile,
                    "GameApplication has no Actor selection policy.");
                AssertTrue(gameApplication.HasPlayerActorSelectionPolicy,
                    "GameApplication Actor selection policy is invalid.");
                completed.Add("game-application-policy-configured");

                PlayerParticipationSnapshot initial = ResolveSnapshot(moduleType, module);
                AssertSame(
                    gameApplication.PlayerActorSelectionPolicyProfile,
                    initial.ActorSelectionPolicyProfile,
                    "Session snapshot does not use the GameApplication policy reference.");
                AssertEqual(
                    PlayerActorSelectionDuplicatePolicy.UniqueAcrossJoinedSlots,
                    initial.ActorSelectionDuplicatePolicy,
                    "Unexpected active duplicate-selection policy.");
                AssertEqual(0, initial.JoinedCount,
                    "P3H.4 smoke is one-shot. Re-enter Play Mode before running it again.");
                string contextId = initial.ContextId;
                completed.Add("policy-composed-into-session-context");

                PlayerParticipationOperationResult open = provisioning.OpenJoining(
                    nameof(QaP3H4RuntimeHostActorSelectionSmoke),
                    "real-join-before-actor-selection");
                AssertTrue(open.Completed && open.Snapshot.JoiningOpen,
                    "Joining did not open. " + open.ToDiagnosticString());

                LocalPlayerJoinResult join = provisioning.RequestJoin(
                    new LocalPlayerJoinRequest(
                        nameof(QaP3H4RuntimeHostActorSelectionSmoke),
                        "real-join-before-actor-selection"));
                AssertTrue(join != null && join.Succeeded,
                    "Real local Player join failed. " + (join != null ? join.ToDiagnosticString() : "Missing result."));
                provisioning.CloseJoining(
                    nameof(QaP3H4RuntimeHostActorSelectionSmoke),
                    "join-complete");
                PlayerSlotId slotId = join.Slot.PlayerSlotId;
                completed.Add("real-player-joined-before-selection");

                PlayerParticipationSnapshot afterJoin = ResolveSnapshot(moduleType, module);
                AssertEqual(contextId, afterJoin.ContextId,
                    "Join replaced the Session participation context.");
                AssertEqual(1, afterJoin.JoinedCount,
                    "Session does not contain one Joined Slot.");
                AssertEqual(0, afterJoin.SelectedActorCount,
                    "Default Actor was applied implicitly during join.");
                AssertEqual(1, afterJoin.JoinedWithoutSelectedActorCount,
                    "Joined-unselected evidence is incorrect.");
                completed.Add("join-does-not-auto-select-default");

                PlayerActorSelectionResult selectedDefault = InvokeSelection(
                    moduleType,
                    module,
                    "TrySelectDefaultActor",
                    slotId,
                    0,
                    nameof(QaP3H4RuntimeHostActorSelectionSmoke),
                    "explicit-default-selection");
                AssertSelectionStatus(
                    selectedDefault,
                    PlayerActorSelectionStatus.SucceededSelected,
                    "Explicit default selection failed.");
                AssertTrue(selectedDefault.StateChanged,
                    "Explicit default selection did not change state.");
                AssertSame(
                    join.Slot.Profile.DefaultActorProfile,
                    selectedDefault.SelectedActorProfile,
                    "Default selection did not use PlayerSlotProfile.DefaultActorProfile.");
                completed.Add("default-applied-explicitly-through-host-module");

                PlayerParticipationSnapshot selectedSnapshot = ResolveSnapshot(moduleType, module);
                AssertEqual(1, selectedSnapshot.SelectedActorCount,
                    "Session snapshot did not count selected Actor.");
                AssertEqual(0, selectedSnapshot.JoinedWithoutSelectedActorCount,
                    "Joined-without-selection count did not clear.");
                AssertTrue(selectedSnapshot.AllJoinedSlotsHaveSelectedActors,
                    "AllJoinedSlotsHaveSelectedActors is false after explicit selection.");
                completed.Add("host-snapshot-exposes-selection");

                PlayerActorSelectionRequest idempotentRequest = new PlayerActorSelectionRequest(
                    slotId,
                    selectedDefault.SelectedActorProfile,
                    nameof(QaP3H4RuntimeHostActorSelectionSmoke),
                    "idempotent-selection",
                    selectedDefault.SelectionRevision);
                PlayerActorSelectionResult idempotent = InvokeSelection(
                    moduleType,
                    module,
                    "TrySelectActorProfile",
                    idempotentRequest);
                AssertSelectionStatus(
                    idempotent,
                    PlayerActorSelectionStatus.SucceededSelected,
                    "Idempotent host-module selection failed.");
                AssertTrue(!idempotent.StateChanged,
                    "Idempotent host-module selection changed state.");
                completed.Add("typed-select-operation-idempotent");

                ActorProfile alternate = AssetDatabase.LoadAssetAtPath<ActorProfile>(AlternateActorPath);
                AssertNotNull(alternate, "P3H.4 alternate ActorProfile asset is missing.");
                PlayerActorSelectionResult replaced = InvokeSelection(
                    moduleType,
                    module,
                    "TryReplaceActorSelection",
                    new PlayerActorSelectionRequest(
                        slotId,
                        alternate,
                        nameof(QaP3H4RuntimeHostActorSelectionSmoke),
                        "explicit-replacement",
                        idempotent.SelectionRevision));
                AssertSelectionStatus(
                    replaced,
                    PlayerActorSelectionStatus.SucceededReplaced,
                    "Host-module Actor replacement failed.");
                AssertSame(alternate, replaced.SelectedActorProfile,
                    "Replacement did not preserve alternate Actor evidence.");
                completed.Add("typed-replace-operation-succeeds");

                PlayerActorSelectionResult cleared = InvokeSelection(
                    moduleType,
                    module,
                    "TryClearActorSelection",
                    new PlayerActorSelectionRequest(
                        slotId,
                        null,
                        nameof(QaP3H4RuntimeHostActorSelectionSmoke),
                        "explicit-clear",
                        replaced.SelectionRevision));
                AssertSelectionStatus(
                    cleared,
                    PlayerActorSelectionStatus.SucceededCleared,
                    "Host-module Actor clear failed.");
                AssertTrue(cleared.Slot.IsJoined && !cleared.Slot.HasSelectedActor,
                    "Clear changed join state or retained selection.");
                completed.Add("typed-clear-preserves-joined-slot");

                PlayerActorSelectionResult reselectedDefault = InvokeSelection(
                    moduleType,
                    module,
                    "TrySelectDefaultActor",
                    slotId,
                    cleared.SelectionRevision,
                    nameof(QaP3H4RuntimeHostActorSelectionSmoke),
                    "restore-default");
                AssertSelectionStatus(
                    reselectedDefault,
                    PlayerActorSelectionStatus.SucceededSelected,
                    "Default could not be restored after clear.");
                completed.Add("default-selection-restored");

                PlayerParticipationSnapshot finalSnapshot = ResolveSnapshot(moduleType, module);
                AssertEqual(contextId, finalSnapshot.ContextId,
                    "Actor selection operations replaced the Session context.");
                AssertEqual(1, finalSnapshot.JoinedCount,
                    "Actor selection operations changed Joined count.");
                AssertEqual(1, finalSnapshot.SelectedActorCount,
                    "Final selected Actor count is incorrect.");
                AssertSame(
                    gameApplication.PlayerActorSelectionPolicyProfile,
                    finalSnapshot.ActorSelectionPolicyProfile,
                    "Final snapshot lost active policy evidence.");
                completed.Add("session-authority-remains-stable");

                Debug.Log(
                    "[P3H4_RUNTIME_HOST_ACTOR_SELECTION_SMOKE] status='Passed' " +
                    $"cases='{completed.Count}' context='{contextId}' " +
                    $"slot='{slotId.StableText}' actor='{reselectedDefault.SelectedActorProfileId.StableText}' " +
                    $"policy='{finalSnapshot.ActorSelectionDuplicatePolicy}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3H4_RUNTIME_HOST_ACTOR_SELECTION_SMOKE] status='Failed' " +
                    $"exception='{exception.GetType().Name}' message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
            }
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun()
        {
            return EditorApplication.isPlaying &&
                TryResolveProvisioningAuthoring(out LocalPlayerProvisioningAuthoring authoring) &&
                authoring.RuntimeReady &&
                authoring.RuntimeSnapshot.JoinedCount == 0;
        }

        private static PlayerActorSelectionResult InvokeSelection(
            Type moduleType,
            Component module,
            string methodName,
            params object[] arguments)
        {
            MethodInfo method = moduleType.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new MissingMethodException(moduleType.FullName, methodName);
            }

            return method.Invoke(module, arguments) as PlayerActorSelectionResult;
        }

        private static PlayerParticipationSnapshot ResolveSnapshot(Type moduleType, Component module)
        {
            MethodInfo method = moduleType.GetMethod(
                "TryGetSnapshot",
                BindingFlags.Instance | BindingFlags.NonPublic);
            object[] arguments = { null };
            bool resolved = method != null && (bool)method.Invoke(module, arguments);
            if (!resolved)
            {
                throw new InvalidOperationException(
                    "Player participation module rejected snapshot access.");
            }

            return arguments[0] as PlayerParticipationSnapshot;
        }

        private static Component ResolveSingleModule(Component runtimeHost, Type moduleType)
        {
            Component[] modules = runtimeHost.GetComponents(moduleType);
            if (modules.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Expected one Player participation module, found '{modules.Length}'.");
            }

            return modules[0];
        }

        private static Type ResolveRuntimeType(string fullName)
        {
            Type type = Type.GetType($"{fullName}, {RuntimeAssemblyName}");
            if (type == null)
            {
                throw new InvalidOperationException($"Runtime type '{fullName}' was not found.");
            }

            return type;
        }

        private static Component ResolveCurrentRuntimeHost(Type hostType)
        {
            MethodInfo tryGetCurrent = hostType.GetMethod(
                "TryGetCurrent",
                BindingFlags.Static | BindingFlags.NonPublic);
            object[] arguments = { null };
            bool resolved = tryGetCurrent != null &&
                (bool)tryGetCurrent.Invoke(null, arguments);
            return resolved ? arguments[0] as Component : null;
        }

        private static GameApplicationAsset ResolveGameApplication(
            Type hostType,
            Component runtimeHost)
        {
            FieldInfo field = hostType.GetField(
                "_gameApplication",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return field != null
                ? field.GetValue(runtimeHost) as GameApplicationAsset
                : null;
        }

        private static LocalPlayerProvisioningAuthoring ResolveProvisioningAuthoring()
        {
            if (!TryResolveProvisioningAuthoring(out LocalPlayerProvisioningAuthoring authoring))
            {
                throw new InvalidOperationException(
                    "Expected exactly one loaded LocalPlayerProvisioningAuthoring.");
            }

            return authoring;
        }

        private static bool TryResolveProvisioningAuthoring(
            out LocalPlayerProvisioningAuthoring authoring)
        {
            authoring = null;
            LocalPlayerProvisioningAuthoring[] candidates =
                UnityEngine.Object.FindObjectsByType<LocalPlayerProvisioningAuthoring>(
                    FindObjectsInactive.Include);
            int loadedCount = 0;
            for (int index = 0; index < candidates.Length; index++)
            {
                LocalPlayerProvisioningAuthoring candidate = candidates[index];
                if (candidate == null ||
                    !candidate.gameObject.scene.IsValid() ||
                    !candidate.gameObject.scene.isLoaded)
                {
                    continue;
                }

                loadedCount++;
                authoring = candidate;
            }

            if (loadedCount == 1)
            {
                return true;
            }

            authoring = null;
            return false;
        }

        private static void AssertSelectionStatus(
            PlayerActorSelectionResult result,
            PlayerActorSelectionStatus expected,
            string message)
        {
            AssertNotNull(result, message + " Missing result.");
            AssertEqual(expected, result.Status,
                message + " " + result.ToDiagnosticString());
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertNotNull(object value, string message)
        {
            if (value == null)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertSame(object expected, object actual, string message)
        {
            if (!ReferenceEquals(expected, actual))
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    $"{message} expected='{expected}' actual='{actual}'.");
            }
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
        }
    }
}
