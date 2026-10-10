using System;
using System.Collections;
using System.Collections.Generic;
using Immersive.Framework.Authoring;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.GameFlow;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.QaFramework.IfAdr045
{
    [DisallowMultipleComponent]
    public sealed class QaIfAdr045PersistentContentLifecycleScenario : MonoBehaviour
    {
        private static readonly string[] ExpectedCases =
        {
            "bootstrap-and-session-composition",
            "exact-source-unload-and-root-transfer",
            "route-a-to-route-b",
            "route-b-to-route-d-startup-activity",
            "activity-d-a-to-d-b",
            "route-restoration-and-cleanup",
            "session-shutdown-release"
        };

        [SerializeField] private string expectedPersistentScenePath;
        [SerializeField] private RouteAsset routeA;
        [SerializeField] private RouteAsset routeB;
        [SerializeField] private RouteAsset routeD;
        [SerializeField] private ActivityAsset activityDA;
        [SerializeField] private ActivityAsset activityDB;
        [SerializeField] private RouteRequestTrigger requestRouteA;
        [SerializeField] private RouteRequestTrigger requestRouteB;
        [SerializeField] private RouteRequestTrigger requestRouteD;
        [SerializeField] private ActivityRequestTrigger requestActivityDB;
        [SerializeField, Min(0.1f)] private float bootstrapTimeoutSeconds = 30f;
        [SerializeField, Min(0.1f)] private float operationTimeoutSeconds = 30f;

        private readonly List<GameObject> _sourceRoots = new List<GameObject>();
        private readonly List<EntityId> _rootEntityIds = new List<EntityId>();
        private readonly List<string> _coLoadedSceneIdentities = new List<string>();
        private Scene _sourceScene;
        private string _sourceScenePath = string.Empty;
        private ulong _sourceSceneHandle;
        private bool _sourceUnloadObserved;
        private bool _started;
        private bool _terminal;
        private bool _failed;
        private string _firstIssue = string.Empty;
        private int _completed;

        private void Awake()
        {
            _sourceScene = gameObject.scene;
            _sourceScenePath = _sourceScene.IsValid() ? _sourceScene.path : string.Empty;
            _sourceSceneHandle = _sourceScene.IsValid()
                ? _sourceScene.handle.GetRawData()
                : 0;

            if (_sourceScene.IsValid())
            {
                for (int index = 0; index < SceneManager.sceneCount; index++)
                {
                    Scene loadedScene = SceneManager.GetSceneAt(index);
                    if (!loadedScene.IsValid() || !loadedScene.isLoaded ||
                        loadedScene.handle.GetRawData() == _sourceSceneHandle)
                    {
                        continue;
                    }

                    _coLoadedSceneIdentities.Add(
                        $"{(string.IsNullOrWhiteSpace(loadedScene.path) ? "<runtime-scene>" : loadedScene.path)}#{loadedScene.handle.GetRawData()}");
                }

                GameObject[] roots = _sourceScene.GetRootGameObjects();
                for (int index = 0; index < roots.Length; index++)
                {
                    GameObject root = roots[index];
                    if (root == null)
                        continue;

                    _sourceRoots.Add(root);
                    _rootEntityIds.Add(root.GetEntityId());
                }
            }

            SceneManager.sceneUnloaded += HandleSceneUnloaded;
        }

        private void Start()
        {
            if (_started)
                return;

            _started = true;
            StartCoroutine(RunScenario());
        }

        private void OnDestroy()
        {
            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
        }

        private IEnumerator RunScenario()
        {
            if (!TryValidateAuthoring(out string authoringIssue))
            {
                PublishTerminal("Failed", authoringIssue, "FreshBootRequired");
                yield break;
            }
            if (_coLoadedSceneIdentities.Count == 0)
            {
                PublishTerminal(
                    "Blocked",
                    "No other loaded Scene identity was observable when the Persistent Content source Awake ran; additive co-load is not claimed.",
                    "FreshBootRequired");
                yield break;
            }

            double deadline = Time.realtimeSinceStartupAsDouble + bootstrapTimeoutSeconds;
            while (!IsBootstrapReady() && Time.realtimeSinceStartupAsDouble < deadline)
                yield return null;

            if (!IsBootstrapReady())
            {
                PublishTerminal(
                    "Failed",
                    BuildBootstrapWaitIssue(),
                    "FreshBootRequired");
                yield break;
            }

            // Bootstrap and Session binding form the first checkpoint. Source-scene unload
            // and Persistent Content ownership transfer are the next, separate checkpoint.
            _completed = 1;

            double sourceUnloadDeadline = Time.realtimeSinceStartupAsDouble + bootstrapTimeoutSeconds;
            while (!_sourceUnloadObserved && Time.realtimeSinceStartupAsDouble < sourceUnloadDeadline)
                yield return null;

            if (!_sourceUnloadObserved)
            {
                PublishTerminal(
                    "Failed",
                    $"exact-source-unload: Framework did not unload the exact Persistent Content source scene. expectedPath='{_sourceScenePath}' sourceHandle='{_sourceSceneHandle}' {DescribeSessionTriggerBindings()}",
                    "FreshBootRequired");
                yield break;
            }

            bool persistentRootsValid = TryValidatePersistentRoots(out string rootIssue);
            if (!persistentRootsValid)
            {
                PublishTerminal(
                    "Failed",
                    $"exact-source-unload-and-root-transfer: {rootIssue}",
                    "FreshBootRequired");
                yield break;
            }

            _completed = 2;

            yield return RequestRoute(requestRouteB, routeB, routeA, "route-a-to-route-b");
            if (_failed) { PublishTerminal("Failed", _firstIssue, "FreshBootRequired"); yield break; }
            _completed++;

            yield return RequestRoute(requestRouteD, routeD, routeB, "route-b-to-route-d-startup-activity");
            if (_failed) { PublishTerminal("Failed", _firstIssue, "FreshBootRequired"); yield break; }
            string activityDAPath = GetFirstActivityScenePath(activityDA);
            double activityLoadDeadline = Time.realtimeSinceStartupAsDouble + operationTimeoutSeconds;
            while (!IsSceneLoaded(activityDAPath) && Time.realtimeSinceStartupAsDouble < activityLoadDeadline)
                yield return null;
            if (!IsSceneLoaded(activityDAPath))
            {
                PublishTerminal("Failed", "Route D succeeded but its Startup Activity D-A Content Scene was not loaded.", "FreshBootRequired");
                yield break;
            }
            _completed++;

            yield return RequestActivity(requestActivityDB, activityDB, "activity-d-a-to-d-b");
            if (_failed) { PublishTerminal("Failed", _firstIssue, "FreshBootRequired"); yield break; }
            string activityDAReleasePath = GetFirstActivityScenePath(activityDA);
            double activityReleaseDeadline = Time.realtimeSinceStartupAsDouble + operationTimeoutSeconds;
            while (!string.IsNullOrWhiteSpace(activityDAReleasePath) && IsSceneLoaded(activityDAReleasePath) &&
                   Time.realtimeSinceStartupAsDouble < activityReleaseDeadline)
            {
                yield return null;
            }
            if (!string.IsNullOrWhiteSpace(activityDAReleasePath) && IsSceneLoaded(activityDAReleasePath))
            {
                PublishTerminal("Failed", $"Activity D-B succeeded but prior Activity Content path '{activityDAReleasePath}' remained loaded.", "FreshBootRequired");
                yield break;
            }
            _completed++;

            yield return RequestRoute(requestRouteA, routeA, routeD, "route-restoration-and-cleanup");
            if (_failed) { PublishTerminal("Failed", _firstIssue, "FreshBootRequired"); yield break; }

            string activeScenePath = SceneManager.GetActiveScene().path;
            if (!string.Equals(activeScenePath, routeA.PrimaryScenePath, StringComparison.Ordinal))
            {
                PublishTerminal(
                    "Failed",
                    $"route-restoration-and-cleanup: Route A Primary Scene is not active. expected='{routeA.PrimaryScenePath}' actual='{activeScenePath}'.",
                    "FreshBootRequired");
                yield break;
            }
            if (IsSceneLoaded(routeD.PrimaryScenePath))
            {
                PublishTerminal(
                    "Failed",
                    $"route-restoration-and-cleanup: Route D Primary Scene remained loaded after restoration. path='{routeD.PrimaryScenePath}'.",
                    "FreshBootRequired");
                yield break;
            }
            if (!TryValidatePersistentRoots(out rootIssue))
            {
                PublishTerminal("Failed", $"route-restoration-and-cleanup: {rootIssue}", "FreshBootRequired");
                yield break;
            }
            if (!AreSessionTriggersBound())
            {
                PublishTerminal(
                    "Failed",
                    $"route-restoration-and-cleanup: Session trigger binding was lost after restoring Route A. {DescribeSessionTriggerBindings()}",
                    "FreshBootRequired");
                yield break;
            }

            _completed++;
            PublishTerminal(
                "Blocked",
                "Route/Activity lifecycle checkpoints passed, but the current public authoring surface does not expose Session-scope release. SceneLifecycleEvents is Scene-scope only; shutdown is not claimed as PASS.",
                "RouteActivityBaselineRestored");
        }

        private IEnumerator RequestRoute(
            RouteRequestTrigger trigger,
            RouteAsset expectedRoute,
            RouteAsset previousRoute,
            string caseId)
        {
            if (trigger == null || !trigger.HasRouteRuntimeBinding || trigger.TargetRoute != expectedRoute)
            {
                _failed = true;
                _firstIssue = $"{caseId}: exact public Route trigger is not Session-bound to its expected Route.";
                yield break;
            }

            trigger.RequestRoute();
            double deadline = Time.realtimeSinceStartupAsDouble + operationTimeoutSeconds;
            while ((trigger.IsRequestInFlight || trigger.LastEventPhase != FlowRequestEventPhase.Completed) &&
                   Time.realtimeSinceStartupAsDouble < deadline)
                yield return null;

            if (trigger.IsRequestInFlight || trigger.LastEventPhase != FlowRequestEventPhase.Completed ||
                trigger.LastOutcome != FlowRequestOutcome.Succeeded || !trigger.LastRequestSucceeded)
            {
                _failed = true;
                _firstIssue = $"{caseId}: public Route request did not succeed. outcome='{trigger.LastOutcome}' message='{trigger.LastMessage}'.";
                yield break;
            }

            deadline = Time.realtimeSinceStartupAsDouble + operationTimeoutSeconds;
            while ((!IsSceneLoaded(expectedRoute.PrimaryScenePath) ||
                    !string.Equals(SceneManager.GetActiveScene().path, expectedRoute.PrimaryScenePath, StringComparison.Ordinal) ||
                    (previousRoute != null &&
                     !string.Equals(previousRoute.PrimaryScenePath, expectedRoute.PrimaryScenePath, StringComparison.Ordinal) &&
                     IsSceneLoaded(previousRoute.PrimaryScenePath))) &&
                   Time.realtimeSinceStartupAsDouble < deadline)
            {
                yield return null;
            }

            bool rootsValid = TryValidatePersistentRoots(out string issue);
            if (!IsSceneLoaded(expectedRoute.PrimaryScenePath))
            {
                _failed = true;
                _firstIssue = $"{caseId}: target Primary Scene was not loaded. target='{expectedRoute.PrimaryScenePath}' active='{SceneManager.GetActiveScene().path}'.";
                yield break;
            }

            string activeScenePath = SceneManager.GetActiveScene().path;
            if (!string.Equals(activeScenePath, expectedRoute.PrimaryScenePath, StringComparison.Ordinal))
            {
                _failed = true;
                _firstIssue = $"{caseId}: target Primary Scene was loaded but not active. target='{expectedRoute.PrimaryScenePath}' active='{activeScenePath}'.";
                yield break;
            }

            if (previousRoute != null &&
                !string.Equals(previousRoute.PrimaryScenePath, expectedRoute.PrimaryScenePath, StringComparison.Ordinal) &&
                IsSceneLoaded(previousRoute.PrimaryScenePath))
            {
                _failed = true;
                _firstIssue = $"{caseId}: previous Route Primary Scene remained loaded. previous='{previousRoute.PrimaryScenePath}' target='{expectedRoute.PrimaryScenePath}'.";
                yield break;
            }

            if (!rootsValid)
            {
                _failed = true;
                _firstIssue = $"{caseId}: Persistent Content roots changed. {issue}";
            }
        }

        private IEnumerator RequestActivity(
            ActivityRequestTrigger trigger,
            ActivityAsset expectedActivity,
            string caseId)
        {
            if (trigger == null || !trigger.HasActivityRuntimeBinding || trigger.TargetActivity != expectedActivity)
            {
                _failed = true;
                _firstIssue = $"{caseId}: exact public Activity trigger is not Session-bound to its expected Activity.";
                yield break;
            }

            trigger.RequestActivity();
            double deadline = Time.realtimeSinceStartupAsDouble + operationTimeoutSeconds;
            while ((trigger.IsRequestInFlight || trigger.LastEventPhase != FlowRequestEventPhase.Completed) &&
                   Time.realtimeSinceStartupAsDouble < deadline)
                yield return null;

            bool rootsValid = TryValidatePersistentRoots(out string issue);
            if (trigger.IsRequestInFlight || trigger.LastEventPhase != FlowRequestEventPhase.Completed ||
                trigger.LastOutcome != FlowRequestOutcome.Succeeded || !trigger.LastRequestSucceeded)
            {
                _failed = true;
                _firstIssue = $"{caseId}: Activity D-B request did not complete successfully. phase='{trigger.LastEventPhase}' outcome='{trigger.LastOutcome}' message='{trigger.LastMessage}'.";
                yield break;
            }

            if (!rootsValid)
            {
                _failed = true;
                _firstIssue = $"{caseId}: Persistent Content roots changed after Activity D-B completed. {issue}";
            }
        }

        private bool TryValidateAuthoring(out string issue)
        {
            if (routeA == null || routeB == null || routeD == null || activityDA == null || activityDB == null ||
                requestRouteA == null || requestRouteB == null || requestRouteD == null || requestActivityDB == null ||
                string.IsNullOrWhiteSpace(expectedPersistentScenePath))
            {
                issue = "D7 requires the expected Persistent Content path, QA Route/Activity assets and four request triggers.";
                return false;
            }

            if (!routeA.HasValidRouteId || !routeB.HasValidRouteId || !routeD.HasValidRouteId ||
                !routeA.HasPrimaryScene || !routeB.HasPrimaryScene || !routeD.HasPrimaryScene ||
                routeA.HasRouteContentProfile || routeB.HasRouteContentProfile || routeD.HasRouteContentProfile ||
                routeD.StartupActivity != activityDA || !activityDA.HasValidActivityId ||
                !activityDA.HasActivityContentProfile || activityDA.ActivityContentProfile.SceneCount != 1 ||
                !activityDB.HasValidActivityId ||
                (activityDB.HasActivityContentProfile && activityDB.ActivityContentProfile.SceneCount > 0))
            {
                issue = "D7 Route/Activity references do not match the expected QA-NEW-004 lifecycle contract.";
                return false;
            }

            ActivityContentSceneEntry activityScene = activityDA.ActivityContentProfile.Scenes[0];
            if (activityScene == null || string.IsNullOrWhiteSpace(activityScene.ScenePath) ||
                activityScene.Requiredness != FrameworkContentRequiredness.Required ||
                activityScene.LoadMode != ActivityContentSceneLoadMode.Additive ||
                activityScene.ReleasePolicy != ActivityContentReleasePolicy.ReleaseOnActivityChange)
            {
                issue = "Activity D-A must declare one Required, Additive Content Scene released on Activity change.";
                return false;
            }

            if (!string.Equals(_sourceScenePath, expectedPersistentScenePath, StringComparison.Ordinal) ||
                _sourceSceneHandle == 0 || _sourceRoots.Count == 0 ||
                _sourceRoots.Count != _rootEntityIds.Count)
            {
                issue = $"Persistent Content source identity is invalid. expectedPath='{expectedPersistentScenePath}' capturedPath='{_sourceScenePath}' handle='{_sourceSceneHandle}' roots='{_sourceRoots.Count}' identities='{_rootEntityIds.Count}'.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private bool IsBootstrapReady()
        {
            return requestRouteA != null && requestRouteA.HasRouteRuntimeBinding &&
                   requestRouteB != null && requestRouteB.HasRouteRuntimeBinding &&
                   requestRouteD != null && requestRouteD.HasRouteRuntimeBinding &&
                   requestActivityDB != null && requestActivityDB.HasActivityRuntimeBinding &&
                   string.Equals(SceneManager.GetActiveScene().path, routeA.PrimaryScenePath, StringComparison.Ordinal);
        }

        private string BuildBootstrapWaitIssue()
        {
            return $"Timed out waiting for Framework bootstrap and Session composition. expectedSourcePath='{expectedPersistentScenePath}' capturedSourcePath='{_sourceScenePath}' sourceHandle='{_sourceSceneHandle}' " +
                   $"sourceUnloaded='{_sourceUnloadObserved}' sessionCompositionEvidence='{DescribeSessionTriggerBindings()}' " +
                   $"routeA='{requestRouteA?.RouteRuntimeBindingStatus}' routeB='{requestRouteB?.RouteRuntimeBindingStatus}' " +
                   $"routeD='{requestRouteD?.RouteRuntimeBindingStatus}' activity='{requestActivityDB?.ActivityRuntimeBindingStatus}' " +
                   $"activeScene='{SceneManager.GetActiveScene().path}'.";
        }

        private bool TryValidatePersistentRoots(out string issue)
        {
            if (_sourceRoots.Count == 0 || _sourceRoots.Count != _rootEntityIds.Count)
            {
                issue = "Captured Persistent Content root cardinality changed.";
                return false;
            }

            ulong persistentSceneHandle = 0;
            bool hasPersistentSceneHandle = false;
            for (int index = 0; index < _sourceRoots.Count; index++)
            {
                GameObject root = _sourceRoots[index];
                if (root == null)
                {
                    issue = $"Persistent Content source root was destroyed before validation. index='{index}' expectedEntityId='{_rootEntityIds[index]}'.";
                    return false;
                }

                EntityId actualEntityId = root.GetEntityId();
                if (!actualEntityId.Equals(_rootEntityIds[index]))
                {
                    issue = $"Persistent Content root instance identity changed. index='{index}' expectedEntityId='{_rootEntityIds[index]}' actualEntityId='{actualEntityId}'.";
                    return false;
                }

                Scene rootScene = root.scene;
                if (!rootScene.IsValid() || !rootScene.isLoaded)
                {
                    issue = $"Persistent Content root is not assigned to a valid loaded scene. index='{index}' entityId='{actualEntityId}' sceneValid='{rootScene.IsValid()}' sceneLoaded='{rootScene.isLoaded}' sceneName='{rootScene.name}'.";
                    return false;
                }

                if (!string.Equals(rootScene.name, "DontDestroyOnLoad", StringComparison.Ordinal))
                {
                    issue = $"Persistent Content root did not enter Unity's DontDestroyOnLoad scene. index='{index}' entityId='{actualEntityId}' sceneName='{rootScene.name}' scenePath='{rootScene.path}' sceneHandle='{rootScene.handle.GetRawData()}'.";
                    return false;
                }

                ulong rootSceneHandle = rootScene.handle.GetRawData();
                if (!hasPersistentSceneHandle)
                {
                    persistentSceneHandle = rootSceneHandle;
                    hasPersistentSceneHandle = true;
                }
                else if (rootSceneHandle != persistentSceneHandle)
                {
                    issue = $"Persistent Content roots were split across scene instances. index='{index}' entityId='{actualEntityId}' expectedSceneHandle='{persistentSceneHandle}' actualSceneHandle='{rootSceneHandle}'.";
                    return false;
                }
            }

            issue = string.Empty;
            return true;
        }

        private void HandleSceneUnloaded(Scene scene)
        {
            if (_sourceSceneHandle != 0 && scene.handle.GetRawData() == _sourceSceneHandle)
                _sourceUnloadObserved = true;
        }

        private void PublishTerminal(string status, string issue, string cleanup)
        {
            if (_terminal)
                return;

            _terminal = true;
            string next = _completed < ExpectedCases.Length ? ExpectedCases[_completed] : "none";
            string completedCases = BuildCompletedCaseEvidence();
            string routeActivityLifecycle = status == "Blocked" && _completed == ExpectedCases.Length - 1
                ? "Passed"
                : "Incomplete";
            Debug.Log(
                $"[IF-ADR-045] status='{status}' verdict='{status.ToUpperInvariant()}' cases='{_completed}/{ExpectedCases.Length}' " +
                $"completed='{_completed}' completedCases='{completedCases}' next='{next}' missing='{next}' scenePath='{Sanitize(_sourceScenePath)}' sceneHandle='{_sourceSceneHandle}' " +
                $"routeActivityLifecycle='{routeActivityLifecycle}' coLoadedScenes='{Sanitize(string.Join(",", _coLoadedSceneIdentities))}' sourceUnloaded='{_sourceUnloadObserved}' rootCount='{_sourceRoots.Count}' roots='{BuildRootIdentityEvidence()}' " +
                $"activeScene='{Sanitize(SceneManager.GetActiveScene().path)}' routeBindings='A:{requestRouteA?.RouteRuntimeBindingStatus},B:{requestRouteB?.RouteRuntimeBindingStatus},D:{requestRouteD?.RouteRuntimeBindingStatus}' " +
                $"activityBinding='{requestActivityDB?.ActivityRuntimeBindingStatus}' sessionShutdown='BLOCKED' " +
                $"execution='{Sanitize(issue)}' cleanup='{cleanup}'.");
        }

        private string BuildCompletedCaseEvidence()
        {
            if (_completed <= 0)
                return "<none>";

            var cases = new string[_completed];
            for (int index = 0; index < _completed; index++)
                cases[index] = ExpectedCases[index];
            return string.Join(",", cases);
        }

        private string BuildRootIdentityEvidence()
        {
            if (_rootEntityIds.Count == 0)
                return "<none>";

            var identifiers = new string[_rootEntityIds.Count];
            for (int index = 0; index < _rootEntityIds.Count; index++)
                identifiers[index] = _rootEntityIds[index].ToString();
            return string.Join(",", identifiers);
        }

        private bool AreSessionTriggersBound() =>
            requestRouteA != null && requestRouteA.HasRouteRuntimeBinding &&
            requestRouteB != null && requestRouteB.HasRouteRuntimeBinding &&
            requestRouteD != null && requestRouteD.HasRouteRuntimeBinding &&
            requestActivityDB != null && requestActivityDB.HasActivityRuntimeBinding;

        private string DescribeSessionTriggerBindings() =>
            $"routeA='{requestRouteA?.RouteRuntimeBindingStatus}' routeB='{requestRouteB?.RouteRuntimeBindingStatus}' " +
            $"routeD='{requestRouteD?.RouteRuntimeBindingStatus}' activity='{requestActivityDB?.ActivityRuntimeBindingStatus}'";

        private static bool IsSceneLoaded(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            Scene scene = SceneManager.GetSceneByPath(path);
            return scene.IsValid() && scene.isLoaded;
        }

        private static string GetFirstActivityScenePath(ActivityAsset activity)
        {
            return activity != null && activity.ActivityContentProfile != null &&
                   activity.ActivityContentProfile.SceneCount > 0
                ? activity.ActivityContentProfile.Scenes[0].ScenePath
                : string.Empty;
        }

        private static string Sanitize(string value)
        {
            return (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Replace("'", "\"");
        }
    }
}
