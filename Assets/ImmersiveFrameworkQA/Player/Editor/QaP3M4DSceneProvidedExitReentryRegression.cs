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
    /// One-shot Play Mode proof that Activity-scoped Scene-Provided admission
    /// can exit and reenter while preserving the exact scene-owned Host and Actor.
    ///
    /// The smoke operates the real active SceneLocalPlayerAdmissionActivityLifecycleRuntime
    /// with the exact retained Activity owner. It restores the entered state before completion.
    /// </summary>
    internal static class QaP3M4DSceneProvidedExitReentryRegression
    {
        private const string RuntimeHostTypeName =
            "Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost";

        private const string PreparationModuleTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerActorPreparationRuntimeHostModule";

        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Player/Run P3M4D Scene-Provided Exit Reentry Regression";

        private const string LogPrefix =
            "[QA][P3M4D Scene-Provided Exit Reentry]";

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
                "P3M4D regression must run in Play Mode.");

            completed.Add(
                "play-mode-required");

            RouteAsset targetRoute =
                AssetDatabase.LoadAssetAtPath<RouteAsset>(
                    QaP3M4BRouteSceneProvidedAdmissionSetup.RoutePath);

            ActivityAsset targetActivity =
                AssetDatabase.LoadAssetAtPath<ActivityAsset>(
                    QaP3M4BRouteSceneProvidedAdmissionSetup.ActivityPath);

            Require(
                targetRoute != null &&
                targetActivity != null,
                "P3M4D fixture assets are missing. Run the P3M4D setup menu in Edit Mode.");

            Require(
                ReferenceEquals(
                    targetRoute.StartupActivity,
                    targetActivity),
                "P3M4D target Route does not reference the expected Startup Activity.");

            completed.Add(
                "fixture-assets-loaded");

            Component runtimeHost =
                ResolveCurrentRuntimeHost();

            completed.Add(
                "runtime-host-resolved");

            RouteAsset previousRoute =
                await WaitForRuntimeHostReadyAsync(
                    runtimeHost);

            completed.Add(
                "runtime-host-ready");

            Require(
                !ReferenceEquals(
                    previousRoute,
                    targetRoute),
                "P3M4D is one-shot. Re-enter Play Mode before running it again.");

            object routeTaskObject =
                InvokeTask(
                    runtimeHost,
                    "RequestRouteAsync",
                    targetRoute,
                    nameof(
                        QaP3M4DSceneProvidedExitReentryRegression),
                    "qa-scene-provided-exit-reentry");

            var routeTask =
                (Task)routeTaskObject;

            await routeTask;

            object routeResult =
                GetTaskResult(
                    routeTaskObject);

            Require(
                routeResult != null &&
                GetBooleanProperty(
                    routeResult,
                    "Succeeded"),
                "P3M4D target Route request failed. " +
                GetStringProperty(
                    routeResult,
                    "Message"));

            Require(
                ReferenceEquals(
                    ResolveCurrentRoute(runtimeHost),
                    targetRoute) &&
                ReferenceEquals(
                    ResolveCurrentActivity(runtimeHost),
                    targetActivity),
                "P3M4D target Route and Startup Activity are not current after the successful request.");

            completed.Add(
                "route-request-succeeded");

            SceneLocalPlayerAdmissionAuthoring authoring =
                ResolveSingleAuthoring(
                    targetRoute.PrimaryScenePath);

            Require(
                authoring != null &&
                authoring.RuntimeReady,
                "P3M4D Route Primary Scene composer is not RuntimeReady.");

            completed.Add(
                "route-primary-composer-resolved");

            LocalPlayerHostAuthoring host =
                authoring.LocalPlayerHost;

            PlayerActorDeclaration actor =
                authoring.SceneLogicalPlayerActor;

            Require(
                host != null &&
                actor != null,
                "P3M4D composer has no Host or Scene Logical Player Actor.");

            Require(
                authoring.TryGetPlayerSlotId(
                    out PlayerSlotId expectedSlot,
                    out string slotIssue),
                "P3M4D composer has no valid Player Slot. " +
                slotIssue);

            Require(
                authoring.HasActiveAdmission,
                "P3M4D initial Scene-Provided admission is not active.");

            completed.Add(
                "initial-admission-active");

            Require(
                host.IsJoined &&
                host.HasJoinedSlot &&
                host.JoinedPlayerSlotId ==
                expectedSlot,
                "P3M4D initial Host is not joined to the expected Slot.");

            completed.Add(
                "initial-host-joined");

            ScenePlayerActorAdoptionResult initialAdoption =
                authoring.LastActorAdoptionResult;

            Require(
                initialAdoption != null &&
                initialAdoption.Succeeded &&
                initialAdoption.Token.IsValid &&
                ReferenceEquals(
                    initialAdoption.SceneActor,
                    actor),
                "P3M4D initial Scene Actor adoption is missing or invalid.");

            completed.Add(
                "initial-actor-adopted");

            GameObject hostObject =
                host.gameObject;

            GameObject actorObject =
                actor.gameObject;

            Transform actorParent =
                actor.transform.parent;

            Scene physicalScene =
                hostObject.scene;

            Require(
                physicalScene.IsValid() &&
                physicalScene.isLoaded &&
                string.Equals(
                    physicalScene.path,
                    targetRoute.PrimaryScenePath,
                    StringComparison.Ordinal),
                "P3M4D physical Host is not in the target Route Primary Scene.");

            Require(
                actorParent != null &&
                host.ActorMount != null &&
                (ReferenceEquals(
                     actor.transform,
                     host.ActorMount) ||
                 actor.transform.IsChildOf(
                     host.ActorMount)),
                "P3M4D Actor is not under the Host Actor Mount.");

            Require(
                CountActors(
                    host.ActorMount) == 1,
                "P3M4D initial Actor Mount does not contain exactly one Player Actor.");

            completed.Add(
                "physical-references-captured");

            LifecycleAccess lifecycle =
                ResolveActiveLifecycle(
                    runtimeHost,
                    targetActivity);

            completed.Add(
                "lifecycle-runtime-resolved");

            Require(
                lifecycle.Owner != null,
                "P3M4D active lifecycle owner was not captured.");

            completed.Add(
                "lifecycle-owner-captured");

            bool exited =
                false;

            bool reentered =
                false;

            try
            {
                SceneLocalPlayerAdmissionActivityLifecycleResult exitResult =
                    InvokeLifecycle(
                        lifecycle.Runtime,
                        lifecycle.TryExit,
                        targetActivity,
                        lifecycle.Owner,
                        "QaP3M4DSceneProvidedExitReentryRegression",
                        "qa-scene-provided-explicit-exit");

                Require(
                    exitResult != null &&
                    exitResult.Succeeded &&
                    exitResult.AffectedCount == 1 &&
                    !exitResult.HasBlockingIssues,
                    "P3M4D Scene-Provided lifecycle exit failed. " +
                    (exitResult != null
                        ? exitResult.ToDiagnosticString()
                        : "<no-result>"));

                exited =
                    true;

                completed.Add(
                    "exit-succeeded");

                Require(
                    !authoring.HasActiveAdmission,
                    "Scene-Provided admission remained active after lifecycle exit.");

                completed.Add(
                    "admission-released");

                Require(
                    !host.IsJoined &&
                    !host.HasJoinedSlot &&
                    host.JoinedConfiguredIndex == -1,
                    "Local Player Host retained joined Slot evidence after lifecycle exit.");

                completed.Add(
                    "host-slot-released");

                ScenePlayerActorAdoptionResult releaseAdoption =
                    authoring.LastActorAdoptionResult;

                Require(
                    releaseAdoption != null &&
                    releaseAdoption.Succeeded &&
                    releaseAdoption.Status ==
                    ScenePlayerActorAdoptionStatus.SucceededReleased &&
                    ReferenceEquals(
                        releaseAdoption.SceneActor,
                        actor),
                    "Scene Actor adoption release did not complete explicitly.");

                completed.Add(
                    "adoption-released");

                Require(
                    !HasSceneAdoption(
                        lifecycle.PreparationModule,
                        expectedSlot),
                    "Player Actor preparation runtime retained Scene adoption bookkeeping after exit.");

                completed.Add(
                    "adoption-bookkeeping-released");

                Require(
                    host != null &&
                    ReferenceEquals(
                        host.gameObject,
                        hostObject) &&
                    hostObject.scene ==
                    physicalScene &&
                    hostObject.scene.isLoaded,
                    "Scene-owned Local Player Host was destroyed, replaced or moved during lifecycle exit.");

                completed.Add(
                    "physical-host-preserved");

                Require(
                    actor != null &&
                    ReferenceEquals(
                        actor.gameObject,
                        actorObject) &&
                    ReferenceEquals(
                        authoring.SceneLogicalPlayerActor,
                        actor) &&
                    actorObject.scene ==
                    physicalScene &&
                    actorObject.scene.isLoaded &&
                    ReferenceEquals(
                        actor.transform.parent,
                        actorParent),
                    "External Scene Actor was destroyed, replaced, moved or reparented during lifecycle exit.");

                Require(
                    CountActors(
                        host.ActorMount) == 1,
                    "Lifecycle exit duplicated or removed the external Scene Actor.");

                completed.Add(
                    "physical-actor-preserved");

                Require(
                    authoring.ActorPhysicalOwnership ==
                    PlayerActorPhysicalOwnership.ExternalSceneOwned,
                    "Scene Actor physical ownership changed during lifecycle exit.");

                completed.Add(
                    "external-ownership-preserved");

                SceneLocalPlayerAdmissionActivityLifecycleResult enterResult =
                    InvokeLifecycle(
                        lifecycle.Runtime,
                        lifecycle.TryEnter,
                        targetActivity,
                        lifecycle.Owner,
                        "QaP3M4DSceneProvidedExitReentryRegression",
                        "qa-scene-provided-explicit-reentry");

                Require(
                    enterResult != null &&
                    enterResult.Succeeded &&
                    enterResult.AffectedCount == 1 &&
                    !enterResult.HasBlockingIssues,
                    "P3M4D Scene-Provided lifecycle reentry failed. " +
                    (enterResult != null
                        ? enterResult.ToDiagnosticString()
                        : "<no-result>"));

                reentered =
                    true;

                completed.Add(
                    "reentry-succeeded");

                Require(
                    authoring.HasActiveAdmission,
                    "Scene-Provided admission was not restored by lifecycle reentry.");

                completed.Add(
                    "admission-restored");

                Require(
                    ReferenceEquals(
                        authoring.LocalPlayerHost,
                        host) &&
                    ReferenceEquals(
                        host.gameObject,
                        hostObject) &&
                    host.IsJoined,
                    "Lifecycle reentry did not readmit the same physical Host.");

                completed.Add(
                    "same-host-readmitted");

                Require(
                    host.HasJoinedSlot &&
                    host.JoinedPlayerSlotId ==
                    expectedSlot,
                    "Lifecycle reentry joined the Host to the wrong Slot.");

                completed.Add(
                    "exact-slot-readmitted");

                ScenePlayerActorAdoptionResult reentryAdoption =
                    authoring.LastActorAdoptionResult;

                Require(
                    reentryAdoption != null &&
                    reentryAdoption.Succeeded &&
                    reentryAdoption.Token.IsValid &&
                    ReferenceEquals(
                        reentryAdoption.SceneActor,
                        actor),
                    "Lifecycle reentry did not readopt the same Scene Actor.");

                Require(
                    !reentryAdoption.Token.Equals(
                        initialAdoption.Token),
                    "Lifecycle reentry reused the stale initial adoption token.");

                completed.Add(
                    "new-adoption-token-created");

                Require(
                    ReferenceEquals(
                        authoring.SceneLogicalPlayerActor,
                        actor) &&
                    ReferenceEquals(
                        actor.gameObject,
                        actorObject) &&
                    ReferenceEquals(
                        actor.transform.parent,
                        actorParent),
                    "Lifecycle reentry replaced or moved the external Scene Actor.");

                completed.Add(
                    "same-actor-readopted");

                Require(
                    CountActors(
                        host.ActorMount) == 1,
                    "Lifecycle reentry created duplicate Player Actors.");

                completed.Add(
                    "no-duplicate-actor");

                Require(
                    HasSceneAdoption(
                        lifecycle.PreparationModule,
                        expectedSlot),
                    "Player Actor preparation runtime did not retain the restored Scene adoption.");

                completed.Add(
                    "adoption-bookkeeping-restored");

                Require(
                    GetIntegerProperty(
                        lifecycle.Runtime,
                        "ActiveEntryCount") == 1,
                    "Scene-Provided lifecycle did not restore exactly one active entry.");

                completed.Add(
                    "active-entry-restored");

                Require(
                    ReferenceEquals(
                        ResolveCurrentRoute(runtimeHost),
                        targetRoute) &&
                    ReferenceEquals(
                        ResolveCurrentActivity(runtimeHost),
                        targetActivity),
                    "Direct Scene lifecycle verification changed the canonical Route or Activity identity.");

                completed.Add(
                    "canonical-route-activity-preserved");
            }
            finally
            {
                if (exited &&
                    !reentered)
                {
                    TryRecoverLifecycle(
                        lifecycle,
                        targetActivity);
                }
            }
        }

        private static LifecycleAccess ResolveActiveLifecycle(
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
                "Player Actor preparation module has no composed Scene Local Player lifecycle participant.");

            object lifecycle =
                GetRequiredField(
                    composite,
                    "sceneLifecycle");

            Require(
                lifecycle != null,
                "Scene Local Player composite has no Activity lifecycle runtime.");

            object activeRecord =
                GetRequiredField(
                    lifecycle,
                    "activeRecord");

            Require(
                activeRecord != null,
                "Scene Local Player lifecycle has no active record after P3M4B Route entry.");

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

            MethodInfo tryExit =
                GetMethod(
                    lifecycle.GetType(),
                    "TryExit",
                    4);

            MethodInfo tryEnter =
                GetMethod(
                    lifecycle.GetType(),
                    "TryEnter",
                    4);

            return new LifecycleAccess(
                preparation,
                lifecycle,
                owner,
                tryExit,
                tryEnter);
        }

        private static SceneLocalPlayerAdmissionActivityLifecycleResult
            InvokeLifecycle(
                object lifecycle,
                MethodInfo method,
                ActivityAsset activity,
                object owner,
                string source,
                string reason)
        {
            object raw =
                method.Invoke(
                    lifecycle,
                    new[]
                    {
                        (object)activity,
                        owner,
                        source,
                        reason
                    });

            return raw as
                SceneLocalPlayerAdmissionActivityLifecycleResult;
        }

        private static void TryRecoverLifecycle(
            LifecycleAccess lifecycle,
            ActivityAsset activity)
        {
            try
            {
                SceneLocalPlayerAdmissionActivityLifecycleResult recovery =
                    InvokeLifecycle(
                        lifecycle.Runtime,
                        lifecycle.TryEnter,
                        activity,
                        lifecycle.Owner,
                        "QaP3M4DSceneProvidedExitReentryRegression",
                        "qa-scene-provided-failure-recovery");

                if (recovery == null ||
                    !recovery.Succeeded)
                {
                    Debug.LogError(
                        $"{LogPrefix} RECOVERY FAIL. result='{recovery?.ToDiagnosticString() ?? "<no-result>"}'.");
                }
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{LogPrefix} RECOVERY EXCEPTION. " +
                    $"exception='{Unwrap(exception).GetType().Name}' " +
                    $"message='{Escape(Unwrap(exception).Message)}'.");
            }
        }

        private static bool HasSceneAdoption(
            Component preparationModule,
            PlayerSlotId playerSlotId)
        {
            MethodInfo method =
                GetMethod(
                    preparationModule.GetType(),
                    "TryGetScenePlayerActorAdoption",
                    2);

            object[] arguments =
            {
                playerSlotId,
                null
            };

            object raw =
                method.Invoke(
                    preparationModule,
                    arguments);

            Require(
                raw is bool,
                "TryGetScenePlayerActorAdoption did not return Boolean.");

            return (bool)raw;
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
                $"P3M4D target scene '{scenePath}' is not loaded.");

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

        private static async Task<RouteAsset> WaitForRuntimeHostReadyAsync(
            Component runtimeHost)
        {
            string diagnostic =
                "FrameworkRuntimeHost readiness was not observed.";

            for (int frame = 0;
                 frame < 300;
                 frame++)
            {
                object state =
                    GetRequiredProperty(
                        runtimeHost,
                        "State");

                bool gameFlowStarted =
                    GetRequiredProperty(
                        state,
                        "GameFlowStarted") is bool started &&
                    started;

                RouteAsset currentRoute =
                    GetRequiredProperty(
                        state,
                        "CurrentRoute") as RouteAsset;

                ActivityAsset currentActivity =
                    GetRequiredProperty(
                        state,
                        "CurrentActivity") as ActivityAsset;

                bool activityReady =
                    GetRequiredProperty(
                        state,
                        "IsActivityReady") is bool ready &&
                    ready;

                if (gameFlowStarted &&
                    currentRoute != null &&
                    currentActivity != null &&
                    activityReady)
                {
                    return currentRoute;
                }

                diagnostic =
                    $"gameFlowStarted='{gameFlowStarted}' " +
                    $"route='{currentRoute?.RouteName ?? string.Empty}' " +
                    $"activity='{currentActivity?.ActivityName ?? string.Empty}' " +
                    $"activityReady='{activityReady}'.";

                await Awaitable.NextFrameAsync();
            }

            throw new InvalidOperationException(
                "FrameworkRuntimeHost did not become ready within 300 frames. " +
                diagnostic);
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

        private static object InvokeTask(
            object target,
            string methodName,
            params object[] arguments)
        {
            MethodInfo method =
                GetMethod(
                    target.GetType(),
                    methodName,
                    arguments.Length);

            object taskObject =
                method.Invoke(
                    target,
                    arguments);

            Require(
                taskObject is Task,
                $"Async method '{methodName}' did not return a Task.");

            return taskObject;
        }

        private static object GetTaskResult(
            object taskObject)
        {
            Require(
                taskObject is Task task &&
                task.IsCompleted,
                "Cannot read an incomplete Route request Task.");

            PropertyInfo resultProperty =
                taskObject.GetType().GetProperty(
                    "Result",
                    InstanceAny);

            Require(
                resultProperty != null,
                "Route request Task has no Result property.");

            return resultProperty.GetValue(
                taskObject);
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

        private readonly struct LifecycleAccess
        {
            internal LifecycleAccess(
                Component preparationModule,
                object runtime,
                object owner,
                MethodInfo tryExit,
                MethodInfo tryEnter)
            {
                PreparationModule =
                    preparationModule;

                Runtime =
                    runtime;

                Owner =
                    owner;

                TryExit =
                    tryExit;

                TryEnter =
                    tryEnter;
            }

            internal Component PreparationModule { get; }

            internal object Runtime { get; }

            internal object Owner { get; }

            internal MethodInfo TryExit { get; }

            internal MethodInfo TryEnter { get; }
        }
    }
}
