using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.Player.Editor
{
    internal static class QaP3M4ASceneLocalPlayerAdmissionAuthoringSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3M4A Scene Local Player Admission Authoring Smoke";

        [MenuItem(MenuPath)]
        internal static void Run()
        {
            var completed = new List<string>();
            var created = new List<UnityEngine.Object>();
            try
            {
                Fixture fixture = CreateFixture(created);
                AssertTrue(
                    fixture.Authoring.TryValidateRuntimeEvidence(out string nominalIssue),
                    $"Nominal authoring failed: {nominalIssue}");
                completed.Add("nominal-explicit-evidence");

                AssertNotNull(
                    new SerializedObject(fixture.Authoring).FindProperty("admissionTiming"),
                    "Scene Local Player Admission does not expose serialized admissionTiming.");
                completed.Add("admission-timing-contract");

                UnityEngine.Object.DestroyImmediate(fixture.Evidence);
                AssertFalse(
                    fixture.Authoring.TryValidateRuntimeEvidence(out string evidenceIssue),
                    "Missing typed evidence unexpectedly passed runtime validation.");
                AssertContains(evidenceIssue, "evidence", "Missing evidence rejection was not explicit.");
                completed.Add("missing-evidence-rejected");

                fixture.Evidence = fixture.Actor.gameObject.AddComponent<SceneLogicalPlayerActorEvidence>();
                fixture.Evidence.EditorSetEvidence(
                    fixture.ActorProfile,
                    fixture.ActorProfile.LogicalActorHostPrefab,
                    "qa-restored-evidence");

                GameObject outsideRoot = NewObject("QA_P3M4A_Outside", created);
                PlayerActorDeclaration outsideActor = outsideRoot.AddComponent<PlayerActorDeclaration>();
                SetObject(fixture.Authoring, "sceneLogicalPlayerActor", outsideActor);
                AssertFalse(
                    fixture.Authoring.TryValidateRuntimeEvidence(out string outsideIssue),
                    "Actor outside Actor Mount unexpectedly passed validation.");
                AssertContains(outsideIssue, "Actor Mount", "Outside-mount rejection was not explicit.");
                completed.Add("actor-outside-mount-rejected");
                SetObject(fixture.Authoring, "sceneLogicalPlayerActor", fixture.Actor);

                GameObject duplicate = NewObject("QA_P3M4A_DuplicateActor", created);
                duplicate.transform.SetParent(fixture.Host.ActorMount, false);
                duplicate.AddComponent<PlayerActorDeclaration>();
                AssertFalse(
                    fixture.Authoring.TryValidateRuntimeEvidence(out string duplicateIssue),
                    "Duplicate PlayerActorDeclaration unexpectedly passed validation.");
                AssertContains(duplicateIssue, "exactly one", "Duplicate declaration rejection was not explicit.");
                UnityEngine.Object.DestroyImmediate(duplicate);
                completed.Add("duplicate-declaration-rejected");

                GameObject nestedInput = NewObject("QA_P3M4A_NestedInput", created);
                nestedInput.transform.SetParent(fixture.Host.ActorMount, false);
                nestedInput.AddComponent<PlayerInput>();
                AssertFalse(
                    fixture.Authoring.TryValidateRuntimeEvidence(out string inputIssue),
                    "Second PlayerInput unexpectedly passed validation.");
                AssertContains(inputIssue, "PlayerInput", "Second PlayerInput rejection was not explicit.");
                UnityEngine.Object.DestroyImmediate(nestedInput);
                completed.Add("second-player-input-rejected");

                GameObject provisionedRoot = NewObject("QA_P3M4A_ProvisionedHost", created);
                PlayerInput provisionedInput = provisionedRoot.AddComponent<PlayerInput>();
                LocalPlayerHostAuthoring provisionedHost =
                    provisionedRoot.AddComponent<LocalPlayerHostAuthoring>();
                GameObject provisionedMount = NewObject("ActorMount", created);
                provisionedMount.transform.SetParent(provisionedRoot.transform, false);
                SetObject(provisionedHost, "playerInput", provisionedInput);
                SetObject(provisionedHost, "actorMount", provisionedMount.transform);
                AssertTrue(
                    provisionedHost.TryValidateConfiguration(out string provisionedIssue),
                    $"Provisioned-host regression failed: {provisionedIssue}");
                completed.Add("manual-provisioned-host-regression");

                Debug.Log(
                    "[P3M4A_SCENE_LOCAL_PLAYER_ADMISSION_AUTHORING_SMOKE] " +
                    $"status='Passed' cases='{completed.Count}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3M4A_SCENE_LOCAL_PLAYER_ADMISSION_AUTHORING_SMOKE] " +
                    $"status='Failed' exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
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

        private static Fixture CreateFixture(ICollection<UnityEngine.Object> created)
        {
            GameObject hostRoot = NewObject("QA_P3M4A_Host", created);
            PlayerInput input = hostRoot.AddComponent<PlayerInput>();
            LocalPlayerHostAuthoring host = hostRoot.AddComponent<LocalPlayerHostAuthoring>();
            GameObject actorMount = NewObject("ActorMount", created);
            actorMount.transform.SetParent(hostRoot.transform, false);
            SetObject(host, "playerInput", input);
            SetObject(host, "actorMount", actorMount.transform);

            GameObject actorRoot = NewObject("QA_P3M4A_Actor", created);
            actorRoot.transform.SetParent(actorMount.transform, false);
            PlayerActorDeclaration actor = actorRoot.AddComponent<PlayerActorDeclaration>();

            var slotProfile = ScriptableObject.CreateInstance<PlayerSlotProfile>();
            slotProfile.name = "QA P3M4A Slot";
            created.Add(slotProfile);
            SetString(slotProfile, "playerSlotId", "qa.p3m4a.slot.1");

            var actorProfile = ScriptableObject.CreateInstance<ActorProfile>();
            actorProfile.name = "QA P3M4A Actor Profile";
            created.Add(actorProfile);
            SetString(actorProfile, "actorProfileId", "qa.p3m4a.actor.profile");
            SetObject(actorProfile, "logicalActorHostPrefab", actorRoot);

            SceneLogicalPlayerActorEvidence evidence =
                actorRoot.AddComponent<SceneLogicalPlayerActorEvidence>();
            evidence.EditorSetEvidence(actorProfile, actorRoot, "qa-evidence");

            GameObject admissionRoot = NewObject("QA_P3M4A_Admission", created);
            SceneLocalPlayerAdmissionAuthoring authoring =
                admissionRoot.AddComponent<SceneLocalPlayerAdmissionAuthoring>();
            SetObject(authoring, "playerSlotProfile", slotProfile);
            SetObject(authoring, "localPlayerHost", host);
            SetObject(authoring, "actorProfile", actorProfile);
            SetObject(authoring, "sceneLogicalPlayerActor", actor);

            return new Fixture(
                host,
                actor,
                slotProfile,
                actorProfile,
                authoring,
                evidence);
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

        private static void AssertContains(string value, string expected, string message)
        {
            AssertTrue(
                !string.IsNullOrEmpty(value) &&
                value.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0,
                $"{message} actual='{value}'.");
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
                PlayerSlotProfile slotProfile,
                ActorProfile actorProfile,
                SceneLocalPlayerAdmissionAuthoring authoring,
                SceneLogicalPlayerActorEvidence evidence)
            {
                Host = host;
                Actor = actor;
                SlotProfile = slotProfile;
                ActorProfile = actorProfile;
                Authoring = authoring;
                Evidence = evidence;
            }

            internal LocalPlayerHostAuthoring Host { get; }
            internal PlayerActorDeclaration Actor { get; }
            internal PlayerSlotProfile SlotProfile { get; }
            internal ActorProfile ActorProfile { get; }
            internal SceneLocalPlayerAdmissionAuthoring Authoring { get; }
            internal SceneLogicalPlayerActorEvidence Evidence { get; set; }
        }
    }
}
