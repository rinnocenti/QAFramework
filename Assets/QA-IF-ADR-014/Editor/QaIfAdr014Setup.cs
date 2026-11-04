using System;
using System.Collections.Generic;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.ObjectEntry;
using Immersive.QaFramework.IfAdr014;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.QaFramework.IfAdr014.Editor
{
    public static class QaIfAdr014Setup
    {
        private const string Root = "Assets/QA-IF-ADR-014";
        private const string Generated = Root + "/Generated";
        private const string Settings = Generated + "/Settings";
        private const string Scenes = Generated + "/Scenes";
        private const string ApplicationSource = "Assets/QA-NEW-001/Settings/GameApplication_QaNew001.asset";
        private const string RouteSource = "Assets/QA-NEW-001/Settings/Route_QaNew001.asset";
        private const string ApplicationPath = Settings + "/GameApplication_QaIfAdr014.asset";
        private const string RoutePath = Settings + "/Route_QaIfAdr014.asset";
        private const string ActivityPath = Settings + "/Activity_QaIfAdr014.asset";
        private const string ActivityProfilePath = Settings + "/ActivityContentProfile_QaIfAdr014.asset";
        private const string PersistentScenePath = Scenes + "/QA_IF_ADR_014_Persistent.unity";
        private const string RouteScenePath = Scenes + "/QA_IF_ADR_014_Route.unity";
        private const string ActivityScenePath = Scenes + "/QA_IF_ADR_014_Activity.unity";

        [MenuItem("Immersive Framework/QA/IF-ADR-014/Generate Object Entry Recognition Demo")]
        public static void Generate()
        {
            string generatedDirectory = System.IO.Path.Combine(
                Application.dataPath, "QA-IF-ADR-014", "Generated");
            if (AssetDatabase.IsValidFolder(Generated) || System.IO.Directory.Exists(generatedDirectory))
            {
                if (!TryValidateOrRepairPreparedAssets(out string issue))
                {
                    AssetDatabase.Refresh();
                    if (!TryValidateOrRepairPreparedAssets(out issue))
                    {
                        throw new InvalidOperationException(
                            "The existing IF-ADR-014 Generated folder is incomplete or inconsistent. " +
                            "Existing generated resources were preserved and not recreated. " + issue);
                    }
                }

                RouteAsset existingRoute = AssetDatabase.LoadAssetAtPath<RouteAsset>(RoutePath);
                ActivityAsset existingActivity =
                    AssetDatabase.LoadAssetAtPath<ActivityAsset>(ActivityPath);
                bool clearedRouteStartupActivity = ClearUnexpectedRouteStartupActivity(existingRoute);
                bool repairedBindings = RepairPersistentSceneBindings(existingActivity);
                if (clearedRouteStartupActivity || repairedBindings)
                    Debug.Log("[IF-ADR-014-DEMO] setup='RepairedPreparedBindings' routeStartupActivityCleared='" +
                        clearedRouteStartupActivity + "' persistentSceneBindingsRepaired='" + repairedBindings + "'");

                Debug.Log("[IF-ADR-014-DEMO] setup='AlreadyPrepared' scenes='3' generated='" + Generated +
                    "' next='Continue with the Build Profile / Shared Scene List and Active Game Application.'");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            ValidateTemplateAssets();
            EnsureFolder("Assets", "QA-IF-ADR-014");
            EnsureFolder(Root, "Generated");
            EnsureFolder(Generated, "Settings");
            EnsureFolder(Generated, "Scenes");

            SceneSetup[] previousSetup = CaptureRestorableSceneSetup();
            try
            {
                ActivityContentProfileAsset profile = CreateActivityProfile();
                ActivityAsset activity = CreateActivity(profile);
                RouteAsset route = CreateRoute();
                AssetDatabase.SaveAssets();
                activity = AssetDatabase.LoadAssetAtPath<ActivityAsset>(ActivityPath);
                route = AssetDatabase.LoadAssetAtPath<RouteAsset>(RoutePath);
                if (route != null && ClearUnexpectedRouteStartupActivity(route))
                    route = AssetDatabase.LoadAssetAtPath<RouteAsset>(RoutePath);
                if (activity == null || route == null)
                    throw new InvalidOperationException("Generated Activity or Route did not persist before scene authoring.");
                CreatePersistentScene(activity);
                CreateRouteScene();
                CreateActivityScene();
                CreateApplication(route);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }

            Debug.Log("[IF-ADR-014-DEMO] setup='Prepared' scenes='3' generated='" + Generated +
                "' next='Add generated scenes to the active Build Profile / Shared Scene List and select GameApplication_QaIfAdr014.'");
        }

        private static SceneSetup[] CaptureRestorableSceneSetup()
        {
            EnsureRestorableActiveScene();
            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
            if (!HasLoadedActiveScene(setup))
                throw new InvalidOperationException(
                    "Could not capture a restorable IF-ADR-014 Editor scene setup.");

            return setup;
        }

        private static void EnsureRestorableActiveScene()
        {
            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
            if (HasLoadedActiveScene(setup))
                return;

            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene loadedScene = SceneManager.GetSceneAt(index);
                if (!loadedScene.IsValid() || !loadedScene.isLoaded)
                    continue;

                if (!SceneManager.SetActiveScene(loadedScene))
                    throw new InvalidOperationException(
                        "Could not establish an active scene while preserving the currently loaded Editor scenes.");

                setup = EditorSceneManager.GetSceneManagerSetup();
                if (HasLoadedActiveScene(setup))
                    return;
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            setup = EditorSceneManager.GetSceneManagerSetup();
            if (!HasLoadedActiveScene(setup))
                throw new InvalidOperationException(
                    "Could not create a restorable empty Editor scene for IF-ADR-014 setup.");
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

        private static bool TryValidateOrRepairPreparedAssets(out string issue)
        {
            var issues = new List<string>();
            GameApplicationAsset application = AssetDatabase.LoadAssetAtPath<GameApplicationAsset>(ApplicationPath);
            RouteAsset route = AssetDatabase.LoadAssetAtPath<RouteAsset>(RoutePath);
            ActivityAsset activity = AssetDatabase.LoadAssetAtPath<ActivityAsset>(ActivityPath);
            ActivityContentProfileAsset profile =
                AssetDatabase.LoadAssetAtPath<ActivityContentProfileAsset>(ActivityProfilePath);
            SceneAsset persistentScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(PersistentScenePath);
            SceneAsset routeScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(RouteScenePath);
            SceneAsset activityScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ActivityScenePath);

            if (application == null) issues.Add("missing GameApplication_QaIfAdr014.asset");
            if (route == null) issues.Add("missing Route_QaIfAdr014.asset");
            if (activity == null) issues.Add("missing Activity_QaIfAdr014.asset");
            if (profile == null) issues.Add("missing ActivityContentProfile_QaIfAdr014.asset");
            if (persistentScene == null) issues.Add("missing QA_IF_ADR_014_Persistent.unity");
            if (routeScene == null) issues.Add("missing QA_IF_ADR_014_Route.unity");
            if (activityScene == null) issues.Add("missing QA_IF_ADR_014_Activity.unity");

            if (issues.Count == 0)
            {
                if (application.PersistentContent == null ||
                    application.PersistentContent.ContainerScenePath != PersistentScenePath)
                    issues.Add("Game Application does not reference the generated Persistent Content scene");
                if (route.PrimaryScenePath != RouteScenePath)
                    issues.Add("generated Route primary scene path is inconsistent");
                if (activity.ActivityContentProfile != profile)
                    issues.Add("generated Activity does not reference the generated Content Profile");
                if (profile.SceneCount != 1 || profile.Scenes[0].ScenePath != ActivityScenePath)
                    issues.Add("generated Activity Content Profile scene path is inconsistent");

                if (issues.Count == 0 && application.StartupRoute == null)
                {
                    Set(application, "startupRoute", route);
                    AssetDatabase.SaveAssets();
                    application = AssetDatabase.LoadAssetAtPath<GameApplicationAsset>(ApplicationPath);
                }

                if (issues.Count == 0 && application == null)
                    issues.Add("Game Application could not be reloaded after restoring its startup Route reference");
                else if (issues.Count == 0 && application.StartupRoute != route)
                    issues.Add("Game Application does not reference the generated Route");
            }

            issue = string.Join("; ", issues);
            return issues.Count == 0;
        }

        private static bool ClearUnexpectedRouteStartupActivity(RouteAsset route)
        {
            if (route == null)
                throw new InvalidOperationException("Cannot validate the generated Route startup Activity because the Route asset is missing.");
            if (route.StartupActivity == null)
                return false;

            Set(route, "startupActivity", (UnityEngine.Object)null);
            AssetDatabase.SaveAssets();
            RouteAsset reloadedRoute = AssetDatabase.LoadAssetAtPath<RouteAsset>(RoutePath);
            if (reloadedRoute == null || reloadedRoute.StartupActivity != null)
                throw new InvalidOperationException("Could not clear the unexpected generated Route startup Activity.");

            return true;
        }

        private static bool RepairPersistentSceneBindings(ActivityAsset activity)
        {
            if (activity == null)
                throw new InvalidOperationException("Cannot repair Persistent scene bindings without the generated Activity asset.");

            EnsureRestorableActiveScene();
            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(PersistentScenePath);
            bool openedForRepair = !scene.IsValid() || !scene.isLoaded;
            bool repaired = false;

            try
            {
                if (openedForRepair)
                    scene = EditorSceneManager.OpenScene(PersistentScenePath, OpenSceneMode.Additive);

                if (!scene.IsValid() || !scene.isLoaded)
                    throw new InvalidOperationException("Could not open the generated Persistent scene for binding repair.");
                if (scene.isDirty)
                    throw new InvalidOperationException(
                        "The generated Persistent scene has unsaved changes. Save or close it, then run setup again.");

                GameObject controller = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (root.name != "IF-ADR-014 Recognition Demo")
                        continue;
                    if (controller != null)
                        throw new InvalidOperationException("Persistent scene contains duplicate IF-ADR-014 demo controllers.");
                    controller = root;
                }

                if (controller == null)
                    throw new InvalidOperationException("Persistent scene is missing the IF-ADR-014 demo controller.");

                ActivityRequestTrigger request = controller.GetComponent<ActivityRequestTrigger>();
                QaIfAdr014ObjectEntryRecognitionDemo demo =
                    controller.GetComponent<QaIfAdr014ObjectEntryRecognitionDemo>();
                if (request == null || demo == null)
                    throw new InvalidOperationException(
                        "Persistent scene is missing its Activity Request Trigger or recognition demo component.");

                SerializedObject requestObject = new SerializedObject(request);
                requestObject.Update();
                SerializedProperty targetActivity = Required(requestObject, "targetActivity");

                SerializedObject demoObject = new SerializedObject(demo);
                demoObject.Update();
                SerializedProperty demoActivity = Required(demoObject, "activity");
                SerializedProperty declaration = Required(demoObject, "unadmittedDeclaration");
                SerializedProperty collider = Required(demoObject, "unadmittedCollider");
                if (declaration.objectReferenceValue == null || collider.objectReferenceValue == null)
                    throw new InvalidOperationException(
                        "Persistent scene is missing the unadmitted declaration or Collider fixture.");

                bool requestChanged = targetActivity.objectReferenceValue != activity;
                bool demoChanged = demoActivity.objectReferenceValue != activity;
                if (requestChanged)
                {
                    targetActivity.objectReferenceValue = activity;
                    requestObject.ApplyModifiedPropertiesWithoutUndo();
                }
                if (demoChanged)
                {
                    demoActivity.objectReferenceValue = activity;
                    demoObject.ApplyModifiedPropertiesWithoutUndo();
                }

                repaired = requestChanged || demoChanged;
                if (repaired)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene))
                        throw new InvalidOperationException("Could not save the repaired IF-ADR-014 Persistent scene.");
                }
            }
            finally
            {
                try
                {
                    if (openedForRepair &&
                        scene.IsValid() &&
                        scene.isLoaded &&
                        !EditorSceneManager.CloseScene(scene, true))
                        throw new InvalidOperationException("Could not close the Persistent scene opened for binding repair.");
                }
                finally
                {
                    if (previousActiveScene.IsValid() &&
                        previousActiveScene.isLoaded &&
                        SceneManager.GetActiveScene() != previousActiveScene &&
                        !SceneManager.SetActiveScene(previousActiveScene))
                        throw new InvalidOperationException("Could not restore the previously active Editor scene after binding repair.");
                }
            }

            return repaired;
        }

        private static void ValidateTemplateAssets()
        {
            if (AssetDatabase.LoadAssetAtPath<GameApplicationAsset>(ApplicationSource) == null)
                throw new InvalidOperationException("Missing QA-NEW-001 Game Application template: " + ApplicationSource);
            if (AssetDatabase.LoadAssetAtPath<RouteAsset>(RouteSource) == null)
                throw new InvalidOperationException("Missing QA-NEW-001 Route template: " + RouteSource);
        }

        private static ActivityContentProfileAsset CreateActivityProfile()
        {
            var profile = ScriptableObject.CreateInstance<ActivityContentProfileAsset>();
            AssetDatabase.CreateAsset(profile, ActivityProfilePath);
            Set(profile, "profileId", "qa.if-adr-014.activity-content");

            SerializedObject serialized = new SerializedObject(profile);
            serialized.Update();
            SerializedProperty scenes = Required(serialized, "scenes");
            scenes.arraySize = 1;
            SerializedProperty entry = scenes.GetArrayElementAtIndex(0);
            Required(entry, "contentId").stringValue = "qa.if-adr-014.activity.main";
            Required(entry, "scenePath").stringValue = ActivityScenePath;
            Required(entry, "sceneName").stringValue = "QA_IF_ADR_014_Activity";
            SetEnum(Required(entry, "requiredness"), "Required");
            SetEnum(Required(entry, "loadMode"), "Additive");
            SetEnum(Required(entry, "releasePolicy"), "ReleaseOnActivityChange");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static ActivityAsset CreateActivity(ActivityContentProfileAsset profile)
        {
            var activity = ScriptableObject.CreateInstance<ActivityAsset>();
            AssetDatabase.CreateAsset(activity, ActivityPath);
            Set(activity, "activityId", "qa.if-adr-014.activity");
            Set(activity, "activityName", "IF-ADR-014 Object Entry Recognition");
            Set(activity, "activityContentProfile", profile);
            return activity;
        }

        private static RouteAsset CreateRoute()
        {
            if (!AssetDatabase.CopyAsset(RouteSource, RoutePath))
                throw new InvalidOperationException("Could not copy QA-NEW-001 Route template.");

            AssetDatabase.ImportAsset(RoutePath, ImportAssetOptions.ForceUpdate);
            RouteAsset route = AssetDatabase.LoadAssetAtPath<RouteAsset>(RoutePath);
            if (route == null)
                throw new InvalidOperationException("Copied Route did not import: " + RoutePath);

            Set(route, "routeId", "qa.if-adr-014.route");
            Set(route, "routeName", "IF-ADR-014 Object Entry Recognition");
            Set(route, "primaryScenePath", RouteScenePath);
            Set(route, "primarySceneName", "QA_IF_ADR_014_Route");
            Set(route, "startupActivity", (UnityEngine.Object)null);
            return route;
        }

        private static void CreatePersistentScene(ActivityAsset activity)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject controller = new GameObject("IF-ADR-014 Recognition Demo");
            ActivityRequestTrigger request = controller.AddComponent<ActivityRequestTrigger>();
            var demo = controller.AddComponent<QaIfAdr014ObjectEntryRecognitionDemo>();

            GameObject unadmittedRoot = new GameObject("Declared but not admitted");
            ObjectEntryDeclaration declaration = unadmittedRoot.AddComponent<ObjectEntryDeclaration>();
            Set(declaration, "objectEntryId", QaIfAdr014ObjectEntryRecognitionDemo.ActivityEntryId);
            BoxCollider collider = unadmittedRoot.AddComponent<BoxCollider>();

            SerializedObject requestObject = new SerializedObject(request);
            requestObject.Update();
            Required(requestObject, "targetActivity").objectReferenceValue = activity;
            requestObject.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject demoObject = new SerializedObject(demo);
            demoObject.Update();
            Required(demoObject, "activityRequest").objectReferenceValue = request;
            Required(demoObject, "activity").objectReferenceValue = activity;
            Required(demoObject, "unadmittedDeclaration").objectReferenceValue = declaration;
            Required(demoObject, "unadmittedCollider").objectReferenceValue = collider;
            demoObject.ApplyModifiedPropertiesWithoutUndo();
            SaveScene(scene, PersistentScenePath);
        }

        private static void CreateRouteScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Route-owned Object Entry query").AddComponent<ObjectEntryOccurrenceQueryBinding>();

            GameObject target = new GameObject("Route Object Entry");
            ObjectEntryDeclaration declaration = target.AddComponent<ObjectEntryDeclaration>();
            Set(declaration, "objectEntryId", QaIfAdr014ObjectEntryRecognitionDemo.RouteEntryId);
            target.AddComponent<BoxCollider>();
            SaveScene(scene, RouteScenePath);
        }

        private static void CreateActivityScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Activity-owned Object Entry query").AddComponent<ObjectEntryOccurrenceQueryBinding>();

            GameObject target = new GameObject("Activity Object Entry");
            ObjectEntryDeclaration declaration = target.AddComponent<ObjectEntryDeclaration>();
            Set(declaration, "objectEntryId", QaIfAdr014ObjectEntryRecognitionDemo.ActivityEntryId);
            CreateColliderChild(target.transform, "Collider A", new Vector3(-0.75f, 0f, 0f));
            CreateColliderChild(target.transform, "Collider B", new Vector3(0.75f, 0f, 0f));
            SaveScene(scene, ActivityScenePath);
        }

        private static void CreateColliderChild(Transform parent, string name, Vector3 position)
        {
            GameObject child = GameObject.CreatePrimitive(PrimitiveType.Cube);
            child.name = name;
            child.transform.SetParent(parent, false);
            child.transform.localPosition = position;
        }

        private static void CreateApplication(RouteAsset route)
        {
            if (!AssetDatabase.CopyAsset(ApplicationSource, ApplicationPath))
                throw new InvalidOperationException("Could not copy QA-NEW-001 Game Application template.");

            AssetDatabase.ImportAsset(ApplicationPath, ImportAssetOptions.ForceUpdate);
            GameApplicationAsset application = AssetDatabase.LoadAssetAtPath<GameApplicationAsset>(ApplicationPath);
            if (application == null)
                throw new InvalidOperationException("Copied Game Application did not import: " + ApplicationPath);

            SceneAsset persistentScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(PersistentScenePath);
            if (persistentScene == null)
                throw new InvalidOperationException("Generated Persistent scene did not import: " + PersistentScenePath);

            Set(application, "applicationName", "IF-ADR-014 Object Entry Recognition Demo");
            Set(application, "startupRoute", route);
            SerializedObject serialized = new SerializedObject(application);
            serialized.Update();
            SerializedProperty persistent = Required(serialized, "persistentContent");
            Required(persistent, "scenePath").stringValue = PersistentScenePath;
            Required(persistent, "sceneName").stringValue = "QA_IF_ADR_014_Persistent";
            Required(persistent, "containerScene").objectReferenceValue = persistentScene;
            Required(serialized, "playerSessionEnabled").boolValue = false;
            Required(serialized, "defaultPlayerSessionProfile").objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(application);
        }

        private static void SaveScene(Scene scene, string path)
        {
            if (!EditorSceneManager.SaveScene(scene, path))
                throw new InvalidOperationException("Could not save generated scene: " + path);
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (AssetDatabase.IsValidFolder(path))
                return;

            if (!AssetDatabase.IsValidFolder(parent))
                throw new InvalidOperationException(
                    "Cannot create folder because its parent is not recognized by AssetDatabase: parent='" +
                    parent + "' child='" + name + "'.");

            string absolutePath = System.IO.Path.Combine(
                Application.dataPath, path.Substring("Assets/".Length));
            if (System.IO.Directory.Exists(absolutePath))
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                if (AssetDatabase.IsValidFolder(path))
                    return;

                throw new InvalidOperationException(
                    "Folder exists on disk but is not available as a Unity folder asset: path='" + path +
                    "' assetGuid='" + AssetDatabase.AssetPathToGUID(path) + "'. Reimport this folder in the Project window.");
            }

            string folderGuid = AssetDatabase.CreateFolder(parent, name);
            if (AssetDatabase.IsValidFolder(path))
                return;

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            if (AssetDatabase.IsValidFolder(path))
                return;

            string returnedPath = string.IsNullOrWhiteSpace(folderGuid)
                ? string.Empty
                : AssetDatabase.GUIDToAssetPath(folderGuid);
            bool existsOnDisk = System.IO.Directory.Exists(absolutePath);
            throw new InvalidOperationException(
                "Could not create or import folder: path='" + path +
                "' parentValid='" + AssetDatabase.IsValidFolder(parent) +
                "' returnedGuid='" + folderGuid +
                "' returnedPath='" + returnedPath +
                "' existsOnDisk='" + existsOnDisk + "'.");
        }

        private static SerializedProperty Required(SerializedObject serialized, string path)
        {
            SerializedProperty property = serialized.FindProperty(path);
            if (property == null)
                throw new InvalidOperationException("Serialized property not found: " +
                    serialized.targetObject.GetType().Name + "." + path);
            return property;
        }

        private static SerializedProperty Required(SerializedProperty serialized, string path)
        {
            SerializedProperty property = serialized.FindPropertyRelative(path);
            if (property == null)
                throw new InvalidOperationException("Serialized property not found: " + path);
            return property;
        }

        private static void SetEnum(SerializedProperty property, string name)
        {
            int index = Array.IndexOf(property.enumNames, name);
            if (index < 0)
                throw new InvalidOperationException("Enum value not found: " +
                    property.propertyPath + " = " + name);
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

        private static void Set(UnityEngine.Object target, string path, UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.Update();
            Required(serialized, path).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
