using System;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.Player.Editor
{
    internal static class QaP3M4B1SceneLocalPlayerAdmissionTransactionSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3M4B1 Scene Local Player Admission Transaction Smoke";

        private const BindingFlags NonPublicStatic =
            BindingFlags.NonPublic | BindingFlags.Static;
        private const BindingFlags NonPublicInstance =
            BindingFlags.NonPublic | BindingFlags.Instance;

        internal static void Run()
        {
            var completed = new List<string>();
            var created = new List<UnityEngine.Object>();
            try
            {
                PlayerSlotProfile slot1 = CreateSlotProfile(
                    "QA P3M4B1 Slot 1",
                    "qa.p3m4b1.slot.1",
                    created);
                PlayerSlotProfile slot2 = CreateSlotProfile(
                    "QA P3M4B1 Slot 2",
                    "qa.p3m4b1.slot.2",
                    created);
                var orderedSlots = new[] { slot1, slot2 };

                object mismatchContext = CreateParticipationContext(orderedSlots, created);
                object mismatchRuntime = CreateAdmissionRuntime(mismatchContext);
                Fixture mismatchFixture = CreateFixture(
                    "Mismatch",
                    slot2,
                    created);
                SceneLocalPlayerAdmissionRuntimeResult mismatch = InvokeAdmit(
                    mismatchRuntime,
                    mismatchFixture.Authoring,
                    "QaP3M4B1",
                    "ordered-slot-mismatch");
                AssertEqual(
                    SceneLocalPlayerAdmissionRuntimeStatus.RejectedSlotOrderMismatch,
                    mismatch.Status,
                    mismatch.ToDiagnosticString());
                PlayerParticipationSnapshot mismatchSnapshot = CreateSnapshot(mismatchContext);
                AssertEqual(2, mismatchSnapshot.AvailableCount, "Rejected exact Slot request stranded capacity.");
                AssertEqual(0, mismatchSnapshot.ReservedCount, "Rejected exact Slot request stranded a reservation.");
                AssertFalse(mismatchFixture.Host.IsJoined, "Rejected exact Slot request joined its Host.");
                completed.Add("exact-ordered-slot-enforced");

                object context = CreateParticipationContext(orderedSlots, created);
                PlayerParticipationSnapshot initialSnapshot = CreateSnapshot(context);
                AssertFalse(initialSnapshot.JoiningOpen, "QA context unexpectedly initialized with public joining open.");
                object runtime = CreateAdmissionRuntime(context);
                Fixture fixture = CreateFixture(
                    "Nominal",
                    slot1,
                    created);
                bool hostActiveBefore = fixture.Host.gameObject.activeSelf;
                bool actorActiveBefore = fixture.Actor.gameObject.activeSelf;

                SceneLocalPlayerAdmissionRuntimeResult admitted = InvokeAdmit(
                    runtime,
                    fixture.Authoring,
                    "QaP3M4B1",
                    "scene-authorized-admission");
                AssertEqual(
                    SceneLocalPlayerAdmissionRuntimeStatus.SucceededAdmitted,
                    admitted.Status,
                    admitted.ToDiagnosticString());
                AssertTrue(admitted.Token.IsValid, "Successful Scene admission returned no typed token.");
                completed.Add("scene-authorized-admission-with-joining-closed");

                AssertTrue(fixture.Host.IsJoined, "Successful Scene admission did not commit Host evidence.");
                AssertEqual(
                    slot1.PlayerSlotId,
                    fixture.Host.JoinedPlayerSlotId,
                    "Host committed a different Player Slot identity.");
                PlayerParticipationSnapshot joinedSnapshot = CreateSnapshot(context);
                AssertEqual(1, joinedSnapshot.JoinedCount, "Session did not commit one Joined Slot.");
                AssertEqual(0, joinedSnapshot.ReservedCount, "Committed Scene admission left a Reserved Slot.");
                completed.Add("host-slot-admission-committed");

                AssertEqual(0, joinedSnapshot.SelectedActorCount, "P3M4B1 unexpectedly selected an Actor.");
                AssertTrue(fixture.Actor.GetComponent<PlayerActorDeclaration>() != null,
                    "P3M4B1 changed the authored Logical Actor declaration.");
                completed.Add("actor-preparation-remains-out-of-scope");

                SceneLocalPlayerAdmissionRuntimeResult idempotent = InvokeAdmit(
                    runtime,
                    fixture.Authoring,
                    "QaP3M4B1",
                    "idempotent-readmission");
                AssertEqual(
                    SceneLocalPlayerAdmissionRuntimeStatus.SucceededAlreadyAdmitted,
                    idempotent.Status,
                    idempotent.ToDiagnosticString());
                AssertEqual(
                    admitted.Token,
                    idempotent.Token,
                    "Idempotent admission changed the typed admission token.");
                completed.Add("idempotent-readmission");

                SceneLocalPlayerAdmissionRuntimeResult foreignRelease = InvokeRelease(
                    runtime,
                    fixture.Authoring,
                    default,
                    "QaP3M4B1",
                    "foreign-token");
                AssertEqual(
                    SceneLocalPlayerAdmissionRuntimeStatus.RejectedForeignOrStaleToken,
                    foreignRelease.Status,
                    foreignRelease.ToDiagnosticString());
                AssertTrue(fixture.Host.IsJoined, "Foreign token rejection changed Host admission.");
                AssertEqual(1, CreateSnapshot(context).JoinedCount,
                    "Foreign token rejection changed Session Slot admission.");
                completed.Add("foreign-token-rejected");

                SceneLocalPlayerAdmissionRuntimeResult released = InvokeRelease(
                    runtime,
                    fixture.Authoring,
                    admitted.Token,
                    "QaP3M4B1",
                    "nominal-release");
                AssertEqual(
                    SceneLocalPlayerAdmissionRuntimeStatus.SucceededReleased,
                    released.Status,
                    released.ToDiagnosticString());
                PlayerParticipationSnapshot releasedSnapshot = CreateSnapshot(context);
                AssertEqual(0, releasedSnapshot.JoinedCount, "Release retained a Joined Slot.");
                AssertEqual(0, releasedSnapshot.LeavingCount, "Release stranded a Leaving Slot.");
                AssertEqual(2, releasedSnapshot.AvailableCount, "Release did not restore Slot availability.");
                AssertFalse(fixture.Host.IsJoined, "Release retained Host admission evidence.");
                completed.Add("release-returns-slot-available");

                AssertNotNull(fixture.Host, "Externally owned Host was destroyed by release.");
                AssertNotNull(fixture.Host.PlayerInput, "Externally owned PlayerInput was destroyed by release.");
                AssertEqual(hostActiveBefore, fixture.Host.gameObject.activeSelf,
                    "Release changed externally owned Host active state.");
                completed.Add("external-host-preserved");

                AssertNotNull(fixture.Actor, "Externally owned Logical Actor was destroyed by release.");
                AssertEqual(actorActiveBefore, fixture.Actor.gameObject.activeSelf,
                    "Release changed externally owned Logical Actor active state.");
                completed.Add("external-actor-preserved");

                SceneLocalPlayerAdmissionRuntimeResult alreadyReleased = InvokeRelease(
                    runtime,
                    fixture.Authoring,
                    default,
                    "QaP3M4B1",
                    "idempotent-release");
                AssertEqual(
                    SceneLocalPlayerAdmissionRuntimeStatus.SucceededAlreadyReleased,
                    alreadyReleased.Status,
                    alreadyReleased.ToDiagnosticString());
                completed.Add("idempotent-release");

                Debug.Log(
                    "[P3M4B1_SCENE_LOCAL_PLAYER_ADMISSION_TRANSACTION_SMOKE] " +
                    $"status='Passed' cases='{completed.Count}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Exception actual = exception is TargetInvocationException invocation &&
                    invocation.InnerException != null
                        ? invocation.InnerException
                        : exception;
                Debug.LogError(
                    "[P3M4B1_SCENE_LOCAL_PLAYER_ADMISSION_TRANSACTION_SMOKE] " +
                    $"status='Failed' exception='{actual.GetType().Name}' " +
                    $"message='{Escape(actual.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw actual;
            }
            finally
            {
                for (int index = created.Count - 1; index >= 0; index--)
                {
                    if (created[index] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(created[index]);
                    }
                }
            }
        }

        private static object CreateParticipationContext(
            IReadOnlyList<PlayerSlotProfile> orderedSlots,
            ICollection<UnityEngine.Object> created)
        {
            Assembly assembly = typeof(PlayerParticipationSnapshot).Assembly;
            Type contextType = assembly.GetType(
                "Immersive.Framework.PlayerParticipation.PlayerParticipationRuntimeContext",
                throwOnError: true);
            MethodInfo create = contextType.GetMethod(
                "TryCreateWithActorSelectionPolicy",
                NonPublicStatic);
            AssertNotNull(create, "Missing PlayerParticipationRuntimeContext factory.");
            object[] arguments =
            {
                orderedSlots,
                orderedSlots.Count,
                false,
                PlayerActorSelectionDuplicatePolicy.AllowDuplicates,
                "QaP3M4B1",
                "initialize-scene-admission-context",
                null
            };
            var result = (PlayerParticipationOperationResult)create.Invoke(null, arguments);
            AssertNotNull(result, "Player participation context factory returned no result.");
            AssertTrue(result.Succeeded, result.ToDiagnosticString());
            AssertNotNull(arguments[6], "Player participation context factory returned no context.");
            return arguments[6];
        }

        private static object CreateAdmissionRuntime(object participationContext)
        {
            Type runtimeType = typeof(SceneLocalPlayerAdmissionRuntimeResult).Assembly.GetType(
                "Immersive.Framework.PlayerParticipation.SceneLocalPlayerAdmissionRuntime",
                throwOnError: true);
            object runtime = Activator.CreateInstance(
                runtimeType,
                NonPublicInstance,
                binder: null,
                args: new[] { participationContext },
                culture: null);
            AssertNotNull(runtime, "Scene Local Player admission runtime could not be constructed.");
            return runtime;
        }

        private static SceneLocalPlayerAdmissionRuntimeResult InvokeAdmit(
            object runtime,
            SceneLocalPlayerAdmissionAuthoring authoring,
            string source,
            string reason)
        {
            MethodInfo method = runtime.GetType().GetMethod("TryAdmit", NonPublicInstance);
            AssertNotNull(method, "Missing Scene Local Player TryAdmit operation.");
            return (SceneLocalPlayerAdmissionRuntimeResult)method.Invoke(
                runtime,
                new object[] { authoring, source, reason });
        }

        private static SceneLocalPlayerAdmissionRuntimeResult InvokeRelease(
            object runtime,
            SceneLocalPlayerAdmissionAuthoring authoring,
            SceneLocalPlayerAdmissionToken token,
            string source,
            string reason)
        {
            MethodInfo method = runtime.GetType().GetMethod(
                "TryRelease",
                NonPublicInstance,
                binder: null,
                types: new[]
                {
                    typeof(SceneLocalPlayerAdmissionAuthoring),
                    typeof(SceneLocalPlayerAdmissionToken),
                    typeof(string),
                    typeof(string)
                },
                modifiers: null);
            AssertNotNull(method, "Missing Scene Local Player typed TryRelease operation.");
            return (SceneLocalPlayerAdmissionRuntimeResult)method.Invoke(
                runtime,
                new object[] { authoring, token, source, reason });
        }

        private static PlayerParticipationSnapshot CreateSnapshot(object context)
        {
            MethodInfo method = context.GetType().GetMethod("CreateSnapshot", NonPublicInstance);
            AssertNotNull(method, "Missing Player participation snapshot operation.");
            return (PlayerParticipationSnapshot)method.Invoke(context, null);
        }

        private static Fixture CreateFixture(
            string suffix,
            PlayerSlotProfile slotProfile,
            ICollection<UnityEngine.Object> created)
        {
            GameObject hostRoot = NewObject($"QA_P3M4B1_{suffix}_Host", created);
            PlayerInput input = hostRoot.AddComponent<PlayerInput>();
            LocalPlayerHostAuthoring host = hostRoot.AddComponent<LocalPlayerHostAuthoring>();
            GameObject actorMount = NewObject("ActorMount", created);
            actorMount.transform.SetParent(hostRoot.transform, false);
            SetObject(host, "playerInput", input);
            SetObject(host, "actorMount", actorMount.transform);

            GameObject actorRoot = NewObject($"QA_P3M4B1_{suffix}_Actor", created);
            actorRoot.transform.SetParent(actorMount.transform, false);
            PlayerActorDeclaration actor = actorRoot.AddComponent<PlayerActorDeclaration>();

            var actorProfile = ScriptableObject.CreateInstance<ActorProfile>();
            actorProfile.name = $"QA P3M4B1 {suffix} Actor Profile";
            created.Add(actorProfile);
            SetString(actorProfile, "actorProfileId", $"qa.p3m4b1.{suffix.ToLowerInvariant()}.actor");
            SetObject(actorProfile, "logicalActorHostPrefab", actorRoot);

            SceneLogicalPlayerActorEvidence evidence =
                actorRoot.AddComponent<SceneLogicalPlayerActorEvidence>();
            evidence.EditorSetEvidence(actorProfile, actorRoot, "qa-p3m4b1-evidence");

            GameObject admissionRoot = NewObject($"QA_P3M4B1_{suffix}_Admission", created);
            SceneLocalPlayerAdmissionAuthoring authoring =
                admissionRoot.AddComponent<SceneLocalPlayerAdmissionAuthoring>();
            SetObject(authoring, "playerSlotProfile", slotProfile);
            SetObject(authoring, "localPlayerHost", host);
            SetObject(authoring, "actorProfile", actorProfile);
            SetObject(authoring, "sceneLogicalPlayerActor", actor);

            return new Fixture(host, actor, authoring);
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

        private static GameObject NewObject(
            string name,
            ICollection<UnityEngine.Object> created)
        {
            var value = new GameObject(name);
            created.Add(value);
            return value;
        }

        private static void SetObject(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            AssertNotNull(property, $"Missing object property '{propertyName}' on '{target.GetType().Name}'.");
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
            AssertNotNull(property, $"Missing string property '{propertyName}' on '{target.GetType().Name}'.");
            property.stringValue = value ?? string.Empty;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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
                SceneLocalPlayerAdmissionAuthoring authoring)
            {
                Host = host;
                Actor = actor;
                Authoring = authoring;
            }

            internal LocalPlayerHostAuthoring Host { get; }
            internal PlayerActorDeclaration Actor { get; }
            internal SceneLocalPlayerAdmissionAuthoring Authoring { get; }
        }
    }
}
