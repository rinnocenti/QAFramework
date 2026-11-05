using System;
using System.Collections;
using System.Collections.Generic;
using Immersive.Framework.ActivityRestart;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.ObjectEntry;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.QaFramework.IfAdr014
{
    public sealed class QaIfAdr014ObjectEntryRecognitionDemo : MonoBehaviour
    {
        public const string RouteEntryId = "qa.if-adr-014.route-entry";
        public const string ActivityEntryId = "qa.if-adr-014.activity-entry";
        private const string RouteSceneName = "QA_IF_ADR_014_Route";
        private const string ReplacementRouteSceneName = "QA_IF_ADR_014_ReplacementRoute";
        private const string ActivitySceneName = "QA_IF_ADR_014_Activity";
        private const int ExpectedCases = 10;
        private const float TimeoutSeconds = 20f;

        [SerializeField] private ActivityRequestTrigger activityRequest;
        [SerializeField] private ActivityRestartTrigger activityRestart;
        [SerializeField] private ActivityAsset activity;
        [SerializeField] private RouteRequestTrigger routeRequest;
        [SerializeField] private RouteAsset replacementRoute;
        [SerializeField] private ObjectEntryDeclaration unadmittedDeclaration;
        [SerializeField] private Collider unadmittedCollider;

        private IObjectEntryRecognitionQuery _routeQuery;
        private IObjectEntryRecognitionQuery _activityQuery;
        private ObjectEntryDeclaration _routeDeclaration;
        private Collider _routeCollider;
        private ObjectEntryDeclaration _activityDeclaration;
        private Collider[] _activityColliders;
        private int _passedCases;
        private bool _activityReleaseObserved;
        private bool _recordingActivityRestartRelease;
        private bool _routeReleaseObserved;
        private bool _finished;
        private ulong _activitySceneHandle;
        private bool _activityTargetMovedToPersistent;
        private GameObject _restartedActivityRootToCleanup;
        private bool _routeTargetMovedToPersistent;
        private ulong _routeSceneHandle;

        private IEnumerator Start()
        {
            yield return RunDemo();
        }

        private IEnumerator RunDemo()
        {
            QaIfAdr014CompositionWitness.ResetEvidence();
            float deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            ObjectEntryOccurrenceQueryBinding routeBinding = null;
            ObjectEntryDeclaration routeDeclaration = null;
            Collider[] routeColliders = null;
            ObjectEntryRecognitionResult routeBaseline = default;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (TryGetFixture(SceneManager.GetSceneByName(RouteSceneName), 1,
                        out routeBinding, out routeDeclaration, out routeColliders))
                {
                    _routeQuery = routeBinding;
                    routeBaseline = _routeQuery.Recognize(routeColliders[0]);
                    if (IsResolved(routeBaseline, RouteEntryId, routeDeclaration.gameObject))
                        break;
                }
                yield return null;
            }

            if (routeBinding == null || routeDeclaration == null ||
                routeColliders == null || routeColliders.Length != 1 ||
                routeBaseline.Status != ObjectEntryRecognitionStatus.Resolved)
            {
                Finish(false, "Route fixture did not become available.");
                yield break;
            }

            _routeQuery = routeBinding;
            _routeDeclaration = routeDeclaration;
            _routeCollider = routeColliders[0];

            bool routeCommitted = IsResolved(routeBaseline, RouteEntryId, _routeDeclaration.gameObject);
            deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            bool routeCompositionReady = false;
            while (Time.realtimeSinceStartup < deadline)
            {
                QaIfAdr014CompositionWitness routeWitness =
                    _routeDeclaration.GetComponentInChildren<QaIfAdr014CompositionWitness>(true);
                routeCompositionReady =
                    _routeDeclaration.GetComponent<ObjectEntryCompositionAuthoring>() != null &&
                    routeWitness != null &&
                    routeWitness.isActiveAndEnabled;
                if (routeCompositionReady) break;
                yield return null;
            }
            if (!routeCompositionReady)
            {
                Finish(false, "Route Object Entry query committed before its composed capability became active.");
                yield break;
            }
            yield return null;

            ObjectEntryRecognitionResult unadmitted = _routeQuery.Recognize(unadmittedCollider);
            bool declarationPresent = unadmittedDeclaration != null &&
                unadmittedCollider != null &&
                unadmittedCollider.GetComponentInParent<ObjectEntryDeclaration>(true) == unadmittedDeclaration;

            RecordCase(
                "declared-but-not-admitted",
                unadmitted,
                ObjectEntryRecognitionStatus.Unavailable,
                declarationPresent && routeCommitted &&
                unadmitted.Status == ObjectEntryRecognitionStatus.Unavailable,
                "declarationPresent='" + declarationPresent +
                "' candidateCollider='" + (unadmittedCollider != null ? unadmittedCollider.name : "<missing>") +
                "' declaredId='" + (unadmittedDeclaration != null && unadmittedDeclaration.HasObjectEntryId
                    ? unadmittedDeclaration.ObjectEntryId.StableText : "<invalid>") +
                "' committedRouteTarget='" + routeBaseline.Status +
                "' routeId='" + IdText(routeBaseline) + "'");

            if (!routeCommitted || !declarationPresent)
            {
                Finish(false, "The Route commit or unadmitted declaration precondition failed.");
                yield break;
            }

            if (activityRequest == null || activity == null || !activityRequest.HasActivityRuntimeBinding)
            {
                string runtimeBinding = activityRequest == null
                    ? "<missing-trigger>"
                    : activityRequest.ActivityRuntimeBindingStatus;
                Finish(false,
                    "The Persistent Activity Request Trigger or Activity asset is unavailable. " +
                    "triggerPresent='" + (activityRequest != null) +
                    "' activityPresent='" + (activity != null) +
                    "' runtimeBinding='" + runtimeBinding + "'.");
                yield break;
            }

            if (activityRestart == null || !activityRestart.HasActivityRestartRuntimeBinding)
            {
                Finish(false,
                    "The Activity Restart trigger is unavailable or not bound. binding='" +
                    (activityRestart != null ? activityRestart.ActivityRestartRuntimeBindingStatus : "<missing>") + "'.");
                yield break;
            }

            activityRequest.TargetActivity = activity;
            activityRequest.RequestActivity();

            deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            ObjectEntryOccurrenceQueryBinding activityBinding = null;
            ObjectEntryDeclaration activityDeclaration = null;
            Collider[] activityColliders = null;
            ObjectEntryRecognitionResult activityReady = default;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (TryGetFixture(SceneManager.GetSceneByName(ActivitySceneName), 2,
                        out activityBinding, out activityDeclaration, out activityColliders))
                {
                    activityReady = _routeQuery.Recognize(activityColliders[0]);
                    if (activityReady.Status == ObjectEntryRecognitionStatus.Resolved)
                        break;
                }
                yield return null;
            }

            if (activityBinding == null || activityDeclaration == null ||
                activityColliders == null || activityColliders.Length != 2 ||
                activityReady.Status != ObjectEntryRecognitionStatus.Resolved)
            {
                Finish(false, "Activity content did not commit with its two Collider fixture.");
                yield break;
            }

            deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (activityRequest.IsRequestInFlight && Time.realtimeSinceStartup < deadline)
                yield return null;

            if (activityRequest.IsRequestInFlight || !activityRequest.LastRequestSucceeded)
            {
                Finish(false, "The Activity request did not complete successfully. " + activityRequest.LastMessage);
                yield break;
            }

            _activityQuery = activityBinding;
            _activityDeclaration = activityDeclaration;
            _activityColliders = activityColliders;
            if (_activityDeclaration.GetComponent<ObjectEntryCompositionAuthoring>() == null ||
                _activityDeclaration.GetComponentInChildren<QaIfAdr014CompositionWitness>(true) == null)
            {
                Finish(false, "Activity Object Entry composition did not materialize its QA witness.");
                yield break;
            }

            ObjectEntryRecognitionResult first = _routeQuery.Recognize(_activityColliders[0]);
            ObjectEntryRecognitionResult second = _routeQuery.Recognize(_activityColliders[1]);
            bool sameIdentity = first.ObjectEntryId.IsValid &&
                first.ObjectEntryId == second.ObjectEntryId &&
                first.ObjectEntryId == ObjectEntryId.From(ActivityEntryId);
            bool sameOccurrence = first.Occurrence == _activityDeclaration.gameObject &&
                second.Occurrence == _activityDeclaration.gameObject;
            bool multiColliderPass =
                first.Status == ObjectEntryRecognitionStatus.Resolved &&
                second.Status == ObjectEntryRecognitionStatus.Resolved &&
                first.Succeeded && second.Succeeded && sameIdentity && sameOccurrence;
            RecordMultipleColliderCase(first, second, multiColliderPass, sameIdentity, sameOccurrence, _activityColliders[0].name, _activityColliders[1].name);

            ObjectEntryRecognitionResult activityAnchorBeforeRelease =
                _activityQuery.Recognize(_routeCollider);
            bool activityAnchorCommitted =
                IsResolved(activityAnchorBeforeRelease, RouteEntryId, _routeDeclaration.gameObject);
            LogEvidence("activity-anchor-before-release", activityAnchorBeforeRelease,
                activityAnchorCommitted, "Activity-owned query is committed before release");

            if (!activityAnchorCommitted)
            {
                Finish(false, "The Activity-owned query anchor was not committed before release.");
                yield break;
            }

            _activitySceneHandle = SceneManager.GetSceneByName(ActivitySceneName).handle.GetRawData();
            DontDestroyOnLoad(_activityDeclaration.gameObject);
            _activityTargetMovedToPersistent = true;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            _recordingActivityRestartRelease = true;
            activityRestart.RequestActivityRestart();

            deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while ((!_activityReleaseObserved || activityRestart.IsRequestInFlight) &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;

            if (!_activityReleaseObserved)
            {
                Finish(false, "Activity content did not begin unloading during Activity Restart.");
                yield break;
            }

            if (activityRestart.IsRequestInFlight || !activityRestart.LastRequestSucceeded)
            {
                Finish(false, "Activity Restart did not complete successfully. " + activityRestart.LastMessage);
                yield break;
            }

            _restartedActivityRootToCleanup = _activityDeclaration.gameObject;
            deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            ObjectEntryOccurrenceQueryBinding restartedActivityBinding = null;
            ObjectEntryDeclaration restartedActivityDeclaration = null;
            Collider[] restartedActivityColliders = null;
            ObjectEntryRecognitionResult restartedActivityReady = default;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (TryGetFixture(SceneManager.GetSceneByName(ActivitySceneName), 2,
                        out restartedActivityBinding,
                        out restartedActivityDeclaration,
                        out restartedActivityColliders))
                {
                    restartedActivityReady = _routeQuery.Recognize(restartedActivityColliders[0]);
                    if (restartedActivityReady.Status == ObjectEntryRecognitionStatus.Resolved)
                        break;
                }
                yield return null;
            }

            bool replacementActivityOccurrenceCommitted =
                restartedActivityBinding != null && restartedActivityDeclaration != null &&
                restartedActivityColliders != null && restartedActivityColliders.Length == 2 &&
                restartedActivityDeclaration.gameObject != _restartedActivityRootToCleanup &&
                IsResolved(restartedActivityReady, ActivityEntryId, restartedActivityDeclaration.gameObject) &&
                restartedActivityDeclaration.GetComponent<ObjectEntryCompositionAuthoring>() != null &&
                restartedActivityDeclaration.GetComponentInChildren<QaIfAdr014CompositionWitness>(true)
                    is { isActiveAndEnabled: true };
            RecordCompositionCase(
                "activity-restart-readmitted-new-physical-occurrence",
                replacementActivityOccurrenceCommitted,
                "oldRootStillLive='" + (_restartedActivityRootToCleanup != null) +
                "' newRoot='" + (restartedActivityDeclaration != null ? restartedActivityDeclaration.name : "<missing>") +
                "' sameObjectEntryId='" + (restartedActivityReady.ObjectEntryId.IsValid &&
                    restartedActivityReady.ObjectEntryId == ObjectEntryId.From(ActivityEntryId)) + "'");
            if (!replacementActivityOccurrenceCommitted)
            {
                Finish(false, "Activity Restart did not admit a new physical Object Entry occurrence.");
                yield break;
            }

            _activityQuery = restartedActivityBinding;
            _activityDeclaration = restartedActivityDeclaration;
            _activityColliders = restartedActivityColliders;
            _activitySceneHandle = SceneManager.GetSceneByName(ActivitySceneName).handle.GetRawData();
            _activityReleaseObserved = false;
            _recordingActivityRestartRelease = false;

            if (routeRequest == null || replacementRoute == null || !routeRequest.HasRouteRuntimeBinding)
            {
                Finish(false, "The replacement Route trigger or Route asset is unavailable.");
                yield break;
            }

            Scene routeScene = SceneManager.GetSceneByName(RouteSceneName);
            if (!routeScene.IsValid() || !routeScene.isLoaded)
            {
                Finish(false, "The current Route scene is unavailable before replacement.");
                yield break;
            }

            _routeSceneHandle = routeScene.handle.GetRawData();
            DontDestroyOnLoad(_routeDeclaration.gameObject);
            _routeTargetMovedToPersistent = true;
            DontDestroyOnLoad(_activityDeclaration.gameObject);
            _activityTargetMovedToPersistent = true;
            _activityReleaseObserved = false;
            routeRequest.TargetRoute = replacementRoute;
            routeRequest.RequestRoute();

            deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while ((!_routeReleaseObserved || routeRequest.IsRequestInFlight) &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;

            if (!_routeReleaseObserved)
            {
                Finish(false, "The original Route scene did not unload after the replacement Route committed.");
                yield break;
            }

            if (routeRequest.IsRequestInFlight || !routeRequest.LastRequestSucceeded ||
                !SceneManager.GetSceneByName(ReplacementRouteSceneName).isLoaded)
            {
                Finish(false, "The replacement Route request did not complete successfully. " + routeRequest.LastMessage);
                yield break;
            }

            Finish(_passedCases == ExpectedCases,
                "Cases passed: " + _passedCases + "/" + ExpectedCases + ".");
        }

        private void OnSceneUnloaded(Scene scene)
        {
            try
            {
                if (scene.handle.GetRawData() == _activitySceneHandle && !_activityReleaseObserved)
                {
                    _activityReleaseObserved = true;
                    RecordActivityRelease(scene);
                }

                if (scene.handle.GetRawData() == _routeSceneHandle && !_routeReleaseObserved)
                {
                    _routeReleaseObserved = true;
                    RecordRouteRelease(scene);
                }
            }
            catch (Exception exception)
            {
                LogLine("status='Failed' checkpoint='scene-release-callback' exception='" +
                    exception.GetType().Name + "' message='" + Clean(exception.Message) + "'");
            }
        }

        private void RecordActivityRelease(Scene scene)
        {
            Collider releasedCollider = _activityColliders != null && _activityColliders.Length > 0
                ? _activityColliders[0] : null;
            bool targetStillLive = releasedCollider != null &&
                releasedCollider.GetComponentInParent<ObjectEntryDeclaration>(true) == _activityDeclaration;

            ObjectEntryRecognitionResult targetAfterRelease = _routeQuery.Recognize(releasedCollider);
            ObjectEntryRecognitionResult routeAfterRelease = _routeQuery.Recognize(_routeCollider);
            bool routeAnchorStillCommitted =
                IsResolved(routeAfterRelease, RouteEntryId, _routeDeclaration.gameObject);

            RecordCase(
                "target-owner-released-query-anchor-valid",
                targetAfterRelease,
                ObjectEntryRecognitionStatus.Unavailable,
                targetStillLive &&
                targetAfterRelease.Status == ObjectEntryRecognitionStatus.Unavailable &&
                routeAnchorStillCommitted,
                "targetStillLive='" + targetStillLive +
                "' routeAnchorTarget='" + routeAfterRelease.Status +
                "' routeId='" + IdText(routeAfterRelease) + "'");

            ObjectEntryRecognitionResult activityAnchorAfterRelease =
                _activityQuery.Recognize(_routeCollider);
            RecordCase(
                "query-anchor-owner-released",
                activityAnchorAfterRelease,
                ObjectEntryRecognitionStatus.Unavailable,
                _routeCollider != null &&
                activityAnchorAfterRelease.Status == ObjectEntryRecognitionStatus.Unavailable,
                "routeTargetStillLive='" + (_routeCollider != null) +
                "' routeTargetId='" + IdText(routeAfterRelease) + "'");

            bool activityCompositionReleased =
                QaIfAdr014CompositionWitness.TryGetDestructionEvidence(ActivityEntryId, out var activityEvidence) &&
                activityEvidence.Root == _activityDeclaration.gameObject &&
                activityEvidence.Declaration == _activityDeclaration &&
                activityEvidence.ObjectEntryId == ActivityEntryId &&
                activityEvidence.RootWasLive &&
                activityEvidence.DeclarationWasIntact &&
                activityEvidence.OwnerSceneWasLoaded &&
                _activityDeclaration.GetComponentInChildren<QaIfAdr014CompositionWitness>(true) == null;
            RecordCompositionCase(
                _recordingActivityRestartRelease
                    ? "activity-restart-composition-released-before-reentry"
                    : "activity-composition-released-before-scene-unload",
                activityCompositionReleased,
                "rootStillSame='" + (_activityDeclaration != null && _activityDeclaration.gameObject == activityEvidence?.Root) +
                "' ownerSceneLoadedAtPartDestroy='" + (activityEvidence?.OwnerSceneWasLoaded ?? false) +
                "' sceneUnloaded='" + scene.name + "'");
        }

        private void RecordRouteRelease(Scene scene)
        {
            bool routeCompositionReleased =
                QaIfAdr014CompositionWitness.TryGetDestructionEvidence(RouteEntryId, out var routeEvidence) &&
                routeEvidence.Root == _routeDeclaration.gameObject &&
                routeEvidence.Declaration == _routeDeclaration &&
                routeEvidence.ObjectEntryId == RouteEntryId &&
                routeEvidence.RootWasLive &&
                routeEvidence.DeclarationWasIntact &&
                routeEvidence.OwnerSceneWasLoaded &&
                _routeDeclaration.GetComponentInChildren<QaIfAdr014CompositionWitness>(true) == null;
            RecordCompositionCase(
                "route-composition-released-before-scene-unload",
                routeCompositionReleased,
                "rootStillSame='" + (_routeDeclaration != null && _routeDeclaration.gameObject == routeEvidence?.Root) +
                "' ownerSceneLoadedAtPartDestroy='" + (routeEvidence?.OwnerSceneWasLoaded ?? false) +
                "' sceneUnloaded='" + scene.name + "'");
        }

        private static bool TryGetFixture(
            Scene scene,
            int expectedColliderCount,
            out ObjectEntryOccurrenceQueryBinding binding,
            out ObjectEntryDeclaration declaration,
            out Collider[] colliders)
        {
            binding = null;
            declaration = null;
            colliders = Array.Empty<Collider>();
            if (!scene.IsValid() || !scene.isLoaded)
                return false;

            ObjectEntryOccurrenceQueryBinding[] bindings = GetSceneComponents<ObjectEntryOccurrenceQueryBinding>(scene);
            ObjectEntryDeclaration[] declarations = GetSceneComponents<ObjectEntryDeclaration>(scene);
            colliders = GetSceneComponents<Collider>(scene);
            if (bindings.Length != 1 || declarations.Length != 1 ||
                colliders.Length != expectedColliderCount)
                return false;

            binding = bindings[0];
            declaration = declarations[0];
            return binding != null && declaration != null;
        }

        private static T[] GetSceneComponents<T>(Scene scene) where T : Component
        {
            var components = new List<T>();
            if (!scene.IsValid() || !scene.isLoaded)
                return components.ToArray();

            foreach (GameObject root in scene.GetRootGameObjects())
                components.AddRange(root.GetComponentsInChildren<T>(true));
            return components.ToArray();
        }

        private static bool IsResolved(
            ObjectEntryRecognitionResult result,
            string expectedId,
            GameObject expectedOccurrence)
        {
            return result.Status == ObjectEntryRecognitionStatus.Resolved &&
                result.Succeeded &&
                result.ObjectEntryId == ObjectEntryId.From(expectedId) &&
                result.Occurrence == expectedOccurrence;
        }

        private void RecordMultipleColliderCase(
            ObjectEntryRecognitionResult first,
            ObjectEntryRecognitionResult second,
            bool passed,
            bool sameIdentity,
            bool sameOccurrence,
            string candidateA,
            string candidateB)
        {
            if (passed) _passedCases++;
            LogLine(
                "case='multiple-colliders-one-occurrence' candidateA='" + candidateA +
                "' candidateB='" + candidateB + "' statusA='" + first.Status +
                "' statusB='" + second.Status + "' expected='Resolved' pass='" + passed +
                "' idA='" + IdText(first) + "' idB='" + IdText(second) +
                "' sameId='" + sameIdentity +
                "' occurrenceA='" + OccurrenceText(first) + "' occurrenceB='" + OccurrenceText(second) +
                "' sameDeclarationRoot='" + sameOccurrence +
                "' diagnosticA='" + Clean(first.Diagnostic) +
                "' diagnosticB='" + Clean(second.Diagnostic) + "'");
        }

        private void RecordCase(
            string name,
            ObjectEntryRecognitionResult result,
            ObjectEntryRecognitionStatus expected,
            bool passed,
            string detail)
        {
            if (passed) _passedCases++;
            LogLine(
                "case='" + name + "' status='" + result.Status + "' expected='" + expected +
                "' pass='" + passed + "' id='" + IdText(result) +
                "' occurrence='" + OccurrenceText(result) + "' " + detail +
                " diagnostic='" + Clean(result.Diagnostic) + "'");
        }

        private void RecordCompositionCase(string name, bool passed, string detail)
        {
            if (passed) _passedCases++;
            LogLine("case='" + name + "' pass='" + passed + "' " + detail);
        }

        private static void LogEvidence(
            string name,
            ObjectEntryRecognitionResult result,
            bool passed,
            string detail)
        {
            LogLine(
                "evidence='" + name + "' status='" + result.Status +
                "' pass='" + passed + "' id='" + IdText(result) +
                "' occurrence='" + OccurrenceText(result) + "' " + detail +
                " diagnostic='" + Clean(result.Diagnostic) + "'");
        }

        private static string IdText(ObjectEntryRecognitionResult result)
        {
            return result.ObjectEntryId.IsValid ? result.ObjectEntryId.StableText : "<none>";
        }

        private static string OccurrenceText(ObjectEntryRecognitionResult result)
        {
            return result.Occurrence != null ? result.Occurrence.name : "<none>";
        }

        private static string Clean(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty :
                value.Replace("\r", " ").Replace("\n", " ").Replace("'", "\"");
        }

        private static void LogLine(string fields)
        {
            Debug.Log("[IF-ADR-014-DEMO] " + fields);
        }

        private void Finish(bool passed, string detail)
        {
            if (_finished) return;
            _finished = true;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            if (_activityTargetMovedToPersistent && _activityDeclaration != null)
                Destroy(_activityDeclaration.gameObject);
            if (_restartedActivityRootToCleanup != null)
                Destroy(_restartedActivityRootToCleanup);
            if (_routeTargetMovedToPersistent && _routeDeclaration != null)
                Destroy(_routeDeclaration.gameObject);
            string status = passed ? "Passed" : "Failed";
            string line = "[IF-ADR-014-DEMO] status='" + status +
                "' cases='" + _passedCases + "/" + ExpectedCases +
                "' detail='" + Clean(detail) + "'";
            if (passed) Debug.Log(line);
            else Debug.LogError(line);
        }
    }
}
