using System;
using System.Collections.Generic;
using Immersive.Framework.ActivityRestart;
using Immersive.Framework.Authoring;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.GameFlow;
using Immersive.Framework.ObjectEntry;
using Immersive.QaFramework.IfAdr014;
using Immersive.QaFramework.IfAdr014.C4;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.QaFramework.IfAdr014.Editor
{
    public static class QaIfAdr014C4Setup
    {
        private const string Root = "Assets/QA-IF-ADR-014/C4";
        private const string Generated = Root + "/Generated";
        private const string Settings = Generated + "/Settings";
        private const string Scenes = Generated + "/Scenes";
        private const string Prefabs = Generated + "/Prefabs";

        private const string ApplicationSource =
            "Assets/QA-NEW-001/Settings/GameApplication_QaNew001.asset";
        private const string RouteSource = "Assets/QA-NEW-001/Settings/Route_QaNew001.asset";

        private const string ApplicationPath = Settings + "/GameApplication_QaIfAdr014C4.asset";
        private const string RoutePath = Settings + "/Route_QaIfAdr014C4.asset";
        private const string ReplacementRoutePath = Settings + "/Route_QaIfAdr014C4_Replacement.asset";
        private const string ActivityPath = Settings + "/Activity_QaIfAdr014C4.asset";
        private const string ActivityProfilePath = Settings + "/ActivityContentProfile_QaIfAdr014C4.asset";
        private const string CompositionProfilePath = Settings + "/ObjectEntryComposition_QaIfAdr014C4_Base.asset";
        private const string PersistentScenePath = Scenes + "/QA_IF_ADR_014_C4_Persistent.unity";
        private const string RouteScenePath = Scenes + "/QA_IF_ADR_014_C4_Route.unity";
        private const string ReplacementRouteScenePath = Scenes + "/QA_IF_ADR_014_C4_ReplacementRoute.unity";
        private const string ActivityScenePath = Scenes + "/QA_IF_ADR_014_C4_Activity.unity";

        private static readonly string[] PartPrefabIds =
        {
            "base",
            "simple",
            "replacement",
            "override",
            "independent-a",
            "independent-b",
            "priority-low",
            "priority-high",
            "conflict-first",
            "conflict-rejected",
            "hidden-tie-first",
            "hidden-tie-second",
            "hidden-tie-winner",
            "dependency-prerequisite",
            "dependent",
            "missing-dependency",
            "activity"
        };

        [MenuItem("Immersive Framework/QA/IF-ADR-014/C4/Generate Dynamic Composition Certification")]
        public static void Generate()
        {
            if (AssetDatabase.IsValidFolder(Generated) && HasGeneratedFiles())
            {
                ValidatePreparedAssets();
                Debug.Log("[IF-ADR-014-C4] setup='AlreadyPrepared' generated='" + Generated +
                    "' next='Add the four C4 scenes to the active Build Profile / Shared Scene List and select GameApplication_QaIfAdr014C4.'");
                return;
            }

            if (!AssetDatabase.IsValidFolder(Generated) &&
                System.IO.Directory.Exists(System.IO.Path.Combine(
                    Application.dataPath, "QA-IF-ADR-014/C4/Generated")))
            {
                AssetDatabase.Refresh();
                if (AssetDatabase.IsValidFolder(Generated))
                {
                    if (HasGeneratedFiles())
                    {
                        ValidatePreparedAssets();
                        return;
                    }
                }
                else
                {
                    throw new InvalidOperationException(
                        "The C4 Generated directory exists on disk but Unity did not import it. Existing files were preserved.");
                }
            }

            ValidateTemplates();
            SceneSetup[] previousSetup = CaptureRestorableSceneSetup();
            try
            {
                EnsureFolder(Root, "Generated");
                EnsureFolder(Generated, "Settings");
                EnsureFolder(Generated, "Scenes");
                EnsureFolder(Generated, "Prefabs");

                GameObject[] partPrefabs = CreatePartPrefabs();
                ObjectEntryCompositionProfile compositionProfile =
                    CreateCompositionProfile(partPrefabs[0]);
                ActivityContentProfileAsset activityProfile = CreateActivityProfile();
                ActivityAsset activity = CreateActivity(activityProfile);
                RouteAsset route = CreateRoute(RoutePath, "qa.if-adr-014.c4.route",
                    "IF-ADR-014 C4 Dynamic Composition", RouteScenePath,
                    "QA_IF_ADR_014_C4_Route");
                RouteAsset replacementRoute = CreateRoute(
                    ReplacementRoutePath,
                    "qa.if-adr-014.c4.replacement-route",
                    "IF-ADR-014 C4 Replacement Route",
                    ReplacementRouteScenePath,
                    "QA_IF_ADR_014_C4_ReplacementRoute");

                CreatePersistentScene(activity, replacementRoute, partPrefabs);
                CreateRouteScene(compositionProfile);
                CreateActivityScene();
                CreateReplacementRouteScene();
                CreateApplication(route);

                AssetDatabase.Refresh();
                ValidatePreparedAssets();
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }

            Debug.Log("[IF-ADR-014-C4] setup='Prepared' scenes='4' generated='" + Generated +
                "' projectSettingsChanged='false' b2AssetsChanged='false' next='Add all generated scenes to the active Build Profile / Shared Scene List and select GameApplication_QaIfAdr014C4.'");
        }

        private static void ValidateTemplates()
        {
            if (AssetDatabase.LoadAssetAtPath<GameApplicationAsset>(ApplicationSource) == null)
                throw new InvalidOperationException("Missing QA-NEW-001 Game Application template: " + ApplicationSource);
            if (AssetDatabase.LoadAssetAtPath<RouteAsset>(RouteSource) == null)
                throw new InvalidOperationException("Missing QA-NEW-001 Route template: " + RouteSource);
        }

        private static bool HasGeneratedFiles()
        {
            string absoluteGeneratedPath = System.IO.Path.Combine(
                Application.dataPath, "QA-IF-ADR-014/C4/Generated");
            if (!System.IO.Directory.Exists(absoluteGeneratedPath))
                return false;

            foreach (string filePath in System.IO.Directory.GetFiles(
                         absoluteGeneratedPath, "*", System.IO.SearchOption.AllDirectories))
            {
                if (!filePath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    return true;

                string metadataOwnerPath = filePath.Substring(0, filePath.Length - ".meta".Length);
                if (!System.IO.Directory.Exists(metadataOwnerPath))
                    return true;
            }

            return false;
        }

        private static SceneSetup[] CaptureRestorableSceneSetup()
        {
            EnsureRestorableActiveScene();
            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
            if (!HasLoadedActiveScene(setup))
                throw new InvalidOperationException(
                    "Could not capture a restorable C4 Editor scene setup.");

            return setup;
        }

        private static void EnsureRestorableActiveScene()
        {
            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
            if (HasLoadedActiveScene(setup))
                return;

            bool hasLoadedScene = false;
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene loadedScene = SceneManager.GetSceneAt(index);
                if (!loadedScene.IsValid() || !loadedScene.isLoaded)
                    continue;

                hasLoadedScene = true;
                if (!SceneManager.SetActiveScene(loadedScene))
                    continue;

                setup = EditorSceneManager.GetSceneManagerSetup();
                if (HasLoadedActiveScene(setup))
                    return;
            }

            Scene fallbackScene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                hasLoadedScene ? NewSceneMode.Additive : NewSceneMode.Single);
            if (!fallbackScene.IsValid() ||
                !fallbackScene.isLoaded ||
                (SceneManager.GetActiveScene() != fallbackScene &&
                 !SceneManager.SetActiveScene(fallbackScene)))
                throw new InvalidOperationException(
                    "Could not create and activate a restorable empty Editor scene for C4 setup.");

            setup = EditorSceneManager.GetSceneManagerSetup();
            if (!HasLoadedActiveScene(setup))
                throw new InvalidOperationException(
                    "Could not create a restorable empty Editor scene for C4 setup.");
        }

        private static bool HasLoadedActiveScene(SceneSetup[] setup)
        {
            if (setup == null)
                return false;

            bool setupHasActiveLoadedScene = false;
            foreach (SceneSetup sceneSetup in setup)
            {
                if (sceneSetup.isLoaded && sceneSetup.isActive)
                {
                    setupHasActiveLoadedScene = true;
                    break;
                }
            }

            Scene activeScene = SceneManager.GetActiveScene();
            return setupHasActiveLoadedScene && activeScene.IsValid() && activeScene.isLoaded;
        }

        private static GameObject[] CreatePartPrefabs()
        {
            var prefabs = new GameObject[PartPrefabIds.Length];
            Scene authoringScene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                if (!SceneManager.SetActiveScene(authoringScene))
                    throw new InvalidOperationException("Could not activate the isolated C4 prefab authoring scene.");

                for (int index = 0; index < PartPrefabIds.Length; index++)
                {
                    string prefabPath = Prefabs + "/QA_IF_ADR_014_C4_Part_" +
                        PartPrefabIds[index] + ".prefab";
                    EnsurePathAvailable(prefabPath);
                    GameObject root = new GameObject("QA_IF_ADR_014_C4_Part");
                    QaIfAdr014C4PartWitness witness = root.AddComponent<QaIfAdr014C4PartWitness>();
                    Set(witness, "prefabId", PartPrefabIds[index]);
                    try
                    {
                        prefabs[index] = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(root);
                    }

                    if (prefabs[index] == null)
                        throw new InvalidOperationException("Could not create C4 part prefab: " + prefabPath);
                }
            }
            finally
            {
                if (authoringScene.IsValid() && authoringScene.isLoaded &&
                    !EditorSceneManager.CloseScene(authoringScene, true))
                    throw new InvalidOperationException("Could not close the isolated C4 prefab authoring scene.");
            }
            return prefabs;
        }

        private static ObjectEntryCompositionProfile CreateCompositionProfile(GameObject basePrefab)
        {
            string path = CompositionProfilePath;
            EnsurePathAvailable(path);
            ObjectEntryCompositionProfile profile =
                ScriptableObject.CreateInstance<ObjectEntryCompositionProfile>();
            AssetDatabase.CreateAsset(profile, path);
            SerializedObject serialized = new SerializedObject(profile);
            serialized.Update();
            SerializedProperty parts = Required(serialized, "parts");
            parts.arraySize = 1;
            SerializedProperty part = parts.GetArrayElementAtIndex(0);
            Required(part, "key").stringValue = "slot.primary";
            Required(part, "prefab").objectReferenceValue = basePrefab;
            Required(part, "dependencies").arraySize = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssetIfDirty(profile);
            if (!profile.TryValidate(out string issue))
                throw new InvalidOperationException("Generated C4 Base Profile is invalid: " + issue);
            return profile;
        }

        private static ActivityContentProfileAsset CreateActivityProfile()
        {
            EnsurePathAvailable(ActivityProfilePath);
            ActivityContentProfileAsset profile =
                ScriptableObject.CreateInstance<ActivityContentProfileAsset>();
            AssetDatabase.CreateAsset(profile, ActivityProfilePath);
            Set(profile, "profileId", "qa.if-adr-014.c4.activity-content");

            SerializedObject serialized = new SerializedObject(profile);
            serialized.Update();
            SerializedProperty scenes = Required(serialized, "scenes");
            scenes.arraySize = 1;
            SerializedProperty entry = scenes.GetArrayElementAtIndex(0);
            Required(entry, "contentId").stringValue = "qa.if-adr-014.c4.activity.main";
            Required(entry, "scenePath").stringValue = ActivityScenePath;
            Required(entry, "sceneName").stringValue = "QA_IF_ADR_014_C4_Activity";
            SetEnum(Required(entry, "requiredness"), "Required");
            SetEnum(Required(entry, "loadMode"), "Additive");
            SetEnum(Required(entry, "releasePolicy"), "ReleaseOnActivityChange");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssetIfDirty(profile);
            return profile;
        }

        private static ActivityAsset CreateActivity(ActivityContentProfileAsset profile)
        {
            EnsurePathAvailable(ActivityPath);
            ActivityAsset activity = ScriptableObject.CreateInstance<ActivityAsset>();
            AssetDatabase.CreateAsset(activity, ActivityPath);
            Set(activity, "activityId", "qa.if-adr-014.c4.activity");
            Set(activity, "activityName", "IF-ADR-014 C4 Dynamic Composition");
            Set(activity, "activityContentProfile", profile);
            AssetDatabase.SaveAssetIfDirty(activity);
            return activity;
        }

        private static RouteAsset CreateRoute(
            string path,
            string routeId,
            string routeName,
            string scenePath,
            string sceneName)
        {
            EnsurePathAvailable(path);
            if (!AssetDatabase.CopyAsset(RouteSource, path))
                throw new InvalidOperationException("Could not copy QA-NEW-001 Route template to " + path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            RouteAsset route = AssetDatabase.LoadAssetAtPath<RouteAsset>(path);
            if (route == null)
                throw new InvalidOperationException("Generated C4 Route did not import: " + path);
            Set(route, "routeId", routeId);
            Set(route, "routeName", routeName);
            Set(route, "primaryScenePath", scenePath);
            Set(route, "primarySceneName", sceneName);
            Set(route, "startupActivity", (UnityEngine.Object)null);
            AssetDatabase.SaveAssetIfDirty(route);
            return route;
        }

        private static void CreatePersistentScene(
            ActivityAsset activity,
            RouteAsset replacementRoute,
            GameObject[] partPrefabs)
        {
            BuildScene(PersistentScenePath, () =>
            {
                GameObject controller = new GameObject("IF-ADR-014 C4 Dynamic Composition Scenario");
                ActivityRequestTrigger activityRequest = controller.AddComponent<ActivityRequestTrigger>();
                ActivityRestartTrigger activityRestart = controller.AddComponent<ActivityRestartTrigger>();
                RouteRequestTrigger routeRequest = controller.AddComponent<RouteRequestTrigger>();
                QaIfAdr014C4DynamicCompositionScenario scenario =
                    controller.AddComponent<QaIfAdr014C4DynamicCompositionScenario>();

                activityRequest.TargetActivity = activity;
                activityRestart.TargetActivity = activity;
                routeRequest.TargetRoute = replacementRoute;

                SerializedObject serialized = new SerializedObject(scenario);
                serialized.Update();
                Required(serialized, "activityRequest").objectReferenceValue = activityRequest;
                Required(serialized, "activityRestart").objectReferenceValue = activityRestart;
                Required(serialized, "activity").objectReferenceValue = activity;
                Required(serialized, "routeRequest").objectReferenceValue = routeRequest;
                Required(serialized, "replacementRoute").objectReferenceValue = replacementRoute;
                SerializedProperty prefabs = Required(serialized, "partPrefabs");
                prefabs.arraySize = partPrefabs.Length;
                for (int index = 0; index < partPrefabs.Length; index++)
                    prefabs.GetArrayElementAtIndex(index).objectReferenceValue = partPrefabs[index];
                serialized.ApplyModifiedPropertiesWithoutUndo();
            });
        }

        private static void CreateRouteScene(ObjectEntryCompositionProfile profile)
        {
            BuildScene(RouteScenePath, () =>
            {
                new GameObject("C4 Route Object Entry Query")
                    .AddComponent<ObjectEntryOccurrenceQueryBinding>();
                CreateEntry(QaIfAdr014C4DynamicCompositionScenario.PlainEntryId, null, false, true);
                CreateEntry(QaIfAdr014C4DynamicCompositionScenario.ProfileEntryId, profile, false, true);
                CreateEntry(QaIfAdr014C4DynamicCompositionScenario.DynamicEntryId, null, true, true);
                CreateEntry(QaIfAdr014C4DynamicCompositionScenario.BaseDynamicEntryId, profile, true, true);
            });
        }

        private static void CreateActivityScene()
        {
            BuildScene(ActivityScenePath, () =>
            {
                new GameObject("C4 Activity Object Entry Query")
                    .AddComponent<ObjectEntryOccurrenceQueryBinding>();
                CreateEntry(QaIfAdr014C4DynamicCompositionScenario.ActivityEntryId, null, true, true);
            });
        }

        private static void CreateReplacementRouteScene()
        {
            BuildScene(ReplacementRouteScenePath, () =>
            {
                new GameObject("C4 Replacement Route Content");
            });
        }

        private static void CreateEntry(
            string stableId,
            ObjectEntryCompositionProfile profile,
            bool allowDynamicContributions,
            bool addCollider)
        {
            GameObject root = new GameObject("C4 Object Entry Fixture");
            ObjectEntryDeclaration declaration = root.AddComponent<ObjectEntryDeclaration>();
            Set(declaration, "objectEntryId", stableId);
            if (profile != null || allowDynamicContributions)
            {
                ObjectEntryCompositionAuthoring authoring =
                    root.AddComponent<ObjectEntryCompositionAuthoring>();
                Set(authoring, "profile", profile);
                Set(authoring, "allowDynamicContributions", allowDynamicContributions);
            }
            if (addCollider)
                root.AddComponent<BoxCollider>();
        }

        private static void CreateApplication(RouteAsset route)
        {
            EnsurePathAvailable(ApplicationPath);
            if (!AssetDatabase.CopyAsset(ApplicationSource, ApplicationPath))
                throw new InvalidOperationException("Could not copy QA-NEW-001 Game Application template.");
            AssetDatabase.ImportAsset(ApplicationPath, ImportAssetOptions.ForceUpdate);
            GameApplicationAsset application =
                AssetDatabase.LoadAssetAtPath<GameApplicationAsset>(ApplicationPath);
            SceneAsset persistentScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(PersistentScenePath);
            if (application == null || persistentScene == null)
                throw new InvalidOperationException("Generated C4 Game Application or Persistent Scene did not import.");

            Set(application, "applicationName", "IF-ADR-014 C4 Dynamic Composition Certification");
            Set(application, "startupRoute", route);
            SerializedObject serialized = new SerializedObject(application);
            serialized.Update();
            SerializedProperty persistent = Required(serialized, "persistentContent");
            Required(persistent, "scenePath").stringValue = PersistentScenePath;
            Required(persistent, "sceneName").stringValue = "QA_IF_ADR_014_C4_Persistent";
            Required(persistent, "containerScene").objectReferenceValue = persistentScene;
            Required(serialized, "playerSessionEnabled").boolValue = false;
            Required(serialized, "defaultPlayerSessionProfile").objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(application);
            AssetDatabase.SaveAssetIfDirty(application);
        }

        private static void BuildScene(string path, Action author)
        {
            EnsurePathAvailable(path);
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                if (!SceneManager.SetActiveScene(scene))
                    throw new InvalidOperationException("Could not set the generated C4 scene active: " + path);
                author();
                if (!EditorSceneManager.SaveScene(scene, path))
                    throw new InvalidOperationException("Could not save generated C4 scene: " + path);
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded &&
                    !EditorSceneManager.CloseScene(scene, true))
                    throw new InvalidOperationException("Could not close generated C4 scene after authoring: " + path);
            }
        }

        private static void ValidatePreparedAssets()
        {
            var issues = new List<string>();
            GameApplicationAsset application =
                AssetDatabase.LoadAssetAtPath<GameApplicationAsset>(ApplicationPath);
            RouteAsset route = AssetDatabase.LoadAssetAtPath<RouteAsset>(RoutePath);
            RouteAsset replacement = AssetDatabase.LoadAssetAtPath<RouteAsset>(ReplacementRoutePath);
            ActivityAsset activity = AssetDatabase.LoadAssetAtPath<ActivityAsset>(ActivityPath);
            ObjectEntryCompositionProfile profile =
                AssetDatabase.LoadAssetAtPath<ObjectEntryCompositionProfile>(CompositionProfilePath);
            ActivityContentProfileAsset activityProfile =
                AssetDatabase.LoadAssetAtPath<ActivityContentProfileAsset>(ActivityProfilePath);

            if (application == null || route == null || replacement == null ||
                activity == null || profile == null || activityProfile == null)
                issues.Add("One or more expected C4 settings assets are missing or have the wrong type.");
            else
            {
                if (!route.HasValidRouteId || route.RouteId.StableText != "qa.if-adr-014.c4.route" ||
                    route.PrimaryScenePath != RouteScenePath)
                    issues.Add("C4 Route identity or primary scene conflicts with the prepared fixture.");
                if (!replacement.HasValidRouteId ||
                    replacement.RouteId.StableText != "qa.if-adr-014.c4.replacement-route" ||
                    replacement.PrimaryScenePath != ReplacementRouteScenePath)
                    issues.Add("C4 replacement Route identity or scene conflicts with the prepared fixture.");
                if (application.StartupRoute != route)
                    issues.Add("C4 Game Application does not reference the generated C4 Route.");
                if (application.PlayerSessionEnabled)
                    issues.Add("C4 Game Application must not enable a Player Session for this non-gameplay QA fixture.");
                if (route.StartupActivity != null || replacement.StartupActivity != null)
                    issues.Add("C4 Routes must not start an Activity before the scenario controller requests it.");
                if (!application.HasPersistentContentComposition ||
                    application.PersistentContent.ContainerScenePath != PersistentScenePath)
                    issues.Add("C4 Game Application does not reference its generated Persistent scene.");
                if (!profile.TryValidate(out string profileIssue))
                    issues.Add("C4 Base Profile is invalid: " + profileIssue);
                if (activityProfile.ProfileId != "qa.if-adr-014.c4.activity-content" ||
                    activityProfile.SceneCount != 1 ||
                    activityProfile.Scenes[0].ContentId != "qa.if-adr-014.c4.activity.main" ||
                    activityProfile.Scenes[0].ScenePath != ActivityScenePath ||
                    activityProfile.Scenes[0].SceneName != "QA_IF_ADR_014_C4_Activity" ||
                    activityProfile.Scenes[0].Requiredness != FrameworkContentRequiredness.Required ||
                    activityProfile.Scenes[0].LoadMode != ActivityContentSceneLoadMode.Additive ||
                    activityProfile.Scenes[0].ReleasePolicy != ActivityContentReleasePolicy.ReleaseOnActivityChange)
                    issues.Add("C4 Activity Content Profile does not reference its generated scene.");
                if (!activity.HasValidActivityId ||
                    activity.ActivityId.StableText != "qa.if-adr-014.c4.activity" ||
                    activity.ActivityContentProfile != activityProfile)
                    issues.Add("C4 Activity identity or content-profile reference conflicts with the prepared fixture.");
            }

            foreach (string id in PartPrefabIds)
            {
                string path = Prefabs + "/QA_IF_ADR_014_C4_Part_" + id + ".prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                QaIfAdr014C4PartWitness witness = prefab != null
                    ? prefab.GetComponent<QaIfAdr014C4PartWitness>()
                    : null;
                if (witness == null || witness.PrefabId != id ||
                    !prefab.activeSelf || !witness.enabled ||
                    prefab.GetComponentsInChildren<Component>(true).Length != 2 ||
                    prefab.GetComponentInChildren<ObjectEntryDeclaration>(true) != null ||
                    !PrefabUtility.IsPartOfPrefabAsset(prefab))
                    issues.Add("C4 part prefab is missing, inactive, contains an unexpected component or has a conflicting witness: " + path);
            }

            foreach (string path in new[]
                     {
                         PersistentScenePath, RouteScenePath,
                         ReplacementRouteScenePath, ActivityScenePath
                     })
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                    issues.Add("C4 scene is missing or has a conflicting asset type: " + path);
            }

            if (issues.Count > 0)
                throw new InvalidOperationException(
                    "Existing C4 generated assets are incomplete or inconsistent. They were preserved; " +
                    "resolve the listed conflict manually before rerunning setup. " +
                    string.Join(" ", issues));

            ValidateCompositionProfileReferences(profile);
            ValidateGeneratedSceneContents(activity, replacement, profile);
        }

        private static void ValidateCompositionProfileReferences(
            ObjectEntryCompositionProfile profile)
        {
            SerializedObject serialized = new SerializedObject(profile);
            serialized.Update();
            SerializedProperty parts = Required(serialized, "parts");
            if (parts.arraySize != 1)
                throw new InvalidOperationException("C4 Base Profile must contain exactly one authored part.");
            SerializedProperty part = parts.GetArrayElementAtIndex(0);
            GameObject expectedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                Prefabs + "/QA_IF_ADR_014_C4_Part_base.prefab");
            if (Required(part, "key").stringValue != "slot.primary" ||
                Required(part, "prefab").objectReferenceValue != expectedPrefab ||
                Required(part, "dependencies").arraySize != 0)
                throw new InvalidOperationException("C4 Base Profile references conflict with the generated Base prefab.");
        }

        private static void ValidateGeneratedSceneContents(
            ActivityAsset activity,
            RouteAsset replacement,
            ObjectEntryCompositionProfile profile)
        {
            SceneSetup[] previousSetup = CaptureRestorableSceneSetup();
            try
            {
                ValidateScene(RouteScenePath, scene =>
                {
                    RequireNoB2Controller(scene);
                    RequireNoC4ScenarioController(scene);
                    RequireOneQueryBinding(scene);
                    if (FindComponents<ObjectEntryDeclaration>(scene).Length != 4)
                        throw new InvalidOperationException("C4 Route scene must contain exactly its four declared test entries.");
                    RequireEntry(scene, QaIfAdr014C4DynamicCompositionScenario.PlainEntryId,
                        null, false, false);
                    RequireEntry(scene, QaIfAdr014C4DynamicCompositionScenario.ProfileEntryId,
                        profile, false, true);
                    RequireEntry(scene, QaIfAdr014C4DynamicCompositionScenario.DynamicEntryId,
                        null, true, true);
                    RequireEntry(scene, QaIfAdr014C4DynamicCompositionScenario.BaseDynamicEntryId,
                        profile, true, true);
                });
                ValidateScene(ActivityScenePath, scene =>
                {
                    RequireNoB2Controller(scene);
                    RequireNoC4ScenarioController(scene);
                    RequireOneQueryBinding(scene);
                    if (FindComponents<ObjectEntryDeclaration>(scene).Length != 1)
                        throw new InvalidOperationException("C4 Activity scene must contain exactly one declared test entry.");
                    RequireEntry(scene, QaIfAdr014C4DynamicCompositionScenario.ActivityEntryId,
                        null, true, true);
                });
                ValidateScene(PersistentScenePath, scene =>
                {
                    RequireNoB2Controller(scene);
                    QaIfAdr014C4DynamicCompositionScenario[] scenarios =
                        FindComponents<QaIfAdr014C4DynamicCompositionScenario>(scene);
                    if (scenarios.Length != 1 || !scenarios[0].isActiveAndEnabled ||
                        !scenarios[0].gameObject.activeInHierarchy)
                        throw new InvalidOperationException("C4 Persistent scene must contain exactly one active scenario controller.");
                    QaIfAdr014C4DynamicCompositionScenario scenario = scenarios[0];
                    ActivityRequestTrigger[] activityRequests = FindComponents<ActivityRequestTrigger>(scene);
                    ActivityRestartTrigger[] activityRestarts = FindComponents<ActivityRestartTrigger>(scene);
                    RouteRequestTrigger[] routeRequests = FindComponents<RouteRequestTrigger>(scene);
                    if (activityRequests.Length != 1 || activityRestarts.Length != 1 ||
                        routeRequests.Length != 1 ||
                        !activityRequests[0].isActiveAndEnabled ||
                        !activityRestarts[0].isActiveAndEnabled ||
                        !routeRequests[0].isActiveAndEnabled ||
                        activityRequests[0].TargetActivity != activity ||
                        activityRestarts[0].TargetActivity != activity ||
                        routeRequests[0].TargetRoute != replacement)
                        throw new InvalidOperationException("C4 Persistent trigger references are missing or inconsistent.");

                    SerializedObject serialized = new SerializedObject(scenario);
                    serialized.Update();
                    if (Required(serialized, "activityRequest").objectReferenceValue != activityRequests[0] ||
                        Required(serialized, "activityRestart").objectReferenceValue != activityRestarts[0] ||
                        Required(serialized, "activity").objectReferenceValue != activity ||
                        Required(serialized, "routeRequest").objectReferenceValue != routeRequests[0] ||
                        Required(serialized, "replacementRoute").objectReferenceValue != replacement)
                        throw new InvalidOperationException("C4 scenario references do not match its public GameFlow triggers.");

                    SerializedProperty prefabs = Required(serialized, "partPrefabs");
                    if (prefabs.arraySize != PartPrefabIds.Length)
                        throw new InvalidOperationException("C4 scenario has an unexpected dynamic-prefab fixture count.");
                    for (int index = 0; index < PartPrefabIds.Length; index++)
                    {
                        string path = Prefabs + "/QA_IF_ADR_014_C4_Part_" +
                            PartPrefabIds[index] + ".prefab";
                        if (prefabs.GetArrayElementAtIndex(index).objectReferenceValue !=
                            AssetDatabase.LoadAssetAtPath<GameObject>(path))
                            throw new InvalidOperationException(
                                "C4 scenario prefab reference conflicts at fixture '" + PartPrefabIds[index] + "'.");
                    }
                });
                ValidateScene(ReplacementRouteScenePath, scene =>
                {
                    RequireNoB2Controller(scene);
                    RequireNoC4ScenarioController(scene);
                    if (FindComponents<ObjectEntryDeclaration>(scene).Length != 0)
                        throw new InvalidOperationException("C4 Replacement Route scene must not declare another test Object Entry.");
                });
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
        }

        private static void ValidateScene(string path, Action<Scene> validate)
        {
            Scene scene = EditorSceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (!opened && scene.isDirty)
                throw new InvalidOperationException(
                    "C4 scene is open with unsaved changes; it was preserved and not validated: " + path);
            if (opened)
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException("Could not inspect generated C4 scene: " + path);
            try
            {
                if (scene.isDirty)
                    throw new InvalidOperationException(
                        "C4 scene became dirty while being inspected; it was preserved: " + path);
                validate(scene);
            }
            finally
            {
                if (opened && scene.IsValid() && scene.isLoaded &&
                    !EditorSceneManager.CloseScene(scene, true))
                    throw new InvalidOperationException("Could not close inspected C4 scene: " + path);
            }
        }

        private static void RequireOneQueryBinding(Scene scene)
        {
            ObjectEntryOccurrenceQueryBinding[] bindings =
                FindComponents<ObjectEntryOccurrenceQueryBinding>(scene);
            if (bindings.Length != 1 || !bindings[0].isActiveAndEnabled ||
                !bindings[0].gameObject.activeInHierarchy)
                throw new InvalidOperationException("C4 owner scene must contain exactly one read-only Object Entry query binding.");
        }

        private static void RequireNoB2Controller(Scene scene)
        {
            if (FindComponents<QaIfAdr014ObjectEntryRecognitionDemo>(scene).Length != 0)
                throw new InvalidOperationException("C4 scenes must not include the B2 controller.");
        }

        private static void RequireNoC4ScenarioController(Scene scene)
        {
            if (FindComponents<QaIfAdr014C4DynamicCompositionScenario>(scene).Length != 0)
                throw new InvalidOperationException("Only the C4 Persistent scene may include its scenario controller.");
        }

        private static void RequireEntry(
            Scene scene,
            string stableId,
            ObjectEntryCompositionProfile expectedProfile,
            bool allowDynamic,
            bool requiresAuthoring)
        {
            ObjectEntryDeclaration[] matches = FindEntriesById(
                FindComponents<ObjectEntryDeclaration>(scene), stableId);
            if (matches.Length != 1)
                throw new InvalidOperationException("C4 scene must contain exactly one Object Entry with ID '" + stableId + "'.");

            ObjectEntryDeclaration declaration = matches[0];
            if (declaration.transform.parent != null ||
                !declaration.gameObject.activeInHierarchy)
                throw new InvalidOperationException(
                    "C4 Object Entry must be an active scene root so the fixture can preserve its exact owner occurrence: " + stableId);
            Collider collider = declaration.GetComponent<Collider>();
            if (collider == null || !collider.enabled)
                throw new InvalidOperationException(
                    "C4 Object Entry requires an enabled root Collider for physical recognition: " + stableId);
            ObjectEntryCompositionAuthoring authoring =
                declaration.GetComponent<ObjectEntryCompositionAuthoring>();
            if (!declaration.TryGetObjectEntryId(out ObjectEntryId identity) ||
                identity != ObjectEntryId.From(stableId))
                throw new InvalidOperationException("C4 Object Entry has an invalid serialized identity: " + stableId);
            if (!requiresAuthoring)
            {
                if (authoring != null)
                    throw new InvalidOperationException("Plain C4 Object Entry unexpectedly has Composition Authoring: " + stableId);
                return;
            }
            if (authoring == null ||
                authoring.Profile != expectedProfile ||
                authoring.AllowsDynamicContributions != allowDynamic)
                throw new InvalidOperationException("C4 Composition Authoring profile or dynamic opt-in conflicts: " + stableId);
        }

        private static T[] FindComponents<T>(Scene scene) where T : Component
        {
            var components = new List<T>();
            foreach (GameObject root in scene.GetRootGameObjects())
                components.AddRange(root.GetComponentsInChildren<T>(true));
            return components.ToArray();
        }

        private static ObjectEntryDeclaration[] FindEntriesById(
            ObjectEntryDeclaration[] declarations,
            string stableId)
        {
            var matches = new List<ObjectEntryDeclaration>();
            ObjectEntryId expected = ObjectEntryId.From(stableId);
            foreach (ObjectEntryDeclaration declaration in declarations)
            {
                if (declaration.TryGetObjectEntryId(out ObjectEntryId actual) &&
                    actual == expected)
                    matches.Add(declaration);
            }
            return matches.ToArray();
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (AssetDatabase.IsValidFolder(path))
                return;
            if (!AssetDatabase.IsValidFolder(parent))
                throw new InvalidOperationException("C4 setup parent folder is missing: " + parent);

            string absolutePath = System.IO.Path.Combine(
                Application.dataPath, path.Substring("Assets/".Length));
            if (System.IO.Directory.Exists(absolutePath))
            {
                AssetDatabase.Refresh();
                if (AssetDatabase.IsValidFolder(path))
                    return;
                throw new InvalidOperationException("C4 folder exists on disk but is not imported: " + path);
            }

            AssetDatabase.CreateFolder(parent, name);
            if (!AssetDatabase.IsValidFolder(path))
                throw new InvalidOperationException("Could not create C4 folder: " + path);
        }

        private static void EnsurePathAvailable(string path)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                throw new InvalidOperationException("Refusing to overwrite an existing C4 asset: " + path);
            string absolutePath = System.IO.Path.Combine(
                Application.dataPath, path.Substring("Assets/".Length));
            if (System.IO.File.Exists(absolutePath))
                throw new InvalidOperationException("Refusing to overwrite an unimported C4 file: " + path);
        }

        private static SerializedProperty Required(SerializedObject serialized, string path)
        {
            SerializedProperty property = serialized.FindProperty(path);
            if (property == null)
                throw new InvalidOperationException(
                    "C4 setup could not find serialized authoring property '" + path +
                    "' on " + serialized.targetObject.GetType().Name + ".");
            return property;
        }

        private static SerializedProperty Required(SerializedProperty serialized, string path)
        {
            SerializedProperty property = serialized.FindPropertyRelative(path);
            if (property == null)
                throw new InvalidOperationException(
                    "C4 setup could not find serialized authoring field '" + path + "'.");
            return property;
        }

        private static void SetEnum(SerializedProperty property, string name)
        {
            int index = Array.IndexOf(property.enumNames, name);
            if (index < 0)
                throw new InvalidOperationException(
                    "C4 setup could not find enum option '" + name + "' for " + property.name + ".");
            property.enumValueIndex = index;
        }

        private static void Set(UnityEngine.Object target, string path, string value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.Update();
            Required(serialized, path).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void Set(UnityEngine.Object target, string path, bool value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.Update();
            Required(serialized, path).boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void Set(
            UnityEngine.Object target,
            string path,
            UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.Update();
            Required(serialized, path).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
