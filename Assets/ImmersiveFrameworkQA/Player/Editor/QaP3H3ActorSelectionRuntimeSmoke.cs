using System;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player.Editor
{
    public static class QaP3H3ActorSelectionRuntimeSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3H.3 Run Actor Selection Runtime Smoke";
        private const string ContextTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerParticipationRuntimeContext";

        private static readonly BindingFlags StaticInternal = BindingFlags.Static | BindingFlags.NonPublic;
        private static readonly BindingFlags InstanceInternal = BindingFlags.Instance | BindingFlags.NonPublic;

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var completed = new List<string>();
            var createdObjects = new List<UnityEngine.Object>();

            try
            {
                ActorProfile actorA = CreateActorProfile(createdObjects, "Actor A", "qa.actor-profile.a");
                ActorProfile actorB = CreateActorProfile(createdObjects, "Actor B", "qa.actor-profile.b");
                ActorProfile actorAClone = CreateActorProfile(createdObjects, "Actor A Clone", "qa.actor-profile.a");
                ActorProfile invalidActor = CreateActorProfile(createdObjects, "Invalid Actor", string.Empty, false);

                PlayerSlotProfile slotOne = CreateSlotProfile(createdObjects, "Slot One", "qa.slot.1", actorB);
                PlayerSlotProfile slotTwo = CreateSlotProfile(createdObjects, "Slot Two", "qa.slot.2", null);
                PlayerSlotProfile slotThree = CreateSlotProfile(createdObjects, "Slot Three", "qa.slot.3", null);

                PlayerActorSelectionPolicyProfile uniquePolicy = CreatePolicy(
                    createdObjects,
                    "Unique",
                    PlayerActorSelectionDuplicatePolicy.UniqueAcrossJoinedSlots);
                PlayerActorSelectionPolicyProfile allowPolicy = CreatePolicy(
                    createdObjects,
                    "Allow",
                    PlayerActorSelectionDuplicatePolicy.AllowDuplicates);

                string actorABefore = EditorJsonUtility.ToJson(actorA);
                string actorBBefore = EditorJsonUtility.ToJson(actorB);
                string slotOneBefore = EditorJsonUtility.ToJson(slotOne);
                string uniqueBefore = EditorJsonUtility.ToJson(uniquePolicy);

                object context = CreateContextWithPolicy(
                    new[] { slotOne, slotTwo, slotThree },
                    uniquePolicy,
                    out PlayerParticipationOperationResult initialization);
                AssertStatus(initialization, PlayerParticipationOperationStatus.Succeeded, "Policy context initialization failed.");
                AssertNotNull(context, "Policy context was not created.");
                AssertSame(uniquePolicy, initialization.Snapshot.ActorSelectionPolicyProfile, "Policy was not preserved in snapshot.");
                completed.Add("selection-policy-context-initialized");

                JoinNext(context, slotOne);
                JoinNext(context, slotTwo);
                PlayerSlotRuntimeSnapshot joinedUnselected = GetSlot(context, slotOne.PlayerSlotId);
                AssertTrue(joinedUnselected.IsJoined && !joinedUnselected.HasSelectedActor, "Joined Slot did not remain valid while Unselected.");
                AssertEqual(0, joinedUnselected.SelectionRevision, "Join changed Actor selection revision.");
                completed.Add("joined-slot-may-remain-unselected");

                PlayerActorSelectionResult unjoinedRejected = Select(context, slotThree.PlayerSlotId, actorA, -1);
                AssertSelectionStatus(unjoinedRejected, PlayerActorSelectionStatus.RejectedSlotNotJoined, "Unjoined Slot accepted Actor selection.");
                completed.Add("unjoined-slot-selection-rejected");

                PlayerActorSelectionResult selected = Select(context, slotOne.PlayerSlotId, actorA, 0);
                AssertSelectionStatus(selected, PlayerActorSelectionStatus.SucceededSelected, "Initial Actor selection failed.");
                AssertTrue(selected.StateChanged, "Initial selection did not report a state change.");
                AssertSame(actorA, selected.SelectedActorProfile, "Initial selection returned the wrong ActorProfile.");
                completed.Add("joined-slot-selected");

                PlayerSlotRuntimeSnapshot selectedSnapshot = GetSlot(context, slotOne.PlayerSlotId);
                AssertSame(actorA, selectedSnapshot.SelectedActorProfile, "Slot snapshot did not preserve selected ActorProfile.");
                AssertEqual(actorA.ActorProfileId, selectedSnapshot.SelectedActorProfileId, "Slot snapshot did not expose selected ActorProfileId.");
                AssertEqual(1, selectedSnapshot.SelectionRevision, "Initial selection revision is incorrect.");
                AssertEqual("QA.P3H3", selectedSnapshot.SelectionSource, "Selection source was not preserved.");
                completed.Add("selection-snapshot-evidence");

                PlayerActorSelectionResult idempotent = Select(context, slotOne.PlayerSlotId, actorA, 1);
                AssertSelectionStatus(idempotent, PlayerActorSelectionStatus.SucceededSelected, "Idempotent selection failed.");
                AssertTrue(!idempotent.StateChanged, "Idempotent selection changed state.");
                AssertEqual(1, idempotent.SelectionRevision, "Idempotent selection incremented revision.");
                completed.Add("selection-idempotent");

                PlayerActorSelectionResult implicitReplace = Select(context, slotOne.PlayerSlotId, actorB, 1);
                AssertSelectionStatus(implicitReplace, PlayerActorSelectionStatus.RejectedInvalidRequest, "Select silently replaced an existing Actor.");
                AssertSame(actorA, GetSlot(context, slotOne.PlayerSlotId).SelectedActorProfile, "Rejected select mutated the previous Actor.");
                completed.Add("replacement-must-be-explicit");

                PlayerActorSelectionResult replaced = Replace(context, slotOne.PlayerSlotId, actorB, 1);
                AssertSelectionStatus(replaced, PlayerActorSelectionStatus.SucceededReplaced, "Explicit replacement failed.");
                AssertSame(actorA, replaced.PreviousActorProfile, "Replacement lost previous Actor evidence.");
                AssertSame(actorB, replaced.SelectedActorProfile, "Replacement lost current Actor evidence.");
                AssertEqual(2, replaced.SelectionRevision, "Replacement revision is incorrect.");
                completed.Add("selection-replaced-with-evidence");

                PlayerActorSelectionResult stale = Replace(context, slotOne.PlayerSlotId, actorA, 1);
                AssertSelectionStatus(stale, PlayerActorSelectionStatus.RejectedStaleSelectionRevision, "Stale selection revision was accepted.");
                AssertSame(actorB, GetSlot(context, slotOne.PlayerSlotId).SelectedActorProfile, "Stale request mutated selection.");
                completed.Add("stale-selection-revision-rejected");

                PlayerActorSelectionResult secondSelected = Select(context, slotTwo.PlayerSlotId, actorAClone, 0);
                AssertSelectionStatus(secondSelected, PlayerActorSelectionStatus.SucceededSelected, "Second Slot initial selection failed.");
                PlayerActorSelectionResult duplicateRejected = Replace(context, slotOne.PlayerSlotId, actorA, 2);
                AssertSelectionStatus(duplicateRejected, PlayerActorSelectionStatus.RejectedDuplicateActorSelection, "Unique policy accepted duplicate ActorProfileId.");
                AssertEqual(slotTwo.PlayerSlotId, duplicateRejected.ConflictingPlayerSlotId, "Duplicate rejection did not identify the conflicting Slot.");
                AssertSame(actorB, GetSlot(context, slotOne.PlayerSlotId).SelectedActorProfile, "Duplicate rejection was not atomic.");
                completed.Add("unique-policy-rejects-duplicate-id-atomically");

                PlayerActorSelectionResult cleared = Clear(context, slotOne.PlayerSlotId, 2);
                AssertSelectionStatus(cleared, PlayerActorSelectionStatus.SucceededCleared, "Clear selection failed.");
                AssertTrue(cleared.StateChanged, "Clear did not report a state change.");
                PlayerSlotRuntimeSnapshot clearedSnapshot = GetSlot(context, slotOne.PlayerSlotId);
                AssertTrue(clearedSnapshot.IsJoined && !clearedSnapshot.HasSelectedActor, "Clear changed join state or retained Actor selection.");
                AssertEqual(3, clearedSnapshot.SelectionRevision, "Clear revision is incorrect.");
                completed.Add("clear-preserves-joined-slot");

                PlayerActorSelectionResult clearAgain = Clear(context, slotOne.PlayerSlotId, 3);
                AssertSelectionStatus(clearAgain, PlayerActorSelectionStatus.SucceededCleared, "Idempotent clear failed.");
                AssertTrue(!clearAgain.StateChanged, "Idempotent clear changed state.");
                completed.Add("clear-idempotent");

                PlayerActorSelectionResult defaultSelected = SelectDefault(context, slotOne.PlayerSlotId, 3);
                AssertSelectionStatus(defaultSelected, PlayerActorSelectionStatus.SucceededSelected, "Explicit default selection failed.");
                AssertSame(actorB, defaultSelected.SelectedActorProfile, "Default selection did not use PlayerSlotProfile.DefaultActorProfile.");
                completed.Add("default-selection-explicit");

                PlayerActorSelectionResult missingDefault = SelectDefault(context, slotTwo.PlayerSlotId, 1);
                AssertSelectionStatus(missingDefault, PlayerActorSelectionStatus.RejectedActorProfileMissing, "Missing default was silently accepted.");
                completed.Add("missing-default-rejected");

                PlayerActorSelectionResult invalidProfile = Replace(context, slotOne.PlayerSlotId, invalidActor, 4);
                AssertSelectionStatus(invalidProfile, PlayerActorSelectionStatus.RejectedActorProfileInvalid, "Invalid ActorProfile was accepted.");
                completed.Add("invalid-actor-profile-rejected");

                PlayerActorSelectionResult foreignSlot = Select(context, PlayerSlotId.From("qa.slot.foreign"), actorB, -1);
                AssertSelectionStatus(foreignSlot, PlayerActorSelectionStatus.RejectedSlotNotConfigured, "Foreign Slot selection was accepted.");
                completed.Add("foreign-slot-rejected");

                object noPolicyContext = CreateContextWithoutPolicy(
                    new[] { slotThree },
                    out PlayerParticipationOperationResult noPolicyInitialization);
                AssertStatus(noPolicyInitialization, PlayerParticipationOperationStatus.Succeeded, "Legacy context initialization failed.");
                JoinNext(noPolicyContext, slotThree);
                PlayerActorSelectionResult noPolicy = Select(noPolicyContext, slotThree.PlayerSlotId, actorB, 0);
                AssertSelectionStatus(noPolicy, PlayerActorSelectionStatus.RejectedPolicyMissing, "Context without policy silently allowed selection.");
                completed.Add("missing-policy-rejected");

                PlayerSlotProfile allowSlotOne = CreateSlotProfile(createdObjects, "Allow Slot One", "qa.allow.slot.1", null);
                PlayerSlotProfile allowSlotTwo = CreateSlotProfile(createdObjects, "Allow Slot Two", "qa.allow.slot.2", null);
                object allowContext = CreateContextWithPolicy(
                    new[] { allowSlotOne, allowSlotTwo },
                    allowPolicy,
                    out PlayerParticipationOperationResult allowInitialization);
                AssertStatus(allowInitialization, PlayerParticipationOperationStatus.Succeeded, "Allow-duplicates context initialization failed.");
                JoinNext(allowContext, allowSlotOne);
                JoinNext(allowContext, allowSlotTwo);
                AssertSelectionStatus(Select(allowContext, allowSlotOne.PlayerSlotId, actorA, 0), PlayerActorSelectionStatus.SucceededSelected, "First duplicate-allowed selection failed.");
                AssertSelectionStatus(Select(allowContext, allowSlotTwo.PlayerSlotId, actorAClone, 0), PlayerActorSelectionStatus.SucceededSelected, "AllowDuplicates rejected equal ActorProfileId.");
                completed.Add("allow-duplicates-policy-permits-equal-id");

                PlayerParticipationSnapshot finalSnapshot = replaced.Snapshot;
                AssertTrue(finalSnapshot.HasActorSelectionPolicy, "Selection result snapshot lost policy evidence.");
                AssertTrue(GetSnapshot(context).SelectedActorCount >= 1, "Session snapshot did not count selected Actors.");
                completed.Add("session-snapshot-selection-counts");

                AssertEqual(actorABefore, EditorJsonUtility.ToJson(actorA), "ActorProfile A was mutated by runtime selection.");
                AssertEqual(actorBBefore, EditorJsonUtility.ToJson(actorB), "ActorProfile B was mutated by runtime selection.");
                AssertEqual(slotOneBefore, EditorJsonUtility.ToJson(slotOne), "PlayerSlotProfile was mutated by runtime selection.");
                AssertEqual(uniqueBefore, EditorJsonUtility.ToJson(uniquePolicy), "Selection policy Profile was mutated by runtime selection.");
                completed.Add("profiles-remain-immutable");

                Debug.Log(
                    "[P3H3_ACTOR_SELECTION_RUNTIME_SMOKE] status='Passed' " +
                    $"cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3H3_ACTOR_SELECTION_RUNTIME_SMOKE] status='Failed' " +
                    $"exception='{exception.GetType().Name}' message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
            }
            finally
            {
                for (int index = createdObjects.Count - 1; index >= 0; index--)
                {
                    if (createdObjects[index] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(createdObjects[index]);
                    }
                }
            }
        }

        private static ActorProfile CreateActorProfile(
            ICollection<UnityEngine.Object> created,
            string displayName,
            string id,
            bool validHost = true)
        {
            var host = new GameObject(displayName + " Host");
            created.Add(host);
            var profile = ScriptableObject.CreateInstance<ActorProfile>();
            profile.name = displayName;
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("actorProfileId").stringValue = id;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("actorKind").intValue = (int)ActorKind.Player;
            serialized.FindProperty("actorRole").intValue = (int)ActorRole.Protagonist;
            serialized.FindProperty("logicalActorHostPrefab").objectReferenceValue = validHost ? host : null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            created.Add(profile);
            return profile;
        }

        private static PlayerSlotProfile CreateSlotProfile(
            ICollection<UnityEngine.Object> created,
            string displayName,
            string id,
            ActorProfile defaultActor)
        {
            var profile = ScriptableObject.CreateInstance<PlayerSlotProfile>();
            profile.name = displayName;
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("playerSlotId").stringValue = id;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("defaultActorProfile").objectReferenceValue = defaultActor;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            created.Add(profile);
            return profile;
        }

        private static PlayerActorSelectionPolicyProfile CreatePolicy(
            ICollection<UnityEngine.Object> created,
            string displayName,
            PlayerActorSelectionDuplicatePolicy duplicatePolicy)
        {
            var profile = ScriptableObject.CreateInstance<PlayerActorSelectionPolicyProfile>();
            profile.name = displayName;
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("duplicatePolicy").intValue = (int)duplicatePolicy;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            created.Add(profile);
            return profile;
        }

        private static object CreateContextWithPolicy(
            IReadOnlyList<PlayerSlotProfile> slots,
            PlayerActorSelectionPolicyProfile policy,
            out PlayerParticipationOperationResult result)
        {
            return CreateContext("TryCreateWithActorSelectionPolicy", new object[]
            {
                slots, slots.Count, true, policy, "QA.P3H3", "create-context", null
            }, 6, out result);
        }

        private static object CreateContextWithoutPolicy(
            IReadOnlyList<PlayerSlotProfile> slots,
            out PlayerParticipationOperationResult result)
        {
            return CreateContext("TryCreate", new object[]
            {
                slots, slots.Count, true, "QA.P3H3", "create-context", null
            }, 5, out result);
        }

        private static object CreateContext(
            string methodName,
            object[] arguments,
            int contextArgumentIndex,
            out PlayerParticipationOperationResult result)
        {
            Type contextType = ResolveContextType();
            MethodInfo method = contextType.GetMethod(methodName, StaticInternal);
            AssertNotNull(method, $"{methodName} was not found.");
            result = method.Invoke(null, arguments) as PlayerParticipationOperationResult;
            AssertNotNull(result, $"{methodName} did not return PlayerParticipationOperationResult.");
            return arguments[contextArgumentIndex];
        }

        private static void JoinNext(object context, PlayerSlotProfile expectedSlot)
        {
            PlayerParticipationOperationResult reservation = InvokeParticipation(context, "TryReserveNextAvailableSlot", "QA.P3H3", "reserve");
            AssertStatus(reservation, PlayerParticipationOperationStatus.Succeeded, "Slot reservation failed.");
            AssertSame(expectedSlot, reservation.Slot.Profile, "Configured join order changed.");
            PlayerParticipationOperationResult joined = InvokeParticipation(
                context,
                "TryMarkJoined",
                reservation.ReservationToken,
                "QA.P3H3",
                "join");
            AssertStatus(joined, PlayerParticipationOperationStatus.Succeeded, "Mark Joined failed.");
        }

        private static PlayerActorSelectionResult Select(object context, PlayerSlotId slotId, ActorProfile actor, int expectedRevision)
        {
            return InvokeSelection(context, "TrySelectActorProfile", new PlayerActorSelectionRequest(slotId, actor, "QA.P3H3", "select", expectedRevision));
        }

        private static PlayerActorSelectionResult Replace(object context, PlayerSlotId slotId, ActorProfile actor, int expectedRevision)
        {
            return InvokeSelection(context, "TryReplaceActorSelection", new PlayerActorSelectionRequest(slotId, actor, "QA.P3H3", "replace", expectedRevision));
        }

        private static PlayerActorSelectionResult Clear(object context, PlayerSlotId slotId, int expectedRevision)
        {
            return InvokeSelection(context, "TryClearActorSelection", new PlayerActorSelectionRequest(slotId, null, "QA.P3H3", "clear", expectedRevision));
        }

        private static PlayerActorSelectionResult SelectDefault(object context, PlayerSlotId slotId, int expectedRevision)
        {
            return InvokeSelection(context, "TrySelectDefaultActor", slotId, expectedRevision, "QA.P3H3", "default");
        }

        private static PlayerActorSelectionResult InvokeSelection(object context, string methodName, params object[] arguments)
        {
            MethodInfo method = context.GetType().GetMethod(methodName, InstanceInternal);
            AssertNotNull(method, $"Runtime method '{methodName}' was not found.");
            var result = method.Invoke(context, arguments) as PlayerActorSelectionResult;
            AssertNotNull(result, $"Runtime method '{methodName}' did not return PlayerActorSelectionResult.");
            return result;
        }

        private static PlayerParticipationOperationResult InvokeParticipation(object context, string methodName, params object[] arguments)
        {
            MethodInfo method = context.GetType().GetMethod(methodName, InstanceInternal);
            AssertNotNull(method, $"Runtime method '{methodName}' was not found.");
            var result = method.Invoke(context, arguments) as PlayerParticipationOperationResult;
            AssertNotNull(result, $"Runtime method '{methodName}' did not return PlayerParticipationOperationResult.");
            return result;
        }

        private static PlayerSlotRuntimeSnapshot GetSlot(object context, PlayerSlotId slotId)
        {
            MethodInfo method = context.GetType().GetMethod("TryGetActorSelection", InstanceInternal);
            object[] args = { slotId, null };
            AssertTrue((bool)method.Invoke(context, args), "Slot snapshot was not found.");
            return (PlayerSlotRuntimeSnapshot)args[1];
        }

        private static PlayerParticipationSnapshot GetSnapshot(object context)
        {
            MethodInfo method = context.GetType().GetMethod("CreateSnapshot", InstanceInternal);
            return (PlayerParticipationSnapshot)method.Invoke(context, null);
        }

        private static Type ResolveContextType()
        {
            Type type = typeof(PlayerSlotProfile).Assembly.GetType(ContextTypeName, false);
            AssertNotNull(type, $"Runtime type '{ContextTypeName}' was not found.");
            return type;
        }

        private static void AssertStatus(PlayerParticipationOperationResult result, PlayerParticipationOperationStatus expected, string message)
        {
            if (result == null || result.Status != expected)
                throw new InvalidOperationException($"{message} expected='{expected}' actual='{result?.Status}' diagnostics='{result?.ToDiagnosticString()}'.");
        }

        private static void AssertSelectionStatus(PlayerActorSelectionResult result, PlayerActorSelectionStatus expected, string message)
        {
            if (result == null || result.Status != expected)
                throw new InvalidOperationException($"{message} expected='{expected}' actual='{result?.Status}' diagnostics='{result?.ToDiagnosticString()}'.");
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException($"{message} expected='{expected}' actual='{actual}'.");
        }

        private static void AssertSame(object expected, object actual, string message)
        {
            if (!ReferenceEquals(expected, actual)) throw new InvalidOperationException(message);
        }

        private static void AssertNotNull(object value, string message)
        {
            if (value == null) throw new InvalidOperationException(message);
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
        }
    }
}
