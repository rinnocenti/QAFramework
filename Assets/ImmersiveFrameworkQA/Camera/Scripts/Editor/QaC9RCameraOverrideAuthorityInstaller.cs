using System;
using System.Collections.Generic;
using Immersive.Framework.Authoring;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Camera
{
    /// <summary>
    /// Canonical C9R installer. The session output belongs exclusively
    /// to QA_UIGlobal.
    /// </summary>
    internal static class QaC9RCameraOverrideAuthorityInstaller
    {
        private const string GlobalScenePath =
            "Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scenes/" +
            "QA_UIGlobal.unity";

        private const string ApplicationPath =
            "Assets/ImmersiveFrameworkQA/GameApplications/" +
            "GameApplication.asset";

        private const string OutputRootName =
            "QA C9R Session Camera Output";

        private const string TargetName =
            "QA C9R Session Target";

        private const string RigName =
            "QA C9R Session Rig";

        private const string CameraName =
            "QA C9R Session Cinemachine Camera";

        [MenuItem(
            "Immersive Framework QA/Camera/" +
            "C9R Install Camera Override Authority QA")]
        private static void Install()
        {
            try
            {
                EnsurePersistentSessionOutput();
                Editor.QaC9RCameraOverrideAuthoritySceneInstaller.Install();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[C9R_CAMERA_OVERRIDE_AUTHORITY_SETUP] " +
                    "status='Succeeded'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[C9R_CAMERA_OVERRIDE_AUTHORITY_SETUP] " +
                    "status='Failed' " +
                    $"exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}'.");

                throw;
            }
        }

        private static void EnsurePersistentSessionOutput()
        {
            GameApplicationAsset application =
                AssetDatabase.LoadAssetAtPath<GameApplicationAsset>(
                    ApplicationPath)
                ?? throw new InvalidOperationException(
                    "QA GameApplication asset is missing.");

            Scene scene = EditorSceneManager.OpenScene(
                GlobalScenePath,
                OpenSceneMode.Single);

            CameraOutputSessionBinding output =
                EnsureSingleOutput(scene);

            GameObject root = output.gameObject;
            root.name = OutputRootName;

            UnityEngine.Camera unityCamera =
                EnsureComponent<UnityEngine.Camera>(root);

            CinemachineBrain brain =
                EnsureComponent<CinemachineBrain>(root);

            Set(output, "outputId", "camera.output.main");
            Set(output, "unityCamera", unityCamera);
            Set(output, "cinemachineBrain", brain);
            Set(output, "initializeOnAwake", true);
            Set(output, "logDiagnostics", true);

            GameObject target =
                EnsureChild(
                    root.transform,
                    TargetName);

            GameObject rig =
                EnsureChild(
                    root.transform,
                    RigName);

            CameraRigComposer composer =
                EnsureComponent<CameraRigComposer>(rig);

            GameObject cameraObject =
                EnsureChild(
                    rig.transform,
                    CameraName);

            CinemachineCamera cinemachine =
                EnsureComponent<CinemachineCamera>(cameraObject);

            cinemachine.enabled = false;

            Set(
                composer,
                "cinemachineCamera",
                cinemachine);

            Set(
                composer,
                "createCinemachineCameraIfMissing",
                false);

            Set(
                composer,
                "logApplyRebuildDiagnostics",
                false);

            Set(
                composer,
                "targetSourceKind",
                30);

            Set(
                composer,
                "explicitFollowTarget",
                target.transform);

            Set(
                composer,
                "explicitLookAtTarget",
                target.transform);

            SessionCameraOverrideBinding session =
                EnsureSingleSessionOverride(
                    scene,
                    root);

            Set(
                session,
                "assignedGameApplication",
                application);

            Set(
                session,
                "persistentOutputSession",
                output);

            Set(
                session,
                "scopeId",
                "qa.c9r.session.camera");

            Set(
                session,
                "requestId",
                "qa.camera.request.c9r.session");

            Set(
                session,
                "rigComposer",
                composer);

            Set(
                session,
                "targetSource",
                target.transform);

            Set(
                session,
                "precedence",
                300);

            Set(
                session,
                "tieBreakerId",
                "session");

            Set(
                session,
                "logDiagnostics",
                true);

            EditorSceneManager.MarkSceneDirty(scene);

            if (!EditorSceneManager.SaveScene(
                    scene,
                    GlobalScenePath))
            {
                throw new InvalidOperationException(
                    "QA_UIGlobal could not be saved.");
            }
        }

        private static CameraOutputSessionBinding EnsureSingleOutput(
            Scene scene)
        {
            List<CameraOutputSessionBinding> outputs =
                FindInScene<CameraOutputSessionBinding>(scene);

            CameraOutputSessionBinding selected =
                outputs.Find(
                    item =>
                        item.gameObject.name == OutputRootName);

            selected ??=
                outputs.Count > 0
                    ? outputs[0]
                    : null;

            for (int index = outputs.Count - 1;
                 index >= 0;
                 index--)
            {
                CameraOutputSessionBinding candidate =
                    outputs[index];

                if (candidate == null ||
                    candidate == selected)
                {
                    continue;
                }

                UnityEngine.Object.DestroyImmediate(
                    candidate.gameObject);
            }

            if (selected != null)
            {
                return selected;
            }

            var root =
                new GameObject(OutputRootName);

            SceneManager.MoveGameObjectToScene(
                root,
                scene);

            return root.AddComponent<
                CameraOutputSessionBinding>();
        }

        private static SessionCameraOverrideBinding
            EnsureSingleSessionOverride(
                Scene scene,
                GameObject outputRoot)
        {
            List<SessionCameraOverrideBinding> overrides =
                FindInScene<SessionCameraOverrideBinding>(scene);

            SessionCameraOverrideBinding selected =
                overrides.Find(
                    item =>
                        item.gameObject == outputRoot);

            selected ??=
                overrides.Count > 0
                    ? overrides[0]
                    : null;

            for (int index = overrides.Count - 1;
                 index >= 0;
                 index--)
            {
                SessionCameraOverrideBinding candidate =
                    overrides[index];

                if (candidate == null ||
                    candidate == selected)
                {
                    continue;
                }

                UnityEngine.Object.DestroyImmediate(candidate);
            }

            if (selected == null)
            {
                return outputRoot.AddComponent<
                    SessionCameraOverrideBinding>();
            }

            if (selected.gameObject != outputRoot)
            {
                throw new InvalidOperationException(
                    "QA_UIGlobal has a SessionCameraOverrideBinding " +
                    "outside the persistent camera output root.");
            }

            return selected;
        }

        private static List<T> FindInScene<T>(
            Scene scene)
            where T : Component
        {
            var results = new List<T>();

            foreach (T item in
                     Resources.FindObjectsOfTypeAll<T>())
            {
                if (item != null &&
                    item.gameObject.scene == scene)
                {
                    results.Add(item);
                }
            }

            return results;
        }

        private static GameObject EnsureChild(
            Transform parent,
            string name)
        {
            for (int index = 0;
                 index < parent.childCount;
                 index++)
            {
                Transform child =
                    parent.GetChild(index);

                if (child.name == name)
                {
                    return child.gameObject;
                }
            }

            var childObject =
                new GameObject(name);

            childObject.transform.SetParent(
                parent,
                false);

            return childObject;
        }

        private static T EnsureComponent<T>(
            GameObject target)
            where T : Component
        {
            T component =
                target.GetComponent<T>();

            return component != null
                ? component
                : target.AddComponent<T>();
        }

        private static void Set(
            UnityEngine.Object target,
            string property,
            object value)
        {
            var serialized =
                new SerializedObject(target);

            serialized.Update();

            SerializedProperty item =
                serialized.FindProperty(property)
                ?? throw new InvalidOperationException(
                    $"Serialized property '{property}' was not found " +
                    $"on '{target.GetType().Name}'.");

            switch (value)
            {
                case UnityEngine.Object reference:
                    item.objectReferenceValue = reference;
                    break;

                case string text:
                    item.stringValue = text;
                    break;

                case int number:
                    item.intValue = number;
                    break;

                case bool flag:
                    item.boolValue = flag;
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported value for '{property}'.");
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static string Escape(
            string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'");
        }
    }
}
