using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
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
    /// One-shot Play Mode proof for Scene Local Player Route transition, Route re-entry,
    /// reverse cleanup and the automatic-authoring negative matrix.
    /// </summary>
    public static class QaP3M5BRouteTransitionAndNegativeMatrixSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3M5B Run Route Transition and Negative Matrix Smoke";
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

        private readonly struct LoadedPlayerFixture
        {
            internal LoadedPlayerFixture(
                Scene scene,
                SceneLocalPlayerAdmissionAuthoring authoring,
                ScenePlayerActorAdoptionToken adoption,
                PlayerActorPreparationSummary preparation)
            {
                Scene = scene;
                Authoring = authoring;
                Adoption = adoption;
                Preparation = preparation;
            }

            internal Scene Scene { get; }
            internal SceneLocalPlayerAdmissionAuthoring Authoring { get; }
            internal ScenePlayerActorAdoptionToken Adoption { get; }
            internal PlayerActorPreparationSummary Preparation { get; }
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
            var loadedNegativeScenes = new List<string>();
            Exception failure = null;
            object runtimeHost = null;
            object preparationModule = null;
            object sceneAdmissionModule = null;
            object participationContext = null;
            object runtimeContent = null;
            RouteAsset originalRoute = null;

            RouteAsset routeA = null;
            RouteAsset routeB = null;
            ActivityAsset routeAActivity = null;
            ActivityAsset routeBActivity = null;
            PlayerSlotId slotId = default;
            RuntimeContentOwner routeAOwner = default;
            RuntimeContentOwner routeBOwner = default;

            try
            {
                AssertTrue(EditorApplication.isPlaying,
                    "P3M5B smoke must run in Play Mode.");
                completed.Add("play-mode-required");

                routeA = LoadAsset<RouteAsset>(
                    QaP3M5BRouteTransitionAndNegativeMatrixSetup.RouteAPath);
                routeB = LoadAsset<RouteAsset>(
                    QaP3M5BRouteTransitionAndNegativeMatrixSetup.RouteBPath);
                routeAActivity = LoadAsset<ActivityAsset>(
                    QaP3M5BRouteTransitionAndNegativeMatrixSetup.RouteAActivityPath);
                routeBActivity = LoadAsset<ActivityAsset>(
                    QaP3M5BRouteTransitionAndNegativeMatrixSetup.RouteBActivityPath);
                ActivityAsset duplicateActivity = LoadAsset<ActivityAsset>(
                    QaP3M5BRouteTransitionAndNegativeMatrixSetup.DuplicateSlotActivityPath);
                ActivityAsset missingActorActivity = LoadAsset<ActivityAsset>(
                    QaP3M5BRouteTransitionAndNegativeMatrixSetup.MissingActorActivityPath);
                ActivityAsset mismatchedProfileActivity = LoadAsset<ActivityAsset>(
                    QaP3M5BRouteTransitionAndNegativeMatrixSetup.MismatchedProfileActivityPath);
                ActivityAsset reusedHostActivity = LoadAsset<ActivityAsset>(
                    QaP3M5BRouteTransitionAndNegativeMatrixSetup.ReusedHostActivityPath);
                ActivityAsset undeclaredActivity = LoadAsset<ActivityAsset>(
                    QaP3M5BRouteTransitionAndNegativeMatrixSetup.UndeclaredSurfaceActivityPath);
                PlayerSlotProfile firstSlot = ResolveFirstConfiguredSlot();
                slotId = firstSlot.PlayerSlotId;
                AssertTrue(slotId.IsValid,
                    "P3M5B first configured Slot has no valid identity.");
                completed.Add("fixture-assets-resolved");

                runtimeHost = await AwaitRuntimeHostAsync();
                preparationModule = ResolveHostComponent(
                    runtimeHost,
                    PreparationModuleTypeName,
                    "PlayerActorPreparationRuntimeHostModule");
                sceneAdmissionModule = ResolveHostComponent(
                    runtimeHost,
                    SceneAdmissionModuleTypeName,
                    "SceneLocalPlayerAdmissionRuntimeHostModule");
                participationContext = GetFieldValue(
                    sceneAdmissionModule,
                    "participationContext");
                runtimeContent = GetPropertyValue(
                    runtimeHost,
                    "RuntimeContentRuntime");
                AssertNotNull(participationContext,
                    "Scene admission module has no participation context.");
                AssertNotNull(runtimeContent,
                    "FrameworkRuntimeHost has no RuntimeContentRuntime.");
                completed.Add("official-runtime-authorities-ready");

                originalRoute = ResolveCurrentRoute(runtimeHost);
                AssertNotNull(originalRoute,
                    "FrameworkRuntimeHost has no current Route before P3M5B.");
                AssertSessionClean(
                    participationContext,
                    preparationModule,
                    sceneAdmissionModule,
                    slotId);
                completed.Add("session-initially-clean");

                object routeARequest = await RequestRouteAsync(
                    runtimeHost,
                    routeA,
                    "route-a-enter");
                AssertRequestSucceeded(
                    routeARequest,
                    "P3M5B Route A request failed.");
                completed.Add("route-a-request-succeeded");

                AssertSame(routeA, ResolveCurrentRoute(runtimeHost),
                    "Route A did not become current.");
                AssertSame(routeAActivity, ResolveCurrentActivity(runtimeHost),
                    "Route A Startup Activity did not become current.");
                completed.Add("route-a-startup-activity-active");

                routeAOwner = RuntimeContentOwner.Activity(
                    routeAActivity.ActivityName,
                    routeAActivity.ActivityName);
                LoadedPlayerFixture routeAFirst = await AwaitActiveFixtureAsync(
                    QaP3M5BRouteTransitionAndNegativeMatrixSetup.RouteAActivityScenePath,
                    preparationModule,
                    slotId,
                    routeAOwner);
                AssertAdmittedState(
                    routeAFirst,
                    participationContext,
                    slotId,
                    routeAOwner);
                completed.Add("route-a-scene-player-admitted");

                AssertEqual(routeAOwner,
                    routeAFirst.Preparation.Materialization.Owner,
                    "Route A preparation owner is not the Startup Activity owner.");
                AssertEqual(1,
                    GetIntProperty(sceneAdmissionModule, "ActiveAdmissionCount"),
                    "Route A retained an unexpected active admission count.");
                completed.Add("route-a-owner-authoritative");

                object routeBRequest = await RequestRouteAsync(
                    runtimeHost,
                    routeB,
                    "route-a-to-route-b");
                AssertRequestSucceeded(
                    routeBRequest,
                    "P3M5B Route B request failed.");
                completed.Add("route-b-request-succeeded");

                await AwaitScenesUnloadedAsync(
                    QaP3M5BRouteTransitionAndNegativeMatrixSetup.RouteAPrimaryScenePath,
                    QaP3M5BRouteTransitionAndNegativeMatrixSetup.RouteAActivityScenePath);
                AssertTrue(routeAFirst.Authoring == null,
                    "Route A Activity surface survived Route replacement.");
                completed.Add("route-a-scenes-released");

                AssertEqual(0,
                    CountRuntimeRoots(runtimeContent, routeAOwner),
                    "Route A Activity RuntimeContent root remained after Route switch.");
                completed.Add("route-a-owner-cleared");

                AssertSame(routeB, ResolveCurrentRoute(runtimeHost),
                    "Route B did not become current.");
                AssertSame(routeBActivity, ResolveCurrentActivity(runtimeHost),
                    "Route B Startup Activity did not become current.");
                routeBOwner = RuntimeContentOwner.Activity(
                    routeBActivity.ActivityName,
                    routeBActivity.ActivityName);
                LoadedPlayerFixture routeBFixture = await AwaitActiveFixtureAsync(
                    QaP3M5BRouteTransitionAndNegativeMatrixSetup.RouteBActivityScenePath,
                    preparationModule,
                    slotId,
                    routeBOwner);
                AssertAdmittedState(
                    routeBFixture,
                    participationContext,
                    slotId,
                    routeBOwner);
                completed.Add("route-b-scene-player-admitted");

                AssertTrue(routeBFixture.Adoption != routeAFirst.Adoption,
                    "Route B reused the Route A adoption token.");
                AssertTrue(routeBFixture.Adoption.ActorId != routeAFirst.Adoption.ActorId,
                    "Route B reused the Route A runtime Actor identity.");
                AssertTrue(
                    routeBFixture.Adoption.RuntimeContentIdentity !=
                    routeAFirst.Adoption.RuntimeContentIdentity,
                    "Route B reused the Route A RuntimeContent identity.");
                completed.Add("route-b-fresh-identities");

                AssertEqual(1,
                    GetIntProperty(sceneAdmissionModule, "ActiveAdmissionCount"),
                    "Route switch retained more than one active Scene Player admission.");
                completed.Add("single-active-admission-after-route-switch");

                object routeAReentryRequest = await RequestRouteAsync(
                    runtimeHost,
                    routeA,
                    "route-b-to-route-a-reentry");
                AssertRequestSucceeded(
                    routeAReentryRequest,
                    "P3M5B Route A re-entry request failed.");
                AssertSame(routeA, ResolveCurrentRoute(runtimeHost),
                    "Route A did not become current after re-entry.");
                completed.Add("route-a-reentry-succeeded");

                await AwaitScenesUnloadedAsync(
                    QaP3M5BRouteTransitionAndNegativeMatrixSetup.RouteBPrimaryScenePath,
                    QaP3M5BRouteTransitionAndNegativeMatrixSetup.RouteBActivityScenePath);
                AssertTrue(routeBFixture.Authoring == null,
                    "Route B Activity surface survived Route A re-entry.");
                completed.Add("route-b-scenes-released");

                AssertEqual(0,
                    CountRuntimeRoots(runtimeContent, routeBOwner),
                    "Route B Activity RuntimeContent root remained after Route A re-entry.");
                completed.Add("route-b-owner-cleared");

                LoadedPlayerFixture routeASecond = await AwaitActiveFixtureAsync(
                    QaP3M5BRouteTransitionAndNegativeMatrixSetup.RouteAActivityScenePath,
                    preparationModule,
                    slotId,
                    routeAOwner);
                AssertTrue(routeASecond.Adoption != routeAFirst.Adoption,
                    "Route A re-entry reused its previous adoption token.");
                AssertTrue(routeASecond.Adoption != routeBFixture.Adoption,
                    "Route A re-entry reused Route B adoption evidence.");
                AssertTrue(routeASecond.Adoption.ActorId != routeAFirst.Adoption.ActorId,
                    "Route A re-entry reused its previous runtime Actor identity.");
                AssertAdmittedState(
                    routeASecond,
                    participationContext,
                    slotId,
                    routeAOwner);
                completed.Add("route-a-reentry-fresh-identities");

                await AssertResolverRejectedAsync(
                    sceneAdmissionModule,
                    duplicateActivity,
                    QaP3M5BRouteTransitionAndNegativeMatrixSetup.DuplicateSlotScenePath,
                    "more than one automatic Scene Local Player Admission",
                    loadedNegativeScenes);
                completed.Add("duplicate-slot-rejected");

                await AssertResolverRejectedAsync(
                    sceneAdmissionModule,
                    missingActorActivity,
                    QaP3M5BRouteTransitionAndNegativeMatrixSetup.MissingActorScenePath,
                    "requires Player Slot Profile, Local Player Host, Actor Profile and Scene Logical Player Actor references",
                    loadedNegativeScenes);
                completed.Add("missing-actor-rejected");

                await AssertResolverRejectedAsync(
                    sceneAdmissionModule,
                    mismatchedProfileActivity,
                    QaP3M5BRouteTransitionAndNegativeMatrixSetup.MismatchedProfileScenePath,
                    "evidence does not match the selected Actor Profile",
                    loadedNegativeScenes);
                completed.Add("mismatched-profile-rejected");

                await AssertResolverRejectedAsync(
                    sceneAdmissionModule,
                    reusedHostActivity,
                    QaP3M5BRouteTransitionAndNegativeMatrixSetup.ReusedHostScenePath,
                    "reuses Local Player Host",
                    loadedNegativeScenes);
                completed.Add("reused-host-rejected");

                SceneLocalPlayerAdmissionAuthoring undeclaredSurface =
                    ResolveSingleSurface(
                        QaP3M5BRouteTransitionAndNegativeMatrixSetup.RouteAPrimaryScenePath);
                AssertNotNull(undeclaredSurface,
                    "Route A primary scene has no undeclared Scene Player surface.");
                AssertTrue(undeclaredSurface.RuntimeReady,
                    "Undeclared Route-primary surface is not bound for diagnostics.");
                AssertFalse(undeclaredSurface.HasActiveAdmission,
                    "Undeclared Route-primary surface was admitted by the Startup Activity.");
                ResolveAutomaticResult undeclaredResolution =
                    ResolveAutomaticAuthoring(
                        sceneAdmissionModule,
                        undeclaredActivity);
                AssertTrue(undeclaredResolution.Succeeded,
                    "Undeclared-surface resolution failed instead of ignoring the surface. " +
                    undeclaredResolution.Issue);
                AssertEqual(0,
                    undeclaredResolution.Count,
                    "Activity resolved a surface from a scene it did not declare.");
                completed.Add("undeclared-surface-ignored");

                AssertEqual(1,
                    GetIntProperty(sceneAdmissionModule, "ActiveAdmissionCount"),
                    "Negative matrix changed the active Route A admission count.");
                AssertFalse(AnyNegativeSceneLoaded(),
                    "Negative matrix retained a loaded negative fixture scene.");
                completed.Add("negative-scenes-left-no-admission");

                AssertSame(routeA, ResolveCurrentRoute(runtimeHost),
                    "Negative matrix changed the current Route.");
                AssertSame(routeAActivity, ResolveCurrentActivity(runtimeHost),
                    "Negative matrix changed the current Activity.");
                ScenePlayerActorAdoptionToken afterNegatives =
                    GetAdoptionToken(preparationModule, slotId);
                AssertEqual(routeASecond.Adoption, afterNegatives,
                    "Negative matrix replaced the active Route A adoption token.");
                completed.Add("route-state-preserved-after-negatives");

                object restoreRequest = await RequestRouteAsync(
                    runtimeHost,
                    originalRoute,
                    "p3m5b-restore-original-route");
                AssertRequestSucceeded(
                    restoreRequest,
                    "P3M5B could not restore the original Route.");
                AssertSame(originalRoute, ResolveCurrentRoute(runtimeHost),
                    "Original Route was not restored.");
                await AwaitAllP3M5BScenesUnloadedAsync();
                completed.Add("original-route-restored");

                AssertSessionClean(
                    participationContext,
                    preparationModule,
                    sceneAdmissionModule,
                    slotId);
                AssertEqual(0,
                    CountRuntimeRoots(runtimeContent, routeAOwner),
                    "Route A RuntimeContent owner remained after restoration.");
                AssertEqual(0,
                    CountRuntimeRoots(runtimeContent, routeBOwner),
                    "Route B RuntimeContent owner remained after restoration.");
                completed.Add("final-session-clean");
            }
            catch (Exception exception)
            {
                failure = Unwrap(exception);
            }

            try
            {
                for (int index = loadedNegativeScenes.Count - 1; index >= 0; index--)
                {
                    await UnloadSceneIfLoadedAsync(loadedNegativeScenes[index]);
                }

                if (runtimeHost != null &&
                    originalRoute != null &&
                    !ReferenceEquals(ResolveCurrentRoute(runtimeHost), originalRoute))
                {
                    object cleanupRestore = await RequestRouteAsync(
                        runtimeHost,
                        originalRoute,
                        "p3m5b-cleanup-restore-original-route");
                    if (!GetBooleanProperty(cleanupRestore, "Succeeded"))
                    {
                        throw new InvalidOperationException(
                            "P3M5B cleanup could not restore the original Route. " +
                            GetStringProperty(cleanupRestore, "Message"));
                    }
                }
            }
            catch (Exception cleanupException)
            {
                Exception actualCleanup = Unwrap(cleanupException);
                failure = failure == null
                    ? new InvalidOperationException(
                        "P3M5B cleanup failed. " + actualCleanup.Message,
                        actualCleanup)
                    : new AggregateException(
                        "P3M5B execution and cleanup both failed.",
                        failure,
                        actualCleanup);
            }

            if (failure != null)
            {
                Debug.LogError(
                    "[P3M5B_ROUTE_TRANSITION_NEGATIVE_MATRIX_SMOKE] " +
                    $"status='Failed' exception='{failure.GetType().Name}' " +
                    $"message='{Escape(failure.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw failure;
            }

            Debug.Log(
                "[P3M5B_ROUTE_TRANSITION_NEGATIVE_MATRIX_SMOKE] " +
                $"status='Passed' cases='{completed.Count}' " +
                $"completed='{string.Join(",", completed)}'.");
        }

        private readonly struct ResolveAutomaticResult
        {
            internal ResolveAutomaticResult(bool succeeded, int count, string issue)
            {
                Succeeded = succeeded;
                Count = count;
                Issue = issue ?? string.Empty;
            }

            internal bool Succeeded { get; }
            internal int Count { get; }
            internal string Issue { get; }
        }

        private static T LoadAsset<T>(string path)
            where T : UnityEngine.Object
        {
            T value = AssetDatabase.LoadAssetAtPath<T>(path);
            AssertNotNull(value,
                $"Missing P3M5B asset '{path}'. Apply the fixture outside Play Mode.");
            return value;
        }

        private static PlayerSlotProfile ResolveFirstConfiguredSlot()
        {
            ImmersiveFrameworkSettingsAsset settings =
                Resources.Load<ImmersiveFrameworkSettingsAsset>(
                    ImmersiveFrameworkSettingsAsset.ResourcesPath);
            AssertNotNull(settings,
                "Immersive Framework settings are missing.");
            AssertNotNull(settings.ActiveGameApplication,
                "Active Game Application is missing.");
            AssertTrue(settings.ActiveGameApplication.TryGetLocalPlayerSlot(
                    0,
                    out PlayerSlotProfile slot) &&
                slot != null,
                "P3M5B requires a configured first Local Player Slot.");
            return slot;
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
                        Component host = current as Component;
                        if (host != null && preparationType != null && sceneType != null)
                        {
                            Component preparation = host.GetComponent(preparationType);
                            Component scene = host.GetComponent(sceneType);
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
            Component host = runtimeHost as Component;
            AssertNotNull(host,
                "FrameworkRuntimeHost is not a Unity Component.");
            Component component = host.GetComponent(componentType);
            AssertNotNull(component,
                $"{label} is not attached to FrameworkRuntimeHost.");
            return component;
        }

        private static RouteAsset ResolveCurrentRoute(object runtimeHost)
        {
            object state = GetPropertyValue(runtimeHost, "State");
            return state == null
                ? null
                : GetPropertyValue(state, "CurrentRoute") as RouteAsset;
        }

        private static ActivityAsset ResolveCurrentActivity(object runtimeHost)
        {
            object state = GetPropertyValue(runtimeHost, "State");
            return state == null
                ? null
                : GetPropertyValue(state, "CurrentActivity") as ActivityAsset;
        }

        private static async Task<object> RequestRouteAsync(
            object runtimeHost,
            RouteAsset route,
            string reason)
        {
            return await InvokeTaskResultAsync(
                runtimeHost,
                "RequestRouteAsync",
                route,
                nameof(QaP3M5BRouteTransitionAndNegativeMatrixSmoke),
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
            object result,
            string message)
        {
            AssertNotNull(result, message + " No request result was returned.");
            AssertTrue(GetBooleanProperty(result, "Succeeded"),
                message + " " + GetStringProperty(result, "Message"));
        }

        private static async Task<LoadedPlayerFixture> AwaitActiveFixtureAsync(
            string scenePath,
            object preparationModule,
            PlayerSlotId playerSlotId,
            RuntimeContentOwner expectedOwner)
        {
            for (int frame = 0; frame < 240; frame++)
            {
                SceneLocalPlayerAdmissionAuthoring authoring =
                    ResolveSingleSurface(scenePath, requireLoaded: false);
                if (authoring != null &&
                    authoring.RuntimeReady &&
                    authoring.HasActiveAdmission &&
                    TryGetAdoptionToken(
                        preparationModule,
                        playerSlotId,
                        out ScenePlayerActorAdoptionToken adoption))
                {
                    PlayerActorPreparationSummary preparation =
                        GetPreparationSummary(preparationModule, playerSlotId);
                    if (preparation.IsPrepared &&
                        preparation.Materialization.Owner == expectedOwner)
                    {
                        return new LoadedPlayerFixture(
                            authoring.gameObject.scene,
                            authoring,
                            adoption,
                            preparation);
                    }
                }

                await Awaitable.NextFrameAsync();
            }

            throw new InvalidOperationException(
                $"P3M5B Scene Player fixture '{scenePath}' did not become active within 240 frames.");
        }

        private static void AssertAdmittedState(
            LoadedPlayerFixture fixture,
            object participationContext,
            PlayerSlotId playerSlotId,
            RuntimeContentOwner expectedOwner)
        {
            AssertTrue(fixture.Scene.IsValid() && fixture.Scene.isLoaded,
                "Admitted Scene Player fixture scene is not loaded.");
            AssertNotNull(fixture.Authoring,
                "Admitted Scene Player fixture has no authoring surface.");
            AssertTrue(fixture.Authoring.RuntimeReady,
                "Admitted Scene Player surface is not runtime-ready.");
            AssertTrue(fixture.Authoring.HasActiveAdmission,
                "Scene Player surface has no active admission.");
            AssertTrue(fixture.Authoring.LocalPlayerHost.IsJoined,
                "Scene Player Host is not Joined.");
            AssertTrue(fixture.Authoring.SceneLogicalPlayerActor.HasPlayerInputEvidence,
                "Scene Player Actor has no contextual PlayerInput evidence.");
            AssertTrue(fixture.Adoption.IsValid,
                "Scene Player adoption token is invalid.");
            AssertEqual(
                PlayerActorPhysicalOwnership.ExternalSceneOwned,
                fixture.Adoption.PhysicalOwnership,
                "Scene Player adoption lost external physical ownership.");
            AssertTrue(fixture.Preparation.IsPrepared,
                "Scene Player canonical preparation is not active.");
            AssertEqual(expectedOwner,
                fixture.Preparation.Materialization.Owner,
                "Scene Player preparation has the wrong Activity owner.");

            PlayerParticipationSnapshot snapshot =
                CreateParticipationSnapshot(participationContext);
            PlayerSlotRuntimeSnapshot slot = FindSlot(snapshot, playerSlotId);
            AssertTrue(slot.IsJoined,
                "Scene Player Slot is not Joined.");
            AssertTrue(slot.HasSelectedActor,
                "Scene Player Slot has no selected Actor.");
            AssertEqual(0, snapshot.ReservedCount,
                "Scene Player admission stranded a Reserved Slot.");
            AssertEqual(0, snapshot.LeavingCount,
                "Scene Player admission stranded a Leaving Slot.");
        }

        private static async Task AssertResolverRejectedAsync(
            object sceneAdmissionModule,
            ActivityAsset activity,
            string scenePath,
            string expectedIssueFragment,
            List<string> loadedNegativeScenes)
        {
            await LoadSceneAsync(scenePath);
            loadedNegativeScenes.Add(scenePath);
            await AwaitSurfacesBoundAsync(scenePath);

            ResolveAutomaticResult result = ResolveAutomaticAuthoring(
                sceneAdmissionModule,
                activity);
            AssertFalse(result.Succeeded,
                $"Negative Activity '{activity.ActivityName}' unexpectedly resolved '{result.Count}' automatic surfaces.");
            AssertTrue(result.Issue.IndexOf(
                    expectedIssueFragment,
                    StringComparison.OrdinalIgnoreCase) >= 0,
                $"Negative Activity '{activity.ActivityName}' returned an unexpected issue. " +
                result.Issue);

            SceneLocalPlayerAdmissionAuthoring[] surfaces =
                ResolveSurfaces(scenePath);
            for (int index = 0; index < surfaces.Length; index++)
            {
                AssertFalse(surfaces[index].HasActiveAdmission,
                    $"Negative scene '{scenePath}' created an active admission.");
            }

            await UnloadSceneIfLoadedAsync(scenePath);
            loadedNegativeScenes.Remove(scenePath);
        }

        private static ResolveAutomaticResult ResolveAutomaticAuthoring(
            object sceneAdmissionModule,
            ActivityAsset activity)
        {
            MethodInfo resolve = sceneAdmissionModule.GetType().GetMethod(
                "TryResolveAutomaticActivityAuthoring",
                InstanceAny);
            AssertNotNull(resolve,
                "Scene admission module has no automatic Activity authoring resolver.");
            object[] arguments = { activity, null, null };
            bool succeeded = (bool)resolve.Invoke(sceneAdmissionModule, arguments);
            int count = CountEnumerable(arguments[1] as IEnumerable);
            string issue = Convert.ToString(arguments[2]);
            return new ResolveAutomaticResult(succeeded, count, issue);
        }

        private static int CountEnumerable(IEnumerable values)
        {
            if (values == null)
            {
                return 0;
            }

            int count = 0;
            foreach (object _ in values)
            {
                count++;
            }

            return count;
        }

        private static async Task LoadSceneAsync(string scenePath)
        {
            Scene existing = SceneManager.GetSceneByPath(scenePath);
            if (existing.IsValid() && existing.isLoaded)
            {
                return;
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(
                scenePath,
                LoadSceneMode.Additive);
            AssertNotNull(operation,
                $"Could not start loading negative scene '{scenePath}'.");
            while (!operation.isDone)
            {
                await Awaitable.NextFrameAsync();
            }

            Scene loaded = SceneManager.GetSceneByPath(scenePath);
            AssertTrue(loaded.IsValid() && loaded.isLoaded,
                $"Negative scene '{scenePath}' did not load.");
        }

        private static async Task AwaitSurfacesBoundAsync(string scenePath)
        {
            for (int frame = 0; frame < 120; frame++)
            {
                SceneLocalPlayerAdmissionAuthoring[] surfaces =
                    ResolveSurfaces(scenePath);
                bool ready = surfaces.Length > 0;
                for (int index = 0; index < surfaces.Length; index++)
                {
                    ready &= surfaces[index] != null && surfaces[index].RuntimeReady;
                }

                if (ready)
                {
                    return;
                }

                await Awaitable.NextFrameAsync();
            }

            throw new InvalidOperationException(
                $"Scene Local Player surfaces in '{scenePath}' did not bind within 120 frames.");
        }

        private static async Task UnloadSceneIfLoadedAsync(string scenePath)
        {
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            AsyncOperation operation = SceneManager.UnloadSceneAsync(scene);
            AssertNotNull(operation,
                $"Could not start unloading scene '{scenePath}'.");
            while (!operation.isDone)
            {
                await Awaitable.NextFrameAsync();
            }

            await Awaitable.NextFrameAsync();
            Scene remaining = SceneManager.GetSceneByPath(scenePath);
            AssertTrue(!remaining.IsValid() || !remaining.isLoaded,
                $"Scene '{scenePath}' remained loaded after unload.");
        }

        private static async Task AwaitScenesUnloadedAsync(params string[] paths)
        {
            for (int frame = 0; frame < 240; frame++)
            {
                bool anyLoaded = false;
                for (int index = 0; index < paths.Length; index++)
                {
                    Scene scene = SceneManager.GetSceneByPath(paths[index]);
                    anyLoaded |= scene.IsValid() && scene.isLoaded;
                }

                if (!anyLoaded)
                {
                    return;
                }

                await Awaitable.NextFrameAsync();
            }

            throw new InvalidOperationException(
                "One or more P3M5B scenes remained loaded after Route transition.");
        }

        private static async Task AwaitAllP3M5BScenesUnloadedAsync()
        {
            await AwaitScenesUnloadedAsync(
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.RouteAPrimaryScenePath,
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.RouteBPrimaryScenePath,
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.RouteAActivityScenePath,
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.RouteBActivityScenePath,
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.DuplicateSlotScenePath,
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.MissingActorScenePath,
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.MismatchedProfileScenePath,
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.ReusedHostScenePath);
        }

        private static bool AnyNegativeSceneLoaded()
        {
            string[] paths =
            {
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.DuplicateSlotScenePath,
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.MissingActorScenePath,
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.MismatchedProfileScenePath,
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.ReusedHostScenePath
            };
            for (int index = 0; index < paths.Length; index++)
            {
                Scene scene = SceneManager.GetSceneByPath(paths[index]);
                if (scene.IsValid() && scene.isLoaded)
                {
                    return true;
                }
            }

            return false;
        }

        private static SceneLocalPlayerAdmissionAuthoring ResolveSingleSurface(
            string scenePath,
            bool requireLoaded = true)
        {
            SceneLocalPlayerAdmissionAuthoring[] surfaces = ResolveSurfaces(scenePath);
            if (surfaces.Length == 0 && !requireLoaded)
            {
                return null;
            }

            AssertEqual(1, surfaces.Length,
                $"Expected exactly one Scene Local Player surface in '{scenePath}'.");
            return surfaces[0];
        }

        private static SceneLocalPlayerAdmissionAuthoring[] ResolveSurfaces(
            string scenePath)
        {
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return Array.Empty<SceneLocalPlayerAdmissionAuthoring>();
            }

            var surfaces = new List<SceneLocalPlayerAdmissionAuthoring>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                surfaces.AddRange(
                    roots[rootIndex].GetComponentsInChildren<
                        SceneLocalPlayerAdmissionAuthoring>(true));
            }

            return surfaces.ToArray();
        }

        private static PlayerParticipationSnapshot CreateParticipationSnapshot(
            object context)
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

        private static void AssertSessionClean(
            object participationContext,
            object preparationModule,
            object sceneAdmissionModule,
            PlayerSlotId playerSlotId)
        {
            PlayerParticipationSnapshot snapshot =
                CreateParticipationSnapshot(participationContext);
            PlayerSlotRuntimeSnapshot slot = FindSlot(snapshot, playerSlotId);
            AssertFalse(slot.IsJoined,
                "P3M5B clean-state Slot remains Joined.");
            AssertFalse(slot.HasSelectedActor,
                "P3M5B clean-state Slot retains Actor selection.");
            AssertEqual(0, snapshot.ReservedCount,
                "P3M5B clean state retains a Reserved Slot.");
            AssertEqual(0, snapshot.LeavingCount,
                "P3M5B clean state retains a Leaving Slot.");
            PlayerActorPreparationSummary preparation =
                GetPreparationSummary(preparationModule, playerSlotId);
            AssertTrue(preparation.IsUnprepared,
                "P3M5B clean state retains Player Actor preparation. " +
                preparation.ToDiagnosticString());
            AssertFalse(TryGetAdoptionToken(
                    preparationModule,
                    playerSlotId,
                    out _),
                "P3M5B clean state retains Scene Actor adoption.");
            AssertEqual(0,
                GetIntProperty(sceneAdmissionModule, "ActiveAdmissionCount"),
                "P3M5B clean state retains an active Scene admission.");
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

        private static int CountRuntimeRoots(
            object runtimeContent,
            RuntimeContentOwner owner)
        {
            if (!owner.IsValid)
            {
                return 0;
            }

            MethodInfo snapshot = runtimeContent.GetType().GetMethod(
                "SnapshotRoots",
                InstanceAny,
                null,
                Type.EmptyTypes,
                null);
            AssertNotNull(snapshot,
                "RuntimeContentRuntime has no parameterless SnapshotRoots method.");
            IEnumerable roots = snapshot.Invoke(runtimeContent, null) as IEnumerable;
            AssertNotNull(roots,
                "RuntimeContentRuntime SnapshotRoots returned no enumerable result.");
            int count = 0;
            foreach (object root in roots)
            {
                object value = GetPropertyValue(root, "Owner");
                if (value is RuntimeContentOwner rootOwner && rootOwner == owner)
                {
                    count++;
                }
            }

            return count;
        }

        private static object GetFieldValue(object target, string fieldName)
        {
            AssertNotNull(target,
                $"Cannot read field '{fieldName}' from null target.");
            FieldInfo field = target.GetType().GetField(fieldName, InstanceAny);
            AssertNotNull(field,
                $"Missing field '{fieldName}' on '{target.GetType().Name}'.");
            return field.GetValue(target);
        }

        private static object GetPropertyValue(
            object target,
            string propertyName)
        {
            AssertNotNull(target,
                $"Cannot read property '{propertyName}' from null target.");
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                InstanceAny);
            AssertNotNull(property,
                $"Missing property '{propertyName}' on '{target.GetType().Name}'.");
            return property.GetValue(target);
        }

        private static int GetIntProperty(object target, string propertyName)
        {
            return Convert.ToInt32(GetPropertyValue(target, propertyName));
        }

        private static bool GetBooleanProperty(
            object target,
            string propertyName)
        {
            return Convert.ToBoolean(GetPropertyValue(target, propertyName));
        }

        private static string GetStringProperty(
            object target,
            string propertyName)
        {
            return Convert.ToString(GetPropertyValue(target, propertyName));
        }

        private static Exception Unwrap(Exception exception)
        {
            if (exception is TargetInvocationException invocation &&
                invocation.InnerException != null)
            {
                return Unwrap(invocation.InnerException);
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
                    $"{message} expected='{expected}' actual='{actual}'.");
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
