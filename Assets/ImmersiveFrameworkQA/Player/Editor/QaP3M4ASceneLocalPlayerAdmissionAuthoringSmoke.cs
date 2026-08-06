using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.Editor.PlayerParticipation;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Edit Mode proof for the current Scene-Provided Player authoring contract.
    /// Creates temporary nested prefabs, validates the same-root Host/composer shape,
    /// proves internal profile evidence and preserves the Manager-Provisioned Host regression.
    /// </summary>
    internal static class QaP3M4ASceneLocalPlayerAdmissionAuthoringSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3M4A Scene-Provided Player Authoring Smoke";

        private const string LogPrefix =
            "[QA][P3M4A Scene-Provided Authoring]";

        private const string TemporaryFolder =
            "Assets/ImmersiveFrameworkQA/Player/Editor/P3M4A_Temporary";

        private const string ActorPrefabPath =
            TemporaryFolder + "/Actor_PlayerSceneProvided_QA.prefab";

        private const string PlayerPrefabPath =
            TemporaryFolder + "/Player_SceneProvided_QA.prefab";

        [MenuItem(MenuPath)]
        internal static void Run()
        {
            var completed = new List<string>();
            Fixture fixture = null;

            try
            {
                ResetTemporaryFolder();
                fixture = CreateFixture();

                Require(
                    ReferenceEquals(
                        fixture.Authoring.LocalPlayerHost,
                        fixture.Host) &&
                    ReferenceEquals(
                        fixture.Authoring.gameObject,
                        fixture.Host.gameObject),
                    "Scene-Provided composer did not resolve the same-root Local Player Host.");
                completed.Add("same-root-host-resolved");

                SceneLocalPlayerAdmissionAuthoringResult applied =
                    SceneLocalPlayerAdmissionAuthoringUtility.ApplyOrRebuild(
                        fixture.Authoring,
                        logDiagnostics: false,
                        useUndo: false);

                Require(
                    applied.Succeeded &&
                    applied.Status ==
                    SceneLocalPlayerAdmissionAuthoringStatus.Valid,
                    "Apply / Rebuild failed. " + applied.Message);
                completed.Add("apply-rebuild-valid");

                Require(
                    ReferenceEquals(
                        fixture.Authoring.EvidenceLogicalActorHostPrefab,
                        fixture.ActorPrefabAsset),
                    "Apply / Rebuild did not resolve the nested Actor prefab boundary.");
                completed.Add("nested-actor-prefab-resolved");

                Require(
                    applied.EvidenceCreated &&
                    fixture.Authoring.HasTypedActorEvidence &&
                    ReferenceEquals(
                        fixture.Authoring.EvidenceActorProfile,
                        fixture.ActorProfile) &&
                    fixture.Authoring.IsTypedActorEvidenceCompatibleWith(
                        fixture.ActorProfile),
                    "Internal typed Actor Profile evidence was not created correctly.");
                completed.Add("internal-profile-evidence-created");

                SceneLocalPlayerAdmissionAuthoringResult validated =
                    SceneLocalPlayerAdmissionAuthoringUtility.Validate(
                        fixture.Authoring,
                        logDiagnostics: false);

                bool runtimeEvidenceValid =
                    fixture.Authoring.TryValidateRuntimeEvidence(
                        out string nominalIssue);

                Require(
                    validated.Succeeded &&
                    runtimeEvidenceValid,
                    "Validation after Apply / Rebuild failed. " +
                    (string.IsNullOrWhiteSpace(nominalIssue)
                        ? validated.Message
                        : nominalIssue));
                completed.Add("validate-after-apply-valid");

                Require(
                    fixture.Actor.GetComponent<
                        SceneLogicalPlayerActorEvidence>() == null,
                    "Legacy SceneLogicalPlayerActorEvidence is still required or was materialized on the Actor.");
                completed.Add("legacy-evidence-component-not-required");

                RunActorOutsideMountCase(
                    fixture,
                    completed);

                RunDuplicateActorCase(
                    fixture,
                    completed);

                RunSecondPlayerInputCase(
                    fixture,
                    completed);

                RunManagerProvisionedRegression(
                    completed);

                Debug.Log(
                    $"{LogPrefix} PASS. status='Passed' " +
                    $"cases='{completed.Count}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{LogPrefix} FAIL. status='Failed' " +
                    $"exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
            }
            finally
            {
                fixture?.Dispose();
                DeleteTemporaryFolder();
            }
        }

        private static void RunActorOutsideMountCase(
            Fixture fixture,
            ICollection<string> completed)
        {
            GameObject outsideRoot =
                new GameObject("QA_P3M4A_OutsideActor");

            try
            {
                PlayerActorDeclaration outsideActor =
                    outsideRoot.AddComponent<
                        PlayerActorDeclaration>();
                SetString(
                    outsideActor,
                    "actorId",
                    "qa.p3m4a.actor.outside");

                SetObject(
                    fixture.Authoring,
                    "sceneLogicalPlayerActor",
                    outsideActor);

                Require(
                    !fixture.Authoring.TryValidateRuntimeEvidence(
                        out string issue),
                    "Actor outside Actor Mount unexpectedly passed validation.");
                RequireContains(
                    issue,
                    "Actor Mount",
                    "Actor outside Actor Mount did not produce an explicit diagnostic.");

                completed.Add("actor-outside-mount-rejected");
            }
            finally
            {
                SetObject(
                    fixture.Authoring,
                    "sceneLogicalPlayerActor",
                    fixture.Actor);
                UnityEngine.Object.DestroyImmediate(
                    outsideRoot);
            }
        }

        private static void RunDuplicateActorCase(
            Fixture fixture,
            ICollection<string> completed)
        {
            GameObject duplicate =
                new GameObject("QA_P3M4A_DuplicateActor");

            try
            {
                duplicate.transform.SetParent(
                    fixture.Host.ActorMount,
                    false);

                PlayerActorDeclaration declaration =
                    duplicate.AddComponent<
                        PlayerActorDeclaration>();
                SetString(
                    declaration,
                    "actorId",
                    "qa.p3m4a.actor.duplicate");

                Require(
                    !fixture.Authoring.TryValidateRuntimeEvidence(
                        out string issue),
                    "Duplicate PlayerActorDeclaration unexpectedly passed validation.");
                RequireContains(
                    issue,
                    "exactly one",
                    "Duplicate PlayerActorDeclaration did not produce an explicit diagnostic.");

                completed.Add("duplicate-actor-rejected");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    duplicate);
            }
        }

        private static void RunSecondPlayerInputCase(
            Fixture fixture,
            ICollection<string> completed)
        {
            GameObject nestedInput =
                new GameObject("QA_P3M4A_SecondPlayerInput");

            try
            {
                nestedInput.transform.SetParent(
                    fixture.Host.ActorMount,
                    false);
                nestedInput.AddComponent<PlayerInput>();

                Require(
                    !fixture.Authoring.TryValidateRuntimeEvidence(
                        out string issue),
                    "Second PlayerInput unexpectedly passed validation.");
                RequireContains(
                    issue,
                    "PlayerInput",
                    "Second PlayerInput did not produce an explicit diagnostic.");

                completed.Add("second-player-input-rejected");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    nestedInput);
            }
        }

        private static void RunManagerProvisionedRegression(
            ICollection<string> completed)
        {
            GameObject root =
                new GameObject("QA_P3M4A_ManagerProvisionedHost");

            try
            {
                PlayerInput input =
                    root.AddComponent<PlayerInput>();
                LocalPlayerHostAuthoring host =
                    root.AddComponent<
                        LocalPlayerHostAuthoring>();

                GameObject actorMount =
                    new GameObject("Actor Mount");
                actorMount.transform.SetParent(
                    root.transform,
                    false);

                SetObject(
                    host,
                    "playerInput",
                    input);
                SetObject(
                    host,
                    "actorMount",
                    actorMount.transform);

                Require(
                    host.TryValidateConfiguration(
                        out string issue),
                    "Manager-Provisioned empty Actor Mount regression failed. " +
                    issue);

                completed.Add(
                    "manager-provisioned-empty-mount-regression");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Fixture CreateFixture()
        {
            GameObject actorPrefabAsset =
                CreateActorPrefab();

            GameObject playerPrefabAsset =
                CreatePlayerPrefab(
                    actorPrefabAsset);

            GameObject playerInstance =
                PrefabUtility.InstantiatePrefab(
                    playerPrefabAsset) as GameObject;

            Require(
                playerInstance != null,
                "Could not instantiate the temporary Scene-Provided Player prefab.");

            LocalPlayerHostAuthoring host =
                playerInstance.GetComponent<
                    LocalPlayerHostAuthoring>();
            SceneLocalPlayerAdmissionAuthoring authoring =
                playerInstance.GetComponent<
                    SceneLocalPlayerAdmissionAuthoring>();
            PlayerActorDeclaration actor =
                playerInstance.GetComponentInChildren<
                    PlayerActorDeclaration>(true);

            Require(
                host != null &&
                authoring != null &&
                actor != null,
                "Temporary Scene-Provided Player prefab is missing Host, composer or Actor declaration.");

            PlayerSlotProfile slotProfile =
                ScriptableObject.CreateInstance<
                    PlayerSlotProfile>();
            slotProfile.name =
                "QA P3M4A Player Slot";
            SetString(
                slotProfile,
                "playerSlotId",
                "qa.p3m4a.slot.1");

            ActorProfile actorProfile =
                ScriptableObject.CreateInstance<
                    ActorProfile>();
            actorProfile.name =
                "QA P3M4A Actor Profile";
            SetString(
                actorProfile,
                "actorProfileId",
                "qa.p3m4a.actor.profile");
            SetObject(
                actorProfile,
                "logicalActorHostPrefab",
                actorPrefabAsset);

            SetObject(
                authoring,
                "playerSlotProfile",
                slotProfile);
            SetObject(
                authoring,
                "actorProfile",
                actorProfile);
            SetObject(
                authoring,
                "sceneLogicalPlayerActor",
                actor);

            return new Fixture(
                playerInstance,
                actorPrefabAsset,
                playerPrefabAsset,
                slotProfile,
                actorProfile,
                host,
                actor,
                authoring);
        }

        private static GameObject CreateActorPrefab()
        {
            GameObject source =
                new GameObject(
                    "Actor_PlayerSceneProvided_QA");

            try
            {
                PlayerActorDeclaration declaration =
                    source.AddComponent<
                        PlayerActorDeclaration>();
                SetString(
                    declaration,
                    "actorId",
                    "qa.p3m4a.actor.1");
                SetString(
                    declaration,
                    "displayName",
                    "QA Scene-Provided Player Actor");

                GameObject prefab =
                    PrefabUtility.SaveAsPrefabAsset(
                        source,
                        ActorPrefabPath);

                Require(
                    prefab != null,
                    "Could not create the temporary Actor prefab.");

                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        private static GameObject CreatePlayerPrefab(
            GameObject actorPrefabAsset)
        {
            GameObject source =
                new GameObject(
                    "Player_SceneProvided_QA");

            try
            {
                PlayerInput input =
                    source.AddComponent<PlayerInput>();
                LocalPlayerHostAuthoring host =
                    source.AddComponent<
                        LocalPlayerHostAuthoring>();

                GameObject actorMount =
                    new GameObject("Actor Mount");
                actorMount.transform.SetParent(
                    source.transform,
                    false);

                SetObject(
                    host,
                    "playerInput",
                    input);
                SetObject(
                    host,
                    "actorMount",
                    actorMount.transform);

                GameObject nestedActor =
                    PrefabUtility.InstantiatePrefab(
                        actorPrefabAsset) as GameObject;
                Require(
                    nestedActor != null,
                    "Could not instantiate the nested Actor prefab.");
                nestedActor.transform.SetParent(
                    actorMount.transform,
                    false);

                source.AddComponent<
                    SceneLocalPlayerAdmissionAuthoring>();

                GameObject prefab =
                    PrefabUtility.SaveAsPrefabAsset(
                        source,
                        PlayerPrefabPath);

                Require(
                    prefab != null,
                    "Could not create the temporary composed Player prefab.");

                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        private static void ResetTemporaryFolder()
        {
            DeleteTemporaryFolder();
            EnsureFolder(TemporaryFolder);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void DeleteTemporaryFolder()
        {
            if (AssetDatabase.IsValidFolder(
                    TemporaryFolder))
            {
                AssetDatabase.DeleteAsset(
                    TemporaryFolder);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static void EnsureFolder(
            string folderPath)
        {
            string[] segments =
                folderPath.Split('/');
            string current =
                segments[0];

            for (int index = 1;
                 index < segments.Length;
                 index++)
            {
                string next =
                    current + "/" + segments[index];

                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(
                        current,
                        segments[index]);
                }

                current = next;
            }
        }

        private static void SetObject(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            var serialized =
                new SerializedObject(target);
            serialized.Update();

            SerializedProperty property =
                serialized.FindProperty(
                    propertyName);

            Require(
                property != null,
                $"Missing object property '{propertyName}' on '{target.GetType().Name}'.");

            property.objectReferenceValue =
                value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(
            UnityEngine.Object target,
            string propertyName,
            string value)
        {
            var serialized =
                new SerializedObject(target);
            serialized.Update();

            SerializedProperty property =
                serialized.FindProperty(
                    propertyName);

            Require(
                property != null,
                $"Missing string property '{propertyName}' on '{target.GetType().Name}'.");

            property.stringValue =
                value ?? string.Empty;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Require(
            bool condition,
            string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(
                    message);
            }
        }

        private static void RequireContains(
            string value,
            string expected,
            string message)
        {
            Require(
                !string.IsNullOrWhiteSpace(value) &&
                value.IndexOf(
                    expected,
                    StringComparison.OrdinalIgnoreCase) >= 0,
                $"{message} actual='{value}'.");
        }

        private static string Escape(
            string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }

        private sealed class Fixture :
            IDisposable
        {
            internal Fixture(
                GameObject playerInstance,
                GameObject actorPrefabAsset,
                GameObject playerPrefabAsset,
                PlayerSlotProfile slotProfile,
                ActorProfile actorProfile,
                LocalPlayerHostAuthoring host,
                PlayerActorDeclaration actor,
                SceneLocalPlayerAdmissionAuthoring authoring)
            {
                PlayerInstance =
                    playerInstance;
                ActorPrefabAsset =
                    actorPrefabAsset;
                PlayerPrefabAsset =
                    playerPrefabAsset;
                SlotProfile =
                    slotProfile;
                ActorProfile =
                    actorProfile;
                Host =
                    host;
                Actor =
                    actor;
                Authoring =
                    authoring;
            }

            internal GameObject PlayerInstance { get; }
            internal GameObject ActorPrefabAsset { get; }
            internal GameObject PlayerPrefabAsset { get; }
            internal PlayerSlotProfile SlotProfile { get; }
            internal ActorProfile ActorProfile { get; }
            internal LocalPlayerHostAuthoring Host { get; }
            internal PlayerActorDeclaration Actor { get; }
            internal SceneLocalPlayerAdmissionAuthoring Authoring { get; }

            public void Dispose()
            {
                if (PlayerInstance != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        PlayerInstance);
                }

                if (SlotProfile != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        SlotProfile);
                }

                if (ActorProfile != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        ActorProfile);
                }
            }
        }
    }
}
