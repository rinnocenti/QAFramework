using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
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
    /// It reuses the canonical QA Hub, P3J.5/P3H.4 Player fixture and the existing
    /// direct-readiness content scene. No scene, prefab or ProjectSettings asset is created.
    /// </summary>
    internal static class QaM07InternalReconcileSetup
    {
        private const string MenuPath =
            "Immersive Framework/QA/Setup/Player/M07 Prepare Internal Reconcile Regression";
        private const string Prefix = "[QA_M07_INTERNAL_SETUP]";
        private const string PlayerFixtureSetupTypeName =
            "ImmersiveFrameworkQA.Player.Editor.QaP3J5RuntimeHostPreparationSetup";
        private const string PreparedKey =
            "ImmersiveFrameworkQA.QA_M07_INTERNAL.Prepared";
        private const string RestoreAfterPlayKey =
            "ImmersiveFrameworkQA.QA_M07_INTERNAL.RestoreAfterPlay";

        internal const string ContentScenePath =
            "Assets/ImmersiveFrameworkQA/GameFlow/Scenes/QA_IF_READY_04_DirectPoliciesContent.unity";
        internal const string ReplacementActorPath =
            "Assets/ImmersiveFrameworkQA/Player/P3H4/Q3_M07_ReconcileReplacementActor.asset";
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

                Require(application.TryGetLocalPlayerSlot(
                        0,
                        out PlayerSlotProfile firstSlot) &&
                    firstSlot != null &&
                    firstSlot.DefaultActorProfile != null &&
                    firstSlot.DefaultActorProfile.LogicalActorHostPrefab != null,
                    "Q3 requires a valid first Local Player Slot with a default Actor.");

                Require(application.TryGetLocalPlayerSlot(
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

                ActorProfile replacement =
                    CreateOrUpdateReplacementActor(alternate);

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
                    $"replacement='{replacement.ActorProfileId.StableText}' " +
                    $"contentScene='{ContentScenePath}' " +
                    "next='Enter fresh Play Mode and run M07 Internal Reconcile Authority Regression'.");
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
            Require(ResolveReplacementActor() != null,
                "Q3 replacement Actor fixture is missing.");
        }

        internal static ActorProfile ResolveReplacementActor()
        {
            return AssetDatabase.LoadAssetAtPath<ActorProfile>(
                ReplacementActorPath);
        }

        private static ActorProfile CreateOrUpdateReplacementActor(
            ActorProfile template)
        {
            ActorProfile profile =
                AssetDatabase.LoadAssetAtPath<ActorProfile>(
                    ReplacementActorPath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<ActorProfile>();
                AssetDatabase.CreateAsset(profile, ReplacementActorPath);
            }

            profile.name = "Q3_M07_ReconcileReplacementActor";
            var serialized = new SerializedObject(profile);
            RequireProperty(serialized, "actorProfileId").stringValue =
                "qa.m07.reconcile.actor-profile.replacement";
            RequireProperty(serialized, "displayName").stringValue =
                "Q3 M07 Reconcile Replacement Actor";
            RequireProperty(serialized, "description").stringValue =
                "Q3-only valid ActorProfile used to prove replacement during active-Activity reconcile.";
            RequireProperty(serialized, "actorKind").intValue =
                (int)ActorKind.Player;
            RequireProperty(serialized, "actorRole").intValue =
                (int)ActorRole.Protagonist;
            RequireProperty(serialized, "logicalActorHostPrefab")
                .objectReferenceValue = template.LogicalActorHostPrefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);

            Require(profile.ActorProfileId.IsValid &&
                profile.LogicalActorHostPrefab != null,
                "Q3 replacement ActorProfile is not execution-ready.");
            return profile;
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
            if (TryValidateCanonicalPlayerFixture(out string existingDiagnostic))
            {
                Debug.Log(
                    $"{Prefix} status='PlayerFixtureReused' " +
                    $"diagnostic='{Escape(existingDiagnostic)}'.");
                return;
            }

            Exception applicationFailure = null;
            try
            {
                InvokeCanonicalPlayerFixtureSetup();
            }
            catch (Exception exception)
            {
                applicationFailure = exception;
            }

            if (TryValidateCanonicalPlayerFixture(out string appliedDiagnostic))
            {
                if (applicationFailure != null)
                {
                    Debug.LogWarning(
                        $"{Prefix} status='PlayerFixtureAppliedWithLegacyDiagnosticFailure' " +
                        $"exception='{applicationFailure.GetType().Name}' " +
                        $"message='{Escape(applicationFailure.Message)}' " +
                        $"postconditions='{Escape(appliedDiagnostic)}'.");
                }
                else
                {
                    Debug.Log(
                        $"{Prefix} status='PlayerFixtureApplied' " +
                        $"postconditions='{Escape(appliedDiagnostic)}'.");
                }

                return;
            }

            if (applicationFailure != null)
            {
                ExceptionDispatchInfo.Capture(applicationFailure).Throw();
            }

            throw new InvalidOperationException(
                "Canonical Player fixture setup returned without an exception, " +
                "but its required postconditions are not valid.");
        }

        private static void InvokeCanonicalPlayerFixtureSetup()
        {
            Type setupType = ResolveLoadedType(
                PlayerFixtureSetupTypeName);
            MethodInfo apply = setupType.GetMethod(
                "Apply",
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic);
            Require(apply != null &&
                apply.GetParameters().Length == 0,
                $"Q3 could not resolve parameterless '{PlayerFixtureSetupTypeName}.Apply'.");

            try
            {
                apply.Invoke(null, null);
            }
            catch (TargetInvocationException exception)
            {
                ExceptionDispatchInfo.Capture(
                    exception.InnerException ?? exception).Throw();
            }
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

            PlayerProvisioningProfile provisioning = application
                .DefaultPlayerSessionProfile.PlayerProvisioningProfile;
            if (provisioning == null ||
                provisioning.DefaultHostProvisioning !=
                    PlayerHostProvisioningMode.ManagerProvisioned ||
                provisioning.ActorResolutionPolicy !=
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

            if (!application.TryGetLocalPlayerSlot(
                    0,
                    out PlayerSlotProfile firstSlot) ||
                firstSlot == null ||
                firstSlot.DefaultActorProfile == null ||
                !firstSlot.DefaultActorProfile.ActorProfileId.IsValid ||
                firstSlot.DefaultActorProfile.LogicalActorHostPrefab == null)
            {
                diagnostic =
                    "First Local Player Slot has no valid default ActorProfile and Logical Actor Host.";
                return false;
            }

            if (!application.TryGetLocalPlayerSlot(
                    1,
                    out PlayerSlotProfile secondSlot) ||
                secondSlot == null)
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

        private static Type ResolveLoadedType(string fullName)
        {
            Assembly[] assemblies =
                AppDomain.CurrentDomain.GetAssemblies();
            for (int index = 0;
                 index < assemblies.Length;
                 index++)
            {
                Type type = assemblies[index].GetType(
                    fullName,
                    false);
                if (type != null)
                {
                    return type;
                }
            }

            throw new TypeLoadException(
                $"Q3 required Editor type '{fullName}' was not found.");
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
