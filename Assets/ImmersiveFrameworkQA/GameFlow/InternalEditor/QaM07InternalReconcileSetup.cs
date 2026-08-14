using System;
using ImmersiveFrameworkQA.Player;
using ImmersiveFrameworkQA.Player.Internal.Editor;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    /// <summary>
    /// Idempotent Edit Mode preparation for Q3 — QA-M07-INTERNAL.
    /// It prepares the canonical Manager-Provisioned Player fixture through typed
    /// Player support, then applies the existing direct-readiness content setup.
    /// </summary>
    public static class QaM07InternalReconcileSetup
    {
        private const string MenuPath =
            "Immersive Framework/QA/Game Flow/Participation/Prepare Reconcile Fixture";
        private const string Prefix = "[QA_M07_INTERNAL_SETUP]";
        private const string PreparedKey =
            "ImmersiveFrameworkQA.QA_M07_INTERNAL.Prepared";
        private const string RestoreAfterPlayKey =
            "ImmersiveFrameworkQA.QA_M07_INTERNAL.RestoreAfterPlay";

        internal const string ContentScenePath =
            "Assets/ImmersiveFrameworkQA/GameFlow/Scenes/QA_IF_READY_04_DirectPoliciesContent.unity";
        private const string AlternateActorPath =
            "Assets/ImmersiveFrameworkQA/Player/P3H4/P3H4_AlternateActor.asset";

        [InitializeOnLoadMethod]
        private static void RegisterRestoration()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidatePrepare() => !EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        private static void Prepare()
        {
            Require(!EditorApplication.isPlaying,
                "Q3 preparation must run outside Play Mode.");

            try
            {
                QaActivityEntryPresentationEvidenceSetup.ApplyCanonicalStandard(
                    "prepare-qa-m07-internal",
                    false);
                EnsureCanonicalPlayerFixture();

                ImmersiveFrameworkSettingsAsset settings =
                    Resources.Load<ImmersiveFrameworkSettingsAsset>(
                        ImmersiveFrameworkSettingsAsset.ResourcesPath);
                Require(settings != null && settings.ActiveGameApplication != null,
                    "Q3 could not resolve the active canonical Game Application.");

                GameApplicationAsset application = settings.ActiveGameApplication;
                GameApplicationAsset canonical =
                    QaActivityEntryPresentationEvidenceSetup
                        .ResolveCanonicalQaHubApplication();
                Require(ReferenceEquals(application, canonical),
                    "Q3 Player setup changed the active application away from the canonical QA Hub.");

                Require(QaPlayerSessionQaSupport.TryGetSupportedSlot(
                        application,
                        0,
                        out PlayerSlotProfile firstSlot) &&
                    firstSlot != null &&
                    firstSlot.DefaultActorProfile != null &&
                    firstSlot.DefaultActorProfile.LogicalActorHostPrefab != null,
                    "Q3 requires a valid first Local Player Slot with a default Actor.");

                Require(QaPlayerSessionQaSupport.TryGetSupportedSlot(
                        application,
                        1,
                        out PlayerSlotProfile secondSlot) &&
                    secondSlot != null,
                    "Q3 requires a configured second Local Player Slot.");

                ActorProfile alternate =
                    AssetDatabase.LoadAssetAtPath<ActorProfile>(
                        AlternateActorPath);
                Require(alternate != null &&
                    alternate.LogicalActorHostPrefab != null,
                    $"Q3 alternate Actor fixture is missing at '{AlternateActorPath}'.");

                var serializedSecondSlot = new SerializedObject(secondSlot);
                SerializedProperty secondDefault =
                    serializedSecondSlot.FindProperty("defaultActorProfile");
                Require(secondDefault != null,
                    "Second Player Slot serialized default Actor field is missing.");
                secondDefault.objectReferenceValue = alternate;
                serializedSecondSlot.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(secondSlot);

                SceneAsset contentScene =
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(
                        ContentScenePath);
                Require(contentScene != null,
                    $"Q3 readiness content scene is missing at '{ContentScenePath}'.");
                Require(CountEnabledBuildScenes(ContentScenePath) == 1,
                    $"Q3 requires exactly one enabled Build Settings entry for '{ContentScenePath}'.");
                Scene loaded = SceneManager.GetSceneByPath(ContentScenePath);
                Require(!loaded.IsValid() || !loaded.isLoaded,
                    "Q3 readiness content scene must be closed during setup.");

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                SessionState.SetBool(PreparedKey, true);
                SessionState.SetBool(RestoreAfterPlayKey, true);

                Debug.Log(
                    $"{Prefix} status='Prepared' " +
                    $"application='{application.ApplicationName}' " +
                    $"firstSlot='{firstSlot.PlayerSlotId.StableText}' " +
                    $"firstDefault='{firstSlot.DefaultActorProfile.ActorProfileId.StableText}' " +
                    $"secondSlot='{secondSlot.PlayerSlotId.StableText}' " +
                    $"secondDefault='{alternate.ActorProfileId.StableText}' " +
                    $"contentScene='{ContentScenePath}' " +
                    "next='Enter fresh Play Mode and run the canonical M07 regressions'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{Prefix} status='Failed' " +
                    $"exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}'.");
                throw;
            }
        }

        /// <summary>
        /// Typed Edit Mode preparation used by the canonical Player QA orchestrator.
        /// </summary>
        public static void PrepareForFullPlayerQa()
        {
            Prepare();
        }

        internal static void RequirePreparedForCurrentPlayMode()
        {
            Require(EditorApplication.isPlaying,
                $"Q3 requires Play Mode. First run '{MenuPath}' outside Play Mode.");
            Require(SessionState.GetBool(PreparedKey, false),
                $"Q3 is not prepared. Exit Play Mode, run '{MenuPath}', then enter a fresh Play Mode.");

            ImmersiveFrameworkSettingsAsset settings =
                Resources.Load<ImmersiveFrameworkSettingsAsset>(
                    ImmersiveFrameworkSettingsAsset.ResourcesPath);
            GameApplicationAsset canonical =
                QaActivityEntryPresentationEvidenceSetup
                    .ResolveCanonicalQaHubApplication();
            Require(settings != null &&
                ReferenceEquals(settings.ActiveGameApplication, canonical) &&
                settings.EditorPlayModeStartup ==
                    FrameworkEditorPlayModeStartup.FrameworkStartup,
                $"Q3 requires the canonical QA Hub startup. Exit Play Mode and run '{MenuPath}'.");
            Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    ContentScenePath) != null &&
                CountEnabledBuildScenes(ContentScenePath) == 1,
                "Q3 direct-readiness content scene is missing or disabled.");
        }

        private static void HandlePlayModeStateChanged(
            PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode ||
                !SessionState.GetBool(RestoreAfterPlayKey, false))
            {
                return;
            }

            try
            {
                QaActivityEntryPresentationEvidenceSetup.ApplyCanonicalStandard(
                    "qa-m07-internal-post-play-restore",
                    false);
                SessionState.EraseBool(PreparedKey);
                SessionState.EraseBool(RestoreAfterPlayKey);
                Debug.Log(
                    $"{Prefix} status='RestoredAfterPlay' " +
                    "startup='CanonicalQaHub'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{Prefix} status='RestoreFailed' " +
                    $"exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}'.");
            }
        }

        private static void EnsureCanonicalPlayerFixture()
        {
            QaManagerProvisionedPlayerFixture.PrepareAndValidate();
            Require(
                TryValidateCanonicalPlayerFixture(out string diagnostic),
                "Activity participation could not validate the explicit " +
                "Manager-Provisioned Player fixture after preparation. " +
                diagnostic);
            Debug.Log(
                $"{Prefix} status='PlayerFixturePrepared' " +
                $"diagnostic='{Escape(diagnostic)}'.");
        }

        private static bool TryValidateCanonicalPlayerFixture(
            out string diagnostic)
        {
            diagnostic = string.Empty;

            ImmersiveFrameworkSettingsAsset settings =
                Resources.Load<ImmersiveFrameworkSettingsAsset>(
                    ImmersiveFrameworkSettingsAsset.ResourcesPath);
            if (settings == null ||
                settings.ActiveGameApplication == null)
            {
                diagnostic =
                    "Active canonical Game Application is missing.";
                return false;
            }

            GameApplicationAsset application =
                settings.ActiveGameApplication;
            string playerSessionIssue = string.Empty;
            if (!application.PlayerSessionEnabled ||
                application.DefaultPlayerSessionProfile == null ||
                !application.DefaultPlayerSessionProfile.TryValidate(
                    out playerSessionIssue))
            {
                diagnostic =
                    "Canonical Player Session is disabled, missing or invalid. " +
                    playerSessionIssue;
                return false;
            }

            PlayerSessionProfile session =
                application.DefaultPlayerSessionProfile;
            if (session.HostProvisioning !=
                    PlayerHostProvisioningMode.ManagerProvisioned ||
                session.ActorResolutionPolicy !=
                    PlayerActorResolutionPolicy.ResolveConfiguredDefault)
            {
                diagnostic =
                    "Canonical Player Session does not use Manager-Provisioned Hosts " +
                    "with configured default Actor resolution.";
                return false;
            }

            if (application.PlayerActorSelectionDuplicatePolicy !=
                PlayerActorSelectionDuplicatePolicy.UniqueAcrossJoinedSlots)
            {
                diagnostic =
                    "Game Application duplicate Actor-selection policy is not UniqueAcrossJoinedSlots.";
                return false;
            }

            if (session.SupportedSlots.Count < 2)
            {
                diagnostic =
                    "Canonical Player Session requires at least two Supported Slots.";
                return false;
            }

            PlayerSlotProfile firstSlot = session.SupportedSlots[0];
            if (firstSlot == null ||
                firstSlot.DefaultActorProfile == null ||
                !firstSlot.DefaultActorProfile.ActorProfileId.IsValid ||
                firstSlot.DefaultActorProfile.LogicalActorHostPrefab == null)
            {
                diagnostic =
                    "First Local Player Slot has no valid default ActorProfile and Logical Actor Host.";
                return false;
            }

            PlayerSlotProfile secondSlot = session.SupportedSlots[1];
            if (secondSlot == null)
            {
                diagnostic =
                    "Second Local Player Slot is missing.";
                return false;
            }

            ActorProfile alternate =
                AssetDatabase.LoadAssetAtPath<ActorProfile>(
                    AlternateActorPath);
            if (alternate == null ||
                !alternate.ActorProfileId.IsValid ||
                alternate.LogicalActorHostPrefab == null)
            {
                diagnostic =
                    $"Alternate Actor fixture is missing or invalid at '{AlternateActorPath}'.";
                return false;
            }

            if (ReferenceEquals(
                    firstSlot.DefaultActorProfile.LogicalActorHostPrefab,
                    alternate.LogicalActorHostPrefab))
            {
                diagnostic =
                    "Default and alternate Actor fixtures share the same Logical Actor Host prefab.";
                return false;
            }

            diagnostic =
                $"application='{application.ApplicationName}' " +
                $"firstSlot='{firstSlot.PlayerSlotId.StableText}' " +
                $"defaultActor='{firstSlot.DefaultActorProfile.ActorProfileId.StableText}' " +
                $"secondSlot='{secondSlot.PlayerSlotId.StableText}' " +
                $"alternateActor='{alternate.ActorProfileId.StableText}'.";
            return true;
        }

        private static SerializedProperty RequireProperty(
            SerializedObject serialized,
            string name)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Required serialized field '{name}' was not found.");
            }

            return property;
        }

        private static int CountEnabledBuildScenes(string scenePath)
        {
            int count = 0;
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int index = 0; index < scenes.Length; index++)
            {
                if (scenes[index].enabled &&
                    string.Equals(
                        scenes[index].path,
                        scenePath,
                        StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "\\'")
                    .Replace("\r", " ")
                    .Replace("\n", " ");
        }
    }
}
