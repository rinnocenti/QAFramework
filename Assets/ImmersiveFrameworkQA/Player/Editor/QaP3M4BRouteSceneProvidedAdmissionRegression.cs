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

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// One-shot Play Mode proof that a Scene-Provided Player authored in the
    /// active Route Primary Scene is admitted by the Route Startup Activity.
    ///
    /// Before the package correction, the intended RED point is
    /// route-primary-player-admitted: the composer is RuntimeReady but has no
    /// active Activity admission token.
    /// </summary>
    internal static class QaP3M4BRouteSceneProvidedAdmissionRegression
    {
        private const string RuntimeHostTypeName =
            "Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost";

        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Player/Run P3M4B Route Scene-Provided Admission Regression";

        private const string LogPrefix =
            "[QA][P3M4B Route Scene-Provided Admission]";

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
                await RunAsync(completed);

                Debug.Log(
                    $"{LogPrefix} PASS. status='Passed' " +
                    $"cases='{completed.Count}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Exception effective =
                    exception is TargetInvocationException invocation &&
                    invocation.InnerException != null
                        ? invocation.InnerException
                        : exception;

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
                "P3M4B regression must run in Play Mode.");

            completed.Add(
                "play-mode-required");

            RouteAsset targetRoute =
                AssetDatabase.LoadAssetAtPath<
                    RouteAsset>(
                        QaP3M4BRouteSceneProvidedAdmissionSetup
                            .RoutePath);

            ActivityAsset targetActivity =
                AssetDatabase.LoadAssetAtPath<
                    ActivityAsset>(
                        QaP3M4BRouteSceneProvidedAdmissionSetup
                            .ActivityPath);

            Require(
                targetRoute != null &&
                targetActivity != null,
                "P3M4B fixture assets are missing. Run the P3M4B setup menu in Edit Mode first.");

            Require(
                targetRoute.StartupActivity ==
                targetActivity,
                "P3M4B target Route does not reference the expected Startup Activity.");

            completed.Add(
                "fixture-assets-loaded");

            object runtimeHost =
                ResolveCurrentRuntimeHost();

            completed.Add(
                "runtime-host-resolved");

            RouteAsset previousRoute =
                ResolveCurrentRoute(
                    runtimeHost);

            Require(
                previousRoute != null,
                "FrameworkRuntimeHost has no current Route before P3M4B.");

            Require(
                !ReferenceEquals(
                    previousRoute,
                    targetRoute),
                "P3M4B is one-shot. Re-enter Play Mode before running it again.");

            object routeTaskObject =
                InvokeTask(
                    runtimeHost,
                    "RequestRouteAsync",
                    targetRoute,
                    nameof(
                        QaP3M4BRouteSceneProvidedAdmissionRegression),
                    "qa-route-primary-scene-provided-admission");

            Require(
                routeTaskObject is Task,
                "FrameworkRuntimeHost.RequestRouteAsync did not return a Task.");

            var routeTask =
                (Task)routeTaskObject;

            completed.Add(
                "route-request-started");

            RouteRequestObservation observation =
                await ObserveRouteRequestAsync(
                    routeTask,
                    targetRoute.PrimaryScenePath);

            await routeTask;

            object requestResult =
                GetTaskResult(
                    routeTaskObject);

            Require(
                requestResult != null,
                "FrameworkRuntimeHost returned no Route request result.");

            completed.Add(
                "route-request-returned");

            Require(
                observation.ComposerSeen,
                "The Route Primary Scene was requested, but no Scene-Provided composer was observed while the Route transaction was active. " +
                BuildRequestDiagnostic(
                    requestResult,
                    runtimeHost));

            completed.Add(
                "route-primary-composer-resolved");

            Require(
                observation.EvidenceValid,
                "Route Primary Scene composer was observed with invalid runtime evidence. " +
                observation.EvidenceIssue);

            completed.Add(
                "authoring-evidence-valid");

            Require(
                observation.RuntimeReadySeen,
                "Route Primary Scene composer was observed, but it never became RuntimeReady during the Route transaction. " +
                $"runtimeDiagnostic='{observation.RuntimeDiagnostic}'. " +
                BuildRequestDiagnostic(
                    requestResult,
                    runtimeHost));

            completed.Add(
                "runtime-ready");

            // Intended RED point before SCENE-PROVIDED-ROUTE-ADMISSION-1.
            Require(
                observation.ActiveAdmissionSeen,
                "Route Primary Scene composer became RuntimeReady during the Route transaction, " +
                "but no active Activity admission token was created before the transaction reached its terminal state. " +
                "This is the expected RED before the package lifecycle correction. " +
                $"runtimeDiagnostic='{observation.RuntimeDiagnostic}'. " +
                BuildRequestDiagnostic(
                    requestResult,
                    runtimeHost));

            completed.Add(
                "route-primary-player-admitted");

            SceneLocalPlayerAdmissionAuthoring authoring =
                observation.Authoring;

            Require(
                authoring != null,
                "The admitted Scene-Provided composer did not survive the successful Route request.");

            LocalPlayerHostAuthoring host =
                authoring.LocalPlayerHost;

            Require(
                host != null &&
                host.IsJoined,
                "Route Primary Scene Local Player Host was not joined.");

            completed.Add(
                "slot-joined");

            Require(
                authoring.TryGetPlayerSlotId(
                    out PlayerSlotId expectedSlot,
                    out string slotIssue),
                "P3M4B composer has no valid Player Slot. " +
                slotIssue);

            Require(
                host.HasJoinedSlot &&
                host.JoinedPlayerSlotId ==
                expectedSlot,
                "Joined Host retained the wrong Player Slot.");

            completed.Add(
                "exact-slot-retained");

            ScenePlayerActorAdoptionResult adoption =
                authoring.LastActorAdoptionResult;

            Require(
                adoption != null,
                "Scene-Provided Actor adoption result is missing.");

            RequireAdoptionSucceeded(
                adoption);

            completed.Add(
                "scene-actor-adopted");

            Require(
                authoring.ActorPhysicalOwnership ==
                PlayerActorPhysicalOwnership
                    .ExternalSceneOwned,
                "Scene-Provided Actor physical ownership was not preserved as ExternalSceneOwned.");

            completed.Add(
                "external-scene-ownership-preserved");

            PlayerActorDeclaration actor =
                authoring.SceneLogicalPlayerActor;

            Require(
                actor != null &&
                actor.gameObject.scene ==
                authoring.gameObject.scene,
                "Scene-Provided Logical Actor is missing or moved out of the Route Primary Scene.");

            Require(
                host.ActorMount != null &&
                host.ActorMount.GetComponentsInChildren<
                    PlayerActorDeclaration>(true).Length == 1,
                "Route admission created or retained duplicate Player Actor declarations.");

            completed.Add(
                "single-scene-actor-preserved");

            Require(
                GetBooleanProperty(
                    requestResult,
                    "Succeeded"),
                "Route request did not complete successfully after Scene-Provided admission. " +
                GetStringProperty(
                    requestResult,
                    "Message"));

            completed.Add(
                "activity-readiness-ready");
        }

        private static async Task<RouteRequestObservation>
            ObserveRouteRequestAsync(
                Task routeTask,
                string targetScenePath)
        {
            const int MaxFrames = 600;
            const int PostCompletionFrames = 2;

            var observation =
                new RouteRequestObservation();

            int remainingPostCompletionFrames =
                PostCompletionFrames;

            for (int frame = 0;
                 frame < MaxFrames;
                 frame++)
            {
                SceneLocalPlayerAdmissionAuthoring candidate =
                    ResolveTargetAuthoringOrNull(
                        targetScenePath);

                observation.Observe(
                    candidate);

                if (routeTask.IsCompleted)
                {
                    if (remainingPostCompletionFrames <= 0)
                    {
                        return observation;
                    }

                    remainingPostCompletionFrames--;
                }

                await Awaitable.NextFrameAsync();
            }

            throw new InvalidOperationException(
                "P3M4B Route request did not reach a terminal state before the observation timeout.");
        }

        private static SceneLocalPlayerAdmissionAuthoring
            ResolveTargetAuthoringOrNull(
                string targetScenePath)
        {
            SceneLocalPlayerAdmissionAuthoring[] candidates =
                UnityEngine.Object.FindObjectsByType<
                    SceneLocalPlayerAdmissionAuthoring>(
                        FindObjectsInactive.Include);

            SceneLocalPlayerAdmissionAuthoring resolved =
                null;

            int count = 0;

            for (int index = 0;
                 index < candidates.Length;
                 index++)
            {
                SceneLocalPlayerAdmissionAuthoring candidate =
                    candidates[index];

                if (candidate == null ||
                    !candidate.gameObject.scene.IsValid() ||
                    !candidate.gameObject.scene.isLoaded ||
                    EditorSceneManager.IsPreviewScene(
                        candidate.gameObject.scene) ||
                    !string.Equals(
                        candidate.gameObject.scene.path,
                        targetScenePath,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                count++;
                resolved =
                    candidate;
            }

            Require(
                count <= 1,
                $"Expected at most one Scene-Provided composer in target Route Primary Scene '{targetScenePath}', found '{count}'.");

            return resolved;
        }

        private static object ResolveCurrentRuntimeHost()
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

                candidates.Add(component);
            }

            Require(
                candidates.Count == 1,
                $"Expected exactly one FrameworkRuntimeHost, found '{candidates.Count}'.");

            return candidates[0];
        }

        private static RouteAsset ResolveCurrentRoute(
            object runtimeHost)
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
            object runtimeHost)
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
                    methodName);

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

        private static string BuildRequestDiagnostic(
            object requestResult,
            object runtimeHost)
        {
            RouteAsset currentRoute =
                ResolveCurrentRoute(
                    runtimeHost);

            ActivityAsset currentActivity =
                ResolveCurrentActivity(
                    runtimeHost);

            return
                $"requestSucceeded='{GetBooleanProperty(requestResult, "Succeeded")}' " +
                $"requestMessage='{GetStringProperty(requestResult, "Message")}' " +
                $"currentRoute='{currentRoute?.RouteName ?? "<none>"}' " +
                $"currentActivity='{currentActivity?.ActivityName ?? "<none>"}'.";
        }

        private static void RequireAdoptionSucceeded(
            ScenePlayerActorAdoptionResult adoption)
        {
            PropertyInfo succeededProperty =
                adoption.GetType().GetProperty(
                    "Succeeded",
                    InstanceAny);

            if (succeededProperty != null &&
                succeededProperty.PropertyType ==
                typeof(bool))
            {
                Require(
                    (bool)succeededProperty.GetValue(
                        adoption),
                    "Scene Player Actor adoption reported failure. " +
                    adoption.ToDiagnosticString());

                return;
            }

            PropertyInfo statusProperty =
                adoption.GetType().GetProperty(
                    "Status",
                    InstanceAny);

            Require(
                statusProperty != null,
                "Scene Player Actor adoption exposes neither Succeeded nor Status.");

            string status =
                statusProperty.GetValue(
                    adoption)?.ToString() ??
                string.Empty;

            Require(
                status.IndexOf(
                    "fail",
                    StringComparison.OrdinalIgnoreCase) < 0 &&
                status.IndexOf(
                    "reject",
                    StringComparison.OrdinalIgnoreCase) < 0 &&
                status.IndexOf(
                    "invalid",
                    StringComparison.OrdinalIgnoreCase) < 0 &&
                status.IndexOf(
                    "none",
                    StringComparison.OrdinalIgnoreCase) < 0,
                "Scene Player Actor adoption did not succeed. " +
                adoption.ToDiagnosticString());
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

        private static string GetStringProperty(
            object target,
            string propertyName)
        {
            object value =
                GetRequiredProperty(
                    target,
                    propertyName);

            return value as string ??
                string.Empty;
        }

        private static object GetRequiredProperty(
            object target,
            string propertyName)
        {
            Require(
                target != null,
                $"Cannot read '{propertyName}' from null.");

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

        private static MethodInfo GetMethod(
            Type type,
            string methodName)
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
                if (!string.Equals(
                        methods[index].Name,
                        methodName,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (resolved != null)
                {
                    throw new InvalidOperationException(
                        $"Method '{type.FullName}.{methodName}' is ambiguous.");
                }

                resolved =
                    methods[index];
            }

            Require(
                resolved != null,
                $"Method '{type.FullName}.{methodName}' was not found.");

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

        private sealed class RouteRequestObservation
        {
            internal SceneLocalPlayerAdmissionAuthoring Authoring { get; private set; }
            internal bool ComposerSeen { get; private set; }
            internal bool EvidenceValid { get; private set; }
            internal string EvidenceIssue { get; private set; } = string.Empty;
            internal bool RuntimeReadySeen { get; private set; }
            internal string RuntimeDiagnostic { get; private set; } = string.Empty;
            internal bool ActiveAdmissionSeen { get; private set; }

            internal void Observe(
                SceneLocalPlayerAdmissionAuthoring candidate)
            {
                if (candidate == null)
                {
                    return;
                }

                ComposerSeen =
                    true;

                Authoring =
                    candidate;

                bool evidenceValid =
                    candidate.TryValidateRuntimeEvidence(
                        out string evidenceIssue);

                if (evidenceValid)
                {
                    EvidenceValid =
                        true;
                }
                else if (!EvidenceValid)
                {
                    EvidenceIssue =
                        evidenceIssue ?? string.Empty;
                }

                if (candidate.RuntimeReady)
                {
                    RuntimeReadySeen =
                        true;

                    RuntimeDiagnostic =
                        candidate.RuntimeDiagnostic ?? string.Empty;
                }
                else if (!RuntimeReadySeen)
                {
                    RuntimeDiagnostic =
                        candidate.RuntimeDiagnostic ?? string.Empty;
                }

                if (candidate.HasActiveAdmission)
                {
                    ActiveAdmissionSeen =
                        true;
                }
            }
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
    }
}
