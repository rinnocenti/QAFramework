using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Contract and authoring smoke for the P3J.2 Local Player Host migration.
    /// Runtime join behavior remains covered by P3G.3 and P3G.4 regression smokes.
    /// </summary>
    public static class QaP3J2LocalPlayerHostJoinMigrationSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3J.2 Run Local Player Host Migration Smoke";

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var completed = new List<string>();
            var created = new List<UnityEngine.Object>();

            try
            {
                AssertTrue(typeof(LocalPlayerHostAuthoring).IsSealed,
                    "LocalPlayerHostAuthoring should be a final product component.");
                AssertTrue(!typeof(IActor).IsAssignableFrom(typeof(LocalPlayerHostAuthoring)),
                    "Local Player Host must not be an Actor.");
                completed.Add("technical-host-is-not-actor-authority");

                GameObject validObject = CreateHost(created, "P3J2 Valid Host", true);
                LocalPlayerHostAuthoring validHost =
                    validObject.GetComponent<LocalPlayerHostAuthoring>();
                AssertTrue(validHost.TryValidateConfiguration(out string validIssue),
                    "Valid Local Player Host was rejected. " + validIssue);
                completed.Add("valid-host-configuration");

                LocalPlayerHostAuthoring createdByProductSurface =
                    CreateThroughProductSurface();
                created.Add(createdByProductSurface.gameObject);
                AssertTrue(createdByProductSurface.TryValidateConfiguration(out string creationIssue),
                    "Official Local Player Host creation surface produced invalid authoring. " + creationIssue);
                completed.Add("official-create-surface-produces-valid-host");

                AssertNotNull(validHost.PlayerInput,
                    "Host has no explicit PlayerInput evidence.");
                AssertNotNull(validHost.ActorMount,
                    "Host has no explicit Actor Mount.");
                AssertTrue(validHost.ActorMount.IsChildOf(validHost.transform),
                    "Actor Mount is not a child of host root.");
                completed.Add("explicit-playerinput-and-actor-mount");

                AssertTrue(!validHost.HasJoinedSlot,
                    "Reusable host prefab starts with joined Slot evidence.");
                AssertTrue(validObject.GetComponents<MonoBehaviour>()
                        .All(component => component.GetType().Name != "PlayerSlotDeclaration"),
                    "Reusable host contains the removed Slot declaration shape.");
                completed.Add("slot-identity-not-preauthored");

                AssertTrue(!validHost.HasLogicalActor &&
                    validHost.ActorMount.GetComponentInChildren<ActorDeclaration>(true) == null,
                    "Reusable host starts with a Logical Actor.");
                completed.Add("actor-mount-starts-empty");

                GameObject missingMountObject = CreateHost(created, "P3J2 Missing Mount", false);
                LocalPlayerHostAuthoring missingMount =
                    missingMountObject.GetComponent<LocalPlayerHostAuthoring>();
                AssertTrue(!missingMount.TryValidateConfiguration(out string mountIssue) &&
                    mountIssue.IndexOf("Actor Mount", StringComparison.OrdinalIgnoreCase) >= 0,
                    "Host without Actor Mount was accepted.");
                completed.Add("missing-actor-mount-rejected");

                GameObject actorContaminatedObject = CreateHost(
                    created,
                    "P3J2 Actor Contaminated Host",
                    true);
                actorContaminatedObject.GetComponent<LocalPlayerHostAuthoring>()
                    .ActorMount.gameObject.AddComponent<PlayerActorDeclaration>();
                AssertTrue(!actorContaminatedObject
                        .GetComponent<LocalPlayerHostAuthoring>()
                        .TryValidateConfiguration(out string actorIssue) &&
                    actorIssue.IndexOf("ActorDeclaration", StringComparison.OrdinalIgnoreCase) >= 0,
                    "Technical host containing ActorDeclaration was accepted.");
                completed.Add("logical-actor-on-provisioning-host-rejected");

                RequireComponent[] playerActorRequirements =
                    typeof(PlayerActorDeclaration).GetCustomAttributes<RequireComponent>(true)
                        .ToArray();
                AssertEqual(0, playerActorRequirements.Length,
                    "PlayerActorDeclaration still declares a RequireComponent dependency.");
                completed.Add("player-actor-no-longer-requires-playerinput");

                GameObject logicalActorObject = new GameObject("P3J2 Logical Player Actor");
                created.Add(logicalActorObject);
                PlayerActorDeclaration logicalActor =
                    logicalActorObject.AddComponent<PlayerActorDeclaration>();
                AssertTrue(logicalActorObject.GetComponent<PlayerInput>() == null,
                    "Adding PlayerActorDeclaration created PlayerInput.");
                AssertTrue(!logicalActor.HasPlayerInputEvidence,
                    "Authored Logical Actor inferred PlayerInput evidence.");
                completed.Add("logical-actor-authors-without-playerinput");

                AssertTrue(Enum.IsDefined(
                        typeof(LocalPlayerJoinStatus),
                        LocalPlayerJoinStatus.RejectedMissingLocalPlayerHost) &&
                    Enum.IsDefined(
                        typeof(LocalPlayerJoinStatus),
                        LocalPlayerJoinStatus.RejectedInvalidLocalPlayerHost),
                    "Local Player Host failure statuses are missing.");
                completed.Add("typed-host-failure-statuses");

                AssertNotNull(typeof(LocalPlayerJoinResult).GetProperty("LocalPlayerHost"),
                    "LocalPlayerJoinResult has no LocalPlayerHost evidence.");
                AssertTrue(typeof(LocalPlayerJoinResult).GetProperty("PlayerActorDeclaration") == null,
                    "LocalPlayerJoinResult still exposes PlayerActorDeclaration authority.");
                completed.Add("join-result-migrated-to-host-evidence");

                AssertNotNull(typeof(LocalPlayerHostAuthoring).GetMethod(
                    "TryStageAdmission",
                    BindingFlags.Instance | BindingFlags.NonPublic),
                    "Host admission staging operation is missing.");
                AssertNotNull(typeof(LocalPlayerHostAuthoring).GetMethod(
                    "CommitStagedAdmission",
                    BindingFlags.Instance | BindingFlags.NonPublic),
                    "Host admission commit operation is missing.");
                AssertNotNull(typeof(LocalPlayerHostAuthoring).GetMethod(
                    "RollbackStagedAdmission",
                    BindingFlags.Instance | BindingFlags.NonPublic),
                    "Host admission rollback operation is missing.");
                completed.Add("host-admission-transaction-surface");

                AssertTrue(typeof(LocalPlayerHostAuthoring).GetMethod(
                        "Awake",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) == null &&
                    typeof(LocalPlayerHostAuthoring).GetMethod(
                        "OnEnable",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) == null &&
                    typeof(LocalPlayerHostAuthoring).GetMethod(
                        "Start",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) == null,
                    "Local Player Host introduced lifecycle gameplay execution.");
                completed.Add("host-has-no-gameplay-lifecycle");

                string before = EditorJsonUtility.ToJson(validHost);
                validHost.TryValidateConfiguration(out _);
                AssertEqual(before, EditorJsonUtility.ToJson(validHost),
                    "Host validation mutated authoring state.");
                completed.Add("host-validation-is-non-mutating");

                AssertEqual(1,
                    validObject.GetComponentsInChildren<PlayerInput>(true).Length,
                    "Valid host does not contain exactly one PlayerInput.");
                completed.Add("single-playerinput-authority");

                Debug.Log(
                    "[P3J2_LOCAL_PLAYER_HOST_MIGRATION_SMOKE] status='Passed' " +
                    $"cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3J2_LOCAL_PLAYER_HOST_MIGRATION_SMOKE] status='Failed' " +
                    $"exception='{exception.GetType().Name}' message='{Escape(exception.Message)}' " +
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

        private static LocalPlayerHostAuthoring CreateThroughProductSurface()
        {
            const string typeName =
                "Immersive.Framework.Editor.Editor.PlayerParticipation.LocalPlayerHostCreationUtility";
            Type utilityType = null;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                utilityType = assembly.GetType(typeName, false);
                if (utilityType != null)
                {
                    break;
                }
            }

            AssertNotNull(utilityType,
                $"Official creation utility was not found: '{typeName}'.");
            MethodInfo method = utilityType.GetMethod(
                "CreateLocalPlayerHost",
                BindingFlags.Static | BindingFlags.Public);
            AssertNotNull(method,
                "LocalPlayerHostCreationUtility.CreateLocalPlayerHost was not found.");
            return method.Invoke(
                null,
                new object[] { "QA P3J2 Created Host", null, false })
                as LocalPlayerHostAuthoring;
        }

        private static GameObject CreateHost(
            ICollection<UnityEngine.Object> created,
            string name,
            bool includeMount)
        {
            var root = new GameObject(name);
            created.Add(root);
            PlayerInput playerInput = root.AddComponent<PlayerInput>();
            Transform mount = null;
            if (includeMount)
            {
                var mountObject = new GameObject("ActorMount");
                mountObject.transform.SetParent(root.transform, false);
                mount = mountObject.transform;
            }

            LocalPlayerHostAuthoring host =
                root.AddComponent<LocalPlayerHostAuthoring>();
            var serialized = new SerializedObject(host);
            serialized.FindProperty("playerInput").objectReferenceValue = playerInput;
            serialized.FindProperty("actorMount").objectReferenceValue = mount;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertNotNull(object value, string message)
        {
            if (value == null)
            {
                throw new InvalidOperationException(message);
            }
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
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
        }
    }
}
