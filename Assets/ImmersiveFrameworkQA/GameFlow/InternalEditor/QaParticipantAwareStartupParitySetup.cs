using System;
using System.Collections.Generic;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.Transition;
using ImmersiveFrameworkQA.UnityBuildSurface;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    internal enum QaParticipantAwareStartupParityMode
    {
        None = 0,
        RouteStartup = 1,
        GameApplicationStartup = 2
    }

    internal readonly struct QaParticipantAwareStartupParityAssets
    {
        internal QaParticipantAwareStartupParityAssets(
            ActivityAsset activity,
            RouteAsset route,
            GameApplicationAsset gameApplication)
        {
            Activity = activity;
            Route = route;
            GameApplication = gameApplication;
        }

        internal ActivityAsset Activity { get; }
        internal RouteAsset Route { get; }
        internal GameApplicationAsset GameApplication { get; }
    }

    internal static class QaParticipantAwareStartupParitySetup
    {
        private const string Prefix = "[QA_READY_PROGRESS_02B_SETUP]";
        private const string MenuRoot =
            "Immersive Framework/QA/Setup/Activity Entry Readiness/";
        private const string PrepareRouteMenuPath =
            MenuRoot + "Prepare Route Startup Progress Parity";
        private const string PrepareGameApplicationMenuPath =
            MenuRoot + "Prepare Game Application Startup Progress Parity";
        private const string RestoreMenuPath =
            MenuRoot + "Restore Startup Progress Parity";
        private const string ReportMenuPath =
            MenuRoot + "Report Startup Progress Parity";

        private const string PreparedKey =
            "ImmersiveFrameworkQA.QA_READY_PROGRESS_02B.Prepared";
        private const string ModeKey =
            "ImmersiveFrameworkQA.QA_READY_PROGRESS_02B.Mode";
        private const string RestoreAfterPlayKey =
            "ImmersiveFrameworkQA.QA_READY_PROGRESS_02B.RestoreAfterPlay";

        internal const string GeneratedRoot =
            "Assets/ImmersiveFrameworkQA/GameFlow/Generated/QA_READY_PROGRESS_02B";
        internal const string FixtureScenePath =
            GeneratedRoot + "/QA_READY_PROGRESS_02B_Startup.unity";
        internal const string FixtureActivityPath =
            GeneratedRoot + "/QA_READY_PROGRESS_02B_StartupActivity.asset";
        internal const string FixtureRoutePath =
            GeneratedRoot + "/QA_READY_PROGRESS_02B_StartupRoute.asset";
        internal const string FixtureGameApplicationPath =
            GeneratedRoot + "/QA_READY_PROGRESS_02B_GameApplication.asset";

        private const string SourceRouteScenePath =
            "Assets/ImmersiveFrameworkQA/Lifecycle/Scenes/" +
            "QA_LifecycleRouteB.unity";
        private const string FixtureSceneName =
            "QA_READY_PROGRESS_02B_Startup";
        private const string FixtureRootName =
            "QA_READY_PROGRESS_02B_Fixture";

        [InitializeOnLoadMethod]
        private static void RegisterRestoration()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        [MenuItem(PrepareRouteMenuPath, true)]
        [MenuItem(PrepareGameApplicationMenuPath, true)]
        private static bool ValidatePrepare() => !EditorApplication.isPlaying;

        [MenuItem(PrepareRouteMenuPath)]
        private static void PrepareRouteStartup()
        {
            Prepare(QaParticipantAwareStartupParityMode.RouteStartup);
        }

        [MenuItem(PrepareGameApplicationMenuPath)]
        private static void PrepareGameApplicationStartup()
        {
            Prepare(QaParticipantAwareStartupParityMode.GameApplicationStartup);
        }

        [MenuItem(RestoreMenuPath, true)]
        private static bool ValidateRestore() => !EditorApplication.isPlaying;

        [MenuItem(RestoreMenuPath)]
        private static void RestoreFromMenu()
        {
            RestoreInternal("manual-restore", logSuccess: true);
        }

        [MenuItem(ReportMenuPath)]
        private static void Report()
        {
            Debug.Log($"{Prefix} status='Current' " +
                $"prepared='{SessionState.GetBool(PreparedKey, false)}' " +
                $"mode='{SessionState.GetString(ModeKey, "None")}' " +
                $"generatedRoot='{AssetDatabase.IsValidFolder(GeneratedRoot)}' " +
                $"sceneInBuildSettings='{CountBuildSettingsEntries(FixtureScenePath)}'.");
        }

        internal static QaParticipantAwareStartupParityMode
            RequirePreparedForCurrentPlayMode()
        {
            Require(EditorApplication.isPlaying,
                "Q2B startup parity regression requires Play Mode.");
            Require(SessionState.GetBool(PreparedKey, false),
                "Q2B startup parity is not prepared. Exit Play Mode and run " +
                $"'{PrepareRouteMenuPath}' or '{PrepareGameApplicationMenuPath}'.");

            Require(Enum.TryParse(
                    SessionState.GetString(ModeKey, "None"),
                    out QaParticipantAwareStartupParityMode mode) &&
                mode != QaParticipantAwareStartupParityMode.None,
                "Q2B startup parity mode is missing or invalid.");
            Require(CountBuildSettingsEntries(FixtureScenePath) == 1,
                "Q2B fixture scene must be enabled exactly once in Build Settings.");
            LoadAssets(mode);
            return mode;
        }

        internal static QaParticipantAwareStartupParityAssets LoadAssets(
            QaParticipantAwareStartupParityMode mode)
        {
            ActivityAsset activity =
                AssetDatabase.LoadAssetAtPath<ActivityAsset>(
                    FixtureActivityPath);
            RouteAsset route =
                AssetDatabase.LoadAssetAtPath<RouteAsset>(FixtureRoutePath);
            GameApplicationAsset gameApplication =
                mode == QaParticipantAwareStartupParityMode
                    .GameApplicationStartup
                    ? AssetDatabase.LoadAssetAtPath<GameApplicationAsset>(
                        FixtureGameApplicationPath)
                    : null;

            SceneAsset fixtureScene =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(FixtureScenePath);
            Require(activity != null && route != null &&
                fixtureScene != null,
                "Q2B generated Activity, Route or Primary Scene is missing.");
            Require(activity.EntryReadinessPolicy ==
                    ActivityEntryReadinessPolicy.WaitCovered &&
                activity.VisualTransitionMode ==
                    ActivityVisualTransitionMode.FadeWithLoading &&
                activity.TransitionGateMode ==
                    TransitionGateMode.InputInteractionAndGameplay,
                "Q2B generated Activity does not preserve the canonical " +
                "WaitCovered presentation contract.");
            Require(route.StartupActivity != null &&
                route.StartupActivity.HasSameIdentity(activity) &&
                route.TransitionGateMode ==
                    TransitionGateMode.InputInteractionAndGameplay &&
                string.Equals(
                    route.PrimaryScenePath,
                    FixtureScenePath,
                    StringComparison.Ordinal) &&
                string.Equals(
                    route.PrimarySceneName,
                    FixtureSceneName,
                    StringComparison.Ordinal),
                "Q2B generated Route does not reference its startup fixture.");
            Require(CountBuildSettingsEntries(FixtureScenePath) == 1,
                "Q2B fixture scene must be enabled exactly once in Build Settings.");
            if (mode == QaParticipantAwareStartupParityMode
                .GameApplicationStartup)
            {
                Require(gameApplication != null &&
                    gameApplication.StartupRoute != null &&
                    gameApplication.StartupRoute.HasSameIdentity(route) &&
                    string.Equals(
                        gameApplication.ApplicationName,
                        "QA READY PROGRESS 02B Game Application",
                        StringComparison.Ordinal),
                    "Q2B generated Game Application does not reference its Route.");
            }

            return new QaParticipantAwareStartupParityAssets(
                activity,
                route,
                gameApplication);
        }

        private static void Prepare(
            QaParticipantAwareStartupParityMode mode)
        {
            Require(!EditorApplication.isPlaying,
                "Q2B preparation must run outside Play Mode.");
            Require(mode != QaParticipantAwareStartupParityMode.None,
                "Q2B preparation mode must be explicit.");
            Require(!AssetDatabase.IsValidFolder(GeneratedRoot),
                $"Q2B generated fixture already exists. Run '{RestoreMenuPath}' first.");

            try
            {
                QaActivityEntryPresentationEvidenceSetup.ApplyCanonicalStandard(
                    "prepare-startup-progress-parity",
                    markPreparedForNextPlay: false);
                EnsureGeneratedFolder();
                CreateFixtureScene();
                AddFixtureSceneToBuildSettings();
                ActivityAsset activity = CreateFixtureActivity();
                RouteAsset route = CreateFixtureRoute(activity);

                if (mode == QaParticipantAwareStartupParityMode
                    .GameApplicationStartup)
                {
                    CreateFixtureGameApplication(route);
                    ApplyFixtureGameApplication();
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                LoadAssets(mode);
                SessionState.SetBool(PreparedKey, true);
                SessionState.SetString(ModeKey, mode.ToString());
                SessionState.SetBool(RestoreAfterPlayKey, true);
                Debug.Log($"{Prefix} status='Prepared' mode='{mode}' " +
                    $"scene='{FixtureScenePath}' " +
                    "next='Enter a fresh Play Mode session and run " +
                    "Participant-Aware Startup Loading Parity Regression'.");
            }
            catch
            {
                RestoreInternal("prepare-failure", logSuccess: false);
                throw;
            }
        }

        private static void CreateFixtureScene()
        {
            SceneAsset source = AssetDatabase.LoadAssetAtPath<SceneAsset>(
                SourceRouteScenePath);
            Require(source != null,
                $"Q2B source Route scene is missing. path='{SourceRouteScenePath}'.");
            Require(AssetDatabase.CopyAsset(
                    SourceRouteScenePath,
                    FixtureScenePath),
                "Q2B source Route scene copy failed.");

            SceneSetup[] previousSetup =
                EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene = EditorSceneManager.OpenScene(
                    FixtureScenePath,
                    OpenSceneMode.Additive);
                Require(scene.IsValid() && scene.isLoaded,
                    "Q2B copied Route scene could not be opened.");
                Require(CountSceneReadinessParticipants(scene) == 0,
                    "Q2B source Route scene already contains Activity readiness participants.");
                Require(CountSceneDrivers(scene) == 0,
                    "Q2B source Route scene already contains a startup parity driver.");
                var root = new GameObject(FixtureRootName);
                SceneManager.MoveGameObjectToScene(root, scene);
                root.AddComponent<QaParticipantAwareStartupParityDriver>();

                for (int index = 0; index < 4; index++)
                {
                    CreateParticipant(
                        root.transform,
                        $"Required {index + 1}",
                        $"qa.ready-progress-02b.required.{index + 1}",
                        ActivityContentExecutionRequiredness.Required,
                        1000 + index * 10);
                }

                CreateParticipant(
                    root.transform,
                    "Optional 1",
                    "qa.ready-progress-02b.optional.1",
                    ActivityContentExecutionRequiredness.Optional,
                    1100);

                Require(CountSceneReadinessParticipants(scene) == 5,
                    "Q2B fixture scene did not materialize exactly five participants.");
                Require(CountSceneDrivers(scene) == 1,
                    "Q2B fixture scene did not materialize exactly one driver.");
                EditorSceneManager.MarkSceneDirty(scene);
                Require(EditorSceneManager.SaveScene(scene),
                    "Q2B fixture scene could not be saved.");
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
        }

        private static ActivityAsset CreateFixtureActivity()
        {
            var activity = ScriptableObject.CreateInstance<ActivityAsset>();
            activity.name = "QA_READY_PROGRESS_02B_StartupActivity";
            var serialized = new SerializedObject(activity);
            SetString(serialized, "activityId",
                "qa.ready-progress-02b.startup-activity");
            SetString(serialized, "activityName",
                "QA READY PROGRESS 02B Startup Activity");
            SetString(serialized, "description",
                "Temporary Q2B Route/Game Application startup parity fixture.");
            SetEnum(serialized, "playerParticipationProjectionMode", "NoSlots");
            SetEnum(serialized, "playerParticipationZeroParticipantPolicy", "Allowed");
            RequireProperty(serialized,
                "playerParticipationExplicitSlotProfiles").arraySize = 0;
            SetEnum(serialized, "playerParticipationRequirementLevel", "None");
            RequireProperty(serialized,
                "activityContentProfile").objectReferenceValue = null;
            SetEnum(serialized, "activityEntryReadinessPolicy", "WaitCovered");
            SetEnum(serialized, "visualTransitionMode", "FadeWithLoading");
            SetEnum(serialized, "transitionGateMode",
                "InputInteractionAndGameplay");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(activity, FixtureActivityPath);
            return activity;
        }

        private static RouteAsset CreateFixtureRoute(ActivityAsset activity)
        {
            var route = ScriptableObject.CreateInstance<RouteAsset>();
            route.name = "QA_READY_PROGRESS_02B_StartupRoute";
            var serialized = new SerializedObject(route);
            SetString(serialized, "routeId",
                "qa.ready-progress-02b.startup-route");
            SetString(serialized, "routeName",
                "QA READY PROGRESS 02B Startup Route");
            SetString(serialized, "primaryScenePath", FixtureScenePath);
            SetString(serialized, "primarySceneName", FixtureSceneName);
            RequireProperty(serialized,
                "routeContentProfile").objectReferenceValue = null;
            RequireProperty(serialized,
                "startupActivity").objectReferenceValue = activity;
            SetEnum(serialized, "transitionGateMode",
                "InputInteractionAndGameplay");
            SetString(serialized, "description",
                "Temporary Q2B participant-aware startup parity Route.");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(route, FixtureRoutePath);
            return route;
        }

        private static void CreateFixtureGameApplication(RouteAsset route)
        {
            GameApplicationAsset canonical =
                QaActivityEntryPresentationEvidenceSetup
                    .ResolveCanonicalQaHubApplication();
            string canonicalPath = AssetDatabase.GetAssetPath(canonical);
            Require(!string.IsNullOrWhiteSpace(canonicalPath),
                "Canonical Game Application has no asset path.");
            Require(AssetDatabase.CopyAsset(
                    canonicalPath,
                    FixtureGameApplicationPath),
                "Q2B Game Application copy failed.");

            GameApplicationAsset copy =
                AssetDatabase.LoadAssetAtPath<GameApplicationAsset>(
                    FixtureGameApplicationPath);
            Require(copy != null,
                "Q2B copied Game Application could not be loaded.");
            var serialized = new SerializedObject(copy);
            SetString(serialized, "applicationName",
                "QA READY PROGRESS 02B Game Application");
            RequireProperty(serialized,
                "startupRoute").objectReferenceValue = route;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(copy);
        }

        private static void ApplyFixtureGameApplication()
        {
            ImmersiveFrameworkSettingsAsset settings =
                ResolveSettingsAsset();
            GameApplicationAsset fixture =
                AssetDatabase.LoadAssetAtPath<GameApplicationAsset>(
                    FixtureGameApplicationPath);
            Require(fixture != null,
                "Q2B fixture Game Application is missing.");
            Undo.RecordObject(
                settings,
                "Prepare Q2B Game Application startup parity");
            var serialized = new SerializedObject(settings);
            RequireProperty(serialized,
                "activeGameApplication").objectReferenceValue = fixture;
            SetEnum(serialized, "editorPlayModeStartup", "FrameworkStartup");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Require(ReferenceEquals(settings.ActiveGameApplication, fixture) &&
                settings.EditorPlayModeStartup ==
                FrameworkEditorPlayModeStartup.FrameworkStartup,
                "Q2B Game Application setup was not persisted.");
        }

        private static void CreateParticipant(
            Transform parent,
            string label,
            string participantId,
            ActivityContentExecutionRequiredness requiredness,
            int order)
        {
            var child = new GameObject(label);
            child.transform.SetParent(parent, false);
            ActivityReadinessParticipant participant =
                child.AddComponent<ActivityReadinessParticipant>();
            var serialized = new SerializedObject(participant);
            SetString(serialized, "participantId", participantId);
            SetEnum(serialized, "requiredness", requiredness.ToString());
            RequireProperty(serialized, "order").intValue = order;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static int CountSceneReadinessParticipants(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return 0;
            }

            int count = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                ActivityReadinessParticipant[] participants =
                    roots[rootIndex] == null
                        ? Array.Empty<ActivityReadinessParticipant>()
                        : roots[rootIndex].GetComponentsInChildren<
                            ActivityReadinessParticipant>(true);
                count += participants.Length;
            }

            return count;
        }

        private static int CountSceneDrivers(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return 0;
            }

            int count = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                QaParticipantAwareStartupParityDriver[] drivers =
                    roots[rootIndex] == null
                        ? Array.Empty<QaParticipantAwareStartupParityDriver>()
                        : roots[rootIndex].GetComponentsInChildren<
                            QaParticipantAwareStartupParityDriver>(true);
                count += drivers.Length;
            }

            return count;
        }

        private static void AddFixtureSceneToBuildSettings()
        {
            Require(CountBuildSettingsEntries(FixtureScenePath) == 0,
                "Q2B fixture scene already exists in Build Settings.");
            var scenes = new List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes)
            {
                new EditorBuildSettingsScene(FixtureScenePath, true)
            };
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void RemoveFixtureSceneFromBuildSettings()
        {
            EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
            var retained = new List<EditorBuildSettingsScene>(current.Length);
            for (int index = 0; index < current.Length; index++)
            {
                if (!string.Equals(
                        current[index].path,
                        FixtureScenePath,
                        StringComparison.Ordinal))
                {
                    retained.Add(current[index]);
                }
            }

            EditorBuildSettings.scenes = retained.ToArray();
        }

        private static int CountBuildSettingsEntries(string path)
        {
            int count = 0;
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int index = 0; index < scenes.Length; index++)
            {
                if (scenes[index].enabled &&
                    string.Equals(scenes[index].path, path,
                        StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
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
                RestoreInternal("automatic-post-play-restore",
                    logSuccess: true);
            }
            catch (Exception exception)
            {
                Debug.LogError($"{Prefix} status='RestoreFailed' " +
                    $"failure='{exception.GetType().Name}: {exception.Message}'.");
            }
        }

        private static void RestoreInternal(
            string reason,
            bool logSuccess)
        {
            Require(!EditorApplication.isPlaying,
                "Q2B restore must run outside Play Mode.");
            QaActivityEntryPresentationEvidenceSetup.ApplyCanonicalStandard(
                reason,
                markPreparedForNextPlay: false);
            RemoveFixtureSceneFromBuildSettings();
            if (AssetDatabase.IsValidFolder(GeneratedRoot))
            {
                Require(AssetDatabase.DeleteAsset(GeneratedRoot),
                    "Q2B generated fixture folder could not be deleted.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            SessionState.EraseBool(PreparedKey);
            SessionState.EraseString(ModeKey);
            SessionState.EraseBool(RestoreAfterPlayKey);
            if (logSuccess)
            {
                Debug.Log($"{Prefix} status='Restored' reason='{reason}' " +
                    "activeGameApplication='Canonical QA Hub' generated='Removed'.");
            }
        }

        private static void EnsureGeneratedFolder()
        {
            string[] segments = GeneratedRoot.Split('/');
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

        private static ImmersiveFrameworkSettingsAsset ResolveSettingsAsset()
        {
            ImmersiveFrameworkSettingsAsset settings =
                Resources.Load<ImmersiveFrameworkSettingsAsset>(
                    ImmersiveFrameworkSettingsAsset.ResourcesPath);
            Require(settings != null,
                "Immersive Framework Settings could not be resolved from Resources.");
            return settings;
        }

        private static SerializedProperty RequireProperty(
            SerializedObject serialized,
            string name)
        {
            SerializedProperty property = serialized.FindProperty(name);
            Require(property != null,
                $"Required serialized property '{name}' was not found.");
            return property;
        }

        private static void SetString(
            SerializedObject serialized,
            string name,
            string value)
        {
            RequireProperty(serialized, name).stringValue = value;
        }

        private static void SetEnum(
            SerializedObject serialized,
            string name,
            string value)
        {
            SerializedProperty property = RequireProperty(serialized, name);
            string[] names = property.enumNames;
            for (int index = 0; index < names.Length; index++)
            {
                if (string.Equals(names[index], value,
                    StringComparison.Ordinal))
                {
                    property.enumValueIndex = index;
                    return;
                }
            }

            throw new InvalidOperationException(
                $"Serialized enum '{name}' has no value '{value}'.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
