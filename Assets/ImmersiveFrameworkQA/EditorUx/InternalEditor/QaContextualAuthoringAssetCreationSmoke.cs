using System;
using System.Reflection;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.EditorUx.Internal.Editor
{
    internal static class QaContextualAuthoringAssetCreationSmoke
    {
        private const string UtilityTypeName =
            "Immersive.Framework.Editor.Editor.Settings.ImmersiveFrameworkEditorSettingsUtility";
        private const string GameApplicationEditorTypeName =
            "Immersive.Framework.Editor.Editor.Authoring.GameApplicationAssetEditor";
        private const string PlayerSessionProfileEditorTypeName =
            "Immersive.Framework.Editor.Editor.PlayerParticipation.PlayerSessionProfileEditor";

        private const string BackendWorkspace =
            "Assets/ImmersiveFrameworkQA/EditorUx/__ContextualAssetCreationBackendSmoke";
        private const string InteractionWorkspace =
            "Assets/ImmersiveFrameworkQA/EditorUx/ContextualAssetCreationSmokeWorkspace";
        private const string InteractionApplicationPath =
            InteractionWorkspace + "/QA_ContextualGameApplication.asset";

        [MenuItem(
            "Immersive Framework/QA/Regressions/Editor UX/Run Contextual Asset Creation Backend Smoke",
            priority = 262)]
        private static void RunBackendSmoke()
        {
            UnityEngine.Object previousSelection = Selection.activeObject;
            PlayerSessionProfile transientProfile = null;
            RouteAsset transientRoute = null;

            try
            {
                Type utilityType = RequireEditorType(UtilityTypeName);
                MethodInfo resolveFolder = RequireStaticMethod(
                    utilityType,
                    "ResolveAuthoredAssetCreationFolder",
                    typeof(string),
                    Type.EmptyTypes,
                    allowPrivate: true);

                VerifyCreationEntrypoints(utilityType);
                VerifyCustomEditors();

                ResetWorkspace(BackendWorkspace);

                UnityEngine.Object folderAsset =
                    AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(BackendWorkspace);
                Require(folderAsset != null, "Backend smoke folder could not be loaded as a Unity asset.");

                Selection.activeObject = folderAsset;
                Require(
                    InvokeFolderResolver(resolveFolder) == BackendWorkspace,
                    "Selecting an Assets folder did not resolve that folder as the authored-asset creation context.");

                var marker = ScriptableObject.CreateInstance<GameApplicationAsset>();
                string markerPath = BackendWorkspace + "/QA_ContextMarker.asset";
                AssetDatabase.CreateAsset(marker, markerPath);
                AssetDatabase.SaveAssets();

                Selection.activeObject = marker;
                Require(
                    InvokeFolderResolver(resolveFolder) == BackendWorkspace,
                    "Selecting an authored asset did not resolve its containing folder as the creation context.");

                transientProfile = ScriptableObject.CreateInstance<PlayerSessionProfile>();
                Selection.activeObject = transientProfile;
                Require(
                    InvokeFolderResolver(resolveFolder) == "Assets",
                    "An unsaved selection did not fall back explicitly to Assets.");

                UnityEngine.Object packageAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                    "Packages/com.immersive.framework/Editor/Settings/ImmersiveFrameworkEditorSettingsUtility.cs");
                Require(
                    packageAsset != null,
                    "Could not load the framework Editor utility through Packages/com.immersive.framework.");

                Selection.activeObject = packageAsset;
                Require(
                    InvokeFolderResolver(resolveFolder) == "Assets",
                    "A package selection leaked Packages ownership into authored-asset creation instead of falling back to Assets.");

                transientRoute = ScriptableObject.CreateInstance<RouteAsset>();
                MethodInfo assignRouteId = RequireStaticMethod(
                    utilityType,
                    "AssignNewRouteId",
                    typeof(void),
                    new[] { typeof(RouteAsset) },
                    allowPrivate: true);
                assignRouteId.Invoke(null, new object[] { transientRoute });

                var routeSerialized = new SerializedObject(transientRoute);
                string routeId = routeSerialized.FindProperty("routeId")?.stringValue;
                Require(
                    !string.IsNullOrWhiteSpace(routeId),
                    "Contextual Startup Route creation no longer has a stable-ID initializer.");

                Debug.Log(
                    "[QA_EDITOR_UX_CONTEXTUAL_ASSET_CREATION_BACKEND] status='Passed' " +
                    "folderSelection='Passed' assetSelection='Passed' unsavedFallback='Assets' " +
                    "packageFallback='Assets' entrypoints='8' customEditors='2' routeStableId='Initialized'.");
            }
            finally
            {
                Selection.activeObject = previousSelection;

                if (transientProfile != null)
                {
                    UnityEngine.Object.DestroyImmediate(transientProfile);
                }

                if (transientRoute != null)
                {
                    UnityEngine.Object.DestroyImmediate(transientRoute);
                }

                if (AssetDatabase.IsValidFolder(BackendWorkspace))
                {
                    AssetDatabase.DeleteAsset(BackendWorkspace);
                    AssetDatabase.Refresh();
                }
            }
        }

        [MenuItem(
            "Immersive Framework/QA/Regressions/Editor UX/Contextual Asset Creation/Prepare Interaction Smoke",
            priority = 263)]
        private static void PrepareInteractionSmoke()
        {
            ResetWorkspace(InteractionWorkspace);

            var application = ScriptableObject.CreateInstance<GameApplicationAsset>();
            AssetDatabase.CreateAsset(application, InteractionApplicationPath);

            var serialized = new SerializedObject(application);
            SerializedProperty applicationName = serialized.FindProperty("applicationName");
            SerializedProperty playerSessionEnabled = serialized.FindProperty("playerSessionEnabled");

            Require(applicationName != null, "GameApplicationAsset is missing serialized property 'applicationName'.");
            Require(playerSessionEnabled != null, "GameApplicationAsset is missing serialized property 'playerSessionEnabled'.");

            applicationName.stringValue = "QA Contextual Asset Creation";
            playerSessionEnabled.boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = application;
            EditorGUIUtility.PingObject(application);

            EditorUtility.DisplayDialog(
                "Contextual Asset Creation Smoke",
                "Workspace prepared and the QA Game Application is selected.\n\n" +
                "1. Startup > Create Startup Route. The Save dialog must start in ContextualAssetCreationSmokeWorkspace. Save the Route there.\n\n" +
                "2. Reselect QA_ContextualGameApplication. Player Session > Create Player Session Profile. Save it in the same workspace.\n\n" +
                "3. In the created Player Session Profile, click Create & Add Player Slot Profile. Save it in the same workspace.\n\n" +
                "4. Run Validate Interaction Smoke from the same QA menu.",
                "OK");

            Debug.Log(
                "[QA_EDITOR_UX_CONTEXTUAL_ASSET_CREATION_INTERACTION] status='Prepared' " +
                "workspace='" + InteractionWorkspace + "'.");
        }

        [MenuItem(
            "Immersive Framework/QA/Regressions/Editor UX/Contextual Asset Creation/Validate Interaction Smoke",
            priority = 264)]
        private static void ValidateInteractionSmoke()
        {
            GameApplicationAsset application =
                AssetDatabase.LoadAssetAtPath<GameApplicationAsset>(InteractionApplicationPath);
            Require(
                application != null,
                "Interaction workspace is not prepared. Run Prepare Interaction Smoke first.");

            var applicationSerialized = new SerializedObject(application);
            SerializedProperty startupRouteProperty =
                applicationSerialized.FindProperty("startupRoute");
            SerializedProperty sessionProfileProperty =
                applicationSerialized.FindProperty("defaultPlayerSessionProfile");

            Require(startupRouteProperty != null, "GameApplicationAsset is missing serialized property 'startupRoute'.");
            Require(sessionProfileProperty != null, "GameApplicationAsset is missing serialized property 'defaultPlayerSessionProfile'.");

            var route = startupRouteProperty.objectReferenceValue as RouteAsset;
            Require(route != null, "Create Startup Route did not assign a Route to the QA Game Application.");
            RequireAssetOwnedByWorkspace(route, "Startup Route");

            var routeSerialized = new SerializedObject(route);
            string routeId = routeSerialized.FindProperty("routeId")?.stringValue;
            Require(
                !string.IsNullOrWhiteSpace(routeId),
                "Created Startup Route has no stable Route ID.");

            var profile = sessionProfileProperty.objectReferenceValue as PlayerSessionProfile;
            Require(
                profile != null,
                "Create Player Session Profile did not assign a Profile to the QA Game Application.");
            RequireAssetOwnedByWorkspace(profile, "Player Session Profile");

            var profileSerialized = new SerializedObject(profile);
            SerializedProperty supportedSlots = profileSerialized.FindProperty("supportedSlots");
            Require(supportedSlots != null, "PlayerSessionProfile is missing serialized property 'supportedSlots'.");
            Require(
                supportedSlots.arraySize == 1,
                "Create & Add Player Slot Profile must leave exactly one Supported Slot in the prepared smoke. Found " +
                supportedSlots.arraySize + ".");

            var slot = supportedSlots.GetArrayElementAtIndex(0).objectReferenceValue as PlayerSlotProfile;
            Require(
                slot != null,
                "Create & Add Player Slot Profile left a null Supported Slot reference.");
            RequireAssetOwnedByWorkspace(slot, "Player Slot Profile");

            Debug.Log(
                "[QA_EDITOR_UX_CONTEXTUAL_ASSET_CREATION_INTERACTION] status='Passed' " +
                "startupRoute='AssignedAndLocal' routeStableId='Initialized' " +
                "playerSessionProfile='AssignedAndLocal' playerSlotProfile='AddedAndLocal'.");

            Selection.activeObject = application;
            EditorGUIUtility.PingObject(application);
            EditorUtility.DisplayDialog(
                "Contextual Asset Creation Smoke",
                "Passed. Startup Route, Player Session Profile and Player Slot Profile were all assigned and remain owned by the QA workspace.\n\nRun Cleanup Interaction Smoke when inspection is complete.",
                "OK");
        }

        [MenuItem(
            "Immersive Framework/QA/Regressions/Editor UX/Contextual Asset Creation/Cleanup Interaction Smoke",
            priority = 265)]
        private static void CleanupInteractionSmoke()
        {
            UnityEngine.Object selected = Selection.activeObject;
            string selectedPath = AssetDatabase.GetAssetPath(selected);
            if (!string.IsNullOrWhiteSpace(selectedPath) &&
                IsOwnedByWorkspace(selectedPath))
            {
                Selection.activeObject = null;
            }

            if (AssetDatabase.IsValidFolder(InteractionWorkspace))
            {
                Require(
                    AssetDatabase.DeleteAsset(InteractionWorkspace),
                    "Could not delete the contextual asset creation interaction workspace.");
                AssetDatabase.Refresh();
            }

            Debug.Log(
                "[QA_EDITOR_UX_CONTEXTUAL_ASSET_CREATION_INTERACTION] status='Cleaned'.");
        }

        private static void VerifyCreationEntrypoints(Type utilityType)
        {
            RequireStaticMethod(
                utilityType,
                "CreateGameApplicationAsset",
                typeof(GameApplicationAsset),
                Type.EmptyTypes,
                allowPrivate: false);
            RequireStaticMethod(
                utilityType,
                "CreateStartupRouteAsset",
                typeof(RouteAsset),
                Type.EmptyTypes,
                allowPrivate: false);
            RequireStaticMethod(
                utilityType,
                "CreatePlayerSessionProfileAsset",
                typeof(PlayerSessionProfile),
                Type.EmptyTypes,
                allowPrivate: false);
            RequireStaticMethod(
                utilityType,
                "CreatePlayerSlotProfileAsset",
                typeof(PlayerSlotProfile),
                Type.EmptyTypes,
                allowPrivate: false);
            RequireStaticMethod(
                utilityType,
                "CreateProgressionSaveProfileAsset",
                RequireRuntimeType("Immersive.Framework.ProgressionSave.ProgressionSaveProfile"),
                new[] { typeof(string) },
                allowPrivate: false);
            RequireStaticMethod(
                utilityType,
                "CreateStartupActivityAsset",
                typeof(ActivityAsset),
                Type.EmptyTypes,
                allowPrivate: false);
            RequireStaticMethod(
                utilityType,
                "CreateRouteContentProfileAsset",
                typeof(RouteContentProfileAsset),
                Type.EmptyTypes,
                allowPrivate: false);
            RequireStaticMethod(
                utilityType,
                "CreateActivityContentProfileAsset",
                typeof(ActivityContentProfileAsset),
                Type.EmptyTypes,
                allowPrivate: false);
        }

        private static void VerifyCustomEditors()
        {
            GameApplicationAsset application = null;
            PlayerSessionProfile sessionProfile = null;
            UnityEditor.Editor applicationEditor = null;
            UnityEditor.Editor sessionEditor = null;

            try
            {
                application = ScriptableObject.CreateInstance<GameApplicationAsset>();
                applicationEditor = UnityEditor.Editor.CreateEditor(application);
                Require(
                    applicationEditor != null,
                    "GameApplicationAsset did not resolve a custom Editor.");
                Require(
                    applicationEditor.GetType().FullName == GameApplicationEditorTypeName,
                    "GameApplicationAsset resolved '" + applicationEditor.GetType().FullName +
                    "' instead of '" + GameApplicationEditorTypeName + "'.");
                Require(
                    applicationEditor.serializedObject.FindProperty("defaultPlayerSessionProfile") != null,
                    "GameApplicationAssetEditor cannot bind Default Player Session Profile.");

                sessionProfile = ScriptableObject.CreateInstance<PlayerSessionProfile>();
                sessionEditor = UnityEditor.Editor.CreateEditor(sessionProfile);
                Require(
                    sessionEditor != null,
                    "PlayerSessionProfile did not resolve a custom Editor.");
                Require(
                    sessionEditor.GetType().FullName == PlayerSessionProfileEditorTypeName,
                    "PlayerSessionProfile resolved '" + sessionEditor.GetType().FullName +
                    "' instead of '" + PlayerSessionProfileEditorTypeName + "'.");
                Require(
                    sessionEditor.serializedObject.FindProperty("supportedSlots") != null,
                    "PlayerSessionProfileEditor cannot bind Supported Slots.");
            }
            finally
            {
                if (applicationEditor != null)
                {
                    UnityEngine.Object.DestroyImmediate(applicationEditor);
                }

                if (sessionEditor != null)
                {
                    UnityEngine.Object.DestroyImmediate(sessionEditor);
                }

                if (application != null)
                {
                    UnityEngine.Object.DestroyImmediate(application);
                }

                if (sessionProfile != null)
                {
                    UnityEngine.Object.DestroyImmediate(sessionProfile);
                }
            }
        }

        private static MethodInfo RequireStaticMethod(
            Type type,
            string methodName,
            Type expectedReturnType,
            Type[] parameterTypes,
            bool allowPrivate)
        {
            BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            if (allowPrivate)
            {
                flags |= BindingFlags.Public;
            }

            MethodInfo method = type.GetMethod(
                methodName,
                flags,
                binder: null,
                types: parameterTypes,
                modifiers: null);

            Require(
                method != null,
                "Required framework Editor method '" + type.FullName + "." + methodName + "' was not found. " +
                "Apply the contextual asset creation package cut before running this smoke.");
            Require(
                method.ReturnType == expectedReturnType,
                "Framework Editor method '" + methodName + "' returns '" + method.ReturnType.FullName +
                "' instead of '" + expectedReturnType.FullName + "'.");

            return method;
        }

        private static string InvokeFolderResolver(MethodInfo resolver)
        {
            try
            {
                return resolver.Invoke(null, null) as string;
            }
            catch (TargetInvocationException exception)
            {
                throw new InvalidOperationException(
                    "Contextual authored-asset folder resolver threw an exception.",
                    exception.InnerException ?? exception);
            }
        }

        private static Type RequireEditorType(string fullName)
        {
            Type type = Type.GetType(fullName + ", Immersive.Framework.Editor");
            Require(
                type != null,
                "Framework Editor type '" + fullName + "' was not found.");
            return type;
        }

        private static Type RequireRuntimeType(string fullName)
        {
            Type type = Type.GetType(fullName + ", Immersive.Framework.Runtime");
            Require(
                type != null,
                "Framework Runtime type '" + fullName + "' was not found.");
            return type;
        }

        private static void RequireAssetOwnedByWorkspace(
            UnityEngine.Object asset,
            string label)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            Require(
                IsOwnedByWorkspace(path),
                label + " was created outside the QA interaction workspace. Actual path: '" + path + "'.");
        }

        private static bool IsOwnedByWorkspace(string path)
        {
            string normalized = (path ?? string.Empty).Replace('\\', '/');
            return normalized.StartsWith(
                InteractionWorkspace + "/",
                StringComparison.Ordinal);
        }

        private static void ResetWorkspace(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                Require(
                    AssetDatabase.DeleteAsset(path),
                    "Could not reset QA workspace '" + path + "'.");
            }

            string parent = path.Substring(0, path.LastIndexOf('/'));
            string name = path.Substring(path.LastIndexOf('/') + 1);
            Require(
                AssetDatabase.IsValidFolder(parent),
                "QA workspace parent does not exist: '" + parent + "'.");

            string guid = AssetDatabase.CreateFolder(parent, name);
            Require(
                !string.IsNullOrWhiteSpace(guid),
                "Could not create QA workspace '" + path + "'.");
            AssetDatabase.Refresh();
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
