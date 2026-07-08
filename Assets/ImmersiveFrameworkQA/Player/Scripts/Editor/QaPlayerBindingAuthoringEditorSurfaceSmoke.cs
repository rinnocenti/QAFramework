using System;
using System.Reflection;
using Immersive.Framework.PlayerBinding;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Player.Editor
{
    public static class QaPlayerBindingAuthoringEditorSurfaceSmoke
    {
        private const string ScenePath = "Assets/ImmersiveFrameworkQA/Player/Scenes/QA_PlayerBindingAuthoringValidator.unity";
        private const string RootName = "QA_PlayerBindingAuthoringValidator_Root";
        private const string UtilityTypeName = "Immersive.Framework.Editor.PlayerBinding.PlayerBindingAuthoringValidationEditorUtility";
        private const string WindowTypeName = "Immersive.Framework.Editor.PlayerBinding.PlayerBindingAuthoringValidationWindow";

        [MenuItem("Immersive Framework QA/Player/Run F50B Player Binding Authoring Editor Surface Smoke")]
        public static void RunSmoke()
        {
            Type utilityType = ResolveEditorType(UtilityTypeName);
            Type windowType = ResolveEditorType(WindowTypeName);
            bool editorSurfaceAvailable = utilityType != null && windowType != null;
            Debug.Log($"[F50B_PLAYER_BINDING_AUTHORING_EDITOR_SURFACE_QA] Editor surface available. passed='{editorSurfaceAvailable}' utility='{(utilityType != null)}' window='{(windowType != null)}'.");

            if (!editorSurfaceAvailable)
            {
                Debug.Log($"[F50B_PLAYER_BINDING_AUTHORING_EDITOR_SURFACE_QA] status='Failed' activeScene='False' root='False' selectedRoot='False' missingSelected='False' window='False' passiveBoundary='False'.");
                return;
            }

            QaPlayerBindingAuthoringValidatorSceneBuilder.CreateOrRefreshPlayerBindingAuthoringValidatorScene();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            PlayerBindingAuthoringValidationReport activeSceneReport = InvokeReport(
                utilityType,
                "ValidateActiveScene",
                new[] { typeof(string) },
                "qa.f50b.active-scene");
            bool activeScene = activeSceneReport != null
                && activeSceneReport.Succeeded
                && activeSceneReport.IsReadyForFullBinding
                && activeSceneReport.BlockingIssueCount == 0;
            LogReport("Active scene editor validation", activeScene, activeSceneReport);

            GameObject root = GameObject.Find(RootName);
            PlayerBindingAuthoringValidationReport rootReport = InvokeReport(
                utilityType,
                "ValidateRoot",
                new[] { typeof(GameObject), typeof(string) },
                root,
                "qa.f50b.root");
            bool rootValidation = root != null
                && rootReport != null
                && rootReport.Succeeded
                && rootReport.IsReadyForFullBinding
                && rootReport.BlockingIssueCount == 0;
            LogReport("Root editor validation", rootValidation, rootReport);

            Selection.activeGameObject = root;
            PlayerBindingAuthoringValidationReport selectedRootReport = InvokeReport(
                utilityType,
                "ValidateSelectedRoot",
                new[] { typeof(string) },
                "qa.f50b.selected-root");
            bool selectedRoot = selectedRootReport != null
                && selectedRootReport.Succeeded
                && selectedRootReport.IsReadyForFullBinding
                && selectedRootReport.BlockingIssueCount == 0;
            LogReport("Selected root editor validation", selectedRoot, selectedRootReport);

            Selection.activeGameObject = null;
            PlayerBindingAuthoringValidationReport missingSelectedReport = InvokeReport(
                utilityType,
                "ValidateSelectedRoot",
                new[] { typeof(string) },
                "qa.f50b.missing-selected-root");
            bool missingSelected = missingSelectedReport != null
                && missingSelectedReport.Failed
                && missingSelectedReport.HasBlockingIssue(PlayerBindingAuthoringIssueKind.MissingValidationRoot);
            LogReport("Missing selected root editor validation", missingSelected, missingSelectedReport);

            bool windowOpened = InvokeWindowOpen(windowType);
            Debug.Log($"[F50B_PLAYER_BINDING_AUTHORING_EDITOR_SURFACE_QA] Window opened. passed='{windowOpened}'.");

            bool passiveBoundary = activeSceneReport != null
                && !activeSceneReport.BindsView
                && !activeSceneReport.BindsControl
                && !activeSceneReport.ActivatesCamera
                && !activeSceneReport.ActivatesInput
                && !activeSceneReport.EnablesMovement
                && !activeSceneReport.SpawnsActor;
            LogReport("Passive boundary", passiveBoundary, activeSceneReport);

            bool passed = activeScene
                && rootValidation
                && selectedRoot
                && missingSelected
                && windowOpened
                && passiveBoundary;

            Debug.Log($"[F50B_PLAYER_BINDING_AUTHORING_EDITOR_SURFACE_QA] status='{(passed ? "Succeeded" : "Failed")}' activeScene='{activeScene}' root='{rootValidation}' selectedRoot='{selectedRoot}' missingSelected='{missingSelected}' window='{windowOpened}' passiveBoundary='{passiveBoundary}'.");
        }

        private static Type ResolveEditorType(string fullName)
        {
            Type direct = Type.GetType(fullName + ", Immersive.Framework.Editor");
            if (direct != null)
            {
                return direct;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type type = assemblies[i].GetType(fullName, false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static PlayerBindingAuthoringValidationReport InvokeReport(
            Type ownerType,
            string methodName,
            Type[] parameterTypes,
            params object[] arguments)
        {
            if (ownerType == null)
            {
                return null;
            }

            MethodInfo method = ownerType.GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.Static,
                null,
                parameterTypes,
                null);
            if (method == null)
            {
                Debug.LogError($"[F50B_PLAYER_BINDING_AUTHORING_EDITOR_SURFACE_QA] Missing method '{methodName}' on '{ownerType.FullName}'.");
                return null;
            }

            return method.Invoke(null, arguments) as PlayerBindingAuthoringValidationReport;
        }

        private static bool InvokeWindowOpen(Type windowType)
        {
            if (windowType == null)
            {
                return false;
            }

            MethodInfo method = windowType.GetMethod(
                "OpenWindow",
                BindingFlags.Public | BindingFlags.Static,
                null,
                Type.EmptyTypes,
                null);
            if (method == null)
            {
                Debug.LogError($"[F50B_PLAYER_BINDING_AUTHORING_EDITOR_SURFACE_QA] Missing method 'OpenWindow' on '{windowType.FullName}'.");
                return false;
            }

            object window = method.Invoke(null, null);
            return window != null;
        }

        private static void LogReport(
            string step,
            bool passed,
            PlayerBindingAuthoringValidationReport report)
        {
            string diagnostic = report != null ? report.ToDiagnosticString() : "<null>";
            Debug.Log($"[F50B_PLAYER_BINDING_AUTHORING_EDITOR_SURFACE_QA] {step}. passed='{passed}' report=\"{diagnostic}\".");
        }
    }
}
