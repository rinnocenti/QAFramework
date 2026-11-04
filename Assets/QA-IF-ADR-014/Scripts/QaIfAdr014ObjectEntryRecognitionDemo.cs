using System;
using System.Collections;
using System.Collections.Generic;
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
        private const string ActivitySceneName = "QA_IF_ADR_014_Activity";
        private const int ExpectedCases = 4;
        private const float TimeoutSeconds = 20f;

        [SerializeField] private ActivityRequestTrigger activityRequest;
        [SerializeField] private ActivityAsset activity;
        [SerializeField] private ObjectEntryDeclaration unadmittedDeclaration;
        [SerializeField] private Collider unadmittedCollider;

        private IObjectEntryRecognitionQuery _routeQuery;
        private IObjectEntryRecognitionQuery _activityQuery;
        private ObjectEntryDeclaration _routeDeclaration;
        private Collider _routeCollider;
        private ObjectEntryDeclaration _activityDeclaration;
        private Collider[] _activityColliders;
        private int _passedCases;
        private bool _releaseObserved;
        private bool _finished;
        private ulong _activitySceneHandle;
        private bool _activityTargetMovedToPersistent;

        private IEnumerator Start()
        {
            yield return RunDemo();
        }

        private IEnumerator RunDemo()
        {
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
            activityRequest.ClearActivity();

            deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while ((!_releaseObserved || activityRequest.IsRequestInFlight) &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;

            if (!_releaseObserved)
            {
                Finish(false, "Activity content did not begin unloading after Clear Activity.");
                yield break;
            }

            if (activityRequest.IsRequestInFlight ||
                !activityRequest.LastRequestSucceeded ||
                !activityRequest.LastRequestClearedActivity)
            {
                Finish(false, "Clear Activity did not complete successfully. " + activityRequest.LastMessage);
                yield break;
            }

            Finish(_passedCases == ExpectedCases,
                "Cases passed: " + _passedCases + "/" + ExpectedCases + ".");
        }

        private void OnSceneUnloaded(Scene scene)
        {
            if (scene.handle.GetRawData() != _activitySceneHandle || _releaseObserved)
                return;

            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            _releaseObserved = true;

            try
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
            }
            catch (Exception exception)
            {
                LogLine("status='Failed' checkpoint='release-callback' exception='" +
                    exception.GetType().Name + "' message='" + Clean(exception.Message) + "'");
            }
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
            string status = passed ? "Passed" : "Failed";
            string line = "[IF-ADR-014-DEMO] status='" + status +
                "' cases='" + _passedCases + "/" + ExpectedCases +
                "' detail='" + Clean(detail) + "'";
            if (passed) Debug.Log(line);
            else Debug.LogError(line);
        }
    }
}
