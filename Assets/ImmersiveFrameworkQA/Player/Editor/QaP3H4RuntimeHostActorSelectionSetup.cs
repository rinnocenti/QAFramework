using System;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Idempotent P3H.4 fixture installer. It extends the real P3G.4 join fixture with
    /// explicit GameApplication Actor-selection policy and valid default/alternate Actor Profiles.
    /// </summary>
    public static class QaP3H4RuntimeHostActorSelectionSetup
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3H.4 Apply Runtime Host Actor Selection Fixture";
        private const string RootFolder =
            "Assets/ImmersiveFrameworkQA/Player/P3H4";
        private const string PlayerPrefabPath =
            "Assets/ImmersiveFrameworkQA/Player/P3G4/P3G4_LocalPlayerHost.prefab";
        private const string PolicyPath =
            RootFolder + "/P3H4_ActorSelectionPolicy.asset";
        private const string DefaultActorPath =
            RootFolder + "/P3H4_DefaultActor.asset";
        private const string AlternateActorPath =
            RootFolder + "/P3H4_AlternateActor.asset";

        [MenuItem(MenuPath)]
        public static void Apply()
        {
            try
            {
                QaP3G4RuntimeIntegrationSetup.Apply();
                EnsureFolder(RootFolder);

                GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
                if (playerPrefab == null)
                {
                    throw new InvalidOperationException(
                        $"P3G.4 Player prefab was not found at '{PlayerPrefabPath}'.");
                }

                PlayerActorSelectionPolicyProfile policy = CreateOrUpdatePolicy();
                ActorProfile defaultActor = CreateOrUpdateActorProfile(
                    DefaultActorPath,
                    "P3H4 Default Player Actor",
                    "qa.p3h4.actor-profile.default",
                    playerPrefab);
                ActorProfile alternateActor = CreateOrUpdateActorProfile(
                    AlternateActorPath,
                    "P3H4 Alternate Player Actor",
                    "qa.p3h4.actor-profile.alternate",
                    playerPrefab);

                ImmersiveFrameworkSettingsAsset settings =
                    Resources.Load<ImmersiveFrameworkSettingsAsset>(
                        ImmersiveFrameworkSettingsAsset.ResourcesPath);
                if (settings == null || settings.ActiveGameApplication == null)
                {
                    throw new InvalidOperationException(
                        "Active Game Application was not resolved from ImmersiveFrameworkSettings.");
                }

                GameApplicationAsset gameApplication = settings.ActiveGameApplication;
                var serializedApplication = new SerializedObject(gameApplication);
                SerializedProperty policyProperty =
                    serializedApplication.FindProperty("playerActorSelectionPolicyProfile");
                if (policyProperty == null)
                {
                    throw new MissingFieldException(
                        nameof(GameApplicationAsset),
                        "playerActorSelectionPolicyProfile");
                }

                policyProperty.objectReferenceValue = policy;
                serializedApplication.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(gameApplication);

                if (!gameApplication.TryGetLocalPlayerSlot(0, out PlayerSlotProfile firstSlot) ||
                    firstSlot == null)
                {
                    throw new InvalidOperationException(
                        "Active Game Application requires a configured first Local Player Slot.");
                }

                var serializedSlot = new SerializedObject(firstSlot);
                SerializedProperty defaultActorProperty =
                    serializedSlot.FindProperty("defaultActorProfile");
                if (defaultActorProperty == null)
                {
                    throw new MissingFieldException(
                        nameof(PlayerSlotProfile),
                        "defaultActorProfile");
                }

                defaultActorProperty.objectReferenceValue = defaultActor;
                serializedSlot.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(firstSlot);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[P3H4_RUNTIME_HOST_ACTOR_SELECTION_FIXTURE] status='Applied' " +
                    $"gameApplication='{gameApplication.name}' policy='{policy.name}' " +
                    $"duplicatePolicy='{policy.DuplicatePolicy}' slot='{firstSlot.PlayerSlotId.StableText}' " +
                    $"defaultActor='{defaultActor.ActorProfileId.StableText}' " +
                    $"alternateActor='{alternateActor.ActorProfileId.StableText}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3H4_RUNTIME_HOST_ACTOR_SELECTION_FIXTURE] status='Failed' " +
                    $"exception='{exception.GetType().Name}' message='{Escape(exception.Message)}'.");
                throw;
            }
        }

        private static PlayerActorSelectionPolicyProfile CreateOrUpdatePolicy()
        {
            PlayerActorSelectionPolicyProfile policy =
                AssetDatabase.LoadAssetAtPath<PlayerActorSelectionPolicyProfile>(PolicyPath);
            if (policy == null)
            {
                policy = ScriptableObject.CreateInstance<PlayerActorSelectionPolicyProfile>();
                AssetDatabase.CreateAsset(policy, PolicyPath);
            }

            policy.name = "P3H4 Unique Actor Selection";
            var serialized = new SerializedObject(policy);
            serialized.FindProperty("displayName").stringValue =
                "P3H4 Actor Selection — Unique Across Joined Slots";
            serialized.FindProperty("description").stringValue =
                "QA policy proving GameApplication-to-Session runtime composition.";
            serialized.FindProperty("duplicatePolicy").intValue =
                (int)PlayerActorSelectionDuplicatePolicy.UniqueAcrossJoinedSlots;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(policy);
            return policy;
        }

        private static ActorProfile CreateOrUpdateActorProfile(
            string assetPath,
            string displayName,
            string actorProfileId,
            GameObject logicalHostPrefab)
        {
            ActorProfile profile = AssetDatabase.LoadAssetAtPath<ActorProfile>(assetPath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<ActorProfile>();
                AssetDatabase.CreateAsset(profile, assetPath);
            }

            profile.name = displayName;
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("actorProfileId").stringValue = actorProfileId;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("description").stringValue =
                "P3H.4 runtime-host Actor selection QA Profile.";
            serialized.FindProperty("actorKind").intValue = (int)ActorKind.Player;
            serialized.FindProperty("actorRole").intValue = (int)ActorRole.Protagonist;
            serialized.FindProperty("logicalActorHostPrefab").objectReferenceValue =
                logicalHostPrefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] segments = folderPath.Split('/');
            string current = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }

                current = next;
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
