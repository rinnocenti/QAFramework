using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Player.Editor
{
    internal static class QaP3M4B2BScenePlayerActorAdoptionSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3M4B2B Scene Player Actor Adoption Smoke";
        private const BindingFlags InstanceAny =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags StaticNonPublic =
            BindingFlags.Static | BindingFlags.NonPublic;

        private static bool ValidateRun()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        internal static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError(
                    "[P3M4B2B_SCENE_PLAYER_ACTOR_ADOPTION_SMOKE] " +
                    "status='RejectedPlayMode' message='This smoke is Editor-only.'.");
                return;
            }

            var completed = new List<string>();
            var created = new List<UnityEngine.Object>();
            Scene activityScene = default;
            Scene previousActiveScene = SceneManager.GetActiveScene();
            string temporaryScenePath =
                $"Assets/QA_P3M4B2B_Temp_{Guid.NewGuid():N}.unity";
            Exception failure = null;

            try
            {
                activityScene = EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Additive);
                AssertTrue(
                    EditorSceneManager.SaveScene(
                        activityScene,
                        temporaryScenePath,
                        false),
                    $"Could not save temporary Activity scene '{temporaryScenePath}'.");

                PlayerSlotProfile slot = CreateSlotProfile(created);
                object participationContext = CreateParticipationContext(slot, created);
                Fixture fixture = CreateFixture(slot, activityScene, created);

                object runtimeHost;
                object runtimeContentRuntime;
                object sceneModule = CreateSceneAdmissionModule(
                    participationContext,
                    created,
                    out runtimeHost,
                    out runtimeContentRuntime);
                object preparationModule;
                IActivityContentExecutionParticipant composite =
                    CreatePreparedCompositeParticipant(
                        runtimeHost,
                        runtimeContentRuntime,
                        participationContext,
                        sceneModule,
                        created,
                        out preparationModule);

                ActivityContentExecutionParticipantDescriptor descriptor =
                    composite.GetActivityContentExecutionDescriptor();
                AssertTrue(descriptor.IsValid,
                    "Composite participant returned an invalid descriptor.");
                AssertEqual(
                    "framework.player-actor.activity-lifecycle",
                    descriptor.ContentId.StableText,
                    "Scene Actor adoption changed the canonical Player lifecycle content id.");
                completed.Add("canonical-participant-identity-preserved");

                ActivityAsset preparedActivity = CreateActivity(
                    "QA P3M4B2B Logical Actors Prepared Activity",
                    slot,
                    temporaryScenePath,
                    PlayerParticipationRequirementLevel.LogicalActorsPrepared,
                    created);
                RuntimeContentOwner owner = RuntimeContentOwner.Activity(
                    preparedActivity.ActivityId.StableText,
                    preparedActivity.ActivityName);
                CreateScopeRoot(runtimeContentRuntime, owner);
                RuntimeScopeContext scope = new RuntimeScopeContext(
                    owner,
                    nameof(QaP3M4B2BScenePlayerActorAdoptionSmoke),
                    "logical-actors-prepared");

                int actorMountChildCountBefore = fixture.Host.ActorMount.childCount;
                bool hostActiveBefore = fixture.Host.gameObject.activeSelf;
                bool actorActiveBefore = fixture.Actor.gameObject.activeSelf;
                ActorId authoredActorIdBefore = fixture.Actor.ActorId;
                string authoredDisplayNameBefore = fixture.Actor.ActorDisplayName;
                string authoredReasonBefore = fixture.Actor.Reason;

                ActivityContentExecutionRequest enterRequest =
                    ActivityContentExecutionRequest.Enter(
                        preparedActivity,
                        null,
                        scope,
                        descriptor.ContentId,
                        descriptor.Requiredness,
                        "QaP3M4B2B",
                        "logical-actors-prepared-enter");
                ActivityContentExecutionResult enter =
                    composite.ExecuteActivityContent(enterRequest);
                AssertTrue(enter.Succeeded, enter.ToDiagnosticString());
                completed.Add("logical-actors-prepared-enter");

                PlayerParticipationSnapshot entered =
                    CreateParticipationSnapshot(participationContext);
                AssertEqual(1, entered.JoinedCount,
                    "Scene Actor adoption did not retain the Joined Slot.");
                AssertEqual(1, entered.SelectedActorCount,
                    "Scene Actor adoption did not retain the Actor selection.");
                AssertTrue(fixture.Host.IsJoined,
                    "Scene Actor adoption did not retain Host admission.");
                completed.Add("host-slot-selection-committed");

                PlayerActorPreparationSummary preparation =
                    GetPreparationSummary(preparationModule, slot.PlayerSlotId);
                AssertTrue(preparation.IsPrepared && preparation.Token.IsValid,
                    preparation.ToDiagnosticString());
                AssertEqual(fixture.ActorProfile.ActorProfileId,
                    preparation.PreparedActorProfileId,
                    "Prepared Scene Actor Profile identity changed.");
                completed.Add("external-actor-prepared-canonically");

                ScenePlayerActorAdoptionToken adoptionToken =
                    GetAdoptionToken(preparationModule, slot.PlayerSlotId);
                AssertTrue(adoptionToken.IsValid,
                    "Scene Actor adoption returned no valid token.");
                AssertEqual(
                    PlayerActorPhysicalOwnership.ExternalSceneOwned,
                    adoptionToken.PhysicalOwnership,
                    "Scene Actor adoption did not preserve explicit external ownership.");
                AssertEqual(preparation.Token, adoptionToken.PreparationToken,
                    "Adoption and canonical preparation tokens diverged.");
                AssertNotNull(fixture.Authoring.LastActorAdoptionResult,
                    "Authoring surface did not receive adoption diagnostics.");
                AssertEqual(ScenePlayerActorAdoptionStatus.SucceededAdopted,
                    fixture.Authoring.LastActorAdoptionResult.Status,
                    "Authoring surface recorded an unexpected adoption status.");
                completed.Add("external-scene-ownership-explicit");

                AssertTrue(fixture.Actor.HasPlayerInputEvidence,
                    "Prepared Scene Actor has no Local PlayerInput evidence.");
                AssertTrue(ReferenceEquals(
                        fixture.Actor.PlayerInput,
                        fixture.Host.PlayerInput),
                    "Prepared Scene Actor references a different PlayerInput authority.");
                AssertEqual(1,
                    fixture.Host.ActorMount.GetComponentsInChildren<PlayerActorDeclaration>(true).Length,
                    "Scene Actor adoption instantiated a second PlayerActorDeclaration.");
                AssertEqual(actorMountChildCountBefore + 1,
                    fixture.Host.ActorMount.childCount,
                    "Scene Actor adoption did not create exactly one technical release proxy.");
                completed.Add("no-logical-actor-duplication");

                ScenePlayerActorAdoptionResult foreignRelease =
                    InvokeAdoptionRelease(
                        preparationModule,
                        fixture.Authoring,
                        default,
                        "foreign-token");
                AssertEqual(
                    ScenePlayerActorAdoptionStatus.RejectedForeignOrStaleAdoption,
                    foreignRelease.Status,
                    "Scene Actor adoption accepted a foreign token.");
                AssertTrue(GetPreparationSummary(
                        preparationModule,
                        slot.PlayerSlotId).IsPrepared,
                    "Foreign token rejection changed preparation state.");
                completed.Add("foreign-token-rejected");

                ActivityContentExecutionResult idempotentEnter =
                    composite.ExecuteActivityContent(enterRequest);
                AssertTrue(idempotentEnter.Succeeded,
                    idempotentEnter.ToDiagnosticString());
                ScenePlayerActorAdoptionToken idempotentToken =
                    GetAdoptionToken(preparationModule, slot.PlayerSlotId);
                AssertEqual(adoptionToken, idempotentToken,
                    "Idempotent Activity enter replaced the Scene Actor adoption token.");
                completed.Add("idempotent-enter-preserves-adoption");

                ActivityContentExecutionRequest exitRequest =
                    ActivityContentExecutionRequest.Exit(
                        preparedActivity,
                        null,
                        scope,
                        descriptor.ContentId,
                        descriptor.Requiredness,
                        "QaP3M4B2B",
                        "logical-actors-prepared-exit");
                ActivityContentExecutionResult exit =
                    composite.ExecuteActivityContent(exitRequest);
                AssertTrue(exit.Succeeded, exit.ToDiagnosticString());
                completed.Add("canonical-exit-releases-adoption");

                PlayerParticipationSnapshot exited =
                    CreateParticipationSnapshot(participationContext);
                AssertEqual(0, exited.JoinedCount,
                    "Scene Actor adoption exit retained a Joined Slot.");
                AssertEqual(0, exited.SelectedActorCount,
                    "Scene Actor adoption exit retained Actor selection.");
                AssertFalse(fixture.Host.IsJoined,
                    "Scene Actor adoption exit retained Host admission.");
                AssertFalse(TryGetAdoptionToken(
                        preparationModule,
                        slot.PlayerSlotId,
                        out _),
                    "Scene Actor adoption exit retained an adoption token.");
                PlayerActorPreparationSummary releasedSummary =
                    GetPreparationSummary(preparationModule, slot.PlayerSlotId);
                AssertTrue(releasedSummary.IsUnprepared,
                    releasedSummary.ToDiagnosticString());
                completed.Add("slot-selection-preparation-cleared");

                AssertNotNull(fixture.Host,
                    "External Local Player Host was destroyed.");
                AssertNotNull(fixture.Actor,
                    "External Scene Logical Player Actor was destroyed.");
                AssertEqual(hostActiveBefore, fixture.Host.gameObject.activeSelf,
                    "Scene Actor adoption changed external Host active state.");
                AssertEqual(actorActiveBefore, fixture.Actor.gameObject.activeSelf,
                    "Scene Actor adoption changed external Actor active state.");
                AssertEqual(actorMountChildCountBefore,
                    fixture.Host.ActorMount.childCount,
                    "Scene Actor adoption retained its technical release proxy.");
                AssertFalse(fixture.Actor.HasPlayerInputEvidence,
                    "Scene Actor adoption exit retained contextual PlayerInput evidence.");
                AssertEqual(authoredActorIdBefore, fixture.Actor.ActorId,
                    "Scene Actor adoption exit did not restore the authored ActorId.");
                AssertEqual(authoredDisplayNameBefore, fixture.Actor.ActorDisplayName,
                    "Scene Actor adoption exit did not restore the authored display name.");
                AssertEqual(authoredReasonBefore, fixture.Actor.Reason,
                    "Scene Actor adoption exit did not restore the authored declaration reason.");
                AssertNotNull(fixture.Authoring.LastActorAdoptionResult,
                    "Authoring surface lost adoption release diagnostics.");
                AssertEqual(ScenePlayerActorAdoptionStatus.SucceededReleased,
                    fixture.Authoring.LastActorAdoptionResult.Status,
                    "Authoring surface recorded an unexpected adoption release status.");
                completed.Add("external-host-and-actor-preserved");

                ActivityContentExecutionResult idempotentExit =
                    composite.ExecuteActivityContent(exitRequest);
                AssertTrue(idempotentExit.Succeeded,
                    idempotentExit.ToDiagnosticString());
                completed.Add("idempotent-exit");

                RemoveScopeRoot(runtimeContentRuntime, owner);

                ActivityAsset gameplayActivity = CreateActivity(
                    "QA P3M4B2B Gameplay Ready Boundary Activity",
                    slot,
                    temporaryScenePath,
                    PlayerParticipationRequirementLevel.GameplayReady,
                    created);
                RuntimeContentOwner gameplayOwner = RuntimeContentOwner.Activity(
                    gameplayActivity.ActivityId.StableText,
                    gameplayActivity.ActivityName);
                CreateScopeRoot(runtimeContentRuntime, gameplayOwner);
                RuntimeScopeContext gameplayScope = new RuntimeScopeContext(
                    gameplayOwner,
                    "QaP3M4B2B",
                    "gameplay-ready-boundary");
                ActivityContentExecutionRequest gameplayEnter =
                    ActivityContentExecutionRequest.Enter(
                        gameplayActivity,
                        preparedActivity,
                        gameplayScope,
                        descriptor.ContentId,
                        descriptor.Requiredness,
                        "QaP3M4B2B",
                        "gameplay-ready-boundary");
                ActivityContentExecutionResult gameplayResult =
                    composite.ExecuteActivityContent(gameplayEnter);
                string gameplayDiagnostic = gameplayResult.ToDiagnosticString();
                AssertFalse(
                    gameplayDiagnostic.IndexOf(
                        "external Actor adoption is the next gate",
                        StringComparison.Ordinal) >= 0 ||
                    gameplayDiagnostic.IndexOf(
                        "RejectedActorAdoptionRequired",
                        StringComparison.Ordinal) >= 0,
                    "GameplayReady was still blocked by the Scene Actor adoption layer.");
                if (gameplayResult.Succeeded)
                {
                    ActivityContentExecutionRequest gameplayExit =
                        ActivityContentExecutionRequest.Exit(
                            gameplayActivity,
                            null,
                            gameplayScope,
                            descriptor.ContentId,
                            descriptor.Requiredness,
                            "QaP3M4B2B",
                            "gameplay-ready-boundary-exit");
                    ActivityContentExecutionResult gameplayExitResult =
                        composite.ExecuteActivityContent(gameplayExit);
                    AssertTrue(gameplayExitResult.Succeeded,
                        gameplayExitResult.ToDiagnosticString());
                }
                else
                {
                    AssertTrue(gameplayResult.Failed,
                        "GameplayReady boundary returned neither success nor explicit failure.");
                    AssertFalse(fixture.Authoring.HasActiveAdmission,
                        "Canonical GameplayReady failure did not roll back Scene admission.");
                    AssertFalse(TryGetAdoptionToken(
                            preparationModule,
                            slot.PlayerSlotId,
                            out _),
                        "Canonical GameplayReady failure retained Scene Actor adoption.");
                }
                completed.Add("gameplay-ready-reaches-canonical-pipeline");
                RemoveScopeRoot(runtimeContentRuntime, gameplayOwner);
            }
            catch (Exception exception)
            {
                failure = Unwrap(exception);
            }

            try
            {
                if (activityScene.IsValid() && activityScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(activityScene, true);
                }

                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActiveScene);
                }

                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(temporaryScenePath) != null &&
                    !AssetDatabase.DeleteAsset(temporaryScenePath))
                {
                    throw new InvalidOperationException(
                        $"Could not delete temporary scene '{temporaryScenePath}'.");
                }

                for (int index = created.Count - 1; index >= 0; index--)
                {
                    if (created[index] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(created[index]);
                    }
                }
            }
            catch (Exception cleanupException)
            {
                Exception actual = Unwrap(cleanupException);
                failure = failure == null
                    ? new InvalidOperationException(
                        $"P3M4B2B cleanup failed. {actual.Message}",
                        actual)
                    : new AggregateException(
                        "P3M4B2B execution and cleanup both failed.",
                        failure,
                        actual);
            }

            if (failure != null)
            {
                Debug.LogError(
                    "[P3M4B2B_SCENE_PLAYER_ACTOR_ADOPTION_SMOKE] " +
                    $"status='Failed' exception='{failure.GetType().Name}' " +
                    $"message='{Escape(failure.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw failure;
            }

            Debug.Log(
                "[P3M4B2B_SCENE_PLAYER_ACTOR_ADOPTION_SMOKE] " +
                $"status='Passed' cases='{completed.Count}' " +
                $"completed='{string.Join(",", completed)}'.");
        }

        private static object CreateParticipationContext(
            PlayerSlotProfile slot,
            ICollection<UnityEngine.Object> created)
        {
            var policy = ScriptableObject.CreateInstance<PlayerActorSelectionPolicyProfile>();
            policy.name = "QA P3M4B2B Actor Selection Policy";
            created.Add(policy);

            Type contextType = typeof(PlayerParticipationSnapshot).Assembly.GetType(
                "Immersive.Framework.PlayerParticipation.PlayerParticipationRuntimeContext",
                true);
            MethodInfo create = contextType.GetMethod(
                "TryCreateWithActorSelectionPolicy",
                StaticNonPublic);
            object[] arguments =
            {
                new[] { slot },
                1,
                false,
                policy,
                "QaP3M4B2B",
                "initialize-scene-actor-adoption",
                null
            };
            var result = (PlayerParticipationOperationResult)create.Invoke(null, arguments);
            AssertTrue(result != null && result.Succeeded,
                result != null ? result.ToDiagnosticString() : "No participation result.");
            return arguments[6];
        }

        private static object CreateSceneAdmissionModule(
            object participationContext,
            ICollection<UnityEngine.Object> created,
            out object runtimeHost,
            out object runtimeContentRuntime)
        {
            Assembly assembly = typeof(PlayerParticipationSnapshot).Assembly;
            Type hostType = assembly.GetType(
                "Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost",
                true);
            Type runtimeContentType = assembly.GetType(
                "Immersive.Framework.RuntimeContent.RuntimeContentRuntime",
                true);
            Type sceneModuleType = assembly.GetType(
                "Immersive.Framework.PlayerParticipation.SceneLocalPlayerAdmissionRuntimeHostModule",
                true);

            GameObject root = NewObject("QA P3M4B2B Runtime Host", created);
            runtimeHost = root.AddComponent(hostType);
            runtimeContentRuntime = Activator.CreateInstance(
                runtimeContentType,
                InstanceAny,
                null,
                Array.Empty<object>(),
                null);
            SetField(runtimeHost, "_runtimeContentRuntime", runtimeContentRuntime);

            object sceneModule = root.AddComponent(sceneModuleType);
            MethodInfo initialize = sceneModuleType.GetMethod("TryInitialize", InstanceAny);
            object[] arguments = { runtimeHost, participationContext, null };
            bool initialized = (bool)initialize.Invoke(sceneModule, arguments);
            AssertTrue(initialized,
                arguments[2] as string ?? "Scene admission module failed.");
            return sceneModule;
        }

        private static IActivityContentExecutionParticipant CreatePreparedCompositeParticipant(
            object runtimeHost,
            object runtimeContentRuntime,
            object participationContext,
            object sceneModule,
            ICollection<UnityEngine.Object> created,
            out object preparationModule)
        {
            Assembly assembly = typeof(PlayerParticipationSnapshot).Assembly;
            Type adapterType = assembly.GetType(
                "Immersive.Framework.PlayerParticipation.AttachedPlayerActorMaterializationAdapter",
                true);
            Type preparationContextType = assembly.GetType(
                "Immersive.Framework.PlayerParticipation.PlayerActorPreparationRuntimeContext",
                true);
            Type preparationModuleType = assembly.GetType(
                "Immersive.Framework.PlayerParticipation.PlayerActorPreparationRuntimeHostModule",
                true);
            Type canonicalType = assembly.GetType(
                "Immersive.Framework.PlayerParticipation.ActivityPlayerActorLifecycleParticipant",
                true);
            Type compositeType = assembly.GetType(
                "Immersive.Framework.PlayerParticipation.SceneLocalPlayerAdmissionCompositeLifecycleParticipant",
                true);

            PlayerParticipationSnapshot snapshot =
                CreateParticipationSnapshot(participationContext);
            object adapter = Activator.CreateInstance(
                adapterType,
                InstanceAny,
                null,
                new[] { runtimeContentRuntime, snapshot.ContextId },
                null);
            MethodInfo createContext = preparationContextType.GetMethod(
                "TryCreate",
                StaticNonPublic);
            object[] createArguments =
            {
                participationContext,
                adapter,
                null,
                null
            };
            bool createdContext = (bool)createContext.Invoke(null, createArguments);
            AssertTrue(createdContext,
                createArguments[3] as string ?? "Preparation context failed.");
            object preparationContext = createArguments[2];

            var runtimeHostComponent = (Component)runtimeHost;
            preparationModule =
                runtimeHostComponent.gameObject.AddComponent(preparationModuleType);
            object canonical = Activator.CreateInstance(
                canonicalType,
                InstanceAny,
                null,
                new[] { preparationModule, participationContext },
                null);
            SetField(preparationModule, "runtimeHost", runtimeHost);
            SetField(preparationModule, "participationContext", participationContext);
            SetField(preparationModule, "preparationContext", preparationContext);
            SetField(preparationModule, "activityLifecycleParticipant", canonical);
            SetField(preparationModule, "diagnostic",
                "QA P3M4B2B Player Actor preparation runtime is ready.");

            object composite = Activator.CreateInstance(
                compositeType,
                InstanceAny,
                null,
                new[] { canonical, sceneModule, preparationModule },
                null);
            AssertTrue(composite is IActivityContentExecutionParticipant,
                "Composite participant does not implement Activity execution.");
            return (IActivityContentExecutionParticipant)composite;
        }

        private static void CreateScopeRoot(
            object runtimeContentRuntime,
            RuntimeContentOwner owner)
        {
            MethodInfo create = runtimeContentRuntime.GetType().GetMethod(
                "CreateScopeRoot",
                InstanceAny);
            object result = create.Invoke(
                runtimeContentRuntime,
                new object[] { owner, "QaP3M4B2B", "create-activity-root" });
            AssertNotNull(result, "RuntimeContent scope root creation returned no result.");
        }

        private static void RemoveScopeRoot(
            object runtimeContentRuntime,
            RuntimeContentOwner owner)
        {
            MethodInfo remove = runtimeContentRuntime.GetType().GetMethod(
                "RemoveScopeRoot",
                InstanceAny);
            object result = remove.Invoke(
                runtimeContentRuntime,
                new object[] { owner, "QaP3M4B2B", "remove-activity-root" });
            AssertNotNull(result, "RuntimeContent scope root removal returned no result.");
        }

        private static PlayerActorPreparationSummary GetPreparationSummary(
            object preparationModule,
            PlayerSlotId playerSlotId)
        {
            MethodInfo get = preparationModule.GetType().GetMethod(
                "TryGetScenePlayerActorPreparationSummary",
                InstanceAny);
            object[] arguments = { playerSlotId, null };
            bool found = (bool)get.Invoke(preparationModule, arguments);
            AssertTrue(found,
                $"No preparation summary for Slot '{playerSlotId.StableText}'.");
            return (PlayerActorPreparationSummary)arguments[1];
        }

        private static ScenePlayerActorAdoptionToken GetAdoptionToken(
            object preparationModule,
            PlayerSlotId playerSlotId)
        {
            AssertTrue(TryGetAdoptionToken(
                    preparationModule,
                    playerSlotId,
                    out ScenePlayerActorAdoptionToken token),
                $"No Scene Actor adoption token for Slot '{playerSlotId.StableText}'.");
            return token;
        }

        private static bool TryGetAdoptionToken(
            object preparationModule,
            PlayerSlotId playerSlotId,
            out ScenePlayerActorAdoptionToken token)
        {
            MethodInfo get = preparationModule.GetType().GetMethod(
                "TryGetScenePlayerActorAdoption",
                InstanceAny);
            object[] arguments = { playerSlotId, null };
            bool found = (bool)get.Invoke(preparationModule, arguments);
            token = found
                ? (ScenePlayerActorAdoptionToken)arguments[1]
                : default;
            return found;
        }

        private static ScenePlayerActorAdoptionResult InvokeAdoptionRelease(
            object preparationModule,
            SceneLocalPlayerAdmissionAuthoring authoring,
            ScenePlayerActorAdoptionToken token,
            string reason)
        {
            MethodInfo release = preparationModule.GetType().GetMethod(
                "TryReleaseSceneLocalPlayerActor",
                InstanceAny);
            return (ScenePlayerActorAdoptionResult)release.Invoke(
                preparationModule,
                new object[] { authoring, token, "QaP3M4B2B", reason });
        }

        private static ActivityAsset CreateActivity(
            string name,
            PlayerSlotProfile slot,
            string scenePath,
            PlayerParticipationRequirementLevel requirementLevel,
            ICollection<UnityEngine.Object> created)
        {
            var projection =
                ScriptableObject.CreateInstance<ActivityParticipationProjectionProfile>();
            projection.name = name + " Projection";
            created.Add(projection);
            SetEnum(projection, "projectionMode",
                ActivityParticipationProjectionMode.ExplicitSlots);
            SetEnum(projection, "zeroParticipantPolicy",
                ActivityParticipationZeroParticipantPolicy.Rejected);
            SetObjectArray(projection, "explicitSlotProfiles", new[] { slot });

            var requirements =
                ScriptableObject.CreateInstance<PlayerParticipationRequirementsProfile>();
            requirements.name = name + " Requirements";
            created.Add(requirements);
            SetEnum(requirements, "requirementLevel", requirementLevel);

            var content = ScriptableObject.CreateInstance<ActivityContentProfileAsset>();
            content.name = name + " Content";
            created.Add(content);
            var sceneEntry = new ActivityContentSceneEntry();
            string normalizedPath = scenePath.Replace('\\', '/');
            SetField(sceneEntry, "contentId", "qa.p3m4b2b.scene");
            SetField(sceneEntry, "scenePath", normalizedPath);
            SetField(sceneEntry, "sceneName", Path.GetFileNameWithoutExtension(normalizedPath));
            SetField(content, "scenes", new[] { sceneEntry });

            var activity = ScriptableObject.CreateInstance<ActivityAsset>();
            activity.name = name;
            created.Add(activity);
            SetObject(activity, "playerParticipationProjectionProfile", projection);
            SetObject(activity, "playerParticipationRequirementsProfile", requirements);
            SetObject(activity, "activityContentProfile", content);
            return activity;
        }

        private static Fixture CreateFixture(
            PlayerSlotProfile slot,
            Scene scene,
            ICollection<UnityEngine.Object> created)
        {
            GameObject hostRoot = NewSceneObject("QA_P3M4B2B_Host", scene);
            PlayerInput input = hostRoot.AddComponent<PlayerInput>();
            LocalPlayerHostAuthoring host =
                hostRoot.AddComponent<LocalPlayerHostAuthoring>();
            GameObject actorMount = NewSceneObject("ActorMount", scene);
            actorMount.transform.SetParent(hostRoot.transform, false);
            SetObject(host, "playerInput", input);
            SetObject(host, "actorMount", actorMount.transform);

            GameObject actorRoot = NewSceneObject("QA_P3M4B2B_Actor", scene);
            actorRoot.transform.SetParent(actorMount.transform, false);
            PlayerActorDeclaration actor =
                actorRoot.AddComponent<PlayerActorDeclaration>();

            var actorProfile = ScriptableObject.CreateInstance<ActorProfile>();
            actorProfile.name = "QA P3M4B2B Actor Profile";
            created.Add(actorProfile);
            SetString(actorProfile, "actorProfileId", "qa.p3m4b2b.actor");
            SetObject(actorProfile, "logicalActorHostPrefab", actorRoot);

            SceneLogicalPlayerActorEvidence evidence =
                actorRoot.AddComponent<SceneLogicalPlayerActorEvidence>();
            evidence.EditorSetEvidence(actorProfile, actorRoot, "qa-p3m4b2b-evidence");

            GameObject admissionRoot = NewSceneObject("QA_P3M4B2B_Admission", scene);
            SceneLocalPlayerAdmissionAuthoring authoring =
                admissionRoot.AddComponent<SceneLocalPlayerAdmissionAuthoring>();
            SetObject(authoring, "playerSlotProfile", slot);
            SetObject(authoring, "localPlayerHost", host);
            SetObject(authoring, "actorProfile", actorProfile);
            SetObject(authoring, "sceneLogicalPlayerActor", actor);
            SetEnum(authoring, "admissionTiming",
                SceneLocalPlayerAdmissionTiming.OnActivityEnter);

            return new Fixture(host, actor, actorProfile, authoring);
        }

        private static PlayerSlotProfile CreateSlotProfile(
            ICollection<UnityEngine.Object> created)
        {
            var profile = ScriptableObject.CreateInstance<PlayerSlotProfile>();
            profile.name = "QA P3M4B2B Slot";
            created.Add(profile);
            SetString(profile, "playerSlotId", "qa.p3m4b2b.slot");
            return profile;
        }

        private static PlayerParticipationSnapshot CreateParticipationSnapshot(
            object context)
        {
            MethodInfo method = context.GetType().GetMethod("CreateSnapshot", InstanceAny);
            return (PlayerParticipationSnapshot)method.Invoke(context, null);
        }

        private static GameObject NewObject(
            string name,
            ICollection<UnityEngine.Object> created)
        {
            var value = new GameObject(name);
            created.Add(value);
            return value;
        }

        private static GameObject NewSceneObject(string name, Scene scene)
        {
            var value = new GameObject(name);
            SceneManager.MoveGameObjectToScene(value, scene);
            return value;
        }

        private static void SetObject(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            AssertNotNull(property,
                $"Missing object property '{propertyName}' on '{target.GetType().Name}'.");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(
            UnityEngine.Object target,
            string propertyName,
            string value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            AssertNotNull(property,
                $"Missing string property '{propertyName}' on '{target.GetType().Name}'.");
            property.stringValue = value ?? string.Empty;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEnum<TEnum>(
            UnityEngine.Object target,
            string propertyName,
            TEnum value)
            where TEnum : struct, Enum
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            AssertNotNull(property,
                $"Missing enum property '{propertyName}' on '{target.GetType().Name}'.");
            property.intValue = Convert.ToInt32(value);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectArray<T>(
            UnityEngine.Object target,
            string propertyName,
            IReadOnlyList<T> values)
            where T : UnityEngine.Object
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            AssertNotNull(property,
                $"Missing array property '{propertyName}' on '{target.GetType().Name}'.");
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, InstanceAny);
            AssertNotNull(field,
                $"Missing field '{fieldName}' on '{target.GetType().Name}'.");
            field.SetValue(target, value);
        }

        private static Exception Unwrap(Exception exception)
        {
            return exception is TargetInvocationException invocation &&
                   invocation.InnerException != null
                ? invocation.InnerException
                : exception;
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

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    $"{message} expected='{expected}' actual='{actual}'.");
            }
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }

        private sealed class Fixture
        {
            internal Fixture(
                LocalPlayerHostAuthoring host,
                PlayerActorDeclaration actor,
                ActorProfile actorProfile,
                SceneLocalPlayerAdmissionAuthoring authoring)
            {
                Host = host;
                Actor = actor;
                ActorProfile = actorProfile;
                Authoring = authoring;
            }

            internal LocalPlayerHostAuthoring Host { get; }
            internal PlayerActorDeclaration Actor { get; }
            internal ActorProfile ActorProfile { get; }
            internal SceneLocalPlayerAdmissionAuthoring Authoring { get; }
        }
    }
}
