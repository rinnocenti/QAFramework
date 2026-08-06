using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Play Mode scope regression for Scene-Provided Player automatic authoring
    /// resolution. It exercises the real host-scoped module without performing
    /// admission, selection or Actor adoption.
    /// </summary>
    internal static class QaP3M4CSceneProvidedAdmissionScopeRegression
    {
        private const string RuntimeHostTypeName =
            "Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost";

        private const string ModuleTypeName =
            "Immersive.Framework.PlayerParticipation.SceneLocalPlayerAdmissionRuntimeHostModule";

        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Player/Run P3M4C Scene-Provided Scope Regression";

        private const string LogPrefix =
            "[QA][P3M4C Scene-Provided Scope]";

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
                "P3M4C regression must run in Play Mode.");

            completed.Add(
                "play-mode-required");

            RouteAsset targetRoute =
                LoadAsset<RouteAsset>(
                    QaP3M4BRouteSceneProvidedAdmissionSetup.RoutePath);

            ActivityAsset targetActivity =
                LoadAsset<ActivityAsset>(
                    QaP3M4BRouteSceneProvidedAdmissionSetup.ActivityPath);

            RouteAsset neutralRoute =
                LoadAsset<RouteAsset>(
                    QaP3M4CSceneProvidedAdmissionScopeSetup.NeutralRoutePath);

            RouteAsset foreignRoute =
                LoadAsset<RouteAsset>(
                    QaP3M4CSceneProvidedAdmissionScopeSetup.ForeignRoutePath);

            ActivityAsset activityContentActivity =
                LoadAsset<ActivityAsset>(
                    QaP3M4CSceneProvidedAdmissionScopeSetup.ActivityContentActivityPath);

            ActivityAsset mismatchActivity =
                LoadAsset<ActivityAsset>(
                    QaP3M4CSceneProvidedAdmissionScopeSetup.MismatchActivityPath);

            GameObject playerPrefab =
                LoadAsset<GameObject>(
                    QaP3M4BRouteSceneProvidedAdmissionSetup.PlayerPrefabPath);

            Require(
                targetRoute != null &&
                targetActivity != null &&
                neutralRoute != null &&
                foreignRoute != null &&
                activityContentActivity != null &&
                mismatchActivity != null &&
                playerPrefab != null,
                "P3M4C fixture assets are incomplete. Run the P3M4C setup menu in Edit Mode.");

            Require(
                string.Equals(
                    foreignRoute.PrimaryScenePath,
                    QaP3M4CSceneProvidedAdmissionScopeSetup.ForeignRouteScenePath,
                    StringComparison.Ordinal),
                "P3M4C Foreign Route does not own the expected Primary Scene.");

            completed.Add(
                "fixture-assets-loaded");

            Component runtimeHost =
                ResolveCurrentRuntimeHost();

            completed.Add(
                "runtime-host-resolved");

            Component module =
                ResolveSceneLocalPlayerModule(
                    runtimeHost);

            MethodInfo setContext =
                GetMethod(
                    module.GetType(),
                    "SetActivityLifecycleContext",
                    2);

            MethodInfo resolveAutomatic =
                GetMethod(
                    module.GetType(),
                    "TryResolveAutomaticActivityAuthoring",
                    3);

            completed.Add(
                "scene-local-player-module-resolved");

            var fixtureScenePaths =
                new[]
                {
                    QaP3M4BRouteSceneProvidedAdmissionSetup.ScenePath,
                    QaP3M4CSceneProvidedAdmissionScopeSetup.ActivityContentScenePath,
                    QaP3M4CSceneProvidedAdmissionScopeSetup.UnrelatedScenePath,
                    QaP3M4CSceneProvidedAdmissionScopeSetup.ForeignRouteScenePath,
                    QaP3M4CSceneProvidedAdmissionScopeSetup.NeutralRouteScenePath
                };

            var loadedBySmoke =
                new List<string>(
                    fixtureScenePaths.Length);

            GameObject duplicate =
                null;

            try
            {
                for (int index = 0;
                     index < fixtureScenePaths.Length;
                     index++)
                {
                    await LoadFixtureSceneAsync(
                        fixtureScenePaths[index]);

                    loadedBySmoke.Add(
                        fixtureScenePaths[index]);
                }

                completed.Add(
                    "fixture-scenes-loaded");

                SceneLocalPlayerAdmissionAuthoring targetAuthoring =
                    ResolveSingleAuthoring(
                        QaP3M4BRouteSceneProvidedAdmissionSetup.ScenePath);

                SceneLocalPlayerAdmissionAuthoring activityContentAuthoring =
                    ResolveSingleAuthoring(
                        QaP3M4CSceneProvidedAdmissionScopeSetup.ActivityContentScenePath);

                SceneLocalPlayerAdmissionAuthoring unrelatedAuthoring =
                    ResolveSingleAuthoring(
                        QaP3M4CSceneProvidedAdmissionScopeSetup.UnrelatedScenePath);

                SceneLocalPlayerAdmissionAuthoring foreignAuthoring =
                    ResolveSingleAuthoring(
                        QaP3M4CSceneProvidedAdmissionScopeSetup.ForeignRouteScenePath);

                await AwaitRuntimeReadyAsync(
                    targetAuthoring,
                    activityContentAuthoring,
                    unrelatedAuthoring,
                    foreignAuthoring);

                completed.Add(
                    "competing-surfaces-runtime-ready");

                SetLifecycleContext(
                    module,
                    setContext,
                    targetRoute,
                    targetActivity);

                AutomaticResolution routeResolution =
                    ResolveAutomatic(
                        module,
                        resolveAutomatic,
                        targetActivity);

                Require(
                    routeResolution.Succeeded,
                    "Target Route Primary Scene scope resolution failed. " +
                    routeResolution.Issue);

                RequireSingle(
                    routeResolution.Authoring,
                    targetAuthoring,
                    "Target Route Primary Scene");

                completed.Add(
                    "route-primary-scope-resolved");

                Require(
                    !ContainsReference(
                        routeResolution.Authoring,
                        activityContentAuthoring),
                    "Activity Content composer leaked into an unrelated Route-only scope.");

                completed.Add(
                    "activity-content-scene-excluded-from-route-scope");

                Require(
                    !ContainsReference(
                        routeResolution.Authoring,
                        unrelatedAuthoring),
                    "Unrelated loaded scene composer was treated as eligible.");

                completed.Add(
                    "unrelated-loaded-scene-excluded");

                Require(
                    !ContainsReference(
                        routeResolution.Authoring,
                        foreignAuthoring),
                    "Another Route Primary Scene composer was treated as eligible.");

                completed.Add(
                    "foreign-route-primary-excluded");

                SetLifecycleContext(
                    module,
                    setContext,
                    neutralRoute,
                    activityContentActivity);

                AutomaticResolution activityContentResolution =
                    ResolveAutomatic(
                        module,
                        resolveAutomatic,
                        activityContentActivity);

                Require(
                    activityContentResolution.Succeeded,
                    "Activity Content Scene scope resolution failed. " +
                    activityContentResolution.Issue);

                RequireSingle(
                    activityContentResolution.Authoring,
                    activityContentAuthoring,
                    "Activity Content Scene");

                completed.Add(
                    "activity-content-scope-resolved");

                Require(
                    !ContainsReference(
                        activityContentResolution.Authoring,
                        targetAuthoring),
                    "Target Route Primary Scene composer leaked into the neutral Route context.");

                completed.Add(
                    "route-primary-excluded-from-neutral-route");

                SetLifecycleContext(
                    module,
                    setContext,
                    targetRoute,
                    targetActivity);

                AutomaticResolution mismatchResolution =
                    ResolveAutomatic(
                        module,
                        resolveAutomatic,
                        mismatchActivity);

                Require(
                    mismatchResolution.Succeeded,
                    "Mismatched Activity scope resolution failed unexpectedly. " +
                    mismatchResolution.Issue);

                Require(
                    mismatchResolution.Authoring.Count == 0,
                    "Retained Route context was reused by a different Activity.");

                completed.Add(
                    "retained-route-context-not-reused");

                Scene targetScene =
                    SceneManager.GetSceneByPath(
                        QaP3M4BRouteSceneProvidedAdmissionSetup.ScenePath);

                Require(
                    targetScene.IsValid() &&
                    targetScene.isLoaded,
                    "P3M4B target Route Primary Scene is not loaded.");

                duplicate =
                    PrefabUtility.InstantiatePrefab(
                        playerPrefab,
                        targetScene) as GameObject;

                Require(
                    duplicate != null,
                    "Could not instantiate the duplicate eligible P3M4C Player.");

                duplicate.name =
                    "QA_P3M4C_DuplicateEligible_Player";

                await Awaitable.NextFrameAsync();

                SetLifecycleContext(
                    module,
                    setContext,
                    targetRoute,
                    targetActivity);

                AutomaticResolution duplicateResolution =
                    ResolveAutomatic(
                        module,
                        resolveAutomatic,
                        targetActivity);

                Require(
                    !duplicateResolution.Succeeded,
                    "Duplicate eligible composers for one Slot were accepted.");

                completed.Add(
                    "duplicate-eligible-slot-rejected");

                Require(
                    duplicateResolution.Issue.IndexOf(
                        "more than one automatic Scene Local Player Admission for Slot",
                        StringComparison.OrdinalIgnoreCase) >= 0,
                    "Duplicate rejection did not expose the expected explicit Slot conflict. " +
                    duplicateResolution.Issue);

                completed.Add(
                    "duplicate-rejection-explicit");

                UnityEngine.Object.Destroy(
                    duplicate);

                duplicate =
                    null;

                await Awaitable.NextFrameAsync();
                await Awaitable.NextFrameAsync();

                SetLifecycleContext(
                    module,
                    setContext,
                    targetRoute,
                    targetActivity);

                AutomaticResolution restoredResolution =
                    ResolveAutomatic(
                        module,
                        resolveAutomatic,
                        targetActivity);

                Require(
                    restoredResolution.Succeeded,
                    "Scope resolution did not recover after duplicate cleanup. " +
                    restoredResolution.Issue);

                RequireSingle(
                    restoredResolution.Authoring,
                    targetAuthoring,
                    "Restored Target Route Primary Scene");

                completed.Add(
                    "duplicate-cleanup-restores-scope");

                RequireNoAdmissionSideEffects(
                    targetAuthoring,
                    activityContentAuthoring,
                    unrelatedAuthoring,
                    foreignAuthoring);

                completed.Add(
                    "scope-resolution-has-no-side-effects");
            }
            finally
            {
                if (duplicate != null)
                {
                    UnityEngine.Object.Destroy(
                        duplicate);

                    await Awaitable.NextFrameAsync();
                }

                await UnloadFixtureScenesAsync(
                    loadedBySmoke);
            }
        }

        private static T LoadAsset<T>(
            string path)
            where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(
                path);
        }

        private static async Task LoadFixtureSceneAsync(
            string scenePath)
        {
            Scene existing =
                SceneManager.GetSceneByPath(
                    scenePath);

            Require(
                !existing.IsValid() ||
                !existing.isLoaded,
                $"Fixture scene '{scenePath}' is already loaded. Re-enter Play Mode before P3M4C.");

            AsyncOperation operation =
                SceneManager.LoadSceneAsync(
                    scenePath,
                    LoadSceneMode.Additive);

            Require(
                operation != null,
                $"Unity did not create a load operation for '{scenePath}'.");

            while (!operation.isDone)
            {
                await Awaitable.NextFrameAsync();
            }

            Scene loaded =
                SceneManager.GetSceneByPath(
                    scenePath);

            Require(
                loaded.IsValid() &&
                loaded.isLoaded,
                $"Fixture scene '{scenePath}' did not load.");
        }

        private static async Task UnloadFixtureScenesAsync(
            IReadOnlyList<string> scenePaths)
        {
            for (int index = scenePaths.Count - 1;
                 index >= 0;
                 index--)
            {
                Scene scene =
                    SceneManager.GetSceneByPath(
                        scenePaths[index]);

                if (!scene.IsValid() ||
                    !scene.isLoaded)
                {
                    continue;
                }

                AsyncOperation operation =
                    SceneManager.UnloadSceneAsync(
                        scene);

                if (operation == null)
                {
                    continue;
                }

                while (!operation.isDone)
                {
                    await Awaitable.NextFrameAsync();
                }
            }
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
                $"Scene '{scenePath}' is not loaded.");

            SceneLocalPlayerAdmissionAuthoring resolved =
                null;

            int count = 0;

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

        private static async Task AwaitRuntimeReadyAsync(
            params SceneLocalPlayerAdmissionAuthoring[] authoring)
        {
            const int MaxFrames = 180;

            for (int frame = 0;
                 frame < MaxFrames;
                 frame++)
            {
                bool ready =
                    true;

                for (int index = 0;
                     index < authoring.Length;
                     index++)
                {
                    if (authoring[index] == null ||
                        !authoring[index].RuntimeReady)
                    {
                        ready =
                            false;
                        break;
                    }
                }

                if (ready)
                {
                    return;
                }

                await Awaitable.NextFrameAsync();
            }

            var diagnostics =
                new List<string>(
                    authoring.Length);

            for (int index = 0;
                 index < authoring.Length;
                 index++)
            {
                SceneLocalPlayerAdmissionAuthoring candidate =
                    authoring[index];

                diagnostics.Add(
                    candidate == null
                        ? "<null>"
                        : $"name='{candidate.name}' ready='{candidate.RuntimeReady}' diagnostic='{candidate.RuntimeDiagnostic}'");
            }

            throw new InvalidOperationException(
                "Competing Scene-Provided composers did not become RuntimeReady. " +
                string.Join("; ", diagnostics));
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

        private static Component ResolveSceneLocalPlayerModule(
            Component runtimeHost)
        {
            Type moduleType =
                ResolveRuntimeType(
                    ModuleTypeName);

            Component module =
                runtimeHost.GetComponent(
                    moduleType);

            Require(
                module != null,
                "FrameworkRuntimeHost has no SceneLocalPlayerAdmissionRuntimeHostModule.");

            return module;
        }

        private static void SetLifecycleContext(
            Component module,
            MethodInfo method,
            RouteAsset route,
            ActivityAsset activity)
        {
            method.Invoke(
                module,
                new object[]
                {
                    route,
                    activity
                });
        }

        private static AutomaticResolution ResolveAutomatic(
            Component module,
            MethodInfo method,
            ActivityAsset activity)
        {
            object[] arguments =
            {
                activity,
                null,
                string.Empty
            };

            object raw =
                method.Invoke(
                    module,
                    arguments);

            Require(
                raw is bool,
                "TryResolveAutomaticActivityAuthoring did not return Boolean.");

            var authoring =
                arguments[1] as IReadOnlyList<
                    SceneLocalPlayerAdmissionAuthoring> ??
                Array.Empty<
                    SceneLocalPlayerAdmissionAuthoring>();

            string issue =
                arguments[2] as string ??
                string.Empty;

            return new AutomaticResolution(
                (bool)raw,
                authoring,
                issue);
        }

        private static void RequireSingle(
            IReadOnlyList<SceneLocalPlayerAdmissionAuthoring> values,
            SceneLocalPlayerAdmissionAuthoring expected,
            string context)
        {
            Require(
                values != null &&
                values.Count == 1,
                $"{context} resolution expected one composer, found '{values?.Count ?? 0}'.");

            Require(
                ReferenceEquals(
                    values[0],
                    expected),
                $"{context} resolution returned the wrong composer.");
        }

        private static bool ContainsReference(
            IReadOnlyList<SceneLocalPlayerAdmissionAuthoring> values,
            SceneLocalPlayerAdmissionAuthoring candidate)
        {
            if (values == null)
            {
                return false;
            }

            for (int index = 0;
                 index < values.Count;
                 index++)
            {
                if (ReferenceEquals(
                        values[index],
                        candidate))
                {
                    return true;
                }
            }

            return false;
        }

        private static void RequireNoAdmissionSideEffects(
            params SceneLocalPlayerAdmissionAuthoring[] authoring)
        {
            for (int index = 0;
                 index < authoring.Length;
                 index++)
            {
                SceneLocalPlayerAdmissionAuthoring candidate =
                    authoring[index];

                Require(
                    candidate != null,
                    "Scope side-effect validation received a null composer.");

                Require(
                    !candidate.HasActiveAdmission,
                    $"Scope resolution admitted '{candidate.name}' unexpectedly.");

                LocalPlayerHostAuthoring host =
                    candidate.LocalPlayerHost;

                Require(
                    host != null &&
                    !host.IsJoined,
                    $"Scope resolution joined Host '{candidate.name}' unexpectedly.");
            }
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
                    $"Method '{type.FullName}.{methodName}' is ambiguous.");

                resolved =
                    candidate;
            }

            Require(
                resolved != null,
                $"Method '{type.FullName}.{methodName}' with '{parameterCount}' parameters was not found. " +
                "Apply SCENE-PROVIDED-ROUTE-ADMISSION-1 before running P3M4C.");

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

        private readonly struct AutomaticResolution
        {
            internal AutomaticResolution(
                bool succeeded,
                IReadOnlyList<SceneLocalPlayerAdmissionAuthoring> authoring,
                string issue)
            {
                Succeeded =
                    succeeded;

                Authoring =
                    authoring ??
                    Array.Empty<
                        SceneLocalPlayerAdmissionAuthoring>();

                Issue =
                    issue ??
                    string.Empty;
            }

            internal bool Succeeded { get; }

            internal IReadOnlyList<SceneLocalPlayerAdmissionAuthoring>
                Authoring { get; }

            internal string Issue { get; }
        }
    }
}
