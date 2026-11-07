using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ActivityRestart;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.ObjectEntry;
using Immersive.Framework.RuntimeContent;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.QaFramework.IfAdr014.C4
{
    public sealed class QaIfAdr014C4DynamicCompositionScenario : MonoBehaviour
    {
        public const string PlainEntryId = "qa.if-adr-014.c4.plain";
        public const string ProfileEntryId = "qa.if-adr-014.c4.profile";
        public const string DynamicEntryId = "qa.if-adr-014.c4.dynamic";
        public const string BaseDynamicEntryId = "qa.if-adr-014.c4.base-dynamic";
        public const string ActivityEntryId = "qa.if-adr-014.c4.activity";

        private const string RouteSceneName = "QA_IF_ADR_014_C4_Route";
        private const string ActivitySceneName = "QA_IF_ADR_014_C4_Activity";
        private const int ExpectedCases = 21;
        private const float TimeoutSeconds = 30f;

        private enum PartFixture
        {
            Base = 0,
            Simple = 1,
            Replacement = 2,
            Override = 3,
            IndependentA = 4,
            IndependentB = 5,
            PriorityLow = 6,
            PriorityHigh = 7,
            ConflictFirst = 8,
            ConflictRejected = 9,
            HiddenTieFirst = 10,
            HiddenTieSecond = 11,
            HiddenTieWinner = 12,
            DependencyPrerequisite = 13,
            Dependent = 14,
            MissingDependency = 15,
            Activity = 16
        }

        private sealed class ExpectedPart
        {
            internal ExpectedPart(GameObject root, ObjectEntryId objectEntryId, GameObject part)
                : this(root, objectEntryId, part, root.scene)
            {
            }

            internal ExpectedPart(
                GameObject root,
                ObjectEntryId objectEntryId,
                GameObject part,
                Scene ownerScene)
                : this(root, objectEntryId, part, ownerScene,
                    ownerScene.IsValid() && ownerScene.isLoaded)
            {
            }

            internal ExpectedPart(
                GameObject root,
                ObjectEntryId objectEntryId,
                GameObject part,
                Scene ownerScene,
                bool ownerSceneWasLoaded)
            {
                Root = root;
                ObjectEntryId = objectEntryId;
                Part = part;
                RootEntityId = root.GetEntityId();
                PartEntityId = part.GetEntityId();
                OwnerSceneHandle = ownerScene.handle;
                OwnerSceneWasLoaded = ownerSceneWasLoaded;
            }

            internal GameObject Root { get; }
            internal ObjectEntryId ObjectEntryId { get; }
            internal GameObject Part { get; }
            internal EntityId RootEntityId { get; }
            internal EntityId PartEntityId { get; }
            internal SceneHandle OwnerSceneHandle { get; }
            internal bool OwnerSceneWasLoaded { get; }
        }

        [SerializeField] private ActivityRequestTrigger activityRequest;
        [SerializeField] private ActivityRestartTrigger activityRestart;
        [SerializeField] private ActivityAsset activity;
        [SerializeField] private RouteRequestTrigger routeRequest;
        [SerializeField] private RouteAsset replacementRoute;
        [SerializeField] private GameObject[] partPrefabs = Array.Empty<GameObject>();

        private readonly List<GameObject> _retainedRoots = new List<GameObject>();
        private readonly List<ExpectedPart> _cleanupExpectedParts = new List<ExpectedPart>();
        private readonly HashSet<SceneHandle> _unloadedSceneHandles = new HashSet<SceneHandle>();

        private int _executedCases;
        private int _passedCases;
        private bool _finished;
        private bool _routeReplacementCompleted;
        private bool _ownerCleanupConfirmed;
        private string _terminalStatus;
        private string _terminalDiagnostic = string.Empty;
        private ObjectEntryCompositionOperationResult _lastOperation;
        private IObjectEntryRecognitionQuery _routeRecognition;

        private ObjectEntryDeclaration _plainDeclaration;
        private Collider _plainCollider;
        private ObjectEntryDeclaration _profileDeclaration;
        private ObjectEntryDeclaration _dynamicDeclaration;
        private Collider _dynamicCollider;
        private ObjectEntryDeclaration _baseDynamicDeclaration;
        private ObjectEntryDeclaration _activityOldDeclaration;
        private GameObject _activityOldRoot;
        private ObjectEntryId _activityOldId;
        private GameObject _activityOldPart;
        private ObjectEntryCompositionAccessGrant _activityOldGrant;
        private ObjectEntryCompositionContribution _activityOldContribution;
        private EntityId _activityOldPartEntityId;
        private GameObject _activityCurrentRoot;
        private ObjectEntryDeclaration _activityCurrentDeclaration;
        private SceneHandle _firstActivitySceneHandle;
        private SceneHandle _readmittedActivitySceneHandle;
        private SceneHandle _routeSceneHandle;
        private IObjectEntryOccurrenceQuery _routeOccurrenceQuery;

        private IEnumerator Start()
        {
            yield return RunSafely();
        }

        private void OnDestroy()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        private IEnumerator RunSafely()
        {
            QaIfAdr014C4PartWitness.ResetEvidence();
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            IEnumerator cases = RunCases();
            while (true)
            {
                bool hasNext;
                object current = null;
                try
                {
                    hasNext = cases.MoveNext();
                    if (hasNext)
                        current = cases.Current;
                }
                catch (Exception exception)
                {
                    SetTerminal("Failed", "Unhandled scenario exception: " + exception);
                    Debug.LogError("[IF-ADR-014-C4] case='scenario-exception' observed='Exception'" +
                        " expected='All defined cases complete' pass='false' objectEntryId='<pending>'" +
                        " evidence='cleanup-required' diagnostic='" + Safe(exception.ToString()) + "'");
                    break;
                }

                if (!hasNext)
                    break;
                yield return current;
            }

            if (_terminalStatus == null && _passedCases != ExpectedCases)
            {
                _terminalStatus = "Failed";
                _terminalDiagnostic = "The scenario ended before all defined cases completed.";
            }

            if (_terminalStatus != "Passed" && !_routeReplacementCompleted)
                yield return RequestOwnerCleanup();

            yield return DestroyRetainedRoots();
            if (_terminalStatus == null)
            {
                _terminalStatus = _passedCases == ExpectedCases ? "Passed" : "Failed";
                if (_terminalStatus == "Failed")
                    _terminalDiagnostic = "The scenario ended before all defined cases completed.";
            }
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            if (!_finished)
                EmitTerminal();
        }

        private IEnumerator RunCases()
        {
            Scene routeScene = default;
            ObjectEntryOccurrenceQueryBinding routeBinding = null;
            ObjectEntryRecognitionResult plainResolution = default;
            float deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                routeScene = SceneManager.GetSceneByName(RouteSceneName);
                if (routeScene.IsValid() && routeScene.isLoaded &&
                    TryFindQueryBinding(routeScene, out routeBinding) &&
                    TryFindEntry(routeScene, PlainEntryId, out _plainDeclaration, out _plainCollider) &&
                    TryFindEntry(routeScene, ProfileEntryId, out _profileDeclaration, out _) &&
                    TryFindEntry(routeScene, DynamicEntryId, out _dynamicDeclaration, out _dynamicCollider) &&
                    TryFindEntry(routeScene, BaseDynamicEntryId, out _baseDynamicDeclaration, out _) &&
                    IsResolved(routeBinding, _plainCollider, PlainEntryId, _plainDeclaration.gameObject) &&
                    HasWitness(_profileDeclaration.gameObject, Fixture(PartFixture.Base)) &&
                    HasWitness(_baseDynamicDeclaration.gameObject, Fixture(PartFixture.Base)))
                    break;
                yield return null;
            }

            if (!routeScene.IsValid() || !routeScene.isLoaded || routeBinding == null ||
                _plainDeclaration == null || _profileDeclaration == null ||
                _dynamicDeclaration == null || _baseDynamicDeclaration == null ||
                _dynamicCollider == null)
            {
                Block("route-fixtures-ready", "Committed C4 Route fixtures were unavailable before the readiness guard expired.",
                    "Route scene loaded with four declared entries, query anchor committed, Profile Base active",
                    "owner='<unavailable>' objectEntryId='<unavailable>'");
                yield break;
            }

            _routeSceneHandle = routeScene.handle;
            _routeRecognition = routeBinding;
            _routeOccurrenceQuery = routeBinding;
            plainResolution = _routeRecognition.Recognize(_plainCollider);

            ObjectEntryId plainId = default;
            bool plainPassed = _plainDeclaration.TryGetObjectEntryId(out plainId) &&
                plainId == ObjectEntryId.From(PlainEntryId) &&
                _plainDeclaration.GetComponent<ObjectEntryCompositionAuthoring>() == null &&
                GetWitnesses(_plainDeclaration.gameObject).Length == 0 &&
                IsResolved(plainResolution, PlainEntryId, _plainDeclaration.gameObject);
            if (!RecordCase(
                    "plain-object-entry",
                    plainPassed,
                    plainResolution.Status.ToString(),
                    "Resolved, valid ID, no authoring and no C4 part",
                    plainId,
                    "owner='Route' rootEntityId='" + _plainDeclaration.gameObject.GetEntityId() +
                    "' rootLive='true' declarationLive='" + (_plainDeclaration != null) +
                    "' dynamicGrantAvailable='false' materializedParts='" +
                    GetWitnesses(_plainDeclaration.gameObject).Length + "'",
                    plainResolution.Diagnostic))
                yield break;

            ObjectEntryCompositionAuthoring profileAuthoring =
                _profileDeclaration.GetComponent<ObjectEntryCompositionAuthoring>();
            string profileIssue = string.Empty;
            ObjectEntryId profileId = default;
            bool profileGrantAvailable = profileAuthoring != null &&
                profileAuthoring.TryGetCompositionAccessGrant(out _, out profileIssue);
            bool profilePassed = profileAuthoring != null &&
                profileAuthoring.Profile != null &&
                !profileAuthoring.AllowsDynamicContributions &&
                HasWitness(_profileDeclaration.gameObject, Fixture(PartFixture.Base)) &&
                !profileGrantAvailable &&
                _profileDeclaration.TryGetObjectEntryId(out profileId) &&
                profileId == ObjectEntryId.From(ProfileEntryId);
            GameObject profileBasePart = FindActiveWitness(
                _profileDeclaration.gameObject, Fixture(PartFixture.Base))?.gameObject;
            if (!RecordCase(
                    "profile-only-base-no-dynamic-grant",
                    profilePassed,
                    "basePart='" + HasWitness(_profileDeclaration.gameObject, Fixture(PartFixture.Base)) +
                    "' grantAvailable='" + profileGrantAvailable + "'",
                    "Base active, opt-in off, no dynamic grant",
                    profileId,
                    "owner='Route' rootEntityId='" + _profileDeclaration.gameObject.GetEntityId() +
                    "' basePartEntityId='" + (profileBasePart != null ? profileBasePart.GetEntityId() : EntityId.None) +
                    "' rootLive='true' declarationLive='true'",
                    profileIssue))
                yield break;

            ObjectEntryCompositionAuthoring dynamicAuthoring =
                _dynamicDeclaration.GetComponent<ObjectEntryCompositionAuthoring>();
            ObjectEntryCompositionAccessGrant dynamicGrant = null;
            string dynamicIssue = string.Empty;
            bool dynamicGrantAvailable = dynamicAuthoring != null &&
                dynamicAuthoring.Profile == null &&
                dynamicAuthoring.AllowsDynamicContributions &&
                dynamicAuthoring.TryGetCompositionAccessGrant(
                    out dynamicGrant,
                    out dynamicIssue);
            if (!dynamicGrantAvailable)
            {
                Block("dynamic-only-grant-ready", "The profileless dynamic grant was not available on the committed exact occurrence.",
                    "Exact authoring grant available after owner commit",
                    "objectEntryId='" + IdText(_dynamicDeclaration) + "' diagnostic='" + dynamicIssue + "'");
                yield break;
            }

            ObjectEntryId dynamicId = _dynamicDeclaration.ObjectEntryId;
            ObjectEntryRecognitionResult beforeMutation = _routeRecognition.Recognize(_dynamicCollider);
            var queryReference = new StableObjectReference(dynamicId);
            ObjectEntryOccurrenceResolution occurrenceBefore =
                _routeOccurrenceQuery.Resolve(queryReference);
            if (!IsResolved(beforeMutation, DynamicEntryId, _dynamicDeclaration.gameObject))
            {
                Block("recognition-before-mutation", "Read-only recognition did not resolve the dynamic entry before its first mutation.",
                    "Resolved exact Object Entry root and ID",
                    beforeMutation.Diagnostic);
                yield break;
            }

            if (!dynamicGrant.TryOpenContribution(10, out ObjectEntryCompositionContribution simple,
                    out string openIssue))
            {
                Block("dynamic-contribution-open", "The exact occurrence grant could not open a contribution.",
                    "Contribution opened from the ObjectEntryCompositionAuthoring grant",
                    openIssue);
                yield break;
            }

            yield return WaitForOperation(simple.SetPartAsync(
                Definition("part.dynamic", PartFixture.Simple)));
            if (_terminalStatus != null)
                yield break;
            GameObject addedPart = FindWitness(
                _dynamicDeclaration.gameObject, Fixture(PartFixture.Simple))?.gameObject;
            bool added = _lastOperation.Status == ObjectEntryCompositionOperationStatus.Committed &&
                IsUnityObjectAlive(addedPart) &&
                HasWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.Simple)) &&
                HasExactRoot(_dynamicDeclaration.gameObject, Fixture(PartFixture.Simple)) &&
                IdentityPreserved(_dynamicDeclaration, DynamicEntryId, dynamicId);
            ObjectEntryRecognitionResult afterFirstMutation =
                _routeRecognition.Recognize(_dynamicCollider);
            ObjectEntryOccurrenceResolution occurrenceAfter =
                _routeOccurrenceQuery.Resolve(queryReference);
            if (!RecordOperationCase(
                    "dynamic-only-add",
                    added,
                    _lastOperation,
                    "Committed physical child under the exact root; declaration and ID preserved",
                    dynamicId,
                    "rootEntityId='" + _dynamicDeclaration.gameObject.GetEntityId() +
                    "' partEntityId='" + (addedPart != null ? addedPart.GetEntityId() : EntityId.None) +
                    "' prefab='" + Fixture(PartFixture.Simple).GetComponent<QaIfAdr014C4PartWitness>().PrefabId +
                    "' rootLive='true' declarationLive='true'"))
                yield break;

            EntityId addedPartEntityId = addedPart.GetEntityId();
            yield return WaitForOperation(simple.SetPartAsync(
                Definition("part.dynamic", PartFixture.Replacement)));
            if (_terminalStatus != null)
                yield break;
            yield return null;
            bool replaced = _lastOperation.Status == ObjectEntryCompositionOperationStatus.Committed &&
                !IsUnityObjectAlive(addedPart) &&
                HasWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.Replacement)) &&
                !HasWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.Simple)) &&
                IdentityPreserved(_dynamicDeclaration, DynamicEntryId, dynamicId);
            if (!RecordOperationCase(
                    "dynamic-only-replace",
                    replaced,
                    _lastOperation,
                    "Committed replacement; prior physical instance destroyed; exact identity preserved",
                    dynamicId,
                    "rootEntityId='" + _dynamicDeclaration.gameObject.GetEntityId() +
                    "' oldPartEntityId='" + addedPartEntityId +
                    "' oldPartDestroyed='" + !IsUnityObjectAlive(addedPart) + "' replacementPresent='" +
                    HasWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.Replacement)) + "'"))
                yield break;

            GameObject replacementPart = FindWitness(
                _dynamicDeclaration.gameObject, Fixture(PartFixture.Replacement))?.gameObject;
            yield return WaitForOperation(simple.RemovePartAsync("part.dynamic"));
            if (_terminalStatus != null)
                yield break;
            yield return null;
            bool removed = _lastOperation.Status == ObjectEntryCompositionOperationStatus.Committed &&
                !IsUnityObjectAlive(replacementPart) &&
                FindWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.Replacement)) == null &&
                GetWitnesses(_dynamicDeclaration.gameObject).Length == 0 &&
                IdentityPreserved(_dynamicDeclaration, DynamicEntryId, dynamicId);
            if (!RecordOperationCase(
                    "dynamic-only-remove",
                    removed,
                    _lastOperation,
                    "Committed removal; no physical residue; exact identity preserved",
                    dynamicId,
                    "remainingParts='" + GetWitnesses(_dynamicDeclaration.gameObject).Length +
                    "' rootLive='true' declarationLive='true'"))
                yield break;

            ObjectEntryCompositionAuthoring baseDynamicAuthoring =
                _baseDynamicDeclaration.GetComponent<ObjectEntryCompositionAuthoring>();
            ObjectEntryCompositionAccessGrant baseDynamicGrant = null;
            string baseDynamicIssue = string.Empty;
            ObjectEntryCompositionContribution overrideHandle = null;
            if (baseDynamicAuthoring == null ||
                !baseDynamicAuthoring.TryGetCompositionAccessGrant(
                    out baseDynamicGrant, out baseDynamicIssue) ||
                !baseDynamicGrant.TryOpenContribution(50, out overrideHandle,
                    out baseDynamicIssue))
            {
                Block("base-dynamic-grant-ready", "The Base-plus-Dynamic entry did not expose its exact occurrence grant.",
                    "Profile Base active and dynamic grant available",
                    baseDynamicIssue);
                yield break;
            }

            ObjectEntryId baseDynamicId = _baseDynamicDeclaration.ObjectEntryId;
            if (!HasWitness(_baseDynamicDeclaration.gameObject, Fixture(PartFixture.Base)))
            {
                Block("base-dynamic-base-ready", "The Base part was not physically active before override.",
                    "Base prefab witness active under the declared root",
                    "objectEntryId='" + baseDynamicId.StableText + "'");
                yield break;
            }

            GameObject basePart = FindWitness(
                _baseDynamicDeclaration.gameObject, Fixture(PartFixture.Base)).gameObject;
            EntityId basePartEntityId = basePart.GetEntityId();
            yield return WaitForOperation(overrideHandle.SetPartAsync(
                Definition("slot.primary", PartFixture.Override)));
            if (_terminalStatus != null)
                yield break;
            yield return null;
            GameObject overridePart = FindActiveWitness(
                _baseDynamicDeclaration.gameObject, Fixture(PartFixture.Override))?.gameObject;
            bool overridePassed = _lastOperation.Status == ObjectEntryCompositionOperationStatus.Committed &&
                !IsUnityObjectAlive(basePart) &&
                IsUnityObjectAlive(overridePart) &&
                HasWitness(_baseDynamicDeclaration.gameObject, Fixture(PartFixture.Override)) &&
                !HasWitness(_baseDynamicDeclaration.gameObject, Fixture(PartFixture.Base)) &&
                IdentityPreserved(_baseDynamicDeclaration, BaseDynamicEntryId, baseDynamicId);
            if (!RecordOperationCase(
                    "base-dynamic-override",
                    overridePassed,
                    _lastOperation,
                    "Committed higher-priority override is the only physical part for the shared key",
                    baseDynamicId,
                    "rootEntityId='" + _baseDynamicDeclaration.gameObject.GetEntityId() +
                    "' basePartEntityId='" + basePartEntityId +
                    "' baseDestroyed='" + !IsUnityObjectAlive(basePart) +
                    "' overridePartEntityId='" + (overridePart != null ? overridePart.GetEntityId() : EntityId.None) +
                    "' overridePresent='" +
                    HasWitness(_baseDynamicDeclaration.gameObject, Fixture(PartFixture.Override)) + "'"))
                yield break;

            yield return WaitForOperation(overrideHandle.RemovePartAsync("slot.primary"));
            if (_terminalStatus != null)
                yield break;
            yield return null;
            bool restored = _lastOperation.Status == ObjectEntryCompositionOperationStatus.Committed &&
                !IsUnityObjectAlive(overridePart) &&
                FindWitness(_baseDynamicDeclaration.gameObject, Fixture(PartFixture.Override)) == null &&
                HasWitness(_baseDynamicDeclaration.gameObject, Fixture(PartFixture.Base)) &&
                IdentityPreserved(_baseDynamicDeclaration, BaseDynamicEntryId, baseDynamicId);
            if (!RecordOperationCase(
                    "base-dynamic-restoration",
                    restored,
                    _lastOperation,
                    "Removing the override restores the original Base part and exact identity",
                    baseDynamicId,
                    "baseRestored='" + HasWitness(_baseDynamicDeclaration.gameObject, Fixture(PartFixture.Base)) + "'"))
                yield break;

            if (!dynamicGrant.TryOpenContribution(10, out ObjectEntryCompositionContribution independentA, out openIssue) ||
                !dynamicGrant.TryOpenContribution(10, out ObjectEntryCompositionContribution independentB, out openIssue))
            {
                Block("independent-contributions-open", "Independent contributions could not be opened on the same exact grant.",
                    "Two distinct contribution handles opened",
                    openIssue);
                yield break;
            }

            yield return WaitForOperation(independentA.SetPartAsync(
                Definition("part.independent-a", PartFixture.IndependentA)));
            if (_terminalStatus != null)
                yield break;
            ObjectEntryCompositionOperationResult independentAResult = _lastOperation;
            yield return WaitForOperation(independentB.SetPartAsync(
                Definition("part.independent-b", PartFixture.IndependentB)));
            if (_terminalStatus != null)
                yield break;
            bool coexist = independentAResult.Status == ObjectEntryCompositionOperationStatus.Committed &&
                _lastOperation.Status == ObjectEntryCompositionOperationStatus.Committed &&
                HasWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.IndependentA)) &&
                HasWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.IndependentB)) &&
                IdentityPreserved(_dynamicDeclaration, DynamicEntryId, dynamicId);
            if (!RecordOperationCase(
                    "independent-contributions-coexist",
                    coexist,
                    _lastOperation,
                    "Different keyed contributions coexist physically on one root",
                    dynamicId,
                    "contributionA='" + independentA.Priority + "' contributionB='" +
                    independentB.Priority + "' parts='" + GetWitnesses(_dynamicDeclaration.gameObject).Length + "'"))
                yield break;

            GameObject independentAPart = FindWitness(
                _dynamicDeclaration.gameObject, Fixture(PartFixture.IndependentA))?.gameObject;
            GameObject independentBPart = FindWitness(
                _dynamicDeclaration.gameObject, Fixture(PartFixture.IndependentB))?.gameObject;
            yield return WaitForOperation(independentA.CloseAsync());
            if (_terminalStatus != null)
                yield break;
            yield return null;
            bool siblingPreserved = _lastOperation.Status == ObjectEntryCompositionOperationStatus.Committed &&
                !IsUnityObjectAlive(independentAPart) &&
                FindWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.IndependentA)) == null &&
                IsUnityObjectAlive(independentBPart) &&
                object.ReferenceEquals(
                    FindWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.IndependentB))?.gameObject,
                    independentBPart) &&
                HasWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.IndependentB)) &&
                IdentityPreserved(_dynamicDeclaration, DynamicEntryId, dynamicId);
            if (!RecordOperationCase(
                    "close-one-preserves-sibling",
                    siblingPreserved,
                    _lastOperation,
                    "Closing one contribution removes only its part and preserves its sibling",
                    dynamicId,
                    "closedPartDestroyed='" + !IsUnityObjectAlive(independentAPart) +
                    "' siblingPresent='" + HasWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.IndependentB)) + "'"))
                yield break;

            if (!dynamicGrant.TryOpenContribution(10, out ObjectEntryCompositionContribution priorityLow, out openIssue) ||
                !dynamicGrant.TryOpenContribution(20, out ObjectEntryCompositionContribution priorityHigh, out openIssue))
            {
                Block("priority-contributions-open", "Priority contributions could not be opened.",
                    "Two independent contributions with different priorities",
                    openIssue);
                yield break;
            }

            yield return WaitForOperation(priorityLow.SetPartAsync(
                Definition("part.priority", PartFixture.PriorityLow)));
            if (_terminalStatus != null)
                yield break;
            ObjectEntryCompositionOperationResult priorityLowResult = _lastOperation;
            GameObject priorityLowPart = FindWitness(
                _dynamicDeclaration.gameObject, Fixture(PartFixture.PriorityLow))?.gameObject;
            yield return WaitForOperation(priorityHigh.SetPartAsync(
                Definition("part.priority", PartFixture.PriorityHigh)));
            if (_terminalStatus != null)
                yield break;
            yield return null;
            GameObject priorityHighPart = FindWitness(
                _dynamicDeclaration.gameObject, Fixture(PartFixture.PriorityHigh))?.gameObject;
            bool priorityPassed = priorityLowResult.Status == ObjectEntryCompositionOperationStatus.Committed &&
                _lastOperation.Status == ObjectEntryCompositionOperationStatus.Committed &&
                !IsUnityObjectAlive(priorityLowPart) &&
                IsUnityObjectAlive(priorityHighPart) &&
                object.ReferenceEquals(
                    FindWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.PriorityHigh))?.gameObject,
                    priorityHighPart) &&
                HasWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.PriorityHigh)) &&
                FindWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.PriorityLow)) == null &&
                IdentityPreserved(_dynamicDeclaration, DynamicEntryId, dynamicId);
            if (!RecordOperationCase(
                    "higher-priority-wins",
                    priorityPassed,
                    _lastOperation,
                    "The higher-priority contribution is the sole physical winner for the shared key",
                    dynamicId,
                    "lowPriority='" + priorityLow.Priority + "' highPriority='" + priorityHigh.Priority +
                    "' winnerPresent='" + HasWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.PriorityHigh)) + "'"))
                yield break;

            if (!dynamicGrant.TryOpenContribution(30, out ObjectEntryCompositionContribution conflictFirst, out openIssue) ||
                !dynamicGrant.TryOpenContribution(30, out ObjectEntryCompositionContribution conflictSecond, out openIssue))
            {
                Block("equal-priority-contributions-open", "Equal-priority contributions could not be opened.",
                    "Two equal highest-priority contributions opened",
                    openIssue);
                yield break;
            }

            yield return WaitForOperation(conflictFirst.SetPartAsync(
                Definition("part.equal-conflict", PartFixture.ConflictFirst)));
            if (_terminalStatus != null)
                yield break;
            ObjectEntryCompositionOperationResult conflictWinner = _lastOperation;
            yield return WaitForOperation(conflictSecond.SetPartAsync(
                Definition("part.equal-conflict", PartFixture.ConflictRejected)));
            if (_terminalStatus != null)
                yield break;
            bool conflictPassed = conflictWinner.Status == ObjectEntryCompositionOperationStatus.Committed &&
                _lastOperation.Status == ObjectEntryCompositionOperationStatus.Rejected &&
                _lastOperation.Diagnostic.Contains("EqualPriorityConflict", StringComparison.Ordinal) &&
                !_lastOperation.EffectsMayHaveApplied &&
                HasWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.ConflictFirst)) &&
                FindWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.ConflictRejected)) == null &&
                IdentityPreserved(_dynamicDeclaration, DynamicEntryId, dynamicId);
            if (!RecordOperationCase(
                    "equal-priority-conflict-no-effects",
                    conflictPassed,
                    _lastOperation,
                    "Equal highest-priority key is Rejected without effects; existing physical winner remains",
                    dynamicId,
                    "winnerPresent='" + HasWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.ConflictFirst)) +
                    "' rejectedPrefabAbsent='" +
                    (FindWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.ConflictRejected)) == null) + "'"))
                yield break;

            if (!dynamicGrant.TryOpenContribution(1, out ObjectEntryCompositionContribution hiddenLowA, out openIssue) ||
                !dynamicGrant.TryOpenContribution(1, out ObjectEntryCompositionContribution hiddenLowB, out openIssue) ||
                !dynamicGrant.TryOpenContribution(10, out ObjectEntryCompositionContribution hiddenHigh, out openIssue))
            {
                Block("hidden-tie-contributions-open", "Hidden-tie contributions could not be opened.",
                    "Two tied low priorities and one higher winner opened",
                    openIssue);
                yield break;
            }

            yield return WaitForOperation(hiddenLowA.SetPartAsync(
                Definition("part.hidden-tie", PartFixture.HiddenTieFirst)));
            if (_terminalStatus != null)
                yield break;
            yield return WaitForOperation(hiddenHigh.SetPartAsync(
                Definition("part.hidden-tie", PartFixture.HiddenTieWinner)));
            if (_terminalStatus != null)
                yield break;
            yield return null;
            yield return WaitForOperation(hiddenLowB.SetPartAsync(
                Definition("part.hidden-tie", PartFixture.HiddenTieSecond)));
            if (_terminalStatus != null)
                yield break;
            ObjectEntryCompositionOperationResult hiddenTieSet = _lastOperation;
            yield return WaitForOperation(hiddenHigh.RemovePartAsync("part.hidden-tie"));
            if (_terminalStatus != null)
                yield break;
            bool hiddenTiePassed = hiddenTieSet.IsSuccessful &&
                _lastOperation.Status == ObjectEntryCompositionOperationStatus.Rejected &&
                _lastOperation.Diagnostic.Contains("EqualPriorityConflict", StringComparison.Ordinal) &&
                !_lastOperation.EffectsMayHaveApplied &&
                HasWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.HiddenTieWinner)) &&
                FindWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.HiddenTieFirst)) == null &&
                FindWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.HiddenTieSecond)) == null &&
                IdentityPreserved(_dynamicDeclaration, DynamicEntryId, dynamicId);
            if (!RecordOperationCase(
                    "hidden-equal-priority-tie",
                    hiddenTiePassed,
                    _lastOperation,
                    "Revealing a hidden lower-priority tie is Rejected and retains the higher winner",
                    dynamicId,
                    "hiddenTieSet='" + hiddenTieSet.Status + "' highWinnerPresent='" +
                    HasWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.HiddenTieWinner)) + "'"))
                yield break;

            if (!dynamicGrant.TryOpenContribution(1, out ObjectEntryCompositionContribution prerequisite, out openIssue) ||
                !dynamicGrant.TryOpenContribution(5, out ObjectEntryCompositionContribution dependent, out openIssue))
            {
                Block("dependency-contributions-open", "Dependency contributions could not be opened.",
                    "Provider and dependent contribution opened",
                    openIssue);
                yield break;
            }

            yield return WaitForOperation(prerequisite.SetPartAsync(
                Definition("part.prerequisite", PartFixture.DependencyPrerequisite)));
            if (_terminalStatus != null)
                yield break;
            yield return WaitForOperation(dependent.SetPartAsync(
                Definition("part.dependent", PartFixture.Dependent, "part.prerequisite")));
            if (_terminalStatus != null)
                yield break;
            bool dependencyPassed = _lastOperation.Status == ObjectEntryCompositionOperationStatus.Committed &&
                HasWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.DependencyPrerequisite)) &&
                HasWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.Dependent)) &&
                IdentityPreserved(_dynamicDeclaration, DynamicEntryId, dynamicId);
            if (!RecordOperationCase(
                    "cross-contribution-dependency",
                    dependencyPassed,
                    _lastOperation,
                    "A dependent contribution resolves a prerequisite provided by a sibling contribution",
                    dynamicId,
                    "prerequisitePresent='" + HasWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.DependencyPrerequisite)) +
                    "' dependentPresent='" + HasWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.Dependent)) + "'"))
                yield break;

            yield return WaitForOperation(dependent.SetPartAsync(
                Definition("part.invalid-dependent", PartFixture.MissingDependency, "part.absent")));
            if (_terminalStatus != null)
                yield break;
            bool invalidDependencyPassed = _lastOperation.Status == ObjectEntryCompositionOperationStatus.Rejected &&
                _lastOperation.Diagnostic.Contains("MissingDependency", StringComparison.Ordinal) &&
                !_lastOperation.EffectsMayHaveApplied &&
                FindWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.MissingDependency)) == null &&
                HasWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.Dependent)) &&
                IdentityPreserved(_dynamicDeclaration, DynamicEntryId, dynamicId);
            if (!RecordOperationCase(
                    "missing-dependency-rejected",
                    invalidDependencyPassed,
                    _lastOperation,
                    "Missing dependency is Rejected without effects and leaves valid parts intact",
                    dynamicId,
                    "invalidPrefabAbsent='" +
                    (FindWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.MissingDependency)) == null) +
                    "' validDependentPreserved='" + HasWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.Dependent)) + "'"))
                yield break;

            ObjectEntryCompositionAccessGrant authoringGrant;
            bool authoringStillOwnsGrant = dynamicAuthoring.TryGetCompositionAccessGrant(
                out authoringGrant, out string postMutationGrantIssue);
            bool queryPassed = IsResolved(beforeMutation, DynamicEntryId, _dynamicDeclaration.gameObject) &&
                IsResolved(afterFirstMutation, DynamicEntryId, _dynamicDeclaration.gameObject) &&
                beforeMutation.ObjectEntryId == afterFirstMutation.ObjectEntryId &&
                beforeMutation.ObjectEntryId == dynamicId &&
                object.ReferenceEquals(beforeMutation.Occurrence, afterFirstMutation.Occurrence) &&
                occurrenceBefore.Succeeded &&
                occurrenceAfter.Succeeded &&
                object.ReferenceEquals(occurrenceBefore.Occurrence, _dynamicDeclaration.gameObject) &&
                object.ReferenceEquals(occurrenceAfter.Occurrence, _dynamicDeclaration.gameObject) &&
                authoringStillOwnsGrant &&
                object.ReferenceEquals(authoringGrant, dynamicGrant);
            if (!RecordCase(
                    "recognition-remains-read-only-and-independent",
                    queryPassed,
                    "before='" + beforeMutation.Status + "' after='" + afterFirstMutation.Status + "' sameRoot='" +
                    object.ReferenceEquals(beforeMutation.Occurrence, afterFirstMutation.Occurrence) +
                    "' occurrenceBefore='" + occurrenceBefore.Status + "' occurrenceAfter='" +
                    occurrenceAfter.Status +
                    "' authoringGrantUnchanged='" + authoringStillOwnsGrant + "'",
                    "Read-only recognition and occurrence queries resolve the same ID/root before and after mutation; write grant remains on exact authoring",
                    dynamicId,
                    "queryInterfaces='IObjectEntryRecognitionQuery,IObjectEntryOccurrenceQuery' queryWriteMethods='none' rootLive='true'",
                    afterFirstMutation.Diagnostic + " " + postMutationGrantIssue))
                yield break;

            if (activityRequest == null || activity == null ||
                !activityRequest.HasActivityRuntimeBinding)
            {
                Block("activity-request-bound", "The persistent Activity Request Trigger or Activity asset is unavailable.",
                    "Public Activity request trigger bound to GameFlow",
                    activityRequest == null ? "<missing-trigger>" : activityRequest.ActivityRuntimeBindingDiagnostic);
                yield break;
            }

            activityRequest.TargetActivity = activity;
            activityRequest.RequestActivity();
            deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            ObjectEntryOccurrenceQueryBinding activityBinding = null;
            Collider activityCollider = null;
            while (Time.realtimeSinceStartup < deadline)
            {
                Scene activityScene = SceneManager.GetSceneByName(ActivitySceneName);
                if (!activityRequest.IsRequestInFlight &&
                    activityRequest.LastRequestSucceeded &&
                    activityScene.IsValid() &&
                    activityScene.isLoaded &&
                    TryFindQueryBinding(activityScene, out activityBinding) &&
                    TryFindEntry(activityScene, ActivityEntryId,
                        out _activityOldDeclaration, out activityCollider) &&
                    IsResolved(activityBinding, activityCollider, ActivityEntryId,
                        _activityOldDeclaration.gameObject))
                    break;
                yield return null;
            }

            if (activityBinding == null || _activityOldDeclaration == null ||
                activityCollider == null || !activityRequest.LastRequestSucceeded)
            {
                Block("activity-admission", "The Activity did not admit its dynamic Object Entry.",
                    "Activity request succeeded and a new physical occurrence resolved",
                    activityRequest.LastMessage);
                yield break;
            }

            _activityOldRoot = _activityOldDeclaration.gameObject;
            _activityOldId = _activityOldDeclaration.ObjectEntryId;
            Scene firstActivityScene = SceneManager.GetSceneByName(ActivitySceneName);
            _firstActivitySceneHandle = firstActivityScene.handle;
            ObjectEntryCompositionAuthoring activityAuthoring =
                _activityOldDeclaration.GetComponent<ObjectEntryCompositionAuthoring>();
            string activityGrantIssue = string.Empty;
            if (activityAuthoring == null ||
                !activityAuthoring.TryGetCompositionAccessGrant(
                    out _activityOldGrant, out activityGrantIssue) ||
                !_activityOldGrant.TryOpenContribution(
                    10, out _activityOldContribution, out activityGrantIssue))
            {
                Block("activity-dynamic-grant", "The committed Activity occurrence did not expose a dynamic grant.",
                    "Grant and contribution opened from the exact Activity declaration",
                    activityGrantIssue);
                yield break;
            }

            yield return WaitForOperation(_activityOldContribution.SetPartAsync(
                Definition("part.activity", PartFixture.Activity)));
            if (_terminalStatus != null)
                yield break;
            _activityOldPart = FindActiveWitness(
                _activityOldRoot, Fixture(PartFixture.Activity))?.gameObject;
            if (_activityOldPart != null)
                _activityOldPartEntityId = _activityOldPart.GetEntityId();
            bool activityPartReady = _lastOperation.Status == ObjectEntryCompositionOperationStatus.Committed &&
                IsUnityObjectAlive(_activityOldPart) &&
                HasWitness(_activityOldRoot, Fixture(PartFixture.Activity)) &&
                HasExactRoot(_activityOldRoot, Fixture(PartFixture.Activity)) &&
                IdentityPreserved(_activityOldDeclaration, ActivityEntryId, _activityOldId);
            if (!RecordOperationCase(
                    "activity-dynamic-materialization",
                    activityPartReady,
                    _lastOperation,
                    "Committed Activity part is physically subordinate to its exact occurrence",
                    _activityOldId,
                    "owner='Activity' rootEntityId='" + _activityOldRoot.GetEntityId() +
                    "' partEntityId='" + (_activityOldPart != null ? _activityOldPart.GetEntityId() : EntityId.None) +
                    "' rootLive='true' declarationLive='true'"))
                yield break;

            PreserveRoot(_activityOldRoot);
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            if (activityRestart == null || !activityRestart.HasActivityRestartRuntimeBinding)
            {
                Block("activity-restart-bound", "The Activity Restart Trigger is unavailable or not bound.",
                    "Public Activity Restart trigger bound",
                    activityRestart == null ? "<missing-trigger>" : activityRestart.ActivityRestartRuntimeBindingDiagnostic);
                yield break;
            }

            activityRestart.TargetActivity = activity;
            activityRestart.RequestActivityRestart();
            deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (Time.realtimeSinceStartup < deadline &&
                   (activityRestart.IsRequestInFlight ||
                    !_unloadedSceneHandles.Contains(_firstActivitySceneHandle) ||
                    IsUnityObjectAlive(_activityOldPart)))
                yield return null;
            yield return null;

            QaIfAdr014C4PartWitness.DestructionEvidence[] activityRelease =
                QaIfAdr014C4PartWitness.GetDestructionEvidence(_activityOldId, _activityOldRoot);
            QaIfAdr014C4PartWitness.DestructionEvidence activityPartRelease =
                FindEvidence(activityRelease, _activityOldPart);
            bool activityCleanupPassed = activityRestart.LastRequestSucceeded &&
                !activityRestart.IsRequestInFlight &&
                _unloadedSceneHandles.Contains(_firstActivitySceneHandle) &&
                !IsUnityObjectAlive(_activityOldPart) &&
                activityPartRelease != null &&
                activityPartRelease.PartIsPhysicallyDestroyed &&
                activityPartRelease.OwnerSceneWasLoaded &&
                activityPartRelease.OwnerSceneHandle == _firstActivitySceneHandle &&
                activityPartRelease.RootWasLive &&
                activityPartRelease.DeclarationWasIntact &&
                IdentityPreserved(_activityOldDeclaration, ActivityEntryId, _activityOldId);
            if (!RecordCase(
                    "activity-release-before-scene-unload",
                    activityCleanupPassed,
                    "restart='" + activityRestart.LastOutcome + "' partDestroyed='" +
                    !IsUnityObjectAlive(_activityOldPart) + "' ownerSceneUnloaded='" +
                    _unloadedSceneHandles.Contains(_firstActivitySceneHandle) + "'",
                    "Part physically destroyed while its owner scene was loaded and root/declaration/ID remained intact",
                    _activityOldId,
                    "owner='Activity' rootLive='" + (_activityOldRoot != null) +
                    "' declarationLive='" + (_activityOldDeclaration != null) +
                    "' rootEntityId='" + (_activityOldRoot != null ? _activityOldRoot.GetEntityId() : EntityId.None) +
                    "' ownerSceneLoadedAtDestroy='" + (activityPartRelease != null && activityPartRelease.OwnerSceneWasLoaded) +
                    "' ownerSceneHandle='" + (activityPartRelease != null ? activityPartRelease.OwnerSceneHandle : SceneHandle.None) +
                    "' partEntityId='" + _activityOldPartEntityId +
                    "' physicalReleaseConfirmed='" + activityCleanupPassed +
                    "' releaseRequestSucceeded='" + activityRestart.LastRequestSucceeded + "'",
                    activityRestart.LastMessage))
                yield break;

            deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                Scene reenteredActivityScene = SceneManager.GetSceneByName(ActivitySceneName);
                if (reenteredActivityScene.IsValid() && reenteredActivityScene.isLoaded &&
                    TryFindEntry(reenteredActivityScene, ActivityEntryId,
                        out _activityCurrentDeclaration, out Collider currentCollider) &&
                    !object.ReferenceEquals(_activityCurrentDeclaration.gameObject, _activityOldRoot) &&
                    TryFindQueryBinding(reenteredActivityScene, out activityBinding) &&
                    IsResolved(activityBinding, currentCollider, ActivityEntryId,
                        _activityCurrentDeclaration.gameObject))
                {
                    _activityCurrentRoot = _activityCurrentDeclaration.gameObject;
                    _readmittedActivitySceneHandle = reenteredActivityScene.handle;
                    break;
                }
                yield return null;
            }

            if (_activityCurrentDeclaration == null || _activityCurrentRoot == null ||
                object.ReferenceEquals(_activityCurrentRoot, _activityOldRoot))
            {
                Block("activity-readmission", "Activity Restart did not admit a different physical root for the same logical entry.",
                    "Same ObjectEntryId with a different live GameObject",
                    "oldRootLive='" + (_activityOldRoot != null) + "'");
                yield break;
            }

            bool sameLogicalDifferentPhysical = _activityCurrentDeclaration.TryGetObjectEntryId(
                    out ObjectEntryId currentActivityId) &&
                currentActivityId == _activityOldId &&
                object.ReferenceEquals(_activityCurrentRoot, _activityCurrentDeclaration.gameObject) &&
                !object.ReferenceEquals(_activityCurrentRoot, _activityOldRoot);
            if (!sameLogicalDifferentPhysical)
            {
                Block("activity-readmission-identity", "Readmitted Activity did not preserve the logical ID while replacing its physical root.",
                    "Same ObjectEntryId, different physical occurrence",
                    "oldId='" + _activityOldId.StableText + "' newId='" + currentActivityId.StableText + "'");
                yield break;
            }

            string staleGrantIssue = string.Empty;
            bool oldGrantCanOpen = _activityOldGrant != null &&
                _activityOldGrant.TryOpenContribution(20, out _, out staleGrantIssue);
            yield return WaitForOperation(_activityOldContribution.SetPartAsync(
                Definition("part.stale-attempt", PartFixture.Simple)));
            if (_terminalStatus != null)
                yield break;
            ObjectEntryCompositionAuthoring currentAuthoring =
                _activityCurrentDeclaration.GetComponent<ObjectEntryCompositionAuthoring>();
            ObjectEntryCompositionAccessGrant currentGrant = null;
            ObjectEntryCompositionContribution currentContribution = null;
            string currentGrantIssue = string.Empty;
            bool staleBlocked = !oldGrantCanOpen &&
                _lastOperation.Status == ObjectEntryCompositionOperationStatus.Stale &&
                !_lastOperation.EffectsMayHaveApplied &&
                FindWitness(_activityCurrentRoot, Fixture(PartFixture.Simple)) == null;
            if (!RecordCase(
                    "readmission-stales-old-grant-and-contribution",
                    staleBlocked,
                    "oldGrantOpened='" + oldGrantCanOpen + "' oldHandle='" + _lastOperation.Status + "'",
                    "Old grant cannot open a handle; old contribution returns Stale and cannot touch the new root",
                    _activityOldId,
                    "owner='Activity' oldRootRetained='" + (_activityOldRoot != null) +
                    "' oldRootEntityId='" + (_activityOldRoot != null ? _activityOldRoot.GetEntityId() : EntityId.None) +
                    "' newRootEntityId='" + _activityCurrentRoot.GetEntityId() +
                    "' newRootDifferent='true' newRootUnaffected='true'",
                    staleGrantIssue + " " + _lastOperation.Diagnostic))
                yield break;

            if (currentAuthoring == null ||
                !currentAuthoring.TryGetCompositionAccessGrant(
                    out currentGrant, out currentGrantIssue) ||
                !currentGrant.TryOpenContribution(
                    10, out currentContribution, out currentGrantIssue))
            {
                Block("readmitted-grant-ready", "The new Activity occurrence did not publish a usable exact grant.",
                    "New grant can open a contribution for the new occurrence",
                    currentGrantIssue);
                yield break;
            }

            yield return WaitForOperation(currentContribution.SetPartAsync(
                Definition("part.activity-current", PartFixture.Activity)));
            if (_terminalStatus != null)
                yield break;
            GameObject currentActivityPart = FindActiveWitness(
                _activityCurrentRoot, Fixture(PartFixture.Activity))?.gameObject;
            bool currentGrantWorks = _lastOperation.Status == ObjectEntryCompositionOperationStatus.Committed &&
                IsUnityObjectAlive(currentActivityPart) &&
                HasWitness(_activityCurrentRoot, Fixture(PartFixture.Activity)) &&
                HasExactRoot(_activityCurrentRoot, Fixture(PartFixture.Activity)) &&
                IdentityPreserved(_activityCurrentDeclaration, ActivityEntryId, _activityOldId) &&
                GetWitnesses(_activityOldRoot).Length == 0;
            if (!RecordOperationCase(
                    "readmitted-grant-mutates-new-occurrence",
                    currentGrantWorks,
                    _lastOperation,
                    "New grant materializes only under the new physical root with the same ObjectEntryId",
                    _activityOldId,
                    "owner='Activity' oldRootEntityId='" + _activityOldRoot.GetEntityId() +
                    "' newRootEntityId='" + _activityCurrentRoot.GetEntityId() +
                    "' partEntityId='" + (currentActivityPart != null ? currentActivityPart.GetEntityId() : EntityId.None) +
                    "' oldRootLive='" + (_activityOldRoot != null) +
                    "' newRootLive='" + (_activityCurrentRoot != null) + "'"))
                yield break;

            Scene currentActivityScene = SceneManager.GetSceneByName(ActivitySceneName);
            Scene currentRouteScene = SceneManager.GetSceneByName(RouteSceneName);
            if (!currentActivityScene.IsValid() || !currentRouteScene.IsValid() ||
                !currentActivityScene.isLoaded || !currentRouteScene.isLoaded)
            {
                Block("route-replacement-scenes-ready", "The current Route or Activity owner scene is not loaded before Route replacement.",
                    "Both current owner scenes loaded before replacement",
                    "routeLoaded='" + currentRouteScene.isLoaded + "' activityLoaded='" + currentActivityScene.isLoaded + "'");
                yield break;
            }

            _readmittedActivitySceneHandle = currentActivityScene.handle;
            _routeSceneHandle = currentRouteScene.handle;
            GameObject[] routeRoots =
            {
                _plainDeclaration.gameObject,
                _profileDeclaration.gameObject,
                _dynamicDeclaration.gameObject,
                _baseDynamicDeclaration.gameObject
            };
            var expectedRouteParts = new List<ExpectedPart>();
            var routeIds = new[]
            {
                ObjectEntryId.From(PlainEntryId),
                ObjectEntryId.From(ProfileEntryId),
                ObjectEntryId.From(DynamicEntryId),
                ObjectEntryId.From(BaseDynamicEntryId)
            };
            for (int rootIndex = 0; rootIndex < routeRoots.Length; rootIndex++)
            {
                GameObject root = routeRoots[rootIndex];
                foreach (QaIfAdr014C4PartWitness witness in GetWitnesses(root))
                    expectedRouteParts.Add(new ExpectedPart(root, routeIds[rootIndex], witness.gameObject));
                PreserveRoot(root);
            }
            var expectedActivityParts = new List<ExpectedPart>();
            foreach (QaIfAdr014C4PartWitness witness in GetWitnesses(_activityCurrentRoot))
                expectedActivityParts.Add(new ExpectedPart(
                    _activityCurrentRoot, _activityOldId, witness.gameObject));
            PreserveRoot(_activityCurrentRoot);

            bool releaseSnapshotReady = expectedRouteParts.Count > 0 &&
                expectedActivityParts.Count > 0 &&
                CountPartsForIdentity(expectedRouteParts, ObjectEntryId.From(ProfileEntryId)) > 0 &&
                CountPartsForIdentity(expectedRouteParts, dynamicId) > 0 &&
                CountPartsForIdentity(expectedRouteParts, ObjectEntryId.From(BaseDynamicEntryId)) > 0 &&
                AreWitnessesActive(_profileDeclaration.gameObject) &&
                AreWitnessesActive(_dynamicDeclaration.gameObject) &&
                AreWitnessesActive(_baseDynamicDeclaration.gameObject) &&
                AreWitnessesActive(_activityCurrentRoot) &&
                HasWitness(_profileDeclaration.gameObject, Fixture(PartFixture.Base)) &&
                HasWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.IndependentB)) &&
                HasWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.PriorityHigh)) &&
                HasWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.ConflictFirst)) &&
                HasWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.HiddenTieWinner)) &&
                HasWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.DependencyPrerequisite)) &&
                HasWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.Dependent)) &&
                HasWitness(_baseDynamicDeclaration.gameObject, Fixture(PartFixture.Base)) &&
                HasWitness(_activityCurrentRoot, Fixture(PartFixture.Activity)) &&
                EveryExpectedPartHasOwner(expectedRouteParts, _routeSceneHandle) &&
                EveryExpectedPartHasOwner(expectedActivityParts, _readmittedActivitySceneHandle);
            if (!RecordCase(
                    "route-release-snapshot-ready",
                    releaseSnapshotReady,
                    "routeParts='" + expectedRouteParts.Count + "' activityParts='" + expectedActivityParts.Count +
                    "' allExpectedPartsActive='" + releaseSnapshotReady + "'",
                    "Active physical parts exist on the Profile, Dynamic, Base+Dynamic and Activity roots before Route replacement; each snapshot records its exact loaded owner scene",
                    dynamicId,
                    "profileRootEntityId='" + _profileDeclaration.gameObject.GetEntityId() +
                    "' dynamicRootEntityId='" + _dynamicDeclaration.gameObject.GetEntityId() +
                    "' baseDynamicRootEntityId='" + _baseDynamicDeclaration.gameObject.GetEntityId() +
                    "' activityRootEntityId='" + _activityCurrentRoot.GetEntityId() +
                    "' routeOwnerScene='" + _routeSceneHandle +
                    "' activityOwnerScene='" + _readmittedActivitySceneHandle + "'",
                    releaseSnapshotReady ? string.Empty :
                        "The final lifecycle transition has no complete physical part snapshot to prove release."))
                yield break;

            if (routeRequest == null || replacementRoute == null ||
                !routeRequest.HasRouteRuntimeBinding)
            {
                Block("route-replacement-bound", "The persistent Route Request Trigger or replacement Route is unavailable.",
                    "Public Route Request Trigger bound to GameFlow",
                    routeRequest == null ? "<missing-trigger>" : routeRequest.RouteRuntimeBindingDiagnostic);
                yield break;
            }

            routeRequest.TargetRoute = replacementRoute;
            routeRequest.RequestRoute();
            deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (Time.realtimeSinceStartup < deadline &&
                   (routeRequest.IsRequestInFlight ||
                    !_unloadedSceneHandles.Contains(_routeSceneHandle) ||
                    !_unloadedSceneHandles.Contains(_readmittedActivitySceneHandle)))
                yield return null;
            yield return null;

            bool allRoutePartsReleased = ArePhysicallyDestroyed(expectedRouteParts);
            bool activityPartsReleased = ArePhysicallyDestroyed(expectedActivityParts);
            bool routeAndActivityReleased = routeRequest.LastRequestSucceeded &&
                !routeRequest.IsRequestInFlight &&
                _unloadedSceneHandles.Contains(_routeSceneHandle) &&
                _unloadedSceneHandles.Contains(_readmittedActivitySceneHandle) &&
                allRoutePartsReleased &&
                activityPartsReleased &&
                RetainedRootsPreserveIdentity(routeRoots, routeIds) &&
                IdentityPreserved(_activityCurrentDeclaration, ActivityEntryId, _activityOldId);

            if (routeAndActivityReleased && dynamicGrant.TryOpenContribution(99, out _, out _))
                routeAndActivityReleased = false;

            yield return WaitForOperation(simple.SetPartAsync(
                Definition("part.route-stale-attempt", PartFixture.Simple)));
            if (_terminalStatus != null)
                yield break;
            bool routeStale = _lastOperation.Status == ObjectEntryCompositionOperationStatus.Stale &&
                !_lastOperation.EffectsMayHaveApplied &&
                FindWitness(_dynamicDeclaration.gameObject, Fixture(PartFixture.Simple)) == null;
            _routeReplacementCompleted = routeRequest.LastRequestSucceeded &&
                _unloadedSceneHandles.Contains(_routeSceneHandle);
            routeAndActivityReleased &= routeStale;
            bool finalPass = routeAndActivityReleased;
            _ownerCleanupConfirmed = routeAndActivityReleased;
            if (!RecordCase(
                    "route-replacement-releases-before-owner-unload",
                    finalPass,
                    "routeRequest='" + routeRequest.LastOutcome + "' routeSceneUnloaded='" +
                    _unloadedSceneHandles.Contains(_routeSceneHandle) + "' activitySceneUnloaded='" +
                    _unloadedSceneHandles.Contains(_readmittedActivitySceneHandle) +
                    "' routePartsDestroyed='" + allRoutePartsReleased + "' activityPartsDestroyed='" +
                    activityPartsReleased + "' staleMutation='" + _lastOperation.Status + "'",
                    "Route replacement drains exact contributions; all acquired parts are physically destroyed while each owner scene is loaded; old handle is Stale",
                    dynamicId,
                    "routeOwner='Route' routeRootLive='" + (_dynamicDeclaration.gameObject != null) +
                    "' activityOwner='Activity' activityRootLive='" + (_activityCurrentRoot != null) +
                    "' routeReleaseEvidence='" + Safe(FormatReleaseEvidence(expectedRouteParts)) +
                    "' activityReleaseEvidence='" + Safe(FormatReleaseEvidence(expectedActivityParts)) +
                    "' physicalReleaseConfirmed='" + routeAndActivityReleased +
                    "' releaseRequestSucceeded='" + routeRequest.LastRequestSucceeded + "'",
                    routeRequest.LastMessage + " " + _lastOperation.Diagnostic))
                yield break;
        }

        private IEnumerator WaitForOperation(Task<ObjectEntryCompositionOperationResult> task)
        {
            float deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (!task.IsCompleted && Time.realtimeSinceStartup < deadline)
                yield return null;

            if (!task.IsCompleted)
            {
                Block("composition-operation-completion",
                    "The operation exceeded the scenario's deadlock guard; elapsed time is not treated as cleanup evidence.",
                    "Public operation task reached a terminal state",
                    "operationPending='true'");
                yield break;
            }

            if (task.IsCanceled)
            {
                const string diagnostic = "A C4 composition operation was cancelled unexpectedly.";
                SetTerminal("Failed", diagnostic);
                Debug.LogError("[IF-ADR-014-C4] case='composition-operation-cancelled' observed='Cancelled'" +
                    " expected='Completed' pass='false' objectEntryId='<pending>' evidence='cleanup-required'" +
                    " diagnostic='" + Safe(diagnostic) + "'");
                yield break;
            }
            if (task.IsFaulted)
            {
                string diagnostic = task.Exception.GetBaseException().ToString();
                SetTerminal("Failed", "A C4 composition operation faulted: " + diagnostic);
                Debug.LogError("[IF-ADR-014-C4] case='composition-operation-faulted' observed='Faulted'" +
                    " expected='Completed' pass='false' objectEntryId='<pending>' evidence='cleanup-required'" +
                    " diagnostic='" + Safe(diagnostic) + "'");
                yield break;
            }

            _lastOperation = task.Result;
        }

        private IEnumerator RequestOwnerCleanup()
        {
            if (routeRequest == null || replacementRoute == null ||
                !routeRequest.HasRouteRuntimeBinding || _routeReplacementCompleted)
                yield break;

            CaptureActiveOwnedPartsForCleanup();
            if (!routeRequest.IsRequestInFlight)
            {
                routeRequest.TargetRoute = replacementRoute;
                routeRequest.RequestRoute();
            }

            float deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (routeRequest.IsRequestInFlight &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;

            _routeReplacementCompleted = routeRequest.LastRequestSucceeded;
            yield return null;
            _ownerCleanupConfirmed = _routeReplacementCompleted &&
                ArePhysicallyDestroyed(_cleanupExpectedParts);
            if (!_routeReplacementCompleted)
                Debug.LogWarning("[IF-ADR-014-C4] cleanup='OwnerReleaseUnconfirmed' routeOutcome='" +
                    routeRequest.LastOutcome + "' diagnostic='" + Safe(routeRequest.LastMessage) + "'");
            else if (!_ownerCleanupConfirmed)
                Debug.LogWarning("[IF-ADR-014-C4] cleanup='PhysicalReleaseUnconfirmed' expectedParts='" +
                    _cleanupExpectedParts.Count + "' diagnostic='Retained Object Entry roots were preserved for inspection.'");
        }

        private IEnumerator DestroyRetainedRoots()
        {
            if (!_ownerCleanupConfirmed)
            {
                if (_retainedRoots.Count > 0)
                    Debug.LogWarning("[IF-ADR-014-C4] cleanup='RetainedRootsPreserved' reason='Owner release was not confirmed.' count='" +
                        _retainedRoots.Count + "'");
                yield break;
            }

            foreach (GameObject root in _retainedRoots)
            {
                if (root != null)
                    Destroy(root);
            }
            yield return null;

            int remaining = 0;
            foreach (GameObject root in _retainedRoots)
            {
                if (root != null)
                    remaining++;
            }

            Debug.Log("[IF-ADR-014-C4] cleanup='RetainedRootsDestroyed' remaining='" + remaining + "'");
            if (remaining != 0)
                SetTerminal("Failed", "One or more retained Object Entry roots survived confirmed cleanup.");
        }

        private void CaptureActiveOwnedPartsForCleanup()
        {
            _cleanupExpectedParts.Clear();
            var candidates = new List<QaIfAdr014C4PartWitness>();
            foreach (GameObject retainedRoot in _retainedRoots)
            {
                if (retainedRoot != null)
                    candidates.AddRange(retainedRoot.GetComponentsInChildren<QaIfAdr014C4PartWitness>(true));
            }

            for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                Scene scene = SceneManager.GetSceneAt(sceneIndex);
                if (!scene.IsValid() || !scene.isLoaded)
                    continue;
                foreach (GameObject root in scene.GetRootGameObjects())
                    candidates.AddRange(root.GetComponentsInChildren<QaIfAdr014C4PartWitness>(true));
            }

            var seenParts = new HashSet<GameObject>();
            foreach (QaIfAdr014C4PartWitness witness in candidates)
            {
                if (witness == null || witness.gameObject == null ||
                    !seenParts.Add(witness.gameObject))
                    continue;
                ObjectEntryDeclaration declaration =
                    witness.GetComponentInParent<ObjectEntryDeclaration>(true);
                if (declaration == null ||
                    !declaration.TryGetObjectEntryId(out ObjectEntryId objectEntryId))
                    continue;

                GameObject root = declaration.gameObject;
                _cleanupExpectedParts.Add(
                    new ExpectedPart(root, objectEntryId, witness.gameObject,
                        witness.OwnerScene, witness.OwnerSceneWasLoadedAtEnable));
                PreserveRoot(root);
            }
        }

        private void OnSceneUnloaded(Scene scene)
        {
            _unloadedSceneHandles.Add(scene.handle);
        }

        private void PreserveRoot(GameObject root)
        {
            if (root == null || _retainedRoots.Contains(root))
                return;
            DontDestroyOnLoad(root);
            _retainedRoots.Add(root);
        }

        private bool RecordOperationCase(
            string caseId,
            bool passed,
            ObjectEntryCompositionOperationResult result,
            string expected,
            ObjectEntryId objectEntryId,
            string evidence)
        {
            return RecordCase(
                caseId,
                passed,
                result.Status + ";effectsMayHaveApplied='" + result.EffectsMayHaveApplied +
                "';revision='" + result.Revision + "';changedKeys='" + string.Join(",", result.ChangedKeys) + "'",
                expected,
                objectEntryId,
                evidence,
                result.Diagnostic);
        }

        private bool RecordCase(
            string caseId,
            bool passed,
            string observed,
            string expected,
            ObjectEntryId objectEntryId,
            string evidence,
            string diagnostic)
        {
            _executedCases++;
            if (passed)
                _passedCases++;
            else
                SetTerminal("Failed", "Case '" + caseId + "' failed. " + diagnostic);

            Debug.Log("[IF-ADR-014-C4] case='" + Safe(caseId) +
                "' observed='" + Safe(observed) +
                "' expected='" + Safe(expected) +
                "' pass='" + passed +
                "' objectEntryId='" + (objectEntryId.IsValid ? Safe(objectEntryId.StableText) : "<invalid>") +
                "' evidence='" + Safe(evidence) +
                "' diagnostic='" + Safe(diagnostic) + "'");
            return passed;
        }

        private void Block(
            string caseId,
            string diagnostic,
            string expected,
            string evidence)
        {
            SetTerminal("Blocked", diagnostic);
            Debug.LogWarning("[IF-ADR-014-C4] case='" + Safe(caseId) +
                "' status='Blocked' expected='" + Safe(expected) +
                "' objectEntryId='<unavailable>' evidence='" + Safe(evidence) +
                "' diagnostic='" + Safe(diagnostic) + "'");
        }

        private void SetTerminal(string status, string diagnostic)
        {
            if (_terminalStatus != null)
                return;
            _terminalStatus = status;
            _terminalDiagnostic = diagnostic ?? string.Empty;
        }

        private void EmitTerminal()
        {
            if (_finished)
                return;
            _finished = true;
            string line = "[IF-ADR-014-C4] status='" + _terminalStatus +
                "' cases='" + _passedCases + "/" + ExpectedCases +
                "' executed='" + _executedCases +
                "' diagnostic='" + Safe(_terminalDiagnostic) + "'";
            if (_terminalStatus == "Passed")
                Debug.Log(line);
            else if (_terminalStatus == "Blocked")
                Debug.LogWarning(line);
            else
                Debug.LogError(line);

            Debug.Log("[IF-ADR-014-C4] c3-evidence='Framework Edit Mode source coverage exists; Controlled Stop requires Project Owner Unity execution; abrupt teardown cleanup remains explicitly unconfirmed and is not asserted by this Play Mode scenario.'");
        }

        private bool TryFindQueryBinding(Scene scene, out ObjectEntryOccurrenceQueryBinding binding)
        {
            binding = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                ObjectEntryOccurrenceQueryBinding candidate =
                    root.GetComponentInChildren<ObjectEntryOccurrenceQueryBinding>(true);
                if (candidate == null)
                    continue;
                if (binding != null)
                    return false;
                binding = candidate;
            }
            return binding != null;
        }

        private bool TryFindEntry(
            Scene scene,
            string stableId,
            out ObjectEntryDeclaration declaration,
            out Collider collider)
        {
            declaration = null;
            collider = null;
            ObjectEntryId expectedId = ObjectEntryId.From(stableId);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                ObjectEntryDeclaration[] declarations =
                    root.GetComponentsInChildren<ObjectEntryDeclaration>(true);
                foreach (ObjectEntryDeclaration candidate in declarations)
                {
                    if (!candidate.TryGetObjectEntryId(out ObjectEntryId currentId) ||
                        currentId != expectedId)
                        continue;
                    if (declaration != null)
                        return false;
                    declaration = candidate;
                    Collider[] colliders = candidate.GetComponents<Collider>();
                    if (colliders.Length > 0)
                        collider = colliders[0];
                    else
                    {
                        Collider[] children = candidate.GetComponentsInChildren<Collider>(true);
                        if (children.Length > 0)
                            collider = children[0];
                    }
                }
            }
            return declaration != null;
        }

        private bool IsResolved(
            IObjectEntryRecognitionQuery query,
            Collider collider,
            string stableId,
            GameObject expectedRoot)
        {
            if (query == null || collider == null ||
                !ObjectEntryId.From(stableId).IsValid)
                return false;
            return IsResolved(query.Recognize(collider), stableId, expectedRoot);
        }

        private bool IsResolved(
            ObjectEntryRecognitionResult result,
            string stableId,
            GameObject expectedRoot)
        {
            if (!ObjectEntryId.From(stableId).IsValid)
                return false;
            return result.Status == ObjectEntryRecognitionStatus.Resolved &&
                result.Succeeded &&
                result.ObjectEntryId == ObjectEntryId.From(stableId) &&
                object.ReferenceEquals(result.Occurrence, expectedRoot);
        }

        private bool IdentityPreserved(
            ObjectEntryDeclaration declaration,
            string expectedId,
            ObjectEntryId expectedValue)
        {
            return declaration != null &&
                declaration.TryGetObjectEntryId(out ObjectEntryId current) &&
                current == expectedValue &&
                current == ObjectEntryId.From(expectedId);
        }

        private bool HasWitness(GameObject root, GameObject prefab) =>
            FindActiveWitness(root, prefab) != null;

        private bool HasExactRoot(GameObject root, GameObject prefab)
        {
            QaIfAdr014C4PartWitness witness = FindActiveWitness(root, prefab);
            if (witness == null)
                return false;
            ObjectEntryDeclaration declaration =
                witness.GetComponentInParent<ObjectEntryDeclaration>(true);
            return declaration != null && object.ReferenceEquals(declaration.gameObject, root);
        }

        private QaIfAdr014C4PartWitness FindWitness(GameObject root, GameObject prefab)
        {
            QaIfAdr014C4PartWitness prefabWitness = prefab != null
                ? prefab.GetComponent<QaIfAdr014C4PartWitness>()
                : null;
            if (root == null || prefabWitness == null)
                return null;
            string prefabId = prefabWitness.PrefabId;
            foreach (QaIfAdr014C4PartWitness witness in
                     root.GetComponentsInChildren<QaIfAdr014C4PartWitness>(true))
            {
                if (string.Equals(witness.PrefabId, prefabId, StringComparison.Ordinal))
                    return witness;
            }
            return null;
        }

        private QaIfAdr014C4PartWitness FindActiveWitness(GameObject root, GameObject prefab)
        {
            QaIfAdr014C4PartWitness prefabWitness = prefab != null
                ? prefab.GetComponent<QaIfAdr014C4PartWitness>()
                : null;
            if (root == null || prefabWitness == null)
                return null;
            string prefabId = prefabWitness.PrefabId;
            foreach (QaIfAdr014C4PartWitness witness in
                     root.GetComponentsInChildren<QaIfAdr014C4PartWitness>(true))
            {
                if (witness.isActiveAndEnabled && witness.gameObject.activeInHierarchy &&
                    string.Equals(witness.PrefabId, prefabId, StringComparison.Ordinal))
                    return witness;
            }
            return null;
        }

        private QaIfAdr014C4PartWitness[] GetWitnesses(GameObject root)
        {
            if (root == null)
                return Array.Empty<QaIfAdr014C4PartWitness>();
            QaIfAdr014C4PartWitness[] all =
                root.GetComponentsInChildren<QaIfAdr014C4PartWitness>(true);
            var parts = new List<QaIfAdr014C4PartWitness>();
            foreach (QaIfAdr014C4PartWitness witness in all)
            {
                if (!object.ReferenceEquals(witness.gameObject, root))
                    parts.Add(witness);
            }
            return parts.ToArray();
        }

        private bool ArePhysicallyDestroyed(IReadOnlyList<ExpectedPart> parts)
        {
            if (parts == null || parts.Count == 0)
                return false;

            foreach (ExpectedPart expected in parts)
            {
                if (IsUnityObjectAlive(expected.Part))
                    return false;

                bool found = false;
                foreach (QaIfAdr014C4PartWitness.DestructionEvidence evidence in
                         QaIfAdr014C4PartWitness.GetDestructionEvidence(
                             expected.ObjectEntryId, expected.Root))
                {
                    if (object.ReferenceEquals(evidence.Part, expected.Part) &&
                        evidence.PartIsPhysicallyDestroyed &&
                        expected.OwnerSceneWasLoaded &&
                        evidence.OwnerSceneWasLoaded &&
                        evidence.OwnerSceneHandle == expected.OwnerSceneHandle &&
                        evidence.RootWasLive &&
                        evidence.DeclarationWasIntact)
                        found = true;
                }
                if (!found)
                    return false;
            }
            return true;
        }

        private string FormatReleaseEvidence(IReadOnlyList<ExpectedPart> parts)
        {
            var entries = new List<string>();
            foreach (ExpectedPart expected in parts)
            {
                QaIfAdr014C4PartWitness.DestructionEvidence evidence = FindEvidence(
                    QaIfAdr014C4PartWitness.GetDestructionEvidence(
                        expected.ObjectEntryId, expected.Root),
                    expected.Part);
                entries.Add(
                    "id=" + expected.ObjectEntryId.StableText +
                    ",rootEntity=" + expected.RootEntityId +
                    ",partEntity=" + expected.PartEntityId +
                    ",prefab=" + (evidence != null ? evidence.PrefabId : "<missing>") +
                    ",destroyed=" + (evidence != null && evidence.PartIsPhysicallyDestroyed) +
                    ",ownerScene=" + expected.OwnerSceneHandle +
                    ",sceneLoadedAtDestroy=" + (evidence != null && evidence.OwnerSceneWasLoaded) +
                    ",destroyedOwnerScene=" + (evidence != null ? evidence.OwnerSceneHandle : SceneHandle.None) +
                    ",rootLive=" + (evidence != null && evidence.RootWasLive) +
                    ",declarationIntact=" + (evidence != null && evidence.DeclarationWasIntact));
            }
            return string.Join(";", entries);
        }

        private bool RetainedRootsPreserveIdentity(GameObject[] roots, ObjectEntryId[] identities)
        {
            if (roots.Length != identities.Length)
                return false;
            for (int index = 0; index < roots.Length; index++)
            {
                if (roots[index] == null ||
                    roots[index].GetComponent<ObjectEntryDeclaration>() == null ||
                    !roots[index].GetComponent<ObjectEntryDeclaration>()
                        .TryGetObjectEntryId(out ObjectEntryId actual) ||
                    actual != identities[index])
                    return false;
            }
            return true;
        }

        private bool AreWitnessesActive(GameObject root)
        {
            QaIfAdr014C4PartWitness[] witnesses = GetWitnesses(root);
            if (witnesses.Length == 0)
                return false;
            foreach (QaIfAdr014C4PartWitness witness in witnesses)
            {
                if (witness == null || !witness.isActiveAndEnabled ||
                    !witness.gameObject.activeInHierarchy)
                    return false;
            }
            return true;
        }

        private int CountPartsForIdentity(
            IReadOnlyList<ExpectedPart> parts,
            ObjectEntryId objectEntryId)
        {
            int count = 0;
            foreach (ExpectedPart part in parts)
            {
                if (part.ObjectEntryId == objectEntryId)
                    count++;
            }
            return count;
        }

        private bool EveryExpectedPartHasOwner(
            IReadOnlyList<ExpectedPart> parts,
            SceneHandle ownerSceneHandle)
        {
            if (parts == null || parts.Count == 0)
                return false;
            foreach (ExpectedPart part in parts)
            {
                if (!part.OwnerSceneWasLoaded || part.OwnerSceneHandle != ownerSceneHandle)
                    return false;
            }
            return true;
        }

        private QaIfAdr014C4PartWitness.DestructionEvidence FindEvidence(
            QaIfAdr014C4PartWitness.DestructionEvidence[] evidence,
            GameObject part)
        {
            foreach (QaIfAdr014C4PartWitness.DestructionEvidence candidate in evidence)
            {
                if (object.ReferenceEquals(candidate.Part, part))
                    return candidate;
            }
            return null;
        }

        private ObjectEntryCompositionPartDefinition Definition(
            string key,
            PartFixture fixture,
            params string[] dependencies)
        {
            return new ObjectEntryCompositionPartDefinition(
                key, Fixture(fixture), dependencies);
        }

        private GameObject Fixture(PartFixture fixture)
        {
            int index = (int)fixture;
            return partPrefabs != null && index < partPrefabs.Length
                ? partPrefabs[index]
                : null;
        }

        private static bool IsUnityObjectAlive(UnityEngine.Object value) => value != null;

        private static string IdText(ObjectEntryDeclaration declaration) =>
            declaration != null && declaration.TryGetObjectEntryId(out ObjectEntryId id)
                ? id.StableText
                : "<invalid>";

        private static string Safe(string value) =>
            (value ?? string.Empty).Replace("'", "\"").Replace("\r", " ").Replace("\n", " ");
    }

}
