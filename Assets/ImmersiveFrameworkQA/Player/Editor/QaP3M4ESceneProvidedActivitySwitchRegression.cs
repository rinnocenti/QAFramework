using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// One-shot Play Mode proof of public Activity A -> B -> A switching while
    /// a Scene-Provided Player remains physically owned by the Route Primary Scene.
    /// </summary>
    internal static class QaP3M4ESceneProvidedActivitySwitchRegression
    {
        private const string RuntimeHostTypeName =
            "Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost";

        private const string PreparationModuleTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerActorPreparationRuntimeHostModule";

        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Player/Run P3M4E Scene-Provided Activity Switch Regression";

        private const string LogPrefix =
            "[QA][P3M4E Scene-Provided Activity Switch]";

        private static readonly BindingFlags InstanceAny =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;

        [MenuItem(MenuPath)]
        private static async void Run()
        {
            var completed =
                new List<string>();

            try
            {
                await RunAsync(
                    completed);

                Debug.Log(
                    $"{LogPrefix} PASS. status='Passed' " +
                    $"cases='{completed.Count}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Exception effective =
                    Unwrap(
                        exception);

                Debug.LogError(
                    $"{LogPrefix} FAIL. status='Failed' " +
                    $"exception='{effective.GetType().Name}' " +
                    $"message='{Escape(effective.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");

                throw effective;
            }
        }

        private static async Task RunAsync(
            ICollection<string> completed)
        {
            Require(
                EditorApplication.isPlaying,
                "P3M4E regression must run in Play Mode.");

            completed.Add(
                "play-mode-required");

            RouteAsset targetRoute =
                AssetDatabase.LoadAssetAtPath<RouteAsset>(
                    QaP3M4BRouteSceneProvidedAdmissionSetup.RoutePath);

            ActivityAsset activityA =
                AssetDatabase.LoadAssetAtPath<ActivityAsset>(
                    QaP3M4BRouteSceneProvidedAdmissionSetup.ActivityPath);

            ActivityAsset activityB =
                AssetDatabase.LoadAssetAtPath<ActivityAsset>(
                    QaP3M4ESceneProvidedActivitySwitchSetup.ActivityBPath);

            Require(
                targetRoute != null &&
                activityA != null &&
                activityB != null,
                "P3M4E fixture assets are missing. Run the P3M4E setup menu in Edit Mode.");

            Require(
                ReferenceEquals(
                    targetRoute.StartupActivity,
                    activityA),
                "P3M4E target Route does not reference Activity A.");

            Require(
                !ReferenceEquals(
                    activityA,
                    activityB) &&
                activityA.HasValidActivityId &&
                activityB.HasValidActivityId &&
                !activityA.HasSameIdentity(
                    activityB),
                "P3M4E Activities A and B must have distinct valid identities.");

            completed.Add(
                "fixture-assets-loaded");

            Component runtimeHost =
                ResolveCurrentRuntimeHost();

            completed.Add(
                "runtime-host-resolved");

            RouteAsset previousRoute =
                ResolveCurrentRoute(
                    runtimeHost);

            Require(
                previousRoute != null,
                "FrameworkRuntimeHost has no current Route before P3M4E.");

            Require(
                !ReferenceEquals(
                    previousRoute,
                    targetRoute),
                "P3M4E is one-shot. Re-enter Play Mode before running it again.");

            object routeResult =
                await InvokeRequestAsync(
                    runtimeHost,
                    "RequestRouteAsync",
                    targetRoute,
                    nameof(
                        QaP3M4ESceneProvidedActivitySwitchRegression),
                    "qa-scene-provided-activity-switch-enter-route");

            RequireSuccessfulRequest(
                routeResult,
                targetActivity: activityA,
                requireActivityTargetProperty: false,
                context: "P3M4E target Route");

            Require(
                ReferenceEquals(
                    ResolveCurrentRoute(runtimeHost),
                    targetRoute) &&
                ReferenceEquals(
                    ResolveCurrentActivity(runtimeHost),
                    activityA),
                "P3M4E target Route and Activity A are not current after Route entry.");

            completed.Add(
                "route-request-succeeded");

            SceneLocalPlayerAdmissionAuthoring authoring =
                ResolveSingleAuthoring(
                    targetRoute.PrimaryScenePath);

            Require(
                authoring != null &&
                authoring.RuntimeReady,
                "P3M4E Route Primary Scene composer is not RuntimeReady.");

            completed.Add(
                "initial-composer-resolved");

            LocalPlayerHostAuthoring host =
                authoring.LocalPlayerHost;

            PlayerActorDeclaration actor =
                authoring.SceneLogicalPlayerActor;

            Require(
                host != null &&
                actor != null,
                "P3M4E initial composition has no Local Player Host or Scene Logical Player Actor.");

            bool hasExpectedSlot =
                authoring.TryGetPlayerSlotId(
                    out PlayerSlotId expectedSlot,
                    out string slotIssue);

            Require(
                hasExpectedSlot,
                "P3M4E initial composition has no valid Player Slot. " +
                slotIssue);

            Require(
                authoring.HasActiveAdmission &&
                host.IsJoined &&
                host.HasJoinedSlot &&
                host.JoinedPlayerSlotId ==
                expectedSlot,
                "P3M4E initial Scene-Provided admission is not active on the expected Slot.");

            ScenePlayerActorAdoptionResult adoptionA1 =
                authoring.LastActorAdoptionResult;

            RequireValidAdoption(
                adoptionA1,
                actor,
                "initial Activity A");

            completed.Add(
                "initial-admission-active");

            GameObject hostObject =
                host.gameObject;

            GameObject actorObject =
                actor.gameObject;

            Transform actorParent =
                actor.transform.parent;

            Transform actorMount =
                host.ActorMount;

            Scene physicalScene =
                hostObject.scene;

            RequirePhysicalComposition(
                authoring,
                host,
                actor,
                hostObject,
                actorObject,
                actorParent,
                actorMount,
                physicalScene,
                expectedSlot,
                "initial Activity A");

            completed.Add(
                "physical-references-captured");

            object ownerA1 =
                ResolveActiveLifecycleOwner(
                    runtimeHost,
                    activityA);

            completed.Add(
                "initial-owner-captured");

            bool activityBCommitted =
                false;

            bool activityARestored =
                false;

            try
            {
                object activityBResult =
                    await InvokeRequestAsync(
                        runtimeHost,
                        "RequestActivityAsync",
                        activityB,
                        nameof(
                            QaP3M4ESceneProvidedActivitySwitchRegression),
                        "qa-scene-provided-switch-a-to-b");

                RequireSuccessfulRequest(
                    activityBResult,
                    activityB,
                    requireActivityTargetProperty: true,
                    context: "Activity A -> B");

                completed.Add(
                    "activity-b-request-succeeded");

                RequireActivityReadyAndCommitted(
                    activityBResult,
                    activityB,
                    activityA,
                    "Activity B");

                completed.Add(
                    "activity-b-ready");

                Require(
                    ReferenceEquals(
                        ResolveCurrentRoute(runtimeHost),
                        targetRoute) &&
                    ReferenceEquals(
                        ResolveCurrentActivity(runtimeHost),
                        activityB),
                    "Activity B is not the current Activity after the successful request.");

                activityBCommitted =
                    true;

                completed.Add(
                    "activity-b-current");

                object ownerB =
                    ResolveActiveLifecycleOwner(
                        runtimeHost,
                        activityB);

                Require(
                    ownerB != null &&
                    ownerA1 != null &&
                    !ownerB.Equals(
                        ownerA1),
                    "Activity B retained the previous Activity A lifecycle owner.");

                completed.Add(
                    "activity-b-owner-changed");

                Require(
                    authoring.HasActiveAdmission,
                    "Scene-Provided admission is not active for Activity B.");

                completed.Add(
                    "activity-b-admission-active");

                RequirePhysicalComposition(
                    authoring,
                    host,
                    actor,
                    hostObject,
                    actorObject,
                    actorParent,
                    actorMount,
                    physicalScene,
                    expectedSlot,
                    "Activity B");

                completed.Add(
                    "activity-b-same-host");

                Require(
                    host.HasJoinedSlot &&
                    host.JoinedPlayerSlotId ==
                    expectedSlot,
                    "Activity B readmitted the Host to a different Slot.");

                completed.Add(
                    "activity-b-exact-slot");

                completed.Add(
                    "activity-b-same-actor");

                ScenePlayerActorAdoptionResult adoptionB =
                    authoring.LastActorAdoptionResult;

                RequireValidAdoption(
                    adoptionB,
                    actor,
                    "Activity B");

                Require(
                    !adoptionB.Token.Equals(
                        adoptionA1.Token),
                    "Activity B reused the stale Activity A adoption token.");

                completed.Add(
                    "activity-b-new-adoption-token");

                Require(
                    CountActors(
                        actorMount) == 1,
                    "Activity B switch created duplicate Player Actors.");

                completed.Add(
                    "activity-b-no-duplicate-actor");

                object activityAResult =
                    await InvokeRequestAsync(
                        runtimeHost,
                        "RequestActivityAsync",
                        activityA,
                        nameof(
                            QaP3M4ESceneProvidedActivitySwitchRegression),
                        "qa-scene-provided-switch-b-to-a");

                RequireSuccessfulRequest(
                    activityAResult,
                    activityA,
                    requireActivityTargetProperty: true,
                    context: "Activity B -> A");

                completed.Add(
                    "activity-a-return-request-succeeded");

                RequireActivityReadyAndCommitted(
                    activityAResult,
                    activityA,
                    activityB,
                    "returned Activity A");

                completed.Add(
                    "activity-a-return-ready");

                Require(
                    ReferenceEquals(
                        ResolveCurrentRoute(runtimeHost),
                        targetRoute) &&
                    ReferenceEquals(
                        ResolveCurrentActivity(runtimeHost),
                        activityA),
                    "Activity A is not current after the return request.");

                activityARestored =
                    true;

                completed.Add(
                    "activity-a-return-current");

                Require(
                    authoring.HasActiveAdmission,
                    "Scene-Provided admission is not active after returning to Activity A.");

                completed.Add(
                    "activity-a-return-admission-active");

                RequirePhysicalComposition(
                    authoring,
                    host,
                    actor,
                    hostObject,
                    actorObject,
                    actorParent,
                    actorMount,
                    physicalScene,
                    expectedSlot,
                    "returned Activity A");

                completed.Add(
                    "activity-a-return-same-host");

                Require(
                    host.HasJoinedSlot &&
                    host.JoinedPlayerSlotId ==
                    expectedSlot,
                    "Returned Activity A readmitted the Host to a different Slot.");

                completed.Add(
                    "activity-a-return-exact-slot");

                completed.Add(
                    "activity-a-return-same-actor");

                ScenePlayerActorAdoptionResult adoptionA2 =
                    authoring.LastActorAdoptionResult;

                RequireValidAdoption(
                    adoptionA2,
                    actor,
                    "returned Activity A");

                Require(
                    !adoptionA2.Token.Equals(
                        adoptionB.Token) &&
                    !adoptionA2.Token.Equals(
                        adoptionA1.Token),
                    "Returned Activity A did not create a fresh adoption token.");

                completed.Add(
                    "activity-a-return-new-adoption-token");

                Require(
                    CountActors(
                        actorMount) == 1,
                    "Returning to Activity A created duplicate Player Actors.");

                completed.Add(
                    "activity-a-return-no-duplicate-actor");

                Require(
                    authoring.ActorPhysicalOwnership ==
                    PlayerActorPhysicalOwnership.ExternalSceneOwned,
                    "Scene Actor physical ownership changed during public Activity switches.");

                completed.Add(
                    "external-ownership-preserved");

                Require(
                    physicalScene.IsValid() &&
                    physicalScene.isLoaded &&
                    string.Equals(
                        physicalScene.path,
                        targetRoute.PrimaryScenePath,
                        StringComparison.Ordinal) &&
                    hostObject.scene ==
                    physicalScene &&
                    actorObject.scene ==
                    physicalScene,
                    "Route Primary Scene or physical Player objects were not preserved.");

                completed.Add(
                    "route-primary-scene-preserved");

                Require(
                    ResolveActiveLifecycleEntryCount(
                        runtimeHost,
                        activityA) == 1,
                    "Scene-Provided lifecycle did not retain exactly one active entry after returning to Activity A.");

                completed.Add(
                    "active-entry-restored");

                Require(
                    ReferenceEquals(
                        ResolveCurrentRoute(runtimeHost),
                        targetRoute),
                    "Public Activity switching changed the canonical Route.");

                completed.Add(
                    "canonical-route-preserved");
            }
            finally
            {
                if (activityBCommitted &&
                    !activityARestored)
                {
                    await TryRecoverActivityAAsync(
                        runtimeHost,
                        activityA);
                }
            }
        }

        private static async Task<object> InvokeRequestAsync(
            object runtimeHost,
            string methodName,
            UnityEngine.Object target,
            string source,
            string reason)
        {
            MethodInfo method =
                GetMethod(
                    runtimeHost.GetType(),
                    methodName,
                    3);

            object taskObject =
                method.Invoke(
                    runtimeHost,
                    new object[]
                    {
                        target,
                        source,
                        reason
                    });

            Require(
                taskObject is Task,
                $"'{methodName}' did not return a Task.");

            var task =
                (Task)taskObject;

            await task;

            PropertyInfo resultProperty =
                taskObject.GetType().GetProperty(
                    "Result",
                    InstanceAny);

            Require(
                resultProperty != null,
                $"'{methodName}' Task has no Result property.");

            return resultProperty.GetValue(
                taskObject);
        }

        private static void RequireSuccessfulRequest(
            object requestResult,
            ActivityAsset targetActivity,
            bool requireActivityTargetProperty,
            string context)
        {
            Require(
                requestResult != null,
                $"{context} returned no request result.");

            Require(
                GetBooleanProperty(
                    requestResult,
                    "Succeeded"),
                $"{context} request failed. " +
                GetStringProperty(
                    requestResult,
                    "Message"));

            if (requireActivityTargetProperty)
            {
                Require(
                    ReferenceEquals(
                        GetRequiredProperty(
                            requestResult,
                            "TargetActivity") as ActivityAsset,
                        targetActivity),
                    $"{context} result retained the wrong target Activity.");
            }
        }

        private static void RequireActivityReadyAndCommitted(
            object requestResult,
            ActivityAsset expectedActivity,
            ActivityAsset expectedPreviousActivity,
            string context)
        {
            object flow =
                GetRequiredProperty(
                    requestResult,
                    "ActivityFlowResult");

            Require(
                GetBooleanProperty(
                    flow,
                    "Completed") &&
                GetBooleanProperty(
                    flow,
                    "IsActivityReady") &&
                GetBooleanProperty(
                    flow,
                    "ActivityAuthorityCommitReached"),
                $"{context} did not complete with committed Ready state.");

            Require(
                !GetBooleanProperty(
                    flow,
                    "ActivityTransitionFailedBeforeCommit") &&
                !GetBooleanProperty(
                    flow,
                    "ActivityTransitionCommittedNotReady") &&
                !GetBooleanProperty(
                    flow,
                    "ActivityTransitionCommittedFinalizationFailed"),
                $"{context} retained a failed Activity transition diagnostic.");

            Require(
                ReferenceEquals(
                    GetRequiredProperty(
                        flow,
                        "Activity") as ActivityAsset,
                    expectedActivity) &&
                ReferenceEquals(
                    GetRequiredProperty(
                        flow,
                        "PreviousActivity") as ActivityAsset,
                    expectedPreviousActivity),
                $"{context} ActivityFlowResult retained the wrong Activity identities.");
        }

        private static void RequirePhysicalComposition(
            SceneLocalPlayerAdmissionAuthoring authoring,
            LocalPlayerHostAuthoring host,
            PlayerActorDeclaration actor,
            GameObject expectedHostObject,
            GameObject expectedActorObject,
            Transform expectedActorParent,
            Transform expectedActorMount,
            Scene expectedScene,
            PlayerSlotId expectedSlot,
            string context)
        {
            Require(
                authoring != null &&
                host != null &&
                actor != null,
                $"{context} physical composition is missing.");

            Require(
                ReferenceEquals(
                    authoring.LocalPlayerHost,
                    host) &&
                ReferenceEquals(
                    host.gameObject,
                    expectedHostObject) &&
                ReferenceEquals(
                    authoring.SceneLogicalPlayerActor,
                    actor) &&
                ReferenceEquals(
                    actor.gameObject,
                    expectedActorObject),
                $"{context} replaced the Scene-Provided Host or Actor.");

            Require(
                ReferenceEquals(
                    host.ActorMount,
                    expectedActorMount) &&
                ReferenceEquals(
                    actor.transform.parent,
                    expectedActorParent) &&
                (ReferenceEquals(
                     actor.transform,
                     expectedActorMount) ||
                 actor.transform.IsChildOf(
                     expectedActorMount)),
                $"{context} moved the Actor or changed its Actor Mount.");

            Require(
                expectedScene.IsValid() &&
                expectedScene.isLoaded &&
                host.gameObject.scene ==
                expectedScene &&
                actor.gameObject.scene ==
                expectedScene,
                $"{context} moved physical Player objects out of the Route Primary Scene.");

            Require(
                host.IsJoined &&
                host.HasJoinedSlot &&
                host.JoinedPlayerSlotId ==
                expectedSlot,
                $"{context} Host is not joined to the expected Slot.");

            Require(
                CountActors(
                    expectedActorMount) == 1,
                $"{context} Actor Mount does not contain exactly one Player Actor.");
        }

        private static void RequireValidAdoption(
            ScenePlayerActorAdoptionResult adoption,
            PlayerActorDeclaration actor,
            string context)
        {
            Require(
                adoption != null &&
                adoption.Succeeded &&
                adoption.Token.IsValid &&
                adoption.PhysicalOwnership ==
                PlayerActorPhysicalOwnership.ExternalSceneOwned &&
                ReferenceEquals(
                    adoption.SceneActor,
                    actor),
                $"{context} Scene Actor adoption is missing or invalid. " +
                (adoption != null
                    ? adoption.ToDiagnosticString()
                    : "<no-result>"));
        }

        private static object ResolveActiveLifecycleOwner(
            Component runtimeHost,
            ActivityAsset expectedActivity)
        {
            return ResolveActiveLifecycleRecord(
                    runtimeHost,
                    expectedActivity)
                .Owner;
        }

        private static int ResolveActiveLifecycleEntryCount(
            Component runtimeHost,
            ActivityAsset expectedActivity)
        {
            LifecycleRecord record =
                ResolveActiveLifecycleRecord(
                    runtimeHost,
                    expectedActivity);

            return GetIntegerProperty(
                record.Lifecycle,
                "ActiveEntryCount");
        }

        private static LifecycleRecord ResolveActiveLifecycleRecord(
            Component runtimeHost,
            ActivityAsset expectedActivity)
        {
            Type preparationType =
                ResolveRuntimeType(
                    PreparationModuleTypeName);

            Component preparation =
                runtimeHost.GetComponent(
                    preparationType);

            Require(
                preparation != null,
                "FrameworkRuntimeHost has no PlayerActorPreparationRuntimeHostModule.");

            object composite =
                GetRequiredField(
                    preparation,
                    "sceneLocalPlayerCompositeLifecycleParticipant");

            Require(
                composite != null,
                "Player Actor preparation module has no Scene Local Player composite lifecycle.");

            object lifecycle =
                GetRequiredField(
                    composite,
                    "sceneLifecycle");

            Require(
                lifecycle != null,
                "Scene Local Player composite has no lifecycle runtime.");

            object activeRecord =
                GetRequiredField(
                    lifecycle,
                    "activeRecord");

            Require(
                activeRecord != null,
                "Scene Local Player lifecycle has no active record.");

            ActivityAsset activity =
                GetRequiredProperty(
                    activeRecord,
                    "Activity") as ActivityAsset;

            Require(
                ReferenceEquals(
                    activity,
                    expectedActivity),
                "Scene Local Player lifecycle retained a different Activity.");

            object owner =
                GetRequiredProperty(
                    activeRecord,
                    "Owner");

            Require(
                owner != null,
                "Scene Local Player lifecycle retained no owner.");

            return new LifecycleRecord(
                lifecycle,
                owner);
        }

        private static async Task TryRecoverActivityAAsync(
            Component runtimeHost,
            ActivityAsset activityA)
        {
            try
            {
                if (ReferenceEquals(
                        ResolveCurrentActivity(runtimeHost),
                        activityA))
                {
                    return;
                }

                object recovery =
                    await InvokeRequestAsync(
                        runtimeHost,
                        "RequestActivityAsync",
                        activityA,
                        nameof(
                            QaP3M4ESceneProvidedActivitySwitchRegression),
                        "qa-scene-provided-activity-switch-recovery");

                if (recovery == null ||
                    !GetBooleanProperty(
                        recovery,
                        "Succeeded"))
                {
                    Debug.LogError(
                        $"{LogPrefix} RECOVERY FAIL. result='{GetStringProperty(recovery, "Message")}'.");
                }
            }
            catch (Exception exception)
            {
                Exception effective =
                    Unwrap(
                        exception);

                Debug.LogError(
                    $"{LogPrefix} RECOVERY EXCEPTION. " +
                    $"exception='{effective.GetType().Name}' " +
                    $"message='{Escape(effective.Message)}'.");
            }
        }

        private static int CountActors(
            Transform actorMount)
        {
            return actorMount != null
                ? actorMount.GetComponentsInChildren<
                    PlayerActorDeclaration>(true).Length
                : 0;
        }

        private static SceneLocalPlayerAdmissionAuthoring
            ResolveSingleAuthoring(
                string scenePath)
        {
            Scene scene =
                SceneManager.GetSceneByPath(
                    scenePath);

            Require(
                scene.IsValid() &&
                scene.isLoaded,
                $"P3M4E target scene '{scenePath}' is not loaded.");

            SceneLocalPlayerAdmissionAuthoring resolved =
                null;

            int count =
                0;

            GameObject[] roots =
                scene.GetRootGameObjects();

            for (int rootIndex = 0;
                 rootIndex < roots.Length;
                 rootIndex++)
            {
                SceneLocalPlayerAdmissionAuthoring[] candidates =
                    roots[rootIndex].GetComponentsInChildren<
                        SceneLocalPlayerAdmissionAuthoring>(true);

                for (int candidateIndex = 0;
                     candidateIndex < candidates.Length;
                     candidateIndex++)
                {
                    if (candidates[candidateIndex] == null)
                    {
                        continue;
                    }

                    count++;
                    resolved =
                        candidates[candidateIndex];
                }
            }

            Require(
                count == 1,
                $"Expected exactly one Scene-Provided composer in '{scenePath}', found '{count}'.");

            return resolved;
        }

        private static Component ResolveCurrentRuntimeHost()
        {
            Type runtimeHostType =
                ResolveRuntimeType(
                    RuntimeHostTypeName);

            UnityEngine.Object[] materialized =
                Resources.FindObjectsOfTypeAll(
                    runtimeHostType);

            var candidates =
                new List<Component>();

            var seen =
                new HashSet<Component>();

            for (int index = 0;
                 index < materialized.Length;
                 index++)
            {
                if (!(materialized[index] is Component component) ||
                    component.gameObject == null ||
                    EditorUtility.IsPersistent(component) ||
                    !runtimeHostType.IsInstanceOfType(component) ||
                    !component.gameObject.scene.IsValid() ||
                    !component.gameObject.scene.isLoaded ||
                    EditorSceneManager.IsPreviewScene(
                        component.gameObject.scene) ||
                    !seen.Add(component))
                {
                    continue;
                }

                candidates.Add(
                    component);
            }

            Require(
                candidates.Count == 1,
                $"Expected exactly one FrameworkRuntimeHost, found '{candidates.Count}'.");

            return candidates[0];
        }

        private static RouteAsset ResolveCurrentRoute(
            Component runtimeHost)
        {
            object state =
                GetRequiredProperty(
                    runtimeHost,
                    "State");

            return GetRequiredProperty(
                    state,
                    "CurrentRoute") as RouteAsset;
        }

        private static ActivityAsset ResolveCurrentActivity(
            Component runtimeHost)
        {
            object state =
                GetRequiredProperty(
                    runtimeHost,
                    "State");

            return GetRequiredProperty(
                    state,
                    "CurrentActivity") as ActivityAsset;
        }

        private static object GetRequiredField(
            object target,
            string fieldName)
        {
            Require(
                target != null,
                $"Cannot read field '{fieldName}' from null.");

            FieldInfo field =
                target.GetType().GetField(
                    fieldName,
                    InstanceAny);

            Require(
                field != null,
                $"Field '{target.GetType().FullName}.{fieldName}' was not found.");

            return field.GetValue(
                target);
        }

        private static object GetRequiredProperty(
            object target,
            string propertyName)
        {
            Require(
                target != null,
                $"Cannot read property '{propertyName}' from null.");

            PropertyInfo property =
                target.GetType().GetProperty(
                    propertyName,
                    InstanceAny);

            Require(
                property != null,
                $"Property '{target.GetType().FullName}.{propertyName}' was not found.");

            return property.GetValue(
                target);
        }

        private static bool GetBooleanProperty(
            object target,
            string propertyName)
        {
            object value =
                GetRequiredProperty(
                    target,
                    propertyName);

            Require(
                value is bool,
                $"Property '{propertyName}' is not Boolean.");

            return (bool)value;
        }

        private static int GetIntegerProperty(
            object target,
            string propertyName)
        {
            object value =
                GetRequiredProperty(
                    target,
                    propertyName);

            Require(
                value is int,
                $"Property '{propertyName}' is not Int32.");

            return (int)value;
        }

        private static string GetStringProperty(
            object target,
            string propertyName)
        {
            if (target == null)
            {
                return string.Empty;
            }

            return GetRequiredProperty(
                    target,
                    propertyName) as string ??
                string.Empty;
        }

        private static MethodInfo GetMethod(
            Type type,
            string methodName,
            int parameterCount)
        {
            MethodInfo[] methods =
                type.GetMethods(
                    InstanceAny);

            MethodInfo resolved =
                null;

            for (int index = 0;
                 index < methods.Length;
                 index++)
            {
                MethodInfo candidate =
                    methods[index];

                if (!string.Equals(
                        candidate.Name,
                        methodName,
                        StringComparison.Ordinal) ||
                    candidate.GetParameters().Length !=
                    parameterCount)
                {
                    continue;
                }

                Require(
                    resolved == null,
                    $"Method '{type.FullName}.{methodName}' with '{parameterCount}' parameters is ambiguous.");

                resolved =
                    candidate;
            }

            Require(
                resolved != null,
                $"Method '{type.FullName}.{methodName}' with '{parameterCount}' parameters was not found.");

            return resolved;
        }

        private static Type ResolveRuntimeType(
            string typeName)
        {
            Type resolved =
                Type.GetType(
                    typeName);

            if (resolved != null)
            {
                return resolved;
            }

            Assembly[] assemblies =
                AppDomain.CurrentDomain
                    .GetAssemblies();

            for (int index = 0;
                 index < assemblies.Length;
                 index++)
            {
                resolved =
                    assemblies[index].GetType(
                        typeName,
                        false);

                if (resolved != null)
                {
                    return resolved;
                }
            }

            throw new InvalidOperationException(
                $"Runtime type '{typeName}' was not found.");
        }

        private static Exception Unwrap(
            Exception exception)
        {
            Exception current =
                exception;

            while (current is TargetInvocationException invocation &&
                   invocation.InnerException != null)
            {
                current =
                    invocation.InnerException;
            }

            return current;
        }

        private static void Require(
            bool condition,
            string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(
                    message);
            }
        }

        private static string Escape(
            string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }

        private readonly struct LifecycleRecord
        {
            internal LifecycleRecord(
                object lifecycle,
                object owner)
            {
                Lifecycle =
                    lifecycle;

                Owner =
                    owner;
            }

            internal object Lifecycle { get; }

            internal object Owner { get; }
        }
    }
}
