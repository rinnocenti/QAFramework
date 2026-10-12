using System;
using System.IO;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.RouteLifecycle;
using Immersive.QaFramework.IfAdr046;
using Immersive.QaFramework.PlayerSession;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.QaFramework.IfAdr046.Editor
{
    public static class QaIfAdr046Setup
    {
        private const string Root = "Assets/QA-IF-ADR-046";
        private const string Settings = Root + "/Settings";
        private const string Scenes = Root + "/Scenes";
        private const string AppPath = Settings + "/GameApplication_I4.asset";
        private const string RoutePath = Settings + "/Route_I4_A.asset";
        private const string ActivityPath = Settings + "/Activity_I4_A.asset";
        private const string ProfilePath = Settings + "/ActivityContentProfile_I4_A.asset";
        private const string RouteScenePath = Scenes + "/I4_Route_A.unity";
        private const string ActivityScenePath = Scenes + "/I4_Activity_A.unity";
        private const string PersistentScenePath = Scenes + "/I4_Persistent.unity";
        private const string SourceRoot = "Assets/QA-IF-ADR-PLAYER-SESSION";

        [MenuItem("Immersive Framework/QA/Player/IF-ADR-046 I4/Configure Minimal Join Fixture")]
        public static void Configure()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureFolder("Assets", "QA-IF-ADR-046");
            EnsureFolder(Root, "Settings");
            EnsureFolder(Root, "Scenes");
            PreflightCopiedAsset<GameApplicationAsset>(SourceRoot + "/Settings/GameApplication_QaPlayerSessionJoin.asset", AppPath);
            PreflightCopiedAsset<RouteAsset>(SourceRoot + "/Settings/Route_QaPlayerSessionJoin.asset", RoutePath);
            PreflightCopiedAsset<ActivityAsset>(SourceRoot + "/Settings/Activity_QaPlayerSessionJoin.asset", ActivityPath);
            PreflightCopiedAsset<ActivityContentProfileAsset>(SourceRoot + "/Settings/ActivityContentProfile_QaPlayerSessionJoin.asset", ProfilePath);
            PreflightCopiedAsset<SceneAsset>(SourceRoot + "/Scenes/QA_PlayerSessionJoin_Route.unity", RouteScenePath);
            PreflightCopiedAsset<SceneAsset>(SourceRoot + "/Scenes/QA_PlayerSessionJoin_Activity.unity", ActivityScenePath);
            PreflightCopiedAsset<SceneAsset>(SourceRoot + "/Scenes/QA_PlayerSessionJoin_Persistent.unity", PersistentScenePath);
            CopyOwnedAsset<GameApplicationAsset>(SourceRoot + "/Settings/GameApplication_QaPlayerSessionJoin.asset", AppPath);
            CopyOwnedAsset<RouteAsset>(SourceRoot + "/Settings/Route_QaPlayerSessionJoin.asset", RoutePath);
            CopyOwnedAsset<ActivityAsset>(SourceRoot + "/Settings/Activity_QaPlayerSessionJoin.asset", ActivityPath);
            CopyOwnedAsset<ActivityContentProfileAsset>(SourceRoot + "/Settings/ActivityContentProfile_QaPlayerSessionJoin.asset", ProfilePath);
            CopyOwnedAsset<SceneAsset>(SourceRoot + "/Scenes/QA_PlayerSessionJoin_Route.unity", RouteScenePath);
            CopyOwnedAsset<SceneAsset>(SourceRoot + "/Scenes/QA_PlayerSessionJoin_Activity.unity", ActivityScenePath);
            CopyOwnedAsset<SceneAsset>(SourceRoot + "/Scenes/QA_PlayerSessionJoin_Persistent.unity", PersistentScenePath);

            SetReference(Load<GameApplicationAsset>(AppPath), "startupRoute", Load<RouteAsset>(RoutePath));
            GameApplicationAsset application = Load<GameApplicationAsset>(AppPath);
            SerializedObject appObject = new SerializedObject(application);
            appObject.FindProperty("applicationName").stringValue = "QA IF-ADR-046 I4";
            SerializedProperty persistent = appObject.FindProperty("persistentContent");
            if (persistent == null) throw new InvalidOperationException("I4 setup cannot author Persistent Content: GameApplication has no persistentContent field.");
            persistent.FindPropertyRelative("scenePath").stringValue = PersistentScenePath;
            persistent.FindPropertyRelative("sceneName").stringValue = "I4_Persistent";
            appObject.ApplyModifiedPropertiesWithoutUndo();

            SetReference(Load<RouteAsset>(RoutePath), "startupActivity", Load<ActivityAsset>(ActivityPath));
            SerializedObject route = new SerializedObject(Load<RouteAsset>(RoutePath));
            route.FindProperty("routeId").stringValue = "qa.if-adr-046.i4.route-a";
            route.FindProperty("routeName").stringValue = "QA IF-ADR-046 Route A";
            route.FindProperty("primaryScenePath").stringValue = RouteScenePath;
            route.FindProperty("primarySceneName").stringValue = "I4_Route_A";
            route.ApplyModifiedPropertiesWithoutUndo();

            SetReference(Load<ActivityAsset>(ActivityPath), "activityContentProfile", Load<ActivityContentProfileAsset>(ProfilePath));
            SerializedObject activityObject = new SerializedObject(Load<ActivityAsset>(ActivityPath));
            activityObject.FindProperty("activityId").stringValue = "qa.if-adr-046.i4.activity-a";
            activityObject.FindProperty("activityName").stringValue = "QA IF-ADR-046 Activity A";
            activityObject.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject profile = new SerializedObject(Load<ActivityContentProfileAsset>(ProfilePath));
            profile.FindProperty("profileId").stringValue = "qa.if-adr-046.i4.activity-a-content";
            SerializedProperty entries = profile.FindProperty("scenes");
            if (entries.arraySize != 1) throw new InvalidOperationException("I4 Activity profile must have exactly one content scene.");
            entries.GetArrayElementAtIndex(0).FindPropertyRelative("scenePath").stringValue = ActivityScenePath;
            entries.GetArrayElementAtIndex(0).FindPropertyRelative("sceneName").stringValue = "I4_Activity_A";
            profile.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();

            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                ConfigureRouteScene();
                ConfigureActivityScene();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EnsureScenesInBuildSettings(RouteScenePath, ActivityScenePath, PersistentScenePath);
                ValidatePersistedAssets();
                Debug.Log("[QA-IF-ADR-046-I4] setup='Configured' fixture='minimal-activity-join' scenes='3/3-registered' slice1='untouched' execution='NOT_RUN'. Select GameApplication_I4 in ImmersiveFrameworkSettings, then open I4_Persistent and enter Play Mode.");
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(previous);
            }
        }

        private static void ConfigureRouteScene()
        {
            Scene scene = EditorSceneManager.OpenScene(RouteScenePath, OpenSceneMode.Single);
            RouteContentContribution[] contributions = FindComponents<RouteContentContribution>(scene);
            if (contributions.Length != 1) throw new InvalidOperationException("I4 Route A must contain exactly one RouteContentContribution.");
            SetReference(contributions[0], "route", Load<RouteAsset>(RoutePath));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureActivityScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ActivityScenePath, OpenSceneMode.Single);
            ActivityContentContribution[] contributions = FindComponents<ActivityContentContribution>(scene);
            PlayerSessionObserver[] observers = FindComponents<PlayerSessionObserver>(scene);
            if (contributions.Length != 1 || observers.Length != 1)
                throw new InvalidOperationException("I4 Activity A requires exactly one content contribution and one Session observer.");
            SetReference(contributions[0], "activity", Load<ActivityAsset>(ActivityPath));

            foreach (QaPlayerSessionJoinScenario oldScenario in FindComponents<QaPlayerSessionJoinScenario>(scene))
                UnityEngine.Object.DestroyImmediate(oldScenario);
            foreach (PlayerSessionJoinCommandTrigger oldTrigger in FindComponents<PlayerSessionJoinCommandTrigger>(scene))
                UnityEngine.Object.DestroyImmediate(oldTrigger);

            GameObject root = observers[0].gameObject;
            PlayerSessionJoinTrigger trigger = root.GetComponent<PlayerSessionJoinTrigger>();
            if (trigger == null) trigger = root.AddComponent<PlayerSessionJoinTrigger>();
            SerializedObject triggerObject = new SerializedObject(trigger);
            SerializedProperty scope = triggerObject.FindProperty("scope");
            scope.enumValueIndex = Array.IndexOf(scope.enumNames, nameof(LocalPlayerProvisioningConsumerScope.Activity));
            if (scope.enumValueIndex < 0) throw new InvalidOperationException("Activity scope is not present in the Join trigger enum.");
            triggerObject.FindProperty("controlScheme").stringValue = "QA.Keyboard";
            triggerObject.ApplyModifiedPropertiesWithoutUndo();
            QaIfAdr046JoinScenario scenario = root.GetComponent<QaIfAdr046JoinScenario>();
            if (scenario == null) scenario = root.AddComponent<QaIfAdr046JoinScenario>();
            SerializedObject scenarioObject = new SerializedObject(scenario);
            scenarioObject.FindProperty("observer").objectReferenceValue = observers[0];
            scenarioObject.FindProperty("joinTrigger").objectReferenceValue = trigger;
            scenarioObject.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ValidatePersistedAssets()
        {
            GameApplicationAsset app = Load<GameApplicationAsset>(AppPath);
            RouteAsset route = Load<RouteAsset>(RoutePath);
            ActivityAsset activity = Load<ActivityAsset>(ActivityPath);
            ActivityContentProfileAsset profile = Load<ActivityContentProfileAsset>(ProfilePath);
            if (app == null || route == null || activity == null || profile == null || app.PersistentContent == null ||
                !ReferenceEquals(app.StartupRoute, route) || app.DefaultPlayerSessionProfile == null ||
                !string.Equals(route.PrimaryScenePath, RouteScenePath, StringComparison.Ordinal) ||
                !string.Equals(route.PrimarySceneName, "I4_Route_A", StringComparison.Ordinal) ||
                !ReferenceEquals(route.StartupActivity, activity) || profile.SceneCount != 1 ||
                !string.Equals(profile.Scenes[0].ScenePath, ActivityScenePath, StringComparison.Ordinal) ||
                !string.Equals(profile.Scenes[0].SceneName, "I4_Activity_A", StringComparison.Ordinal) ||
                !string.Equals(app.PersistentContent.ContainerScenePath, PersistentScenePath, StringComparison.Ordinal) ||
                !string.Equals(app.PersistentContent.ContainerSceneName, "I4_Persistent", StringComparison.Ordinal))
                throw new InvalidOperationException("Persisted I4 asset graph failed postflight validation.");
            ValidateSceneRegistration(RouteScenePath, "I4_Route_A");
            ValidateSceneRegistration(ActivityScenePath, "I4_Activity_A");
            ValidateSceneRegistration(PersistentScenePath, "I4_Persistent");
            if (!app.DefaultPlayerSessionProfile.TryValidate(out string sessionIssue) ||
                app.DefaultPlayerSessionProfile.HostProvisioning != PlayerHostProvisioningMode.ManagerProvisioned)
                throw new InvalidOperationException("I4 Session profile is invalid or not Manager-Provisioned. " + sessionIssue);
            if (FindComponents<PlayerSessionJoinTrigger>(EditorSceneManager.OpenScene(ActivityScenePath, OpenSceneMode.Single)).Length != 1)
                throw new InvalidOperationException("Persisted I4 Activity must contain exactly one new public Join trigger.");
            Scene persistentScene = EditorSceneManager.OpenScene(PersistentScenePath, OpenSceneMode.Single);
            LocalPlayerProvisioningEndpointRegistration[] endpoint = FindComponents<LocalPlayerProvisioningEndpointRegistration>(persistentScene);
            LocalPlayerProvisioningAuthoring[] authoring = FindComponents<LocalPlayerProvisioningAuthoring>(persistentScene);
            UnityEngine.InputSystem.PlayerInputManager[] managers = FindComponents<UnityEngine.InputSystem.PlayerInputManager>(persistentScene);
            if (endpoint.Length != 1 || authoring.Length != 1 || managers.Length != 1 ||
                !ReferenceEquals(endpoint[0].ProvisioningAuthoring, authoring[0]) ||
                !ReferenceEquals(authoring[0].PlayerInputManager, managers[0]) ||
                authoring[0].LocalPlayerHostPrefab == null || !authoring[0].IsManagerPrefabMaterialized)
                throw new InvalidOperationException("Persisted I4 Persistent scene requires one explicit Manager-Provisioned endpoint, authoring component and PlayerInputManager.");
        }

        private static void PreflightCopiedAsset<T>(string source, string destination) where T : UnityEngine.Object
        {
            if (Load<T>(source) == null) throw new InvalidOperationException("Missing or wrong-typed source asset: " + source);
            if (File.Exists(destination) && Load<T>(destination) == null)
                throw new InvalidOperationException("Refusing to overwrite unexpected I4 content: " + destination);
        }

        private static void CopyOwnedAsset<T>(string source, string destination) where T : UnityEngine.Object
        {
            if (File.Exists(destination))
            {
                if (Load<T>(destination) == null) throw new InvalidOperationException("Unexpected content at owned I4 path: " + destination);
                return;
            }
            if (Load<T>(source) == null) throw new InvalidOperationException("Missing or wrong-typed source asset: " + source);
            if (!AssetDatabase.CopyAsset(source, destination)) throw new InvalidOperationException("Could not copy QA source to I4-owned path: " + destination);
        }

        private static T Load<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path);

        private static void SetReference(UnityEngine.Object target, string property, UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty field = serialized.FindProperty(property);
            if (field == null) throw new InvalidOperationException("Serialized field not found: " + property);
            field.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T[] FindComponents<T>(Scene scene) where T : Component
        {
            var found = new System.Collections.Generic.List<T>();
            foreach (GameObject root in scene.GetRootGameObjects())
                found.AddRange(root.GetComponentsInChildren<T>(true));
            return found.ToArray();
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }

        internal static void EnsureScenesInBuildSettings(params string[] paths)
        {
            EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
            var result = new System.Collections.Generic.List<EditorBuildSettingsScene>(existing);
            foreach (string path in paths)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                    throw new InvalidOperationException($"I4 scene registration failed: SceneAsset is missing at '{path}'.");

                int retained = -1;
                for (int index = result.Count - 1; index >= 0; index--)
                {
                    if (!string.Equals(result[index].path, path, StringComparison.Ordinal)) continue;
                    if (retained < 0)
                    {
                        retained = index;
                        result[index] = new EditorBuildSettingsScene(path, true);
                    }
                    else
                    {
                        result.RemoveAt(index);
                        retained--;
                    }
                }
                if (retained < 0) result.Add(new EditorBuildSettingsScene(path, true));
            }

            EditorBuildSettings.scenes = result.ToArray();
            foreach (string path in paths) ValidateSceneRegistration(path, Path.GetFileNameWithoutExtension(path));
        }

        internal static void ValidateSceneRegistration(string path, string expectedName)
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            if (sceneAsset == null)
                throw new InvalidOperationException($"I4 pre-validation failed: scene asset missing at '{path}'.");
            if (!string.Equals(sceneAsset.name, expectedName, StringComparison.Ordinal))
                throw new InvalidOperationException($"I4 pre-validation failed: expected scene name '{expectedName}' at '{path}', found '{sceneAsset.name}'.");

            int enabledMatches = 0;
            foreach (EditorBuildSettingsScene entry in EditorBuildSettings.scenes)
                if (string.Equals(entry.path, path, StringComparison.Ordinal) && entry.enabled) enabledMatches++;
            if (enabledMatches != 1)
                throw new InvalidOperationException($"I4 pre-validation failed: expected exactly one enabled Build Settings/Shared Scene List entry for '{path}', found '{enabledMatches}'.");
        }
    }
}
