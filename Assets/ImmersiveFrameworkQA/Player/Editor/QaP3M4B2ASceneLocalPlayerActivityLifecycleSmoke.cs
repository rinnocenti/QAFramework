using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.RuntimeContent;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Player.Editor
{
    internal static class QaP3M4B2ASceneLocalPlayerActivityLifecycleSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3M4B2A Scene Local Player Activity Lifecycle Smoke";

        private const BindingFlags InstanceAny =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags StaticNonPublic =
            BindingFlags.Static | BindingFlags.NonPublic;

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        [MenuItem(MenuPath)]
        internal static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError(
                    "[P3M4B2A_SCENE_LOCAL_PLAYER_ACTIVITY_LIFECYCLE_SMOKE] " +
                    "status='RejectedPlayMode' message='This smoke is Editor-only. Exit Play Mode and run it again from the QA menu.'.");
                return;
            }

            var completed = new List<string>();
            var created = new List<UnityEngine.Object>();
            Scene activityScene = default;
            string temporaryScenePath = $"Assets/QA_P3M4B2A_Temp_{Guid.NewGuid():N}.unity";
            Scene previousActiveScene = SceneManager.GetActiveScene();
            Exception failure = null;

            try
            {
                activityScene = EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Additive);
                AssertTrue(
                    EditorSceneManager.SaveScene(activityScene, temporaryScenePath, false),
                    $"Could not save the temporary Activity scene '{temporaryScenePath}'.");
                AssertEqual(
                    temporaryScenePath,
                    activityScene.path.Replace('\\', '/'),
                    "Temporary Activity scene path was not materialized as expected.");

                PlayerSlotProfile slot1 = CreateSlotProfile(
                    "QA P3M4B2A Slot 1",
                    "qa.p3m4b2a.slot.1",
                    created);
                PlayerSlotProfile slot2 = CreateSlotProfile(
                    "QA P3M4B2A Slot 2",
                    "qa.p3m4b2a.slot.2",
                    created);
                var orderedSlots = new[] { slot1, slot2 };

                object participationContext = CreateParticipationContext(
                    orderedSlots,
                    created);

                // Create in reverse hierarchy order. Runtime must still admit in configured Slot order.
                Fixture fixture2 = CreateFixture(
                    "Player2",
                    slot2,
                    activityScene,
                    created);
                Fixture fixture1 = CreateFixture(
                    "Player1",
                    slot1,
                    activityScene,
                    created);

                object runtimeHost;
                object sceneModule = CreateSceneAdmissionModule(
                    participationContext,
                    created,
                    out runtimeHost);
                IActivityContentExecutionParticipant composite = CreateCompositeParticipant(
                    runtimeHost,
                    participationContext,
                    sceneModule,
                    created);

                ActivityAsset selectedActorsActivity = CreateActivity(
                    "QA P3M4B2A Selected Actors Activity",
                    orderedSlots,
                    temporaryScenePath,
                    PlayerParticipationRequirementLevel.SelectedActors,
                    created);
                AssertAutomaticAuthoringResolved(
                    sceneModule,
                    selectedActorsActivity,
                    fixture1.Authoring,
                    fixture2.Authoring);
                RuntimeContentOwner owner = RuntimeContentOwner.Activity(
                    "qa.p3m4b2a.activity.owner",
                    selectedActorsActivity.ActivityName);
                RuntimeScopeContext scope = new RuntimeScopeContext(
                    owner,
                    nameof(QaP3M4B2ASceneLocalPlayerActivityLifecycleSmoke),
                    "qa-p3m4b2a");
                ActivityContentExecutionParticipantDescriptor descriptor =
                    composite.GetActivityContentExecutionDescriptor();
                AssertTrue(descriptor.IsValid, "Composite participant returned an invalid descriptor.");
                AssertEqual(
                    "framework.player-actor.activity-lifecycle",
                    descriptor.ContentId.StableText,
                    "Composite participant changed the canonical Player lifecycle content id.");
                completed.Add("canonical-participant-identity-preserved");

                ActivityContentExecutionRequest enterRequest =
                    ActivityContentExecutionRequest.Enter(
                        selectedActorsActivity,
                        null,
                        scope,
                        descriptor.ContentId,
                        descriptor.Requiredness,
                        "QaP3M4B2A",
                        "selected-actors-enter");
                ActivityContentExecutionResult enter =
                    composite.ExecuteActivityContent(enterRequest);
                AssertTrue(enter.Succeeded, enter.ToDiagnosticString());

                PlayerParticipationSnapshot entered = CreateSnapshot(participationContext);
                AssertEqual(2, entered.JoinedCount,
                    "Composite Activity enter did not admit both Scene Local Players.");
                AssertEqual(0, entered.ReservedCount,
                    "Composite Activity enter stranded a Reserved Slot.");
                AssertTrue(fixture1.Host.IsJoined && fixture2.Host.IsJoined,
                    "Composite Activity enter did not commit both Host admissions.");
                completed.Add("ordered-two-player-enter");

                AssertEqual(slot1.PlayerSlotId, entered.Slots[0].PlayerSlotId,
                    "Configured Slot order changed during Scene Local Player admission.");
                AssertEqual(slot2.PlayerSlotId, entered.Slots[1].PlayerSlotId,
                    "Configured Slot order changed during Scene Local Player admission.");
                AssertEqual(2, entered.SelectedActorCount,
                    "Composite Activity enter did not select both authored Actor Profiles.");
                AssertEqual(fixture1.ActorProfile, entered.Slots[0].SelectedActorProfile,
                    "Slot 1 selected a different Actor Profile.");
                AssertEqual(fixture2.ActorProfile, entered.Slots[1].SelectedActorProfile,
                    "Slot 2 selected a different Actor Profile.");
                completed.Add("actor-selection-committed-before-canonical-enter");

                AssertFalse(entered.JoiningOpen,
                    "Scene-authorized Activity admission changed public joining policy.");
                completed.Add("joining-closed-preserved");

                ActivityContentExecutionResult idempotentEnter =
                    composite.ExecuteActivityContent(enterRequest);
                AssertTrue(idempotentEnter.Succeeded, idempotentEnter.ToDiagnosticString());
                PlayerParticipationSnapshot afterIdempotentEnter =
                    CreateSnapshot(participationContext);
                AssertEqual(2, afterIdempotentEnter.JoinedCount,
                    "Idempotent Activity enter duplicated or removed admission.");
                AssertEqual(2, afterIdempotentEnter.SelectedActorCount,
                    "Idempotent Activity enter changed Actor selections.");
                completed.Add("idempotent-enter");

                bool host1Active = fixture1.Host.gameObject.activeSelf;
                bool host2Active = fixture2.Host.gameObject.activeSelf;
                bool actor1Active = fixture1.Actor.gameObject.activeSelf;
                bool actor2Active = fixture2.Actor.gameObject.activeSelf;

                ActivityContentExecutionRequest exitRequest =
                    ActivityContentExecutionRequest.Exit(
                        selectedActorsActivity,
                        null,
                        scope,
                        descriptor.ContentId,
                        descriptor.Requiredness,
                        "QaP3M4B2A",
                        "selected-actors-exit");
                ActivityContentExecutionResult exit =
                    composite.ExecuteActivityContent(exitRequest);
                AssertTrue(exit.Succeeded, exit.ToDiagnosticString());

                PlayerParticipationSnapshot exited = CreateSnapshot(participationContext);
                AssertEqual(0, exited.SelectedActorCount,
                    "Composite Activity exit retained Activity-owned Actor selections.");
                completed.Add("exit-clears-selection-after-canonical-exit");

                AssertEqual(0, exited.JoinedCount,
                    "Composite Activity exit retained Joined Scene Local Player Slots.");
                AssertEqual(0, exited.LeavingCount,
                    "Composite Activity exit stranded a Leaving Slot.");
                AssertEqual(2, exited.AvailableCount,
                    "Composite Activity exit did not restore Slot availability.");
                AssertFalse(fixture1.Host.IsJoined || fixture2.Host.IsJoined,
                    "Composite Activity exit retained Host admission evidence.");
                completed.Add("exit-releases-admission");

                AssertNotNull(fixture1.Host, "Scene-owned Host 1 was destroyed.");
                AssertNotNull(fixture2.Host, "Scene-owned Host 2 was destroyed.");
                AssertNotNull(fixture1.Actor, "Scene-owned Actor 1 was destroyed.");
                AssertNotNull(fixture2.Actor, "Scene-owned Actor 2 was destroyed.");
                AssertEqual(host1Active, fixture1.Host.gameObject.activeSelf,
                    "Activity lifecycle changed Host 1 active state.");
                AssertEqual(host2Active, fixture2.Host.gameObject.activeSelf,
                    "Activity lifecycle changed Host 2 active state.");
                AssertEqual(actor1Active, fixture1.Actor.gameObject.activeSelf,
                    "Activity lifecycle changed Actor 1 active state.");
                AssertEqual(actor2Active, fixture2.Actor.gameObject.activeSelf,
                    "Activity lifecycle changed Actor 2 active state.");
                completed.Add("external-hosts-and-actors-preserved");

                ActivityContentExecutionResult idempotentExit =
                    composite.ExecuteActivityContent(exitRequest);
                AssertTrue(idempotentExit.Succeeded, idempotentExit.ToDiagnosticString());
                completed.Add("idempotent-exit");

                ActivityAsset gameplayReadyActivity = CreateActivity(
                    "QA P3M4B2A Gameplay Ready Activity",
                    orderedSlots,
                    temporaryScenePath,
                    PlayerParticipationRequirementLevel.GameplayReady,
                    created);
                RuntimeContentOwner gameplayOwner = RuntimeContentOwner.Activity(
                    "qa.p3m4b2a.gameplay.owner",
                    gameplayReadyActivity.ActivityName);
                RuntimeScopeContext gameplayScope = new RuntimeScopeContext(
                    gameplayOwner,
                    "QaP3M4B2A",
                    "gameplay-ready-explicit-rejection");
                ActivityContentExecutionRequest gameplayEnter =
                    ActivityContentExecutionRequest.Enter(
                        gameplayReadyActivity,
                        selectedActorsActivity,
                        gameplayScope,
                        descriptor.ContentId,
                        descriptor.Requiredness,
                        "QaP3M4B2A",
                        "gameplay-ready-explicit-rejection");
                ActivityContentExecutionResult rejected =
                    composite.ExecuteActivityContent(gameplayEnter);
                AssertTrue(rejected.Failed && rejected.HasBlockingIssues,
                    "P3M4B2A silently accepted a requirement that needs external Actor adoption.");
                PlayerParticipationSnapshot afterRejection =
                    CreateSnapshot(participationContext);
                AssertEqual(0, afterRejection.JoinedCount,
                    "Rejected GameplayReady enter changed Slot admission.");
                AssertEqual(0, afterRejection.SelectedActorCount,
                    "Rejected GameplayReady enter changed Actor selection.");
                completed.Add("actor-adoption-requirement-explicitly-rejected");

                AssertFalse(fixture1.Authoring.HasActiveAdmission ||
                            fixture2.Authoring.HasActiveAdmission,
                    "Lifecycle completion retained an active Scene admission token.");
                completed.Add("no-retained-admission-token");

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

                if (!string.IsNullOrEmpty(temporaryScenePath) &&
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(temporaryScenePath) != null &&
                    !AssetDatabase.DeleteAsset(temporaryScenePath))
                {
                    throw new InvalidOperationException(
                        $"Could not delete temporary Activity scene asset '{temporaryScenePath}'.");
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
                Exception actualCleanup = Unwrap(cleanupException);
                failure = failure == null
                    ? new InvalidOperationException(
                        $"P3M4B2A smoke cleanup failed. {actualCleanup.Message}",
                        actualCleanup)
                    : new AggregateException(
                        "P3M4B2A smoke execution and cleanup both failed.",
                        failure,
                        actualCleanup);
            }

            if (failure != null)
            {
                Debug.LogError(
                    "[P3M4B2A_SCENE_LOCAL_PLAYER_ACTIVITY_LIFECYCLE_SMOKE] " +
                    $"status='Failed' exception='{failure.GetType().Name}' " +
                    $"message='{Escape(failure.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw failure;
            }

            Debug.Log(
                "[P3M4B2A_SCENE_LOCAL_PLAYER_ACTIVITY_LIFECYCLE_SMOKE] " +
                $"status='Passed' cases='{completed.Count}' " +
                $"completed='{string.Join(",", completed)}'.");
        }

        private static Exception Unwrap(Exception exception)
        {
            return exception is TargetInvocationException invocation &&
                   invocation.InnerException != null
                ? invocation.InnerException
                : exception;
        }

        private static object CreateParticipationContext(
            IReadOnlyList<PlayerSlotProfile> orderedSlots,
            ICollection<UnityEngine.Object> created)
        {
            var policy = ScriptableObject.CreateInstance<PlayerActorSelectionPolicyProfile>();
            policy.name = "QA P3M4B2A Actor Selection Policy";
            created.Add(policy);

            Type contextType = typeof(PlayerParticipationSnapshot).Assembly.GetType(
                "Immersive.Framework.PlayerParticipation.PlayerParticipationRuntimeContext",
                throwOnError: true);
            MethodInfo create = contextType.GetMethod(
                "TryCreateWithActorSelectionPolicy",
                StaticNonPublic);
            AssertNotNull(create, "Missing PlayerParticipationRuntimeContext factory.");
            object[] arguments =
            {
                orderedSlots,
                orderedSlots.Count,
                false,
                policy,
                "QaP3M4B2A",
                "initialize-activity-lifecycle-context",
                null
            };
            var result = (PlayerParticipationOperationResult)create.Invoke(null, arguments);
            AssertTrue(result != null && result.Succeeded,
                result != null ? result.ToDiagnosticString() : "No participation result.");
            AssertNotNull(arguments[6], "Participation context factory returned no context.");
            return arguments[6];
        }

        private static object CreateSceneAdmissionModule(
            object participationContext,
            ICollection<UnityEngine.Object> created,
            out object runtimeHost)
        {
            Assembly assembly = typeof(PlayerParticipationSnapshot).Assembly;
            Type hostType = assembly.GetType(
                "Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost",
                throwOnError: true);
            Type moduleType = assembly.GetType(
                "Immersive.Framework.PlayerParticipation.SceneLocalPlayerAdmissionRuntimeHostModule",
                throwOnError: true);

            GameObject runtimeRoot = NewObject("QA P3M4B2A Runtime Host", created);
            runtimeHost = runtimeRoot.AddComponent(hostType);
            object module = runtimeRoot.AddComponent(moduleType);
            MethodInfo initialize = moduleType.GetMethod("TryInitialize", InstanceAny);
            AssertNotNull(initialize, "Missing Scene admission module initialization operation.");
            object[] arguments = { runtimeHost, participationContext, null };
            bool initialized = (bool)initialize.Invoke(module, arguments);
            AssertTrue(initialized, arguments[2] as string ?? "Scene admission module failed.");
            return module;
        }

        private static IActivityContentExecutionParticipant CreateCompositeParticipant(
            object runtimeHost,
            object participationContext,
            object sceneModule,
            ICollection<UnityEngine.Object> created)
        {
            Assembly assembly = typeof(PlayerParticipationSnapshot).Assembly;
            Type preparationType = assembly.GetType(
                "Immersive.Framework.PlayerParticipation.PlayerActorPreparationRuntimeHostModule",
                throwOnError: true);
            Type canonicalType = assembly.GetType(
                "Immersive.Framework.PlayerParticipation.ActivityPlayerActorLifecycleParticipant",
                throwOnError: true);
            Type compositeType = assembly.GetType(
                "Immersive.Framework.PlayerParticipation.SceneLocalPlayerAdmissionCompositeLifecycleParticipant",
                throwOnError: true);

            var runtimeHostComponent = (Component)runtimeHost;
            object preparation = runtimeHostComponent.gameObject.AddComponent(preparationType);
            object canonical = Activator.CreateInstance(
                canonicalType,
                InstanceAny,
                binder: null,
                args: new[] { preparation, participationContext },
                culture: null);
            object composite = Activator.CreateInstance(
                compositeType,
                InstanceAny,
                binder: null,
                args: new[] { canonical, sceneModule },
                culture: null);
            AssertNotNull(composite, "Composite Scene Local Player participant could not be created.");
            AssertTrue(composite is IActivityContentExecutionParticipant,
                "Composite participant does not implement the canonical execution contract.");
            return (IActivityContentExecutionParticipant)composite;
        }

        private static void AssertAutomaticAuthoringResolved(
            object sceneModule,
            ActivityAsset activity,
            SceneLocalPlayerAdmissionAuthoring firstExpected,
            SceneLocalPlayerAdmissionAuthoring secondExpected)
        {
            AssertNotNull(sceneModule, "Scene admission module is missing.");
            MethodInfo resolve = sceneModule.GetType().GetMethod(
                "TryResolveAutomaticActivityAuthoring",
                InstanceAny);
            AssertNotNull(resolve,
                "Missing automatic Scene Local Player authoring resolution operation.");

            object[] arguments = { activity, null, null };
            bool succeeded = (bool)resolve.Invoke(sceneModule, arguments);
            string issue = arguments[2] as string ?? string.Empty;
            AssertTrue(succeeded,
                string.IsNullOrEmpty(issue)
                    ? "Automatic Scene Local Player authoring resolution failed without diagnostics."
                    : issue);

            var resolved = arguments[1] as IReadOnlyList<SceneLocalPlayerAdmissionAuthoring>;
            AssertNotNull(resolved,
                "Automatic Scene Local Player authoring resolution returned no collection.");
            AssertEqual(2, resolved.Count,
                "Automatic Scene Local Player authoring resolution did not find both Activity surfaces.");
            AssertTrue(ReferenceEquals(firstExpected, resolved[0]),
                "Automatic Scene Local Player authoring resolution did not preserve configured Slot 1 order.");
            AssertTrue(ReferenceEquals(secondExpected, resolved[1]),
                "Automatic Scene Local Player authoring resolution did not preserve configured Slot 2 order.");
        }

        private static ActivityAsset CreateActivity(
            string name,
            IReadOnlyList<PlayerSlotProfile> slots,
            string scenePath,
            PlayerParticipationRequirementLevel requirementLevel,
            ICollection<UnityEngine.Object> created)
        {
            var projection = ScriptableObject.CreateInstance<ActivityParticipationProjectionProfile>();
            projection.name = name + " Projection";
            created.Add(projection);
            SetEnum(projection, "projectionMode", ActivityParticipationProjectionMode.ExplicitSlots);
            SetEnum(projection, "zeroParticipantPolicy", ActivityParticipationZeroParticipantPolicy.Rejected);
            SetObjectArray(projection, "explicitSlotProfiles", slots);

            var requirements = ScriptableObject.CreateInstance<PlayerParticipationRequirementsProfile>();
            requirements.name = name + " Requirements";
            created.Add(requirements);
            SetEnum(requirements, "requirementLevel", requirementLevel);

            var contentProfile = ScriptableObject.CreateInstance<ActivityContentProfileAsset>();
            contentProfile.name = name + " Content";
            created.Add(contentProfile);
            var sceneEntry = new ActivityContentSceneEntry();
            string normalizedScenePath = (scenePath ?? string.Empty).Replace('\\', '/');
            SetField(sceneEntry, "contentId", "qa.p3m4b2a.scene");
            SetField(sceneEntry, "scenePath", normalizedScenePath);
            SetField(
                sceneEntry,
                "sceneName",
                Path.GetFileNameWithoutExtension(normalizedScenePath));
            SetField(contentProfile, "scenes", new[] { sceneEntry });

            var activity = ScriptableObject.CreateInstance<ActivityAsset>();
            activity.name = name;
            created.Add(activity);
            SetObject(activity, "playerParticipationProjectionProfile", projection);
            SetObject(activity, "playerParticipationRequirementsProfile", requirements);
            SetObject(activity, "activityContentProfile", contentProfile);
            return activity;
        }

        private static Fixture CreateFixture(
            string suffix,
            PlayerSlotProfile slotProfile,
            Scene scene,
            ICollection<UnityEngine.Object> created)
        {
            GameObject hostRoot = NewSceneObject(
                $"QA_P3M4B2A_{suffix}_Host",
                scene);
            PlayerInput input = hostRoot.AddComponent<PlayerInput>();
            LocalPlayerHostAuthoring host = hostRoot.AddComponent<LocalPlayerHostAuthoring>();
            GameObject actorMount = NewSceneObject("ActorMount", scene);
            actorMount.transform.SetParent(hostRoot.transform, false);
            SetObject(host, "playerInput", input);
            SetObject(host, "actorMount", actorMount.transform);

            GameObject actorRoot = NewSceneObject(
                $"QA_P3M4B2A_{suffix}_Actor",
                scene);
            actorRoot.transform.SetParent(actorMount.transform, false);
            PlayerActorDeclaration actor = actorRoot.AddComponent<PlayerActorDeclaration>();

            var actorProfile = ScriptableObject.CreateInstance<ActorProfile>();
            actorProfile.name = $"QA P3M4B2A {suffix} Actor Profile";
            created.Add(actorProfile);
            SetString(actorProfile, "actorProfileId",
                $"qa.p3m4b2a.{suffix.ToLowerInvariant()}.actor");
            SetObject(actorProfile, "logicalActorHostPrefab", actorRoot);

            SceneLogicalPlayerActorEvidence evidence =
                actorRoot.AddComponent<SceneLogicalPlayerActorEvidence>();
            evidence.EditorSetEvidence(actorProfile, actorRoot, "qa-p3m4b2a-evidence");

            GameObject admissionRoot = NewSceneObject(
                $"QA_P3M4B2A_{suffix}_Admission",
                scene);
            SceneLocalPlayerAdmissionAuthoring authoring =
                admissionRoot.AddComponent<SceneLocalPlayerAdmissionAuthoring>();
            SetObject(authoring, "playerSlotProfile", slotProfile);
            SetObject(authoring, "localPlayerHost", host);
            SetObject(authoring, "actorProfile", actorProfile);
            SetObject(authoring, "sceneLogicalPlayerActor", actor);
            SetEnum(authoring, "admissionTiming", SceneLocalPlayerAdmissionTiming.OnActivityEnter);

            return new Fixture(host, actor, actorProfile, authoring);
        }

        private static PlayerSlotProfile CreateSlotProfile(
            string name,
            string id,
            ICollection<UnityEngine.Object> created)
        {
            var profile = ScriptableObject.CreateInstance<PlayerSlotProfile>();
            profile.name = name;
            created.Add(profile);
            SetString(profile, "playerSlotId", id);
            return profile;
        }

        private static PlayerParticipationSnapshot CreateSnapshot(object context)
        {
            MethodInfo method = context.GetType().GetMethod("CreateSnapshot", InstanceAny);
            AssertNotNull(method, "Missing Player participation snapshot operation.");
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
