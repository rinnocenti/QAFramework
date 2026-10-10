using System;
using System.IO;
using Immersive.Framework.Authoring;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.Editor.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.QaFramework.IfAdr045;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.QaFramework.IfAdr045.Editor
{
    public static class QaIfAdr045Setup
    {
        private const string Root = "Assets/QA-IF-ADR-045";
        private const string Generated = Root + "/Generated";
        private const string Settings = Generated + "/Settings";
        private const string Scenes = Generated + "/Scenes";
        private const string PersistentScenePath = Scenes + "/QA_IF_ADR_045_Persistent.unity";
        private const string UnavailableScenePath = Scenes + "/QA_IF_ADR_045_Unavailable.unity";
        private const string TemplateApplicationPath = "Assets/QA-NEW-004/Settings/GameApplication_QaNew004.asset";
        private const string RouteAPath = "Assets/QA-NEW-004/Settings/Route_QaNew004_A.asset";
        private const string RouteBPath = "Assets/QA-NEW-004/Settings/Route_QaNew004_B.asset";
        private const string RouteDPath = "Assets/QA-NEW-004/Settings/Route_QaNew004_D.asset";
        private const string ActivityDBPath = "Assets/QA-NEW-004/Settings/Activity_QaNew004_D_B.asset";

        [MenuItem("Immersive Framework/QA/IF-ADR-045/Prepare Persistent Content Lifecycle QA")]
        public static void Prepare()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Exit Play Mode before preparing IF-ADR-045 QA assets.");
            EnsureNoModifiedOpenScenes("prepare");

            RequireSourceAssets(out _, out _, out _, out _, out _, out GameApplicationAsset templateApplication);
            TemplateAssetIdentity templateIdentity = CaptureTemplateIdentity(templateApplication);
            EnsureFolder("Assets", "QA-IF-ADR-045");
            EnsureFolder(Root, "Scripts");
            EnsureFolder(Root, "Editor");

            if (AssetDatabase.IsValidFolder(Generated))
            {
                throw new InvalidOperationException(
                    $"Generated QA folder '{Generated}' already exists. Inspect it and run the scoped cleanup before preparing again; unknown assets will not be overwritten or deleted.");
            }

            EnsureFolder(Root, "Generated");
            EnsureFolder(Generated, "Settings");
            EnsureFolder(Generated, "Scenes");

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(PersistentScenePath) != null ||
                AssetDatabase.LoadAssetAtPath<GameApplicationAsset>(Settings + "/GameApplication_QaIfAdr045.asset") != null)
            {
                throw new InvalidOperationException(
                    "IF-ADR-045 QA assets already exist. Use the documented cleanup command before recreating this fixture.");
            }

            CreatePersistentScene();
            string unavailableAbsolutePath = GetAbsoluteAssetPath(UnavailableScenePath);
            if (!AssetDatabase.IsValidFolder(Scenes) || File.Exists(unavailableAbsolutePath) ||
                AssetDatabase.LoadMainAssetAtPath(UnavailableScenePath) != null)
            {
                throw new InvalidOperationException(
                    $"Cannot create QA unavailable-scene fixture. source='{PersistentScenePath}', destination='{UnavailableScenePath}', " +
                    $"destinationFolderValid='{AssetDatabase.IsValidFolder(Scenes)}', destinationAlreadyExists='{File.Exists(unavailableAbsolutePath) || AssetDatabase.LoadMainAssetAtPath(UnavailableScenePath) != null}'.");
            }
            if (!AssetDatabase.CopyAsset(PersistentScenePath, UnavailableScenePath))
                throw new InvalidOperationException(
                    $"Could not copy QA scene fixture. source='{PersistentScenePath}', destination='{UnavailableScenePath}', " +
                    $"sourceExists='{File.Exists(GetAbsoluteAssetPath(PersistentScenePath))}', destinationExists='{File.Exists(unavailableAbsolutePath)}'.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(UnavailableScenePath) == null)
                throw new InvalidOperationException("QA-only unavailable scene fixture could not be reloaded.");

            CreateApplication(
                "GameApplication_QaIfAdr045.asset",
                "IF-ADR-045 Persistent Content Lifecycle",
                templateIdentity,
                RouteAPath,
                PersistentScenePath,
                PersistentScenePath,
                "QA_IF_ADR_045_Persistent");
            CreateApplication(
                "GameApplication_QaIfAdr045_InvalidPath.asset",
                "IF-ADR-045 Invalid Explicit Path",
                templateIdentity,
                RouteAPath,
                PersistentScenePath,
                Scenes + "/Missing/QA_IF_ADR_045_Persistent.unity",
                "QA_IF_ADR_045_Persistent",
                scenePathMustExist: false);
            CreateApplication(
                "GameApplication_QaIfAdr045_UnavailablePath.asset",
                "IF-ADR-045 Valid but Unavailable Path",
                templateIdentity,
                RouteAPath,
                UnavailableScenePath,
                UnavailableScenePath,
                "QA_IF_ADR_045_Persistent");

            CreateMigrationFixtures();
            AssetDatabase.SaveAssets();
            ValidateGeneratedBaseline();
            Debug.Log(
                $"[IF-ADR-045-SETUP] status='Prepared' application='{Settings}/GameApplication_QaIfAdr045.asset' " +
                $"scene='{PersistentScenePath}' unavailableScene='{UnavailableScenePath}' buildListsChanged='False' " +
                "activeGameApplicationChanged='False'. Add the valid Persistent Content scene and QA-NEW-004 dependencies manually to the active Build Profile.");
        }

        [MenuItem("Immersive Framework/QA/IF-ADR-045/Run Scoped Persistent Content Migration Cases")]
        public static void RunScopedMigrationCases()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Exit Play Mode before mutating the D7-owned migration fixtures.");

            try
            {
                RunScopedMigrationCasesCore();
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[IF-ADR-045-MIGRATION] status='Failed' verdict='FAIL' execution='{Sanitize(exception.GetType().Name + ": " + exception.Message)}'.");
                throw;
            }
        }

        private static void RunScopedMigrationCasesCore()
        {
            GameApplicationAsset legacy = LoadApplication("Migration_Legacy.asset");
            GameApplicationAsset invalid = LoadApplication("Migration_InvalidLegacy.asset");
            GameApplicationAsset conflict = LoadApplication("Migration_Conflict.asset");
            SceneAsset expectedScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(PersistentScenePath);
            RouteAsset invalidReference = AssetDatabase.LoadAssetAtPath<RouteAsset>(Settings + "/Migration_InvalidReference.asset");
            RouteAsset routeB = AssetDatabase.LoadAssetAtPath<RouteAsset>("Assets/QA-NEW-004/Settings/Route_QaNew004_B.asset");

            if (expectedScene == null || invalidReference == null || routeB == null)
                throw new InvalidOperationException("Run Prepare before the scoped migration cases.");

            string scenePath = AssetDatabase.GetAssetPath(expectedScene);
            string conflictPath = routeB.PrimaryScenePath;
            string conflictName = Path.GetFileNameWithoutExtension(conflictPath);
            RequireMigrationFixture(legacy, expectedScene, string.Empty, string.Empty, "legacy before migration");
            RequireMigrationFixture(invalid, invalidReference, string.Empty, string.Empty, "invalid before migration");
            RequireMigrationFixture(conflict, expectedScene, conflictPath, conflictName, "conflict before migration");

            var legacyFirst = PersistentContentSceneReferenceMigration.MigrateAsset(legacy);
            RequireStatus(legacyFirst.Status, PersistentContentSceneReferenceMigrationStatus.Migrated, "legacy first run");
            RequireReferenceAndModernValues(legacy, expectedScene, scenePath);

            var legacySecond = PersistentContentSceneReferenceMigration.MigrateAsset(legacy);
            RequireStatus(legacySecond.Status, PersistentContentSceneReferenceMigrationStatus.Unchanged, "legacy second run");
            RequireReferenceAndModernValues(legacy, expectedScene, scenePath);

            var invalidResult = PersistentContentSceneReferenceMigration.MigrateAsset(invalid);
            RequireStatus(invalidResult.Status, PersistentContentSceneReferenceMigrationStatus.Invalid, "invalid legacy reference");
            RequireMigrationFixture(invalid, invalidReference, string.Empty, string.Empty, "invalid after migration");

            var conflictResult = PersistentContentSceneReferenceMigration.MigrateAsset(conflict);
            RequireStatus(conflictResult.Status, PersistentContentSceneReferenceMigrationStatus.Conflict, "modern path conflict");
            RequireMigrationFixture(conflict, expectedScene, conflictPath, conflictName, "conflict after migration");

            AssetDatabase.SaveAssets();
            Debug.Log(
                "[IF-ADR-045-MIGRATION] status='Passed' cases='4/4' first='Migrated' second='Unchanged' " +
                "invalid='Invalid/Preserved' conflict='Conflict/Preserved' scope='QA-IF-ADR-045-only'.");
        }

        [MenuItem("Immersive Framework/QA/IF-ADR-045/Remove Generated Persistent Content Lifecycle QA")]
        public static void RemoveGeneratedAssets()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Exit Play Mode before removing IF-ADR-045 QA assets.");
            EnsureNoModifiedOpenScenes("cleanup");
            EnsureNoGeneratedApplicationIsActive();

            for (int index = SceneManager.sceneCount - 1; index >= 0; index--)
            {
                Scene scene = SceneManager.GetSceneAt(index);
                if (scene.IsValid() &&
                    (string.Equals(scene.path, PersistentScenePath, StringComparison.Ordinal) ||
                     string.Equals(scene.path, UnavailableScenePath, StringComparison.Ordinal)))
                    EditorSceneManager.CloseScene(scene, true);
            }

            string[] generatedAssets =
            {
                PersistentScenePath,
                UnavailableScenePath,
                Settings + "/GameApplication_QaIfAdr045.asset",
                Settings + "/GameApplication_QaIfAdr045_InvalidPath.asset",
                Settings + "/GameApplication_QaIfAdr045_UnavailablePath.asset",
                Settings + "/Migration_Legacy.asset",
                Settings + "/Migration_InvalidLegacy.asset",
                Settings + "/Migration_Conflict.asset",
                Settings + "/Migration_InvalidReference.asset"
            };
            for (int index = 0; index < generatedAssets.Length; index++)
            {
                string absoluteAssetPath = Path.Combine(
                    Application.dataPath,
                    generatedAssets[index].Substring("Assets/".Length)
                        .Replace('/', Path.DirectorySeparatorChar));
                if ((File.Exists(absoluteAssetPath) || AssetDatabase.LoadMainAssetAtPath(generatedAssets[index]) != null) &&
                    !AssetDatabase.DeleteAsset(generatedAssets[index]))
                {
                    throw new InvalidOperationException($"Could not remove QA-owned asset '{generatedAssets[index]}'.");
                }
            }

            DeleteFolderIfEmpty(Settings);
            DeleteFolderIfEmpty(Scenes);
            DeleteFolderIfEmpty(Generated);

            Debug.Log($"[IF-ADR-045-CLEANUP] status='{(AssetDatabase.IsValidFolder(Generated) ? "BlockedResiduals" : "Removed")}' generatedRoot='{Generated}' authoredQaPreserved='True' buildListsChanged='False'.");
        }

        private static void CreatePersistentScene()
        {
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            Scene scene = default;
            try
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SetActiveScene(scene);
                // NewScene(Single) and restoration can invalidate wrappers. Resolve
                // every source dependency from its canonical path after the switch.
                RequireSourceAssets(out RouteAsset routeA, out RouteAsset routeB, out RouteAsset routeD,
                    out ActivityAsset activityDA, out ActivityAsset activityDB, out _);
                RouteRequestTrigger requestA = CreateRouteTrigger("QA Route A Request", routeA);
                RouteRequestTrigger requestB = CreateRouteTrigger("QA Route B Request", routeB);
                RouteRequestTrigger requestD = CreateRouteTrigger("QA Route D Request", routeD);
                ActivityRequestTrigger requestActivityDB = CreateActivityTrigger("QA Activity D-B Request", activityDB);

                var observerRoot = new GameObject("IF-ADR-045 lifecycle evidence root");
                QaIfAdr045PersistentContentLifecycleScenario scenario =
                    observerRoot.AddComponent<QaIfAdr045PersistentContentLifecycleScenario>();
                var serialized = new SerializedObject(scenario);
                RequireSerializedProperty(serialized, "expectedPersistentScenePath", PersistentScenePath).stringValue = PersistentScenePath;
                RequireSerializedProperty(serialized, "routeA", PersistentScenePath).objectReferenceValue = routeA;
                RequireSerializedProperty(serialized, "routeB", PersistentScenePath).objectReferenceValue = routeB;
                RequireSerializedProperty(serialized, "routeD", PersistentScenePath).objectReferenceValue = routeD;
                RequireSerializedProperty(serialized, "activityDA", PersistentScenePath).objectReferenceValue = activityDA;
                RequireSerializedProperty(serialized, "activityDB", PersistentScenePath).objectReferenceValue = activityDB;
                RequireSerializedProperty(serialized, "requestRouteA", PersistentScenePath).objectReferenceValue = requestA;
                RequireSerializedProperty(serialized, "requestRouteB", PersistentScenePath).objectReferenceValue = requestB;
                RequireSerializedProperty(serialized, "requestRouteD", PersistentScenePath).objectReferenceValue = requestD;
                RequireSerializedProperty(serialized, "requestActivityDB", PersistentScenePath).objectReferenceValue = requestActivityDB;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                if (!EditorSceneManager.SaveScene(scene, PersistentScenePath))
                    throw new InvalidOperationException("Could not save the generated Persistent Content scene.");
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(PersistentScenePath) == null)
                throw new InvalidOperationException("Generated Persistent Content SceneAsset could not be loaded.");
        }

        private static RouteRequestTrigger CreateRouteTrigger(string objectName, RouteAsset target)
        {
            var gameObject = new GameObject(objectName);
            RouteRequestTrigger trigger = gameObject.AddComponent<RouteRequestTrigger>();
            trigger.TargetRoute = target;
            return trigger;
        }

        private static ActivityRequestTrigger CreateActivityTrigger(string objectName, ActivityAsset target)
        {
            var gameObject = new GameObject(objectName);
            ActivityRequestTrigger trigger = gameObject.AddComponent<ActivityRequestTrigger>();
            trigger.TargetActivity = target;
            return trigger;
        }

        private static void CreateApplication(
            string fileName,
            string applicationName,
            TemplateAssetIdentity templateIdentity,
            string startupRoutePath,
            string legacyScenePath,
            string scenePath,
            string sceneName,
            bool scenePathMustExist = true)
        {
            string path = Settings + "/" + fileName;
            GameApplicationAsset templateApplication =
                LoadRequiredSourceAsset<GameApplicationAsset>(TemplateApplicationPath, "Game Application template");
            TemplateAssetIdentity currentIdentity = CaptureTemplateIdentity(templateApplication);
            if (!string.Equals(currentIdentity.AssetPath, templateIdentity.AssetPath, StringComparison.Ordinal) ||
                !string.Equals(currentIdentity.Guid, templateIdentity.Guid, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"QA-NEW-004 Game Application template identity changed during preparation. expectedPath='{templateIdentity.AssetPath}', actualPath='{currentIdentity.AssetPath}', " +
                    $"expectedGuid='{templateIdentity.Guid}', actualGuid='{currentIdentity.Guid}'.");
            }
            string templatePath = TemplateApplicationPath;
            string sourceFile = string.IsNullOrWhiteSpace(templatePath)
                ? string.Empty
                : GetAbsoluteAssetPath(templatePath);
            string destinationFile = GetAbsoluteAssetPath(path);
            bool settingsFolderValid = AssetDatabase.IsValidFolder(Settings);
            bool sourceFileExists = !string.IsNullOrWhiteSpace(sourceFile) && File.Exists(sourceFile);
            bool sourceIsMainAsset = !string.IsNullOrWhiteSpace(templatePath) &&
                                     AssetDatabase.LoadMainAssetAtPath(templatePath) == templateApplication;
            bool destinationAssetExists = AssetDatabase.LoadMainAssetAtPath(path) != null;
            bool destinationFileExists = File.Exists(destinationFile);

            if (templateApplication == null || templateApplication.GetType() != typeof(GameApplicationAsset) ||
                !sourceIsMainAsset || !sourceFileExists || !settingsFolderValid ||
                destinationAssetExists || destinationFileExists)
            {
                throw new InvalidOperationException(
                    $"Cannot copy QA-NEW-004 Game Application template. sourceAssetPath='{templatePath}', " +
                    $"sourceFile='{sourceFile}', sourceType='{(templateApplication == null ? "<null>" : templateApplication.GetType().FullName)}', " +
                    $"sourceIsMainAsset='{sourceIsMainAsset}', sourceFileExists='{sourceFileExists}', " +
                    $"destinationAssetPath='{path}', destinationFile='{destinationFile}', " +
                    $"settingsFolderValid='{settingsFolderValid}', destinationAssetExists='{destinationAssetExists}', " +
                    $"destinationFileExists='{destinationFileExists}'. Existing destination content is preserved.");
            }

            if (!AssetDatabase.CopyAsset(templatePath, path))
            {
                bool destinationCreated = AssetDatabase.LoadMainAssetAtPath(path) != null || File.Exists(destinationFile);
                throw new InvalidOperationException(
                    $"AssetDatabase.CopyAsset returned false for the QA-NEW-004 Game Application template. " +
                    $"sourceAssetPath='{templatePath}', sourceFile='{sourceFile}', sourceType='{typeof(GameApplicationAsset).FullName}', " +
                    $"destinationAssetPath='{path}', destinationFile='{destinationFile}', settingsFolderValid='{AssetDatabase.IsValidFolder(Settings)}', " +
                    $"destinationExistedBeforeCopy='False', destinationCreatedByFailedCopy='{destinationCreated}'. " +
                    "No existing asset was overwritten or deleted.");
            }

            GameApplicationAsset application = AssetDatabase.LoadAssetAtPath<GameApplicationAsset>(path);
            if (application == null)
            {
                UnityEngine.Object copiedMainAsset = AssetDatabase.LoadMainAssetAtPath(path);
                throw new InvalidOperationException(
                    $"Copied D7 Game Application could not be loaded as its expected type. source='{templatePath}', " +
                    $"destination='{path}', destinationFileExists='{File.Exists(destinationFile)}', " +
                    $"destinationMainAssetType='{(copiedMainAsset == null ? "<null>" : copiedMainAsset.GetType().FullName)}', " +
                    $"expectedType='{typeof(GameApplicationAsset).FullName}'.");
            }

            // Resolve references after CopyAsset and immediately before assigning
            // them. No Unity object wrapper is carried across the copy boundary.
            RouteAsset startupRoute = LoadRequiredSourceAsset<RouteAsset>(startupRoutePath, "Generated Game Application Startup Route");
            SceneAsset legacyScene = LoadRequiredSourceAsset<SceneAsset>(legacyScenePath, "Generated Game Application legacy Persistent Content Scene");
            if (scenePathMustExist)
                RequireSceneAsset(scenePath, "Generated Game Application Persistent Content scene");

            var serialized = new SerializedObject(application);
            serialized.Update();
            RequireSerializedProperty(serialized, "applicationName", path).stringValue = applicationName;
            RequireSerializedProperty(serialized, "startupRoute", path).objectReferenceValue = startupRoute;
            RequireSerializedProperty(serialized, "persistentContent.containerScene", path).objectReferenceValue = legacyScene;
            RequireSerializedProperty(serialized, "persistentContent.scenePath", path).stringValue = scenePath;
            RequireSerializedProperty(serialized, "persistentContent.sceneName", path).stringValue = sceneName;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(application);
            AssetDatabase.SaveAssetIfDirty(application);
            if (application.StartupRoute != startupRoute || application.PersistentContent == null ||
                application.PersistentContent.ContainerScene != legacyScene ||
                !string.Equals(application.PersistentContent.ContainerScenePath, scenePath, StringComparison.Ordinal) ||
                !string.Equals(application.PersistentContent.ContainerSceneName, sceneName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Copied D7 Game Application did not retain the expected serialized authoring. asset='{path}', " +
                    $"startupRoute='{application.StartupRoute?.name}', expectedRoute='{startupRoute?.name}', " +
                    $"scenePath='{application.PersistentContent?.ContainerScenePath}', expectedScenePath='{scenePath}', " +
                    $"sceneName='{application.PersistentContent?.ContainerSceneName}', expectedSceneName='{sceneName}'.");
            }
        }

        private static void CreateMigrationFixtures()
        {
            CreateMigrationApplication("Migration_Legacy.asset", PersistentScenePath, string.Empty, string.Empty);

            RouteAsset invalidReference = ScriptableObject.CreateInstance<RouteAsset>();
            string invalidReferencePath = Settings + "/Migration_InvalidReference.asset";
            EnsureGeneratedAssetPathAvailable(invalidReferencePath);
            AssetDatabase.CreateAsset(invalidReference, invalidReferencePath);
            if (AssetDatabase.LoadAssetAtPath<RouteAsset>(invalidReferencePath) == null)
                throw new InvalidOperationException($"Could not create D7 invalid-reference fixture asset '{invalidReferencePath}'.");
            CreateMigrationApplication("Migration_InvalidLegacy.asset", invalidReferencePath, string.Empty, string.Empty);

            RouteAsset routeB = LoadRequiredSourceAsset<RouteAsset>(RouteBPath, "Route B migration conflict source");
            string modernName = Path.GetFileNameWithoutExtension(routeB.PrimaryScenePath);
            string modernPath = routeB.PrimaryScenePath;
            CreateMigrationApplication("Migration_Conflict.asset", PersistentScenePath, modernPath, modernName);
            AssetDatabase.SaveAssets();
        }

        private static void CreateMigrationApplication(
            string fileName,
            string legacyReferencePath,
            string modernPath,
            string modernName)
        {
            string path = Settings + "/" + fileName;
            EnsureGeneratedAssetPathAvailable(path);
            GameApplicationAsset application = ScriptableObject.CreateInstance<GameApplicationAsset>();
            AssetDatabase.CreateAsset(application, path);
            application = AssetDatabase.LoadAssetAtPath<GameApplicationAsset>(path);
            if (application == null)
                throw new InvalidOperationException($"Could not create D7 migration Game Application fixture '{path}'.");
            UnityEngine.Object legacyReference = AssetDatabase.LoadMainAssetAtPath(legacyReferencePath);
            if (legacyReference == null)
                throw new InvalidOperationException($"Could not reload D7 migration legacy reference '{legacyReferencePath}' for '{path}'.");
            var serialized = new SerializedObject(application);
            RequireSerializedProperty(serialized, "persistentContent.containerScene", path).objectReferenceValue = legacyReference;
            RequireSerializedProperty(serialized, "persistentContent.scenePath", path).stringValue = modernPath;
            RequireSerializedProperty(serialized, "persistentContent.sceneName", path).stringValue = modernName;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(application);
        }

        private static void RequireReferenceAndModernValues(
            GameApplicationAsset application,
            SceneAsset expectedScene,
            string expectedPath)
        {
            RequireMigrationFixture(application, expectedScene, expectedPath, expectedScene.name, "converted legacy fixture");
        }

        private static void RequireMigrationFixture(
            GameApplicationAsset application,
            UnityEngine.Object expectedLegacyReference,
            string expectedPath,
            string expectedName,
            string label)
        {
            if (application == null)
                throw new InvalidOperationException($"Migration fixture '{label}' is missing.");

            var serializedApplication = new SerializedObject(application);
            serializedApplication.Update();
            SerializedProperty persistent = serializedApplication.FindProperty("persistentContent");
            SerializedProperty legacy = persistent?.FindPropertyRelative("containerScene");
            SerializedProperty path = persistent?.FindPropertyRelative("scenePath");
            SerializedProperty name = persistent?.FindPropertyRelative("sceneName");
            if (legacy == null || path == null || name == null ||
                legacy.objectReferenceValue != expectedLegacyReference ||
                !string.Equals(path.stringValue, expectedPath, StringComparison.Ordinal) ||
                !string.Equals(name.stringValue, expectedName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Migration fixture '{label}' does not contain its expected serialized legacy/path/name values.");
            }
        }

        private static void RequireStatus(
            PersistentContentSceneReferenceMigrationStatus actual,
            PersistentContentSceneReferenceMigrationStatus expected,
            string label)
        {
            if (actual != expected)
                throw new InvalidOperationException($"Migration case '{label}' expected '{expected}', received '{actual}'.");
        }

        private static GameApplicationAsset LoadApplication(string fileName)
        {
            GameApplicationAsset application = AssetDatabase.LoadAssetAtPath<GameApplicationAsset>(Settings + "/" + fileName);
            if (application == null)
                throw new InvalidOperationException("Run Prepare before the scoped migration cases.");
            return application;
        }

        private static void ValidateGeneratedBaseline()
        {
            ValidateGeneratedPersistentScene(PersistentScenePath, PersistentScenePath);
            ValidateGeneratedPersistentScene(UnavailableScenePath, PersistentScenePath);

            // Scene restoration may invalidate Unity wrappers; reacquire every asset
            // used by the persisted GameApplication checks only after that boundary.
            RequireSourceAssets(out RouteAsset routeA, out _, out _, out _, out _, out GameApplicationAsset template);
            SceneAsset persistentScene = LoadRequiredSourceAsset<SceneAsset>(PersistentScenePath, "Generated Persistent Content scene");
            SceneAsset unavailableScene = LoadRequiredSourceAsset<SceneAsset>(UnavailableScenePath, "Generated unavailable-scene fixture");

            ValidateGeneratedApplication(
                Settings + "/GameApplication_QaIfAdr045.asset",
                "IF-ADR-045 Persistent Content Lifecycle",
                routeA,
                template,
                persistentScene,
                PersistentScenePath,
                "QA_IF_ADR_045_Persistent");
            ValidateGeneratedApplication(
                Settings + "/GameApplication_QaIfAdr045_InvalidPath.asset",
                "IF-ADR-045 Invalid Explicit Path",
                routeA,
                template,
                persistentScene,
                Scenes + "/Missing/QA_IF_ADR_045_Persistent.unity",
                "QA_IF_ADR_045_Persistent",
                scenePathMustExist: false);
            ValidateGeneratedApplication(
                Settings + "/GameApplication_QaIfAdr045_UnavailablePath.asset",
                "IF-ADR-045 Valid but Unavailable Path",
                routeA,
                template,
                unavailableScene,
                UnavailableScenePath,
                "QA_IF_ADR_045_Persistent");

            ValidateGeneratedMigrationFixtures();
        }

        private static void ValidateGeneratedMigrationFixtures()
        {
            SceneAsset persistentScene = LoadRequiredSourceAsset<SceneAsset>(PersistentScenePath, "Migration fixture Persistent Content scene");
            RouteAsset invalidReference = LoadRequiredSourceAsset<RouteAsset>(Settings + "/Migration_InvalidReference.asset", "Migration invalid-reference fixture");
            RouteAsset routeB = LoadRequiredSourceAsset<RouteAsset>(RouteBPath, "Migration conflict Route B");
            GameApplicationAsset legacy = LoadApplication("Migration_Legacy.asset");
            GameApplicationAsset invalid = LoadApplication("Migration_InvalidLegacy.asset");
            GameApplicationAsset conflict = LoadApplication("Migration_Conflict.asset");
            string conflictPath = routeB.PrimaryScenePath;
            string conflictName = Path.GetFileNameWithoutExtension(conflictPath);

            RequireMigrationFixture(legacy, persistentScene, string.Empty, string.Empty, "persisted legacy fixture");
            RequireMigrationFixture(invalid, invalidReference, string.Empty, string.Empty, "persisted invalid fixture");
            RequireMigrationFixture(conflict, persistentScene, conflictPath, conflictName, "persisted conflict fixture");
        }

        private static void ValidateGeneratedPersistentScene(string sceneAssetPath, string expectedScenarioScenePath)
        {
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            Scene scene = default;
            try
            {
                scene = EditorSceneManager.OpenScene(sceneAssetPath, OpenSceneMode.Additive);
                if (!scene.IsValid() || !scene.isLoaded ||
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(sceneAssetPath) == null)
                {
                    throw new InvalidOperationException(
                        $"D7 postflight could not independently open persisted scene '{sceneAssetPath}'.");
                }

                // Reacquire dependencies after opening a scene, which may invalidate wrappers.
                RequireSourceAssets(out RouteAsset routeA, out RouteAsset routeB, out RouteAsset routeD,
                    out ActivityAsset activityDA, out ActivityAsset activityDB, out _);
                QaIfAdr045PersistentContentLifecycleScenario[] scenarios = FindComponents<QaIfAdr045PersistentContentLifecycleScenario>(scene);
                RouteRequestTrigger[] routeTriggers = FindComponents<RouteRequestTrigger>(scene);
                ActivityRequestTrigger[] activityTriggers = FindComponents<ActivityRequestTrigger>(scene);
                if (scenarios.Length != 1 || routeTriggers.Length != 3 || activityTriggers.Length != 1 ||
                    scenarios[0] == null || routeTriggers[0] == null || routeTriggers[1] == null ||
                    routeTriggers[2] == null || activityTriggers[0] == null)
                {
                    throw new InvalidOperationException(
                        $"D7 persisted scene composition cardinality is invalid. scenarioCount='{scenarios.Length}', routeTriggerCount='{routeTriggers.Length}', activityTriggerCount='{activityTriggers.Length}'.");
                }

                RequireTriggerTarget(routeTriggers, "QA Route A Request", routeA, sceneAssetPath);
                RequireTriggerTarget(routeTriggers, "QA Route B Request", routeB, sceneAssetPath);
                RequireTriggerTarget(routeTriggers, "QA Route D Request", routeD, sceneAssetPath);
                if (!string.Equals(activityTriggers[0].gameObject.name, "QA Activity D-B Request", StringComparison.Ordinal) ||
                    activityTriggers[0].TargetActivity != activityDB)
                {
                    throw new InvalidOperationException(
                        $"D7 persisted Activity trigger does not target Activity D-B. scene='{sceneAssetPath}', target='{activityTriggers[0].TargetActivity?.name ?? "<null>"}'.");
                }

                var serialized = new SerializedObject(scenarios[0]);
                serialized.Update();
                RequirePersistedSceneReference(serialized, "routeA", routeA, sceneAssetPath);
                RequirePersistedSceneReference(serialized, "routeB", routeB, sceneAssetPath);
                RequirePersistedSceneReference(serialized, "routeD", routeD, sceneAssetPath);
                RequirePersistedSceneReference(serialized, "activityDA", activityDA, sceneAssetPath);
                RequirePersistedSceneReference(serialized, "activityDB", activityDB, sceneAssetPath);
                RequirePersistedSceneReference(serialized, "requestRouteA", FindTrigger(routeTriggers, "QA Route A Request"), sceneAssetPath);
                RequirePersistedSceneReference(serialized, "requestRouteB", FindTrigger(routeTriggers, "QA Route B Request"), sceneAssetPath);
                RequirePersistedSceneReference(serialized, "requestRouteD", FindTrigger(routeTriggers, "QA Route D Request"), sceneAssetPath);
                RequirePersistedSceneReference(serialized, "requestActivityDB", activityTriggers[0], sceneAssetPath);
                if (!string.Equals(RequirePersistedSceneValue(serialized, "expectedPersistentScenePath", sceneAssetPath).stringValue,
                        expectedScenarioScenePath, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"D7 persisted Scenario path is invalid in '{sceneAssetPath}'.");
                }
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
        }

        private static void ValidateGeneratedApplication(
            string assetPath,
            string expectedApplicationName,
            RouteAsset expectedStartupRoute,
            GameApplicationAsset template,
            SceneAsset expectedLegacyScene,
            string expectedScenePath,
            string expectedSceneName,
            bool scenePathMustExist = true)
        {
            GameApplicationAsset application = LoadRequiredSourceAsset<GameApplicationAsset>(assetPath, "D7 generated Game Application");
            if (AssetDatabase.LoadMainAssetAtPath(assetPath) != application ||
                !string.Equals(application.ApplicationName, expectedApplicationName, StringComparison.Ordinal) ||
                application.StartupRoute != expectedStartupRoute || application.PersistentContent == null ||
                application.PersistentContent.ContainerScene != expectedLegacyScene ||
                !string.Equals(application.PersistentContent.ContainerScenePath, expectedScenePath, StringComparison.Ordinal) ||
                !string.Equals(application.PersistentContent.ContainerSceneName, expectedSceneName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"D7 postflight found persisted Game Application composition mismatch. asset='{assetPath}', " +
                    $"applicationName='{application.ApplicationName}', startupRoute='{application.StartupRoute?.name ?? "<null>"}', " +
                    $"legacyScene='{application.PersistentContent?.ContainerScene?.name ?? "<null>"}', expectedScene='{expectedLegacyScene?.name ?? "<null>"}', " +
                    $"scenePath='{application.PersistentContent?.ContainerScenePath}', expectedScenePath='{expectedScenePath}', " +
                    $"sceneName='{application.PersistentContent?.ContainerSceneName}', expectedSceneName='{expectedSceneName}'.");
            }

            if (application.PlayerSessionEnabled || application.DefaultPlayerSessionProfile != null ||
                application.CameraSession == null)
            {
                throw new InvalidOperationException(
                    $"D7 postflight found invalid Camera Session composition or unexpected Player Session in '{assetPath}'.");
            }
            string cameraIssue = string.Empty;
            if (!application.CameraSession.TryValidate(out cameraIssue) ||
                application.CameraSession.OutputPrefabs.Count != template.CameraSession.OutputPrefabs.Count ||
                application.StartupCameraAssignments.Count != template.StartupCameraAssignments.Count)
            {
                throw new InvalidOperationException(
                    $"D7 postflight found invalid/persisted Camera Session composition or unexpected Player Session in '{assetPath}'. cameraIssue='{cameraIssue}'.");
            }

            for (int index = 0; index < template.CameraSession.OutputPrefabs.Count; index++)
            {
                GameObject expectedPrefab = template.CameraSession.OutputPrefabs[index];
                GameObject actualPrefab = application.CameraSession.OutputPrefabs[index];
                RequirePersistentAssetReference(actualPrefab, $"{assetPath} Camera Session Output Prefab[{index}]");
                if (actualPrefab != expectedPrefab)
                    throw new InvalidOperationException($"D7 postflight Camera Session Output Prefab[{index}] changed in '{assetPath}'.");
            }

            for (int index = 0; index < template.StartupCameraAssignments.Count; index++)
            {
                SessionCameraAssignmentAsset expectedAssignment = template.StartupCameraAssignments[index];
                SessionCameraAssignmentAsset actualAssignment = application.StartupCameraAssignments[index];
                string assignmentIssue = string.Empty;
                if (actualAssignment == null || actualAssignment != expectedAssignment ||
                    !actualAssignment.TryBuild(out _, out assignmentIssue))
                {
                    throw new InvalidOperationException(
                        $"D7 postflight Session Camera Assignment[{index}] is missing, changed, or invalid in '{assetPath}'. {assignmentIssue}");
                }
                RequirePersistentAssetReference(actualAssignment, $"{assetPath} Session Camera Assignment[{index}]");
                RequirePersistentAssetReference(actualAssignment.RigPrefab, $"{assetPath} Session Camera Assignment[{index}] Rig Prefab");
            }

            if (!expectedStartupRoute.HasValidRouteId || !expectedStartupRoute.HasPrimaryScene)
                throw new InvalidOperationException($"D7 postflight Startup Route is invalid for '{assetPath}'.");
            RequirePersistentAssetReference(expectedStartupRoute, $"{assetPath} Startup Route");
            RequireSceneAsset(expectedStartupRoute.PrimaryScenePath, $"{assetPath} Startup Route Primary Scene");
            RequirePersistentAssetReference(expectedLegacyScene, $"{assetPath} Persistent Content SceneAsset");
            if (scenePathMustExist)
                RequireSceneAsset(expectedScenePath, $"{assetPath} Persistent Content Scene Path");
            else if (AssetDatabase.LoadMainAssetAtPath(expectedScenePath) != null ||
                     File.Exists(GetAbsoluteAssetPath(expectedScenePath)))
                throw new InvalidOperationException($"D7 invalid-path application unexpectedly resolves scene path '{expectedScenePath}'.");
        }

        private static T[] FindComponents<T>(Scene scene) where T : Component
        {
            var components = new System.Collections.Generic.List<T>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
                components.AddRange(roots[index].GetComponentsInChildren<T>(true));
            return components.ToArray();
        }

        private static void RequireTriggerTarget(RouteRequestTrigger[] triggers, string objectName, RouteAsset expected, string scenePath)
        {
            int matches = 0;
            for (int index = 0; index < triggers.Length; index++)
            {
                if (string.Equals(triggers[index].gameObject.name, objectName, StringComparison.Ordinal))
                {
                    matches++;
                    if (triggers[index].TargetRoute != expected)
                        throw new InvalidOperationException($"D7 Route trigger '{objectName}' has an invalid persisted target in '{scenePath}'.");
                }
            }
            if (matches == 1)
                return;
            if (matches > 1)
                throw new InvalidOperationException($"D7 persisted scene '{scenePath}' contains duplicate Route trigger '{objectName}'.");
            throw new InvalidOperationException($"D7 persisted scene '{scenePath}' is missing Route trigger '{objectName}'.");
        }

        private static RouteRequestTrigger FindTrigger(RouteRequestTrigger[] triggers, string objectName)
        {
            for (int index = 0; index < triggers.Length; index++)
                if (string.Equals(triggers[index].gameObject.name, objectName, StringComparison.Ordinal))
                    return triggers[index];
            throw new InvalidOperationException($"D7 persisted Route trigger '{objectName}' could not be reacquired.");
        }

        private static SerializedProperty RequirePersistedSceneValue(SerializedObject serialized, string propertyName, string scenePath)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"D7 Scenario serialized property '{propertyName}' is missing in '{scenePath}'.");
            return property;
        }

        private static void RequirePersistedSceneReference(SerializedObject serialized, string propertyName, UnityEngine.Object expected, string scenePath)
        {
            SerializedProperty property = RequirePersistedSceneValue(serialized, propertyName, scenePath);
            if (property.objectReferenceValue == null || property.objectReferenceValue != expected)
                throw new InvalidOperationException($"D7 Scenario serialized reference '{propertyName}' is missing or incorrect in '{scenePath}'.");
        }

        private static void RequireSourceAssets(
            out RouteAsset routeA,
            out RouteAsset routeB,
            out RouteAsset routeD,
            out ActivityAsset activityDA,
            out ActivityAsset activityDB,
            out GameApplicationAsset templateApplication)
        {
            const string source = "Assets/QA-NEW-004/Settings/";
            routeA = LoadRequiredSourceAsset<RouteAsset>(source + "Route_QaNew004_A.asset", "Route A");
            routeB = LoadRequiredSourceAsset<RouteAsset>(source + "Route_QaNew004_B.asset", "Route B");
            routeD = LoadRequiredSourceAsset<RouteAsset>(source + "Route_QaNew004_D.asset", "Route D");
            activityDA = LoadRequiredSourceAsset<ActivityAsset>(source + "Activity_QaNew004_D_A.asset", "Activity D-A");
            activityDB = LoadRequiredSourceAsset<ActivityAsset>(source + "Activity_QaNew004_D_B.asset", "Activity D-B");
            templateApplication = LoadRequiredSourceAsset<GameApplicationAsset>(TemplateApplicationPath, "Game Application template");

            if (!routeA.HasValidRouteId || !routeB.HasValidRouteId || !routeD.HasValidRouteId)
                throw new InvalidOperationException("QA-NEW-004 Routes A, B and D must have valid Route IDs for public Game Flow requests.");

            if (routeD.StartupActivity == null || routeD.StartupActivity != activityDA)
                throw new InvalidOperationException($"QA-NEW-004 Route D has no loaded Startup Activity reference. routeAsset='{source}Route_QaNew004_D.asset'.");
            RequirePersistentAssetReference(routeD.StartupActivity, "Route D Startup Activity");
            RequireSceneAsset(routeA.PrimaryScenePath, "QA-NEW-004 Route A Primary Scene");
            RequireSceneAsset(routeB.PrimaryScenePath, "QA-NEW-004 Route B Primary Scene");
            RequireSceneAsset(routeD.PrimaryScenePath, "QA-NEW-004 Route D Primary Scene");
            if (!routeD.StartupActivity.HasActivityContentProfile ||
                routeD.StartupActivity.ActivityContentProfile == null ||
                routeD.StartupActivity.ActivityContentProfile.SceneCount != 1)
            {
                throw new InvalidOperationException(
                    $"QA-NEW-004 Route D Startup Activity must reference exactly one Activity Content Scene. activity='{routeD.StartupActivity.name}'.");
            }
            ActivityContentSceneEntry startupActivityScene = routeD.StartupActivity.ActivityContentProfile.Scenes[0];
            RequirePersistentAssetReference(routeD.StartupActivity.ActivityContentProfile, "Activity D-A Content Profile");
            RequireSceneAsset(
                startupActivityScene.ScenePath,
                "QA-NEW-004 Route D Startup Activity Content Scene");
            if (!routeD.StartupActivity.HasValidActivityId ||
                startupActivityScene.LoadMode != ActivityContentSceneLoadMode.Additive ||
                startupActivityScene.ReleasePolicy != ActivityContentReleasePolicy.ReleaseOnActivityChange ||
                startupActivityScene.Requiredness != FrameworkContentRequiredness.Required)
            {
                throw new InvalidOperationException(
                    $"QA-NEW-004 Activity D-A must have a valid identity and exactly one Required, Additive, ReleaseOnActivityChange scene declaration. activity='{routeD.StartupActivity.name}', contentId='{startupActivityScene.ContentId}', loadMode='{startupActivityScene.LoadMode}', releasePolicy='{startupActivityScene.ReleasePolicy}', requiredness='{startupActivityScene.Requiredness}'.");
            }

            if (!activityDB.HasValidActivityId)
                throw new InvalidOperationException($"QA-NEW-004 Activity D-B has no valid Activity ID. activityAsset='{source}Activity_QaNew004_D_B.asset'.");
            if (activityDB.HasActivityContentProfile && activityDB.ActivityContentProfile.SceneCount > 0)
                throw new InvalidOperationException($"QA-NEW-004 Activity D-B must remain content-free in this scenario so the Activity transition proves release of D-A content. activityAsset='{source}Activity_QaNew004_D_B.asset'.");
            if (routeA.HasRouteContentProfile || routeB.HasRouteContentProfile || routeD.HasRouteContentProfile)
                throw new InvalidOperationException("QA-NEW-004 Routes A, B and D must not declare additional Route Content scenes in this Persistent Content lifecycle fixture.");

            string cameraIssue = string.Empty;
            bool cameraConfigurationValid = templateApplication.CameraSession != null &&
                                            templateApplication.CameraSession.TryValidate(out cameraIssue);
            if (!cameraConfigurationValid)
            {
                throw new InvalidOperationException(
                    $"QA-NEW-004 Game Application template Camera Session is invalid. templateAsset='{TemplateApplicationPath}'. {cameraIssue}");
            }

            string assignmentIssue = "Exactly one Session Camera Assignment is required.";
            bool assignmentValid = templateApplication.StartupCameraAssignments.Count == 1 &&
                                  templateApplication.StartupCameraAssignments[0] != null &&
                                  templateApplication.StartupCameraAssignments[0].TryBuild(out _, out assignmentIssue);
            if (!assignmentValid)
            {
                throw new InvalidOperationException(
                    $"QA-NEW-004 Game Application template must contain one valid Session Camera Assignment. templateAsset='{TemplateApplicationPath}'. {assignmentIssue}");
            }

            if (templateApplication.StartupRoute == null || !templateApplication.StartupRoute.HasPrimaryScene ||
                !templateApplication.StartupRoute.HasValidRouteId)
                throw new InvalidOperationException($"QA-NEW-004 Game Application template has no valid Startup Route. templateAsset='{TemplateApplicationPath}'.");
            if (templateApplication.PlayerSessionEnabled || templateApplication.DefaultPlayerSessionProfile != null)
                throw new InvalidOperationException($"QA-NEW-004 Game Application template must keep Player Session disabled for this non-Player lifecycle fixture. templateAsset='{TemplateApplicationPath}'.");
            RequireSceneAsset(templateApplication.StartupRoute.PrimaryScenePath, "QA-NEW-004 template Startup Route Primary Scene");
            RequirePersistentAssetReference(templateApplication.StartupRoute, "Game Application template Startup Route");
            for (int index = 0; index < templateApplication.CameraSession.OutputPrefabs.Count; index++)
                RequirePersistentAssetReference(templateApplication.CameraSession.OutputPrefabs[index], $"Camera Session Output Prefab[{index}]");
            RequirePersistentAssetReference(templateApplication.StartupCameraAssignments[0], "Session Camera Assignment");

            UnityEngine.Object legacyTemplateScene = templateApplication.PersistentContent?.ContainerScene;
            if (!(legacyTemplateScene is SceneAsset templateScene))
                throw new InvalidOperationException($"QA-NEW-004 Game Application template legacy Persistent Content reference is not a SceneAsset. templateAsset='{TemplateApplicationPath}', referenceType='{(legacyTemplateScene == null ? "<null>" : legacyTemplateScene.GetType().FullName)}'.");
            RequireSceneAsset(AssetDatabase.GetAssetPath(templateScene), "QA-NEW-004 template legacy Persistent Content Scene");
        }

        private static T LoadRequiredSourceAsset<T>(string assetPath, string role)
            where T : UnityEngine.Object
        {
            string absolutePath = GetAbsoluteAssetPath(assetPath);
            UnityEngine.Object mainAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            T typedAsset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (string.IsNullOrWhiteSpace(absolutePath) || !File.Exists(absolutePath) || mainAsset == null ||
                mainAsset.GetType() != typeof(T) || typedAsset == null)
            {
                throw new InvalidOperationException(
                    $"Required QA-NEW-004 source asset is unavailable. role='{role}', assetPath='{assetPath}', " +
                    $"fileExists='{(!string.IsNullOrWhiteSpace(absolutePath) && File.Exists(absolutePath))}', " +
                    $"mainAssetType='{(mainAsset == null ? "<null>" : mainAsset.GetType().FullName)}', " +
                    $"expectedType='{typeof(T).FullName}', typedAssetLoaded='{typedAsset != null}'. " +
                    "Restore/import the canonical QA-NEW-004 asset and resolve Framework compile/import errors before rerunning Prepare.");
            }

            return typedAsset;
        }

        private static TemplateAssetIdentity CaptureTemplateIdentity(GameApplicationAsset templateApplication)
        {
            if (templateApplication == null)
                return new TemplateAssetIdentity(TemplateApplicationPath, string.Empty);

            string assetPath = AssetDatabase.GetAssetPath(templateApplication);
            string guid = string.IsNullOrWhiteSpace(assetPath)
                ? string.Empty
                : AssetDatabase.AssetPathToGUID(assetPath);
            if (!string.Equals(assetPath, TemplateApplicationPath, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(guid))
                throw new InvalidOperationException(
                    $"Game Application template did not resolve to its canonical path and GUID. expectedPath='{TemplateApplicationPath}', actualPath='{assetPath}', guid='{guid}'.");
            return new TemplateAssetIdentity(assetPath, guid);
        }

        private readonly struct TemplateAssetIdentity
        {
            public TemplateAssetIdentity(string assetPath, string guid)
            {
                AssetPath = assetPath ?? string.Empty;
                Guid = guid ?? string.Empty;
            }

            public string AssetPath { get; }
            public string Guid { get; }
        }

        private static void RequireSceneAsset(string scenePath, string role)
        {
            if (string.IsNullOrWhiteSpace(scenePath) ||
                !scenePath.StartsWith("Assets/", StringComparison.Ordinal) ||
                LoadRequiredSourceAsset<SceneAsset>(scenePath, role) == null)
            {
                throw new InvalidOperationException($"Required QA scene reference is invalid. role='{role}', scenePath='{scenePath}'.");
            }
        }

        private static void RequirePersistentAssetReference(UnityEngine.Object asset, string role)
        {
            string assetPath = asset != null ? AssetDatabase.GetAssetPath(asset) : string.Empty;
            string absolutePath = GetAbsoluteAssetPath(assetPath);
            if (asset == null || string.IsNullOrWhiteSpace(assetPath) ||
                string.IsNullOrWhiteSpace(absolutePath) || !File.Exists(absolutePath) ||
                AssetDatabase.LoadMainAssetAtPath(assetPath) != asset)
            {
                throw new InvalidOperationException(
                    $"QA-NEW-004 template dependency is missing, transient, or not the main asset. role='{role}', " +
                    $"assetPath='{assetPath}', fileExists='{(!string.IsNullOrWhiteSpace(absolutePath) && File.Exists(absolutePath))}', " +
                    $"assetType='{(asset == null ? "<null>" : asset.GetType().FullName)}'.");
            }
        }

        private static SerializedProperty RequireSerializedProperty(
            SerializedObject serializedObject,
            string propertyPath,
            string assetPath)
        {
            SerializedProperty property = serializedObject != null
                ? serializedObject.FindProperty(propertyPath)
                : null;
            if (property == null)
                throw new InvalidOperationException($"Serialized field '{propertyPath}' was not found while authoring QA asset '{assetPath}'. Check Framework/QA assembly compatibility.");
            return property;
        }

        private static void EnsureGeneratedAssetPathAvailable(string path)
        {
            string absolutePath = GetAbsoluteAssetPath(path);
            if (AssetDatabase.LoadMainAssetAtPath(path) != null ||
                (!string.IsNullOrWhiteSpace(absolutePath) && File.Exists(absolutePath)))
            {
                throw new InvalidOperationException($"Refusing to overwrite existing D7 asset path '{path}'. Inspect partial preparation and use scoped cleanup.");
            }
        }

        private static void EnsureNoGeneratedApplicationIsActive()
        {
            string[] generatedApplications =
            {
                Settings + "/GameApplication_QaIfAdr045.asset",
                Settings + "/GameApplication_QaIfAdr045_InvalidPath.asset",
                Settings + "/GameApplication_QaIfAdr045_UnavailablePath.asset"
            };
            string[] settingsGuids = AssetDatabase.FindAssets("t:ImmersiveFrameworkSettingsAsset");
            for (int index = 0; index < settingsGuids.Length; index++)
            {
                string settingsPath = AssetDatabase.GUIDToAssetPath(settingsGuids[index]);
                ImmersiveFrameworkSettingsAsset settings =
                    AssetDatabase.LoadAssetAtPath<ImmersiveFrameworkSettingsAsset>(settingsPath);
                string activeApplicationPath = settings != null && settings.ActiveGameApplication != null
                    ? AssetDatabase.GetAssetPath(settings.ActiveGameApplication)
                    : string.Empty;
                for (int applicationIndex = 0; applicationIndex < generatedApplications.Length; applicationIndex++)
                {
                    if (string.Equals(activeApplicationPath, generatedApplications[applicationIndex], StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"Cannot clean D7 assets while a generated Game Application is active. activeApplication='{activeApplicationPath}', " +
                            $"settingsAsset='{settingsPath}'. Restore the previous Active Game Application, then rerun cleanup; settings are not modified by QA.");
                    }
                }
            }
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (AssetDatabase.IsValidFolder(path))
                return;
            if (string.IsNullOrWhiteSpace(AssetDatabase.CreateFolder(parent, name)))
                throw new InvalidOperationException($"Could not create QA folder '{path}'.");
            if (!AssetDatabase.IsValidFolder(path))
                throw new InvalidOperationException($"Unity returned a folder GUID but the QA folder is not available in AssetDatabase: '{path}'.");
        }

        private static string GetAbsoluteAssetPath(string assetPath)
        {
            const string assetsPrefix = "Assets/";
            if (string.IsNullOrWhiteSpace(assetPath) || !assetPath.StartsWith(assetsPrefix, StringComparison.Ordinal))
                return string.Empty;

            return Path.GetFullPath(Path.Combine(Application.dataPath, assetPath.Substring(assetsPrefix.Length)));
        }

        private static void EnsureNoModifiedOpenScenes(string operation)
        {
            var modified = new System.Collections.Generic.List<string>();
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene scene = SceneManager.GetSceneAt(index);
                if (scene.IsValid() && scene.isLoaded && (scene.isDirty || string.IsNullOrWhiteSpace(scene.path)))
                {
                    string reason = scene.isDirty ? "unsaved changes" : "untitled scene has no restorable asset path";
                    modified.Add($"'{(string.IsNullOrWhiteSpace(scene.path) ? "<untitled scene>" : scene.path)}' ({reason})");
                }
            }

            if (modified.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Cannot {operation} IF-ADR-045 QA while open scenes are dirty or untitled. Save/name them explicitly and rerun; no save/discard prompt is used. Scenes: {string.Join(", ", modified)}");
            }
        }

        private static void DeleteFolderIfEmpty(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
                return;

            string relativePath = path.Substring("Assets/".Length)
                .Replace('/', Path.DirectorySeparatorChar);
            string absolutePath = Path.Combine(Application.dataPath, relativePath);
            if (Directory.Exists(absolutePath) &&
                Directory.GetFileSystemEntries(absolutePath, "*", SearchOption.AllDirectories).Length > 0)
            {
                return;
            }

            if (!AssetDatabase.DeleteAsset(path))
                throw new InvalidOperationException($"Could not remove empty generated folder '{path}'.");
        }

        private static string Sanitize(string value) =>
            (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Replace("'", "\"");
    }
}
