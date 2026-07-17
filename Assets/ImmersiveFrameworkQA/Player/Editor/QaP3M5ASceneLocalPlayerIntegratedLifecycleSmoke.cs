using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// One-shot Play Mode proof that the real FrameworkRuntimeHost Activity request path loads,
    /// admits, adopts, releases and re-enters an ExternalSceneOwned local Player without calling
    /// the lifecycle participant directly.
    /// </summary>
    public static class QaP3M5ASceneLocalPlayerIntegratedLifecycleSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3M5A Run Scene Local Player Integrated Lifecycle Smoke";
        private const string RuntimeHostTypeName =
            "Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost";
        private const string PreparationModuleTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerActorPreparationRuntimeHostModule";
        private const string SceneAdmissionModuleTypeName =
            "Immersive.Framework.PlayerParticipation.SceneLocalPlayerAdmissionRuntimeHostModule";

        private static readonly BindingFlags InstanceAny =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly BindingFlags StaticAny =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private readonly struct LoadedFixture
        {
            internal LoadedFixture(
                Scene scene,
                SceneLocalPlayerAdmissionAuthoring authoring)
            {
                Scene = scene;
                Authoring = authoring;
            }

            internal Scene Scene { get; }
            internal SceneLocalPlayerAdmissionAuthoring Authoring { get; }
            internal LocalPlayerHostAuthoring Host => Authoring.LocalPlayerHost;
            internal PlayerActorDeclaration Actor =>
                Authoring.SceneLogicalPlayerActor;
            internal PlayerSlotProfile SlotProfile =>
                Authoring.PlayerSlotProfile;
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun()
        {
            return EditorApplication.isPlaying;
        }

        [MenuItem(MenuPath)]
        public static async void Run()
        {
            var completed = new List<string>();
            Exception failure = null;
            object runtimeHost = null;
            ActivityAsset originalActivity = null;

            try
            {
                AssertTrue(EditorApplication.isPlaying,
                    "P3M5A integrated lifecycle smoke must run in Play Mode.");
                completed.Add("play-mode-required");

                ActivityAsset preparedActivity = LoadActivity(
                    QaP3M5ASceneLocalPlayerIntegratedLifecycleSetup.PreparedActivityPath);
                ActivityAsset noPlayersActivity = LoadActivity(
                    QaP3M5ASceneLocalPlayerIntegratedLifecycleSetup.NoPlayersActivityPath);
                ActivityAsset gameplayActivity = LoadActivity(
                    QaP3M5ASceneLocalPlayerIntegratedLifecycleSetup.GameplayActivityPath);
                completed.Add("fixture-assets-resolved");

                runtimeHost = await AwaitRuntimeHostAsync();
                originalActivity = ResolveCurrentActivity(runtimeHost);
                object preparationModule = ResolveHostComponent(
                    runtimeHost,
                    PreparationModuleTypeName,
                    "Player Actor preparation module");
                object sceneAdmissionModule = ResolveHostComponent(
                    runtimeHost,
                    SceneAdmissionModuleTypeName,
                    "Scene Local Player admission module");
                AssertTrue(GetBooleanProperty(preparationModule, "IsReady"),
                    "Player Actor preparation module is not ready.");
                AssertTrue(GetBooleanProperty(sceneAdmissionModule, "IsReady"),
                    "Scene Local Player admission module is not ready.");
                completed.Add("official-runtime-authorities-ready");

                object participationContext = GetFieldValue(
                    sceneAdmissionModule,
                    "participationContext");
                AssertNotNull(participationContext,
                    "Scene admission module has no Session participation context.");
                object runtimeContent = GetPropertyValue(
                    runtimeHost,
                    "RuntimeContentRuntime");
                AssertNotNull(runtimeContent,
                    "FrameworkRuntimeHost has no RuntimeContentRuntime.");

                PlayerParticipationSnapshot initial =
                    CreateParticipationSnapshot(participationContext);
                AssertEqual(0, initial.JoinedCount,
                    "P3M5A is one-shot. Re-enter Play Mode before running again.");
                AssertEqual(0, initial.SelectedActorCount,
                    "P3M5A requires a clean Session Actor selection state.");
                completed.Add("session-initially-clean");

                object firstRequest = await RequestActivityAsync(
                    runtimeHost,
                    preparedActivity,
                    "first-real-scene-player-enter");
                AssertRequestSucceeded(firstRequest,
                    "First Scene Local Player Activity request failed.");
                AssertSame(preparedActivity, ResolveCurrentActivity(runtimeHost),
                    "Prepared Scene Player Activity did not become current.");
                completed.Add("real-activity-request-entered");

                LoadedFixture firstFixture = await AwaitLoadedFixtureAsync(
                    runtimeHost,
                    sceneAdmissionModule);
                AssertTrue(firstFixture.Authoring.RuntimeReady,
                    "Loaded Scene Local Player surface is not runtime-bound. " +
                    firstFixture.Authoring.RuntimeDiagnostic);
                AssertTrue(firstFixture.Authoring.HasActiveAdmission,
                    "Real Activity enter did not retain an active Scene admission token.");
                AssertTrue(firstFixture.Authoring.TryValidateRuntimeEvidence(
                        out string firstEvidenceIssue),
                    "Loaded Scene Local Player authoring is invalid. " +
                    firstEvidenceIssue);
                completed.Add("activity-scene-surface-bound");

                PlayerSlotId slotId = firstFixture.SlotProfile.PlayerSlotId;
                PlayerParticipationSnapshot firstEntered =
                    CreateParticipationSnapshot(participationContext);
                PlayerSlotRuntimeSnapshot firstSlot = FindSlot(firstEntered, slotId);
                AssertTrue(firstSlot.IsJoined,
                    "Real Activity enter did not join the authored Player Slot.");
                AssertTrue(firstSlot.HasSelectedActor,
                    "Real Activity enter did not select the authored Actor Profile.");
                AssertSame(firstFixture.Authoring.ActorProfile,
                    firstSlot.SelectedActorProfile,
                    "Session selection differs from the Scene surface Actor Profile.");
                AssertTrue(firstFixture.Host.IsJoined &&
                    firstFixture.Host.JoinedPlayerSlotId == slotId,
                    "Scene Local Player Host did not commit the exact Slot.");
                AssertFalse(firstEntered.JoiningOpen,
                    "Scene-authorized admission changed public joining policy.");
                completed.Add("slot-host-selection-committed");

                PlayerActorPreparationSummary firstPreparation =
                    GetPreparationSummary(preparationModule, slotId);
                ScenePlayerActorAdoptionToken firstAdoption =
                    GetAdoptionToken(preparationModule, slotId);
                AssertTrue(firstPreparation.IsPrepared &&
                    firstPreparation.Token.IsValid,
                    firstPreparation.ToDiagnosticString());
                AssertTrue(firstAdoption.IsValid,
                    "Real Activity enter produced no Scene Actor adoption token.");
                AssertEqual(
                    PlayerActorPhysicalOwnership.ExternalSceneOwned,
                    firstAdoption.PhysicalOwnership,
                    "Real Activity enter changed Scene Actor physical ownership.");
                AssertEqual(firstPreparation.Token,
                    firstAdoption.PreparationToken,
                    "Preparation and adoption tokens differ.");
                AssertTrue(firstFixture.Actor.HasPlayerInputEvidence &&
                    ReferenceEquals(
                        firstFixture.Actor.PlayerInput,
                        firstFixture.Host.PlayerInput),
                    "Adopted Scene Actor has no exact Host PlayerInput evidence.");
                AssertEqual(1,
                    firstFixture.Host.ActorMount
                        .GetComponentsInChildren<PlayerActorDeclaration>(true)
                        .Length,
                    "Real Activity enter duplicated the Scene Logical Player Actor.");
                completed.Add("external-actor-adopted-canonically");

                RuntimeContentOwner firstOwner = RuntimeContentOwner.Activity(
                    preparedActivity.ActivityName,
                    preparedActivity.ActivityName);
                AssertEqual(1, CountRuntimeRoots(runtimeContent, firstOwner),
                    "Prepared Activity RuntimeContent owner root is missing.");
                completed.Add("activity-runtime-owner-authoritative");

                object switchToNoPlayers = await RequestActivityAsync(
                    runtimeHost,
                    noPlayersActivity,
                    "release-first-scene-player");
                AssertRequestSucceeded(switchToNoPlayers,
                    "Switch to No Players Activity failed.");
                AssertSame(noPlayersActivity, ResolveCurrentActivity(runtimeHost),
                    "No Players Activity did not become current.");
                await AwaitSceneUnloadedAsync();
                completed.Add("same-route-exit-completed");

                AssertReleasedState(
                    participationContext,
                    preparationModule,
                    runtimeContent,
                    firstOwner,
                    slotId);
                completed.Add("reverse-release-left-no-residue");

                object secondRequest = await RequestActivityAsync(
                    runtimeHost,
                    preparedActivity,
                    "second-real-scene-player-enter");
                AssertRequestSucceeded(secondRequest,
                    "Second Scene Local Player Activity request failed.");
                LoadedFixture secondFixture = await AwaitLoadedFixtureAsync(
                    runtimeHost,
                    sceneAdmissionModule);
                PlayerActorPreparationSummary secondPreparation =
                    GetPreparationSummary(preparationModule, slotId);
                ScenePlayerActorAdoptionToken secondAdoption =
                    GetAdoptionToken(preparationModule, slotId);
                AssertTrue(secondPreparation.IsPrepared &&
                    secondAdoption.IsValid,
                    "Second real Activity enter did not prepare and adopt the Scene Actor.");
                AssertFalse(firstAdoption.Equals(secondAdoption),
                    "Re-entry reused the stale Scene Actor adoption token.");
                AssertFalse(firstPreparation.Token.Equals(
                        secondPreparation.Token),
                    "Re-entry reused the stale preparation token.");
                completed.Add("reentry-created-new-identities");

                ScenePlayerActorAdoptionResult staleRelease =
                    ReleaseSceneActorAdoption(
                        preparationModule,
                        secondFixture.Authoring,
                        firstAdoption,
                        "stale-first-entry-token");
                AssertNotNull(staleRelease,
                    "Stale Scene Actor release returned no result.");
                AssertEqual(
                    ScenePlayerActorAdoptionStatus.RejectedForeignOrStaleAdoption,
                    staleRelease.Status,
                    "Re-entry accepted the previous Activity adoption token.");
                AssertEqual(secondAdoption,
                    GetAdoptionToken(preparationModule, slotId),
                    "Stale token rejection changed the current adoption.");
                AssertTrue(GetPreparationSummary(
                        preparationModule,
                        slotId).IsPrepared,
                    "Stale token rejection changed current preparation.");
                completed.Add("stale-reentry-token-rejected");

                object secondExit = await RequestActivityAsync(
                    runtimeHost,
                    noPlayersActivity,
                    "release-second-scene-player");
                AssertRequestSucceeded(secondExit,
                    "Second switch to No Players Activity failed.");
                await AwaitSceneUnloadedAsync();
                AssertReleasedState(
                    participationContext,
                    preparationModule,
                    runtimeContent,
                    firstOwner,
                    slotId);
                completed.Add("second-exit-clean");

                object gameplayRequest = await RequestActivityAsync(
                    runtimeHost,
                    gameplayActivity,
                    "gameplay-ready-negative-integration");
                AssertFalse(GetBooleanProperty(gameplayRequest, "Succeeded"),
                    "Bare Scene Player unexpectedly satisfied GameplayReady.");
                string gameplayMessage = GetStringProperty(
                    gameplayRequest,
                    "Message");
                AssertTrue(!string.IsNullOrWhiteSpace(gameplayMessage),
                    "Failed GameplayReady request returned no diagnostic message.");
                AssertTrue(
                    gameplayMessage.IndexOf(
                        "canonical-player-enter-failed",
                        StringComparison.OrdinalIgnoreCase) >= 0 ||
                    gameplayMessage.IndexOf(
                        "Canonical Player Activity enter failed",
                        StringComparison.OrdinalIgnoreCase) >= 0,
                    "GameplayReady failed before the canonical Player pipeline. " +
                    gameplayMessage);
                completed.Add("gameplay-ready-reached-canonical-pipeline");
                await AwaitSceneUnloadedAsync();
                AssertSame(noPlayersActivity, ResolveCurrentActivity(runtimeHost),
                    "Failed GameplayReady request replaced the current Activity.");
                RuntimeContentOwner gameplayOwner = RuntimeContentOwner.Activity(
                    gameplayActivity.ActivityName,
                    gameplayActivity.ActivityName);
                AssertReleasedState(
                    participationContext,
                    preparationModule,
                    runtimeContent,
                    gameplayOwner,
                    slotId);
                completed.Add("gameplay-ready-failure-rolled-back");

                AssertFalse(TryResolveLoadedFixture(out _),
                    "Failed target Activity retained its Scene Local Player scene.");
                completed.Add("failed-target-scene-released");
            }
            catch (Exception exception)
            {
                failure = Unwrap(exception);
            }

            try
            {
                if (runtimeHost != null)
                {
                    ActivityAsset current = ResolveCurrentActivity(runtimeHost);
                    if (originalActivity != null &&
                        !ReferenceEquals(current, originalActivity))
                    {
                        object restore = await RequestActivityAsync(
                            runtimeHost,
                            originalActivity,
                            "p3m5a-restore-original-activity");
                        if (!GetBooleanProperty(restore, "Succeeded"))
                        {
                            throw new InvalidOperationException(
                                "Could not restore the original Activity. " +
                                GetStringProperty(restore, "Message"));
                        }
                    }
                    else if (originalActivity == null && current != null)
                    {
                        object clear = await ClearActivityAsync(
                            runtimeHost,
                            "p3m5a-restore-no-activity");
                        if (!GetBooleanProperty(clear, "Succeeded"))
                        {
                            throw new InvalidOperationException(
                                "Could not restore the original empty Activity state. " +
                                GetStringProperty(clear, "Message"));
                        }
                    }
                }
            }
            catch (Exception cleanupException)
            {
                Exception actualCleanup = Unwrap(cleanupException);
                failure = failure == null
                    ? new InvalidOperationException(
                        "P3M5A smoke cleanup failed. " +
                        actualCleanup.Message,
                        actualCleanup)
                    : new AggregateException(
                        "P3M5A smoke execution and cleanup both failed.",
                        failure,
                        actualCleanup);
            }

            if (failure != null)
            {
                Debug.LogError(
                    "[P3M5A_SCENE_LOCAL_PLAYER_INTEGRATED_LIFECYCLE_SMOKE] " +
                    $"status='Failed' exception='{failure.GetType().Name}' " +
                    $"message='{Escape(failure.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw failure;
            }

            Debug.Log(
                "[P3M5A_SCENE_LOCAL_PLAYER_INTEGRATED_LIFECYCLE_SMOKE] " +
                $"status='Passed' cases='{completed.Count}' " +
                $"completed='{string.Join(",", completed)}'.");
        }

        private static ActivityAsset LoadActivity(string path)
        {
            ActivityAsset activity =
                AssetDatabase.LoadAssetAtPath<ActivityAsset>(path);
            AssertNotNull(activity,
                $"Missing P3M5A Activity asset '{path}'. Apply the fixture outside Play Mode.");
            return activity;
        }

        private static async Task<object> AwaitRuntimeHostAsync()
        {
            Type runtimeHostType = typeof(ImmersiveFrameworkSettingsAsset)
                .Assembly
                .GetType(RuntimeHostTypeName, true);
            FieldInfo currentField = runtimeHostType.GetField(
                "_current",
                StaticAny);
            AssertNotNull(currentField,
                "FrameworkRuntimeHost current field is unavailable.");

            for (int frame = 0; frame < 300; frame++)
            {
                object current = currentField.GetValue(null);
                if (current != null)
                {
                    object state = GetPropertyValue(current, "State");
                    if (state != null &&
                        GetBooleanProperty(state, "GameFlowStarted"))
                    {
                        Type preparationType = typeof(PlayerParticipationSnapshot)
                            .Assembly
                            .GetType(PreparationModuleTypeName, false);
                        Type sceneType = typeof(PlayerParticipationSnapshot)
                            .Assembly
                            .GetType(SceneAdmissionModuleTypeName, false);
                        Component hostComponent = current as Component;
                        if (hostComponent != null &&
                            preparationType != null &&
                            sceneType != null)
                        {
                            Component preparation =
                                hostComponent.GetComponent(preparationType);
                            Component scene = hostComponent.GetComponent(sceneType);
                            if (preparation != null && scene != null &&
                                GetBooleanProperty(preparation, "IsReady") &&
                                GetBooleanProperty(scene, "IsReady"))
                            {
                                return current;
                            }
                        }
                    }
                }

                await Awaitable.NextFrameAsync();
            }

            throw new InvalidOperationException(
                "FrameworkRuntimeHost did not become ready within 300 frames.");
        }

        private static object ResolveHostComponent(
            object runtimeHost,
            string typeName,
            string label)
        {
            Type componentType = typeof(PlayerParticipationSnapshot)
                .Assembly
                .GetType(typeName, true);
            Component hostComponent = runtimeHost as Component;
            AssertNotNull(hostComponent,
                "FrameworkRuntimeHost is not a Unity Component.");
            Component component = hostComponent.GetComponent(componentType);
            AssertNotNull(component,
                $"{label} is not attached to FrameworkRuntimeHost.");
            return component;
        }

        private static ActivityAsset ResolveCurrentActivity(object runtimeHost)
        {
            object state = GetPropertyValue(runtimeHost, "State");
            return state == null
                ? null
                : GetPropertyValue(state, "CurrentActivity") as ActivityAsset;
        }

        private static async Task<object> RequestActivityAsync(
            object runtimeHost,
            ActivityAsset activity,
            string reason)
        {
            return await InvokeTaskResultAsync(
                runtimeHost,
                "RequestActivityAsync",
                activity,
                nameof(QaP3M5ASceneLocalPlayerIntegratedLifecycleSmoke),
                reason);
        }

        private static async Task<object> ClearActivityAsync(
            object runtimeHost,
            string reason)
        {
            return await InvokeTaskResultAsync(
                runtimeHost,
                "ClearActivityAsync",
                nameof(QaP3M5ASceneLocalPlayerIntegratedLifecycleSmoke),
                reason);
        }

        private static async Task<object> InvokeTaskResultAsync(
            object target,
            string methodName,
            params object[] arguments)
        {
            MethodInfo method = FindMethod(
                target.GetType(),
                methodName,
                arguments.Length);
            AssertNotNull(method,
                $"Missing method '{methodName}' with '{arguments.Length}' arguments on '{target.GetType().Name}'.");
            object invocation = method.Invoke(target, arguments);
            Task task = invocation as Task;
            AssertNotNull(task,
                $"Method '{methodName}' did not return a Task.");
            await task;
            PropertyInfo resultProperty = invocation.GetType().GetProperty(
                "Result",
                InstanceAny);
            AssertNotNull(resultProperty,
                $"Task returned by '{methodName}' has no Result property.");
            return resultProperty.GetValue(invocation);
        }

        private static MethodInfo FindMethod(
            Type type,
            string methodName,
            int argumentCount)
        {
            MethodInfo[] methods = type.GetMethods(InstanceAny);
            for (int index = 0; index < methods.Length; index++)
            {
                if (string.Equals(
                        methods[index].Name,
                        methodName,
                        StringComparison.Ordinal) &&
                    methods[index].GetParameters().Length == argumentCount)
                {
                    return methods[index];
                }
            }

            return null;
        }

        private static void AssertRequestSucceeded(
            object requestResult,
            string message)
        {
            AssertNotNull(requestResult,
                message + " No request result was returned.");
            AssertTrue(GetBooleanProperty(requestResult, "Succeeded"),
                message + " " + GetStringProperty(requestResult, "Message"));
        }

        private static async Task<LoadedFixture> AwaitLoadedFixtureAsync(
            object runtimeHost,
            object sceneAdmissionModule)
        {
            for (int frame = 0; frame < 180; frame++)
            {
                if (TryResolveLoadedFixture(out LoadedFixture fixture) &&
                    fixture.Authoring.RuntimeReady &&
                    fixture.Authoring.HasActiveAdmission)
                {
                    return fixture;
                }

                await Awaitable.NextFrameAsync();
            }

            throw new InvalidOperationException(
                "P3M5A Activity scene did not load and admit its Scene Local Player " +
                "within 180 frames. " +
                BuildLoadedFixtureTimeoutDiagnostic(
                    runtimeHost,
                    sceneAdmissionModule));
        }

        private static string BuildLoadedFixtureTimeoutDiagnostic(
            object runtimeHost,
            object sceneAdmissionModule)
        {
            Scene scene = SceneManager.GetSceneByPath(
                QaP3M5ASceneLocalPlayerIntegratedLifecycleSetup.ScenePath);
            bool sceneLoaded = scene.IsValid() && scene.isLoaded;
            var surfaces = new List<SceneLocalPlayerAdmissionAuthoring>();
            if (sceneLoaded)
            {
                GameObject[] roots = scene.GetRootGameObjects();
                for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                {
                    surfaces.AddRange(
                        roots[rootIndex]
                            .GetComponentsInChildren<
                                SceneLocalPlayerAdmissionAuthoring>(true));
                }
            }

            object state = runtimeHost != null
                ? GetPropertyValue(runtimeHost, "State")
                : null;
            string activityName = state != null
                ? Convert.ToString(GetPropertyValue(state, "CurrentActivityName"))
                : string.Empty;
            bool activityReady = state != null &&
                GetBooleanProperty(state, "IsActivityReady");
            object readiness = state != null
                ? GetPropertyValue(state, "ActivityReadinessState")
                : null;
            object lifecycle = state != null
                ? GetPropertyValue(state, "ActivityContentLifecycleResult")
                : null;

            string surfaceDiagnostic = surfaces.Count == 1 && surfaces[0] != null
                ? $"runtimeReady='{surfaces[0].RuntimeReady}' " +
                  $"activeAdmission='{surfaces[0].HasActiveAdmission}' " +
                  $"runtimeDiagnostic='{Escape(surfaces[0].RuntimeDiagnostic)}' " +
                  $"lastAdmission='{Escape(ToDiagnosticText(surfaces[0].LastRuntimeResult))}' " +
                  $"lastAdoption='{Escape(ToDiagnosticText(surfaces[0].LastActorAdoptionResult))}'"
                : "surface-detail='<unavailable>'";

            return
                $"sceneLoaded='{sceneLoaded}' surfaces='{surfaces.Count}' " +
                $"boundAuthoring='{GetPropertyValue(sceneAdmissionModule, "BoundAuthoringCount")}' " +
                $"activeAdmissions='{GetPropertyValue(sceneAdmissionModule, "ActiveAdmissionCount")}' " +
                $"moduleDiagnostic='{Escape(Convert.ToString(GetPropertyValue(sceneAdmissionModule, "Diagnostic")))}' " +
                $"currentActivity='{Escape(activityName)}' activityReady='{activityReady}' " +
                $"readiness='{Escape(ToDiagnosticText(readiness))}' " +
                $"lifecycle='{Escape(ToDiagnosticText(lifecycle))}' " +
                surfaceDiagnostic + ".";
        }

        private static string ToDiagnosticText(object value)
        {
            if (value == null)
            {
                return "<none>";
            }

            MethodInfo diagnosticMethod = value.GetType().GetMethod(
                "ToDiagnosticString",
                InstanceAny,
                null,
                Type.EmptyTypes,
                null);
            if (diagnosticMethod != null &&
                diagnosticMethod.ReturnType == typeof(string))
            {
                return Convert.ToString(
                    diagnosticMethod.Invoke(value, null));
            }

            return Convert.ToString(value);
        }

        private static async Task AwaitSceneUnloadedAsync()
        {
            for (int frame = 0; frame < 180; frame++)
            {
                Scene scene = SceneManager.GetSceneByPath(
                    QaP3M5ASceneLocalPlayerIntegratedLifecycleSetup.ScenePath);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    return;
                }

                await Awaitable.NextFrameAsync();
            }

            throw new InvalidOperationException(
                "P3M5A Activity scene remained loaded after Activity release.");
        }

        private static bool TryResolveLoadedFixture(
            out LoadedFixture fixture)
        {
            fixture = default;
            Scene scene = SceneManager.GetSceneByPath(
                QaP3M5ASceneLocalPlayerIntegratedLifecycleSetup.ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return false;
            }

            var surfaces = new List<SceneLocalPlayerAdmissionAuthoring>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                surfaces.AddRange(
                    roots[rootIndex]
                        .GetComponentsInChildren<
                            SceneLocalPlayerAdmissionAuthoring>(true));
            }

            if (surfaces.Count != 1 || surfaces[0] == null)
            {
                return false;
            }

            fixture = new LoadedFixture(scene, surfaces[0]);
            return true;
        }

        private static PlayerParticipationSnapshot
            CreateParticipationSnapshot(object context)
        {
            MethodInfo create = context.GetType().GetMethod(
                "CreateSnapshot",
                InstanceAny);
            AssertNotNull(create,
                "Player participation context has no CreateSnapshot method.");
            return (PlayerParticipationSnapshot)create.Invoke(context, null);
        }

        private static PlayerSlotRuntimeSnapshot FindSlot(
            PlayerParticipationSnapshot snapshot,
            PlayerSlotId playerSlotId)
        {
            for (int index = 0; index < snapshot.Slots.Count; index++)
            {
                if (snapshot.Slots[index].PlayerSlotId == playerSlotId)
                {
                    return snapshot.Slots[index];
                }
            }

            throw new InvalidOperationException(
                $"Player Slot '{playerSlotId.StableText}' is not configured in the Session snapshot.");
        }

        private static PlayerActorPreparationSummary GetPreparationSummary(
            object preparationModule,
            PlayerSlotId playerSlotId)
        {
            MethodInfo get = preparationModule.GetType().GetMethod(
                "TryGetScenePlayerActorPreparationSummary",
                InstanceAny);
            AssertNotNull(get,
                "Preparation module has no Scene Player preparation summary operation.");
            object[] arguments = { playerSlotId, null };
            bool found = (bool)get.Invoke(preparationModule, arguments);
            AssertTrue(found,
                $"No preparation summary exists for Slot '{playerSlotId.StableText}'.");
            return (PlayerActorPreparationSummary)arguments[1];
        }

        private static bool TryGetAdoptionToken(
            object preparationModule,
            PlayerSlotId playerSlotId,
            out ScenePlayerActorAdoptionToken token)
        {
            MethodInfo get = preparationModule.GetType().GetMethod(
                "TryGetScenePlayerActorAdoption",
                InstanceAny);
            AssertNotNull(get,
                "Preparation module has no Scene Player adoption lookup.");
            object[] arguments = { playerSlotId, null };
            bool found = (bool)get.Invoke(preparationModule, arguments);
            token = found
                ? (ScenePlayerActorAdoptionToken)arguments[1]
                : default;
            return found;
        }

        private static ScenePlayerActorAdoptionToken GetAdoptionToken(
            object preparationModule,
            PlayerSlotId playerSlotId)
        {
            AssertTrue(TryGetAdoptionToken(
                    preparationModule,
                    playerSlotId,
                    out ScenePlayerActorAdoptionToken token),
                $"No Scene Player adoption exists for Slot '{playerSlotId.StableText}'.");
            return token;
        }

        private static ScenePlayerActorAdoptionResult
            ReleaseSceneActorAdoption(
                object preparationModule,
                SceneLocalPlayerAdmissionAuthoring authoring,
                ScenePlayerActorAdoptionToken token,
                string reason)
        {
            MethodInfo release = preparationModule.GetType().GetMethod(
                "TryReleaseSceneLocalPlayerActor",
                InstanceAny);
            AssertNotNull(release,
                "Preparation module has no Scene Player adoption release operation.");
            return (ScenePlayerActorAdoptionResult)release.Invoke(
                preparationModule,
                new object[]
                {
                    authoring,
                    token,
                    nameof(QaP3M5ASceneLocalPlayerIntegratedLifecycleSmoke),
                    reason
                });
        }

        private static void AssertReleasedState(
            object participationContext,
            object preparationModule,
            object runtimeContent,
            RuntimeContentOwner releasedOwner,
            PlayerSlotId playerSlotId)
        {
            PlayerParticipationSnapshot snapshot =
                CreateParticipationSnapshot(participationContext);
            PlayerSlotRuntimeSnapshot slot = FindSlot(snapshot, playerSlotId);
            AssertFalse(slot.IsJoined,
                "Released Scene Local Player Slot remains Joined.");
            AssertFalse(slot.HasSelectedActor,
                "Released Scene Local Player Slot retains Actor selection.");
            AssertEqual(0, snapshot.ReservedCount,
                "Released Scene Local Player transaction stranded a Reserved Slot.");
            AssertEqual(0, snapshot.LeavingCount,
                "Released Scene Local Player transaction stranded a Leaving Slot.");

            PlayerActorPreparationSummary preparation =
                GetPreparationSummary(preparationModule, playerSlotId);
            AssertTrue(preparation.IsUnprepared,
                "Released Scene Local Player retains canonical preparation. " +
                preparation.ToDiagnosticString());
            AssertFalse(TryGetAdoptionToken(
                    preparationModule,
                    playerSlotId,
                    out _),
                "Released Scene Local Player retains an adoption token.");
            AssertEqual(0, CountRuntimeRoots(runtimeContent, releasedOwner),
                $"Released Activity owner '{releasedOwner.StableText}' retains a RuntimeContent root.");
        }

        private static int CountRuntimeRoots(
            object runtimeContent,
            RuntimeContentOwner owner)
        {
            MethodInfo snapshot = runtimeContent.GetType().GetMethod(
                "SnapshotRoots",
                InstanceAny,
                null,
                Type.EmptyTypes,
                null);
            AssertNotNull(snapshot,
                "RuntimeContentRuntime has no parameterless SnapshotRoots method.");
            object result = snapshot.Invoke(runtimeContent, null);
            IEnumerable roots = result as IEnumerable;
            AssertNotNull(roots,
                "RuntimeContentRuntime SnapshotRoots returned no enumerable result.");

            int count = 0;
            foreach (object root in roots)
            {
                object rootOwner = GetPropertyValue(root, "Owner");
                if (rootOwner is RuntimeContentOwner typedOwner &&
                    typedOwner == owner)
                {
                    count++;
                }
            }

            return count;
        }

        private static object GetFieldValue(object target, string fieldName)
        {
            if (target == null)
            {
                return null;
            }

            FieldInfo field = target.GetType().GetField(
                fieldName,
                InstanceAny);
            AssertNotNull(field,
                $"Missing field '{fieldName}' on '{target.GetType().Name}'.");
            return field.GetValue(target);
        }

        private static object GetPropertyValue(
            object target,
            string propertyName)
        {
            if (target == null)
            {
                return null;
            }

            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                InstanceAny);
            AssertNotNull(property,
                $"Missing property '{propertyName}' on '{target.GetType().Name}'.");
            return property.GetValue(target);
        }

        private static bool GetBooleanProperty(
            object target,
            string propertyName)
        {
            object value = GetPropertyValue(target, propertyName);
            return value is bool boolean && boolean;
        }

        private static string GetStringProperty(
            object target,
            string propertyName)
        {
            object value = GetPropertyValue(target, propertyName);
            return value as string ?? string.Empty;
        }

        private static Exception Unwrap(Exception exception)
        {
            while (exception is TargetInvocationException invocation &&
                   invocation.InnerException != null)
            {
                exception = invocation.InnerException;
            }

            return exception;
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertFalse(bool condition, string message)
        {
            AssertTrue(!condition, message);
        }

        private static void AssertNotNull(object value, string message)
        {
            AssertTrue(value != null, message);
        }

        private static void AssertSame(
            object expected,
            object actual,
            string message)
        {
            AssertTrue(ReferenceEquals(expected, actual), message);
        }

        private static void AssertEqual<T>(
            T expected,
            T actual,
            string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    $"{message} Expected='{expected}' Actual='{actual}'.");
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
