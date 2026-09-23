using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.GameFlow;
using Immersive.Framework.Transition;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    internal enum QaTemporaryActivityCleanupKind
    {
        Unknown = 0,
        TargetCleared = 1,
        InitialAuthorityPreserved = 2,
        AlreadyClear = 3
    }

    internal sealed class QaActivityEntryReadinessFixture : IAsyncDisposable
    {
        internal const string ParticipantId = "qa.if-ready-04.foundation.required";
        private const string RootName = "QA_IF_READY_04_Foundation";

        private readonly GameObject _root;
        private readonly Scene _primaryScene;
        private readonly List<string> _eventOrder = new List<string>();
        private readonly UnityAction _participantPreparationStarted;
        private readonly UnityAction _participantPreparationReleased;
        private readonly UnityAction _readinessPreparing;
        private readonly UnityAction _readinessReady;
        private readonly UnityAction _readinessNotReady;
        private bool _listenersRemoved;
        private bool _readinessSurfaceDestroyed;
        private bool _initialAuthorityRestored;
        private bool _targetActivityWasCreated;
        private bool _targetActivityDestructionConfirmed;
        private bool _targetContentProfileWasCreated;
        private bool _targetContentProfileDestructionConfirmed;
        private bool _targetContentSceneReleaseConfirmed;
        private bool _finalParticipantEvidenceCaptured;
        private int _expectedParticipantPreparationCycles = 1;

        private QaActivityEntryReadinessFixture(
            FrameworkRuntimeHost runtimeHost,
            IRouteRuntimePort routes,
            IActivityRuntimePort activities,
            RouteAsset initialRoute,
            ActivityAsset initialActivity,
            Scene primaryScene,
            GameObject root,
            ActivityReadinessParticipant participant,
            ActivityReadinessEvents events)
        {
            RuntimeHost = runtimeHost;
            Routes = routes;
            Activities = activities;
            InitialRoute = initialRoute;
            InitialActivity = initialActivity;
            _primaryScene = primaryScene;
            _root = root;
            Participant = participant;
            Events = events;

            PreparationStarted = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            ReadinessReady = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            _participantPreparationStarted = () =>
            {
                PreparationStartedCount++;
                _eventOrder.Add("participant-preparation-started");
                PreparationStarted.TrySetResult(true);
            };
            _participantPreparationReleased = () =>
            {
                PreparationReleasedCount++;
                _eventOrder.Add("participant-preparation-released");
            };
            _readinessPreparing = () =>
            {
                ReadinessPreparingCount++;
                _eventOrder.Add("readiness-preparing");
            };
            _readinessReady = () =>
            {
                ReadinessReadyCount++;
                _eventOrder.Add("readiness-ready");
                ReadinessReady.TrySetResult(true);
            };
            _readinessNotReady = () =>
            {
                ReadinessNotReadyCount++;
                _eventOrder.Add("readiness-not-ready");
            };

            Participant.PreparationStarted.AddListener(_participantPreparationStarted);
            Participant.PreparationReleased.AddListener(_participantPreparationReleased);
            Events.Preparing.AddListener(_readinessPreparing);
            Events.Ready.AddListener(_readinessReady);
            Events.NotReady.AddListener(_readinessNotReady);
        }

        internal FrameworkRuntimeHost RuntimeHost { get; }
        internal IRouteRuntimePort Routes { get; }
        internal IActivityRuntimePort Activities { get; }
        internal RouteAsset InitialRoute { get; }
        internal ActivityAsset InitialActivity { get; }
        internal ActivityReadinessParticipant Participant { get; }
        internal ActivityReadinessEvents Events { get; }
        internal TaskCompletionSource<bool> PreparationStarted { get; }
        internal TaskCompletionSource<bool> ReadinessReady { get; }
        internal IReadOnlyList<string> EventOrder => _eventOrder;
        internal int PreparationStartedCount { get; private set; }
        internal int PreparationReleasedCount { get; private set; }
        internal int ReadinessPreparingCount { get; private set; }
        internal int ReadinessReadyCount { get; private set; }
        internal int ReadinessNotReadyCount { get; private set; }
        internal ActivityAsset TargetActivity { get; private set; }
        internal ActivityContentProfileAsset TargetContentProfile { get; private set; }
        internal string TargetContentScenePath { get; private set; }
        internal int FinalPreparationStartedCount { get; private set; }
        internal int FinalPreparationReleasedCount { get; private set; }
        internal int FinalOccurrence { get; private set; }
        internal bool ReadinessSurfaceDestroyed => _readinessSurfaceDestroyed;
        internal QaTemporaryActivityCleanupKind CleanupKind { get; private set; }
        internal bool TargetActivityWasCreated => _targetActivityWasCreated;
        internal bool TargetActivityDestructionConfirmed =>
            _targetActivityDestructionConfirmed;
        internal bool TargetContentProfileWasCreated => _targetContentProfileWasCreated;
        internal bool TargetContentProfileDestructionConfirmed =>
            _targetContentProfileDestructionConfirmed;
        internal bool TargetContentSceneReleaseConfirmed =>
            _targetContentSceneReleaseConfirmed;

        internal void ExpectParticipantPreparationCycles(int expectedCycles)
        {
            Require(expectedCycles > 0,
                "Expected participant preparation cycle count must be greater than zero.");
            Require(PreparationStartedCount == 0 &&
                PreparationReleasedCount == 0 &&
                Participant.Occurrence == 0,
                "Expected participant preparation cycles must be configured before readiness starts.");

            _expectedParticipantPreparationCycles = expectedCycles;
        }

        internal static Task<QaActivityEntryReadinessFixture> CreateAsync()
        {
            Require(QaH2FrameworkReadiness.TryResolveUniqueHost(
                    out FrameworkRuntimeHost host, out string diagnostic),
                diagnostic);
            Require(host.State.GameFlowStarted,
                "Activity entry readiness fixture requires Game Flow to be started.");
            Require(host.State.CurrentRoute != null,
                "Activity entry readiness fixture requires a current Route.");

            IRouteRuntimePort routes = host as IRouteRuntimePort;
            IActivityRuntimePort activities = host as IActivityRuntimePort;
            Require(routes != null,
                "FrameworkRuntimeHost does not expose IRouteRuntimePort.");
            Require(activities != null,
                "FrameworkRuntimeHost does not expose IActivityRuntimePort.");

            RouteAsset initialRoute = host.State.CurrentRoute;
            Scene primaryScene = ResolveLoadedPrimaryScene(initialRoute);
            RequireNoConflictingParticipant(primaryScene);

            var root = new GameObject(RootName);
            try
            {
                SceneManager.MoveGameObjectToScene(root, primaryScene);
                ActivityReadinessParticipant participant =
                    root.AddComponent<ActivityReadinessParticipant>();
                ActivityReadinessEvents events = root.AddComponent<ActivityReadinessEvents>();
                ConfigureParticipant(participant);

                return Task.FromResult(new QaActivityEntryReadinessFixture(
                    host,
                    routes,
                    activities,
                    initialRoute,
                    host.State.CurrentActivity,
                    primaryScene,
                    root,
                    participant,
                    events));
            }
            catch
            {
                UnityEngine.Object.Destroy(root);
                throw;
            }
        }

        internal ActivityAsset CreateActivity(
            string activityId,
            string activityName,
            ActivityEntryReadinessPolicy policy)
        {
            Require(TargetActivity == null,
                "The fixture already owns a target Activity.");

            return CreateActivityCore(activityId, activityName, policy,
                ActivityVisualTransitionMode.Seamless, TransitionGateMode.LifecycleRequestsOnly,
                null);
        }

        internal ActivityAsset CreateActivity(
            string activityId,
            string activityName,
            ActivityEntryReadinessPolicy policy,
            ActivityVisualTransitionMode visualTransitionMode,
            TransitionGateMode transitionGateMode,
            string activityContentScenePath)
        {
            Require(!string.IsNullOrWhiteSpace(activityContentScenePath),
                "Direct readiness policies requires an Activity content scene path.");
            return CreateActivityCore(activityId, activityName, policy,
                visualTransitionMode, transitionGateMode, activityContentScenePath);
        }

        private ActivityAsset CreateActivityCore(
            string activityId,
            string activityName,
            ActivityEntryReadinessPolicy policy,
            ActivityVisualTransitionMode visualTransitionMode,
            TransitionGateMode transitionGateMode,
            string activityContentScenePath)
        {
            Require(TargetActivity == null,
                "The fixture already owns a target Activity.");
            Require(TargetContentProfile == null,
                "The fixture already owns a target Activity Content Profile.");

            ActivityContentProfileAsset profile = null;
            if (!string.IsNullOrWhiteSpace(activityContentScenePath))
            {
                profile = CreateContentProfile(activityContentScenePath);
            }

            ActivityAsset activity = ScriptableObject.CreateInstance<ActivityAsset>();
            try
            {
                var serialized = new SerializedObject(activity);
                RequireProperty(serialized, "activityId").stringValue = activityId;
                RequireProperty(serialized, "activityName").stringValue = activityName;
                SetEnumName(RequireProperty(serialized,
                    "playerParticipationProjectionMode"), "NoSlots");
                SetEnumName(RequireProperty(serialized,
                    "playerParticipationZeroParticipantPolicy"), "Allowed");
                RequireProperty(serialized, "playerParticipationExplicitSlotProfiles")
                    .arraySize = 0;
                SetEnumName(RequireProperty(serialized,
                    "playerParticipationRequirementLevel"), "None");
                RequireProperty(serialized, "activityContentProfile").objectReferenceValue = profile;
                SetEnumName(RequireProperty(serialized,
                    "activityEntryReadinessPolicy"), policy.ToString());
                SetEnumName(RequireProperty(serialized,
                    "visualTransitionMode"), visualTransitionMode.ToString());
                SetEnumName(RequireProperty(serialized,
                    "transitionGateMode"), transitionGateMode.ToString());
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Require(activity.HasValidActivityId,
                    "Runtime Activity has an invalid stable ActivityId.");
                Require(activity.EntryReadinessPolicy == policy,
                    "Runtime Activity entry readiness policy was not applied.");
                Require(activity.VisualTransitionMode == visualTransitionMode &&
                    activity.TransitionGateMode == transitionGateMode,
                    "Runtime Activity transition policy was not applied.");
                Require(ReferenceEquals(activity.ActivityContentProfile, profile),
                    "Runtime Activity content profile reference was not applied.");
                if (profile == null)
                {
                    Require(!activity.HasActivityContentProfile,
                        "Runtime Activity unexpectedly requires Activity content.");
                }
                else
                {
                    Require(activity.HasActivityContentProfile && profile.SceneCount == 1 &&
                        profile.Scenes[0] != null &&
                        string.Equals(profile.Scenes[0].ScenePath, activityContentScenePath,
                            StringComparison.Ordinal) &&
                        profile.Scenes[0].Requiredness == FrameworkContentRequiredness.Required &&
                        profile.Scenes[0].LoadMode == ActivityContentSceneLoadMode.Additive &&
                        profile.Scenes[0].ReleasePolicy == ActivityContentReleasePolicy.ReleaseOnActivityChange,
                        "Runtime Activity Content Profile is not execution-ready.");
                }

                TargetActivity = activity;
                _targetActivityWasCreated = true;
                return activity;
            }
            catch
            {
                UnityEngine.Object.Destroy(activity);
                if (profile != null)
                {
                    UnityEngine.Object.Destroy(profile);
                    TargetContentProfile = null;
                    TargetContentScenePath = null;
                    _targetContentProfileWasCreated = false;
                }
                throw;
            }
        }

        internal async Task PrepareForReadinessSurfaceDestructionAsync()
        {
            if (CleanupKind != QaTemporaryActivityCleanupKind.Unknown)
            {
                return;
            }

            ActivityAsset currentActivity = RuntimeHost.State.CurrentActivity;
            if (TargetActivity != null && currentActivity != null &&
                currentActivity.HasSameIdentity(TargetActivity))
            {
                FrameworkActivityRequestResult clearResult =
                    await Activities.ClearActivityAsync(
                        nameof(QaActivityEntryReadinessFixture),
                        "clear-temporary-activity");
                Require(clearResult.Succeeded, clearResult.Message);
                Require(HasExpectedEnteredParticipantEvidence(
                        PreparationStartedCount,
                        PreparationReleasedCount,
                        Participant.Occurrence) &&
                    Participant.State == ActivityReadinessParticipantState.Released,
                    $"Temporary participant release diverged. expectedCycles='{_expectedParticipantPreparationCycles}' " +
                    $"started='{PreparationStartedCount}' released='{PreparationReleasedCount}' " +
                    $"occurrence='{Participant.Occurrence}' state='{Participant.State}'.");
                Require(RuntimeHost.State.CurrentActivity == null,
                    "Temporary Activity clear did not leave Activity authority empty.");
                ConfirmTargetContentSceneReleased();
                CleanupKind = QaTemporaryActivityCleanupKind.TargetCleared;
                return;
            }

            if (MatchesInitialActivity(currentActivity))
            {
                Require(PreparationStartedCount == 0 &&
                    PreparationReleasedCount == 0 && Participant.Occurrence == 0,
                    $"Initial Activity remained authoritative after temporary readiness started. {DescribeAuthority(currentActivity)}");
                CleanupKind = QaTemporaryActivityCleanupKind.InitialAuthorityPreserved;
                return;
            }

            if (currentActivity == null)
            {
                RequireCurrentParticipantEvidence();
                ConfirmTargetContentSceneReleased();
                CleanupKind = QaTemporaryActivityCleanupKind.AlreadyClear;
                return;
            }

            throw new InvalidOperationException(
                $"Unexpected Activity authority during readiness fixture cleanup. {DescribeAuthority(currentActivity)}");
        }

        internal void RemoveEventListeners()
        {
            if (_listenersRemoved)
            {
                return;
            }

            Participant.PreparationStarted.RemoveListener(_participantPreparationStarted);
            Participant.PreparationReleased.RemoveListener(_participantPreparationReleased);
            Events.Preparing.RemoveListener(_readinessPreparing);
            Events.Ready.RemoveListener(_readinessReady);
            Events.NotReady.RemoveListener(_readinessNotReady);
            _listenersRemoved = true;
        }

        internal async Task DestroyReadinessSurfaceAsync()
        {
            if (_readinessSurfaceDestroyed)
            {
                return;
            }

            Require(_listenersRemoved,
                "Readiness surface listeners must be removed before destruction.");
            Require(CleanupKind != QaTemporaryActivityCleanupKind.Unknown,
                "Readiness surface destruction requires prior cleanup authority classification.");
            ActivityAsset currentActivity = RuntimeHost.State.CurrentActivity;
            Require(currentActivity == null ||
                (CleanupKind == QaTemporaryActivityCleanupKind.InitialAuthorityPreserved &&
                 MatchesInitialActivity(currentActivity) &&
                 PreparationStartedCount == 0 &&
                 PreparationReleasedCount == 0 && Participant.Occurrence == 0),
                $"Readiness surface destruction rejected current Activity authority. {DescribeAuthority(currentActivity)}");
            CaptureFinalParticipantEvidence();
            RequireValidFinalParticipantEvidence();
            UnityEngine.Object.Destroy(_root);
            await Awaitable.NextFrameAsync();
            RequireNoTemporaryReadinessSurface(_primaryScene);
            _readinessSurfaceDestroyed = true;
        }

        internal async Task RestoreInitialAuthorityAsync()
        {
            if (_initialAuthorityRestored)
            {
                return;
            }

            Require(_readinessSurfaceDestroyed,
                "Initial authority cannot be restored before the readiness surface is destroyed.");
            Require(InitialRoute != null,
                "Initial Route is required for restoration.");
            if (RuntimeHost.State.CurrentRoute == null ||
                !RuntimeHost.State.CurrentRoute.HasSameIdentity(InitialRoute))
            {
                FrameworkRouteRequestResult routeResult = await Routes.RequestRouteAsync(
                    InitialRoute,
                    nameof(QaActivityEntryReadinessFixture),
                    "restore-initial-route");
                Require(routeResult.Succeeded, routeResult.Message);
            }

            ActivityAsset currentActivity = RuntimeHost.State.CurrentActivity;
            if (InitialActivity == null)
            {
                Require(currentActivity == null,
                    $"Initial Activity was absent but another Activity is authoritative after cleanup. {DescribeAuthority(currentActivity)}");
            }
            else if (InitialActivity != null &&
                (currentActivity == null || !currentActivity.HasSameIdentity(InitialActivity)))
            {
                FrameworkActivityRequestResult activityResult =
                    await Activities.RequestActivityAsync(
                        InitialActivity,
                        nameof(QaActivityEntryReadinessFixture),
                        "restore-initial-activity");
                Require(activityResult.Succeeded, activityResult.Message);
            }

            Require(RuntimeHost.State.CurrentRoute != null &&
                RuntimeHost.State.CurrentRoute.HasSameIdentity(InitialRoute),
                "Initial Route was not restored.");
            Require((InitialActivity == null && RuntimeHost.State.CurrentActivity == null) ||
                (InitialActivity != null && RuntimeHost.State.CurrentActivity != null &&
                 RuntimeHost.State.CurrentActivity.HasSameIdentity(InitialActivity)),
                "Initial Activity was not restored.");
            RequireValidFinalParticipantEvidence();
            Require(PreparationStartedCount == FinalPreparationStartedCount &&
                PreparationReleasedCount == FinalPreparationReleasedCount,
                "Temporary participant started or released again during initial authority restoration.");
            _initialAuthorityRestored = true;
        }

        internal async Task DestroyTargetActivityAsync()
        {
            if (_targetActivityDestructionConfirmed)
            {
                return;
            }

            if (!_targetActivityWasCreated)
            {
                _targetActivityDestructionConfirmed = true;
                return;
            }

            ActivityAsset target = TargetActivity;
            Require(target != null,
                "Runtime target Activity was created but is unavailable for destruction confirmation.");
            Require(RuntimeHost.State.CurrentActivity == null ||
                !RuntimeHost.State.CurrentActivity.HasSameIdentity(target),
                $"Runtime target Activity cannot be destroyed while it is authoritative. {DescribeAuthority(RuntimeHost.State.CurrentActivity)}");
            UnityEngine.Object.Destroy(target);
            await Awaitable.NextFrameAsync();
            Require(target == null,
                "Runtime target Activity destruction was not confirmed after one frame.");
            _targetActivityDestructionConfirmed = true;
        }

        internal async Task DestroyTargetContentProfileAsync()
        {
            if (_targetContentProfileDestructionConfirmed)
            {
                return;
            }

            if (!_targetContentProfileWasCreated)
            {
                _targetContentProfileDestructionConfirmed = true;
                return;
            }

            ActivityContentProfileAsset profile = TargetContentProfile;
            Require(profile != null,
                "Runtime Activity Content Profile was created but is unavailable for destruction confirmation.");
            Require(TargetActivity == null || RuntimeHost.State.CurrentActivity == null ||
                !RuntimeHost.State.CurrentActivity.HasSameIdentity(TargetActivity),
                "Runtime Activity Content Profile cannot be destroyed while its Activity is authoritative.");
            ConfirmTargetContentSceneReleased();
            UnityEngine.Object.Destroy(profile);
            await Awaitable.NextFrameAsync();
            Require(profile == null,
                "Runtime Activity Content Profile destruction was not confirmed after one frame.");
            _targetContentProfileDestructionConfirmed = true;
        }

        public async ValueTask DisposeAsync()
        {
            await PrepareForReadinessSurfaceDestructionAsync();
            RemoveEventListeners();
            await DestroyReadinessSurfaceAsync();
            await RestoreInitialAuthorityAsync();
            await DestroyTargetActivityAsync();
            await DestroyTargetContentProfileAsync();
        }

        internal async ValueTask DisposeAsync(
            QaOwnedAsyncOperation<FrameworkActivityRequestResult> ownedOperation)
        {
            Require(ownedOperation != null,
                "A QA-owned Activity request operation is required before fixture disposal.");
            Require(!ownedOperation.HasOperation || ownedOperation.ReachedTerminal,
                $"Fixture disposal requires the owned operation '{ownedOperation.Name}' to reach terminal. phase='{ownedOperation.Phase}'.");
            await DisposeAsync();
        }

        internal void RequireValidFinalParticipantEvidence()
        {
            Require(_finalParticipantEvidenceCaptured,
                "Final participant evidence must be captured before validation.");
            bool targetEntered = HasExpectedEnteredParticipantEvidence(
                FinalPreparationStartedCount,
                FinalPreparationReleasedCount,
                FinalOccurrence);
            bool targetNeverEntered = FinalPreparationStartedCount == 0 &&
                FinalPreparationReleasedCount == 0 && FinalOccurrence == 0;
            Require(targetEntered || targetNeverEntered,
                $"Invalid final readiness participant evidence. started='{FinalPreparationStartedCount}' released='{FinalPreparationReleasedCount}' occurrence='{FinalOccurrence}'.");
        }

        private bool MatchesInitialActivity(ActivityAsset activity)
        {
            return InitialActivity != null && activity != null &&
                activity.HasSameIdentity(InitialActivity);
        }

        private void RequireCurrentParticipantEvidence()
        {
            bool targetEntered = HasExpectedEnteredParticipantEvidence(
                PreparationStartedCount,
                PreparationReleasedCount,
                Participant.Occurrence);
            bool targetNeverEntered = PreparationStartedCount == 0 &&
                PreparationReleasedCount == 0 && Participant.Occurrence == 0;
            Require(targetEntered || targetNeverEntered,
                $"Invalid readiness participant evidence before surface destruction. started='{PreparationStartedCount}' released='{PreparationReleasedCount}' occurrence='{Participant.Occurrence}'.");
        }

        private bool HasExpectedEnteredParticipantEvidence(
            int started,
            int released,
            int occurrence)
        {
            return started == _expectedParticipantPreparationCycles &&
                released == _expectedParticipantPreparationCycles &&
                occurrence > 0;
        }

        private string DescribeAuthority(ActivityAsset currentActivity)
        {
            int participantStarts = _finalParticipantEvidenceCaptured
                ? FinalPreparationStartedCount
                : PreparationStartedCount;
            int participantReleases = _finalParticipantEvidenceCaptured
                ? FinalPreparationReleasedCount
                : PreparationReleasedCount;
            int participantOccurrence = _finalParticipantEvidenceCaptured
                ? FinalOccurrence
                : Participant.Occurrence;
            return $"current='{DescribeActivity(currentActivity)}' " +
                $"initial='{DescribeActivity(InitialActivity)}' " +
                $"target='{DescribeActivity(TargetActivity)}' " +
                $"participantStarts='{participantStarts}' " +
                $"participantReleases='{participantReleases}' " +
                $"participantOccurrence='{participantOccurrence}'.";
        }

        private static string DescribeActivity(ActivityAsset activity)
        {
            if (activity == null)
            {
                return "<none>";
            }

            string id = activity.HasValidActivityId
                ? activity.ActivityId.StableText
                : "<invalid>";
            return $"name='{activity.ActivityName}' id='{id}'";
        }

        private void CaptureFinalParticipantEvidence()
        {
            if (_finalParticipantEvidenceCaptured)
            {
                return;
            }

            FinalPreparationStartedCount = PreparationStartedCount;
            FinalPreparationReleasedCount = PreparationReleasedCount;
            FinalOccurrence = Participant.Occurrence;
            _finalParticipantEvidenceCaptured = true;
        }

        private static Scene ResolveLoadedPrimaryScene(RouteAsset route)
        {
            Require(route != null && route.HasPrimaryScene,
                "Current Route does not declare a primary scene.");
            Scene scene = SceneManager.GetSceneByPath(route.PrimaryScenePath);
            Require(scene.IsValid() && scene.isLoaded,
                $"Current Route primary scene is not loaded. path='{route.PrimaryScenePath}'.");
            return scene;
        }

        private static void RequireNoConflictingParticipant(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                GameObject root = roots[rootIndex];
                Require(root == null || !string.Equals(root.name, RootName,
                    StringComparison.Ordinal),
                    $"Route primary scene already contains temporary readiness root '{RootName}'.");
                ActivityReadinessParticipant[] participants = root == null
                    ? Array.Empty<ActivityReadinessParticipant>()
                    : root.GetComponentsInChildren<ActivityReadinessParticipant>(true);
                for (int participantIndex = 0;
                     participantIndex < participants.Length;
                     participantIndex++)
                {
                    ActivityReadinessParticipant participant =
                        participants[participantIndex];
                    Require(participant == null ||
                        !string.Equals(participant.ParticipantId, ParticipantId,
                            StringComparison.Ordinal),
                        $"Route primary scene already contains readiness participant '{ParticipantId}'.");
                }
            }
        }

        private static void RequireNoTemporaryReadinessSurface(Scene scene)
        {
            Require(scene.IsValid() && scene.isLoaded,
                "Route primary scene is unavailable while verifying readiness surface destruction.");
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                GameObject root = roots[rootIndex];
                Require(root == null || !string.Equals(root.name, RootName,
                    StringComparison.Ordinal),
                    $"Temporary readiness root '{RootName}' remains in the Route primary scene.");

                ActivityReadinessParticipant[] participants = root == null
                    ? Array.Empty<ActivityReadinessParticipant>()
                    : root.GetComponentsInChildren<ActivityReadinessParticipant>(true);
                for (int participantIndex = 0;
                     participantIndex < participants.Length;
                     participantIndex++)
                {
                    ActivityReadinessParticipant participant =
                        participants[participantIndex];
                    Require(participant == null ||
                        !string.Equals(participant.ParticipantId, ParticipantId,
                            StringComparison.Ordinal),
                        $"Temporary readiness participant '{ParticipantId}' remains in the Route primary scene.");
                }
            }
        }

        private static void ConfigureParticipant(ActivityReadinessParticipant participant)
        {
            var serialized = new SerializedObject(participant);
            RequireProperty(serialized, "participantId").stringValue = ParticipantId;
            SetEnumName(RequireProperty(serialized, "requiredness"), "Required");
            RequireProperty(serialized, "order").intValue = 1000;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Require(participant.ParticipantId == ParticipantId &&
                participant.Requiredness == ActivityContentExecutionRequiredness.Required,
                "Temporary readiness participant configuration was not applied.");
        }

        private ActivityContentProfileAsset CreateContentProfile(string activityContentScenePath)
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(activityContentScenePath);
            Require(sceneAsset != null,
                $"Activity content scene asset was not found at '{activityContentScenePath}'.");
            Scene loadedScene = SceneManager.GetSceneByPath(activityContentScenePath);
            Require(!loadedScene.IsValid() || !loadedScene.isLoaded,
                $"Activity content scene must not be loaded before the request. path='{activityContentScenePath}'.");

            var profile = ScriptableObject.CreateInstance<ActivityContentProfileAsset>();
            try
            {
                var serialized = new SerializedObject(profile);
                RequireProperty(serialized, "profileId").stringValue =
                    "qa.if-ready-04.direct-policies.content";
                SerializedProperty scenes = RequireProperty(serialized, "scenes");
                scenes.arraySize = 1;
                SerializedProperty entry = scenes.GetArrayElementAtIndex(0);
                RequireProperty(entry, "contentId").stringValue =
                    "qa.if-ready-04.direct-policies.activity-content";
                RequireProperty(entry, "scenePath").stringValue = activityContentScenePath;
                RequireProperty(entry, "sceneName").stringValue = "ActivityAdditionalContent";
                SetEnumName(RequireProperty(entry, "requiredness"), "Required");
                SetEnumName(RequireProperty(entry, "loadMode"), "Additive");
                SetEnumName(RequireProperty(entry, "releasePolicy"), "ReleaseOnActivityChange");
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Require(string.Equals(profile.ProfileId,
                        "qa.if-ready-04.direct-policies.content", StringComparison.Ordinal) &&
                    profile.SceneCount == 1 && profile.Scenes[0] != null &&
                    profile.Scenes[0].HasExplicitContentId,
                    "Runtime Activity Content Profile was not configured.");
                TargetContentProfile = profile;
                TargetContentScenePath = activityContentScenePath;
                _targetContentProfileWasCreated = true;
                return profile;
            }
            catch
            {
                UnityEngine.Object.Destroy(profile);
                TargetContentProfile = null;
                TargetContentScenePath = null;
                _targetContentProfileWasCreated = false;
                throw;
            }
        }

        private void ConfirmTargetContentSceneReleased()
        {
            if (!_targetContentProfileWasCreated || _targetContentSceneReleaseConfirmed)
            {
                return;
            }

            Scene scene = SceneManager.GetSceneByPath(TargetContentScenePath);
            Require(!scene.IsValid() || !scene.isLoaded,
                $"Target Activity content scene remained loaded after target authority cleared. path='{TargetContentScenePath}'.");
            _targetContentSceneReleaseConfirmed = true;
        }

        private static SerializedProperty RequireProperty(
            SerializedObject serialized,
            string name)
        {
            SerializedProperty property = serialized.FindProperty(name);
            Require(property != null,
                $"Required serialized property '{name}' was not found.");
            return property;
        }

        private static SerializedProperty RequireProperty(
            SerializedProperty parent,
            string name)
        {
            SerializedProperty property = parent.FindPropertyRelative(name);
            Require(property != null,
                $"Required serialized property '{name}' was not found.");
            return property;
        }

        private static void SetEnumName(SerializedProperty property, string value)
        {
            string[] names = property.enumNames;
            for (int index = 0; index < names.Length; index++)
            {
                if (string.Equals(names[index], value, StringComparison.Ordinal))
                {
                    property.enumValueIndex = index;
                    return;
                }
            }

            throw new InvalidOperationException(
                $"Serialized enum value '{value}' is not available.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
