using System;
using System.Collections.Generic;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Editor.CameraAuthoring;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace ImmersiveFrameworkQA.Camera.Editor
{
    /// <summary>
    /// Canonical  installer. The session output belongs exclusively
    /// to QA_UIGlobal.
    /// </summary>
    internal static class QaCameraOverrideAuthorityInstaller
    {
        private const string GlobalScenePath =
            "Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scenes/" +
            "QA_UIGlobal.unity";

        private const string OutputRootName =
            "QA C9R Session Camera Output";

        private const string TargetName =
            "QA C9R Session Target";

        private const string RigName =
            "QA C9R Session Rig";

        private const string CameraName =
            "QA C9R Session Cinemachine Camera";

        [MenuItem("Immersive Framework/QA/Setup/Camera/Install Camera Override Authority QA")]
        private static void Install()
        {
            try
            {
                EnsurePersistentSessionOutput();
                Editor.QaCameraOverrideAuthoritySceneInstaller.Install();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[_CAMERA_OVERRIDE_AUTHORITY_SETUP] " +
                    "status='Succeeded'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[_CAMERA_OVERRIDE_AUTHORITY_SETUP] " +
                    "status='Failed' " +
                    $"exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}'.");

                throw;
            }
        }

        private static void EnsurePersistentSessionOutput()
        {
            Scene scene = EditorSceneManager.OpenScene(
                GlobalScenePath,
                OpenSceneMode.Single);

            CameraOutputSessionBinding output =
                EnsureSingleOutput(scene);

            GameObject root = output.gameObject;
            root.name = OutputRootName;
            RemoveSupersededSessionChildren(root.transform);

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

            CinemachineCamera cinemachine =
                EnsureSingleQaOwnedCinemachineCamera(
                    rig,
                    composer);

            cinemachine.enabled = false;

            Set(
                composer,
                "presentationIntent",
                (int)CameraRigPresentationIntent.Follow);

            Set(
                composer,
                "targetSourceKind",
                (int)CameraTargetSourceKind.ExplicitTransform);

            Set(
                composer,
                "targetSource",
                null);

            Set(
                composer,
                "explicitFollowTarget",
                target.transform);

            Set(
                composer,
                "explicitLookAtTarget",
                target.transform);

            Set(
                composer,
                "followRequirement",
                (int)CameraTargetRequirement.Required);

            Set(
                composer,
                "lookAtRequirement",
                (int)CameraTargetRequirement.Optional);

            Set(
                composer,
                "cinemachineCamera",
                cinemachine);

            Set(
                composer,
                "logApplyRebuildDiagnostics",
                false);

            CameraRigComposerApplyRebuildResult composerResult =
                CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(
                    composer,
                    false,
                    false);

            if (!composerResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "QA persistent Session Camera rig could not be materialized. " +
                    composerResult.BlockingIssue);
            }

            cinemachine.enabled = false;

            Set(
                output,
                "defaultCameraRig",
                composer);

            SessionCameraOverrideBinding session =
                EnsureSingleSessionOverride(
                    scene,
                    root);

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

            ValidatePersistentSessionComposition(
                scene,
                output,
                session,
                composer,
                target.transform);

            EditorSceneManager.MarkSceneDirty(scene);

            if (!EditorSceneManager.SaveScene(
                    scene,
                    GlobalScenePath))
            {
                throw new InvalidOperationException(
                    "QA_UIGlobal could not be saved.");
            }
        }

        private static CinemachineCamera
            EnsureSingleQaOwnedCinemachineCamera(
                GameObject rig,
                CameraRigComposer composer)
        {
            if (rig == null || composer == null)
            {
                throw new InvalidOperationException(
                    "QA C9R Camera repair requires the canonical rig and composer.");
            }

            GameObject cameraObject =
                EnsureChild(
                    rig.transform,
                    CameraName);

            CinemachineCamera selected =
                EnsureComponent<CinemachineCamera>(
                    cameraObject);

            CinemachineCamera[] localCameras =
                rig.GetComponentsInChildren<
                    CinemachineCamera>(
                    true);

            int removed = 0;

            for (int index =
                     localCameras.Length - 1;
                 index >= 0;
                 index--)
            {
                CinemachineCamera candidate =
                    localCameras[index];

                if (candidate == null ||
                    ReferenceEquals(
                        candidate,
                        selected))
                {
                    continue;
                }

                // The complete QA C9R rig subtree is authored and owned by this
                // installer. Removing an extra CinemachineCamera component here
                // repairs fixture state; it is not product materialization and
                // does not weaken ADR-022 external/unknown ownership protection.
                UnityEngine.Object.DestroyImmediate(
                    candidate);

                removed++;
            }

            if (removed > 0)
            {
                Debug.Log(
                    "[_CAMERA_OVERRIDE_AUTHORITY_SETUP] " +
                    "status='Repaired' " +
                    "repair='RemovedExtraQaOwnedCinemachineCameraComponents' " +
                    $"count='{removed}'.");
            }

            return selected;
        }

        private static void RemoveSupersededSessionChildren(Transform root)
        {
            string[] supersededNames =
            {
                "QA  Session Target",
                "QA Session Target",
                "QA  Session Rig",
                "QA Session Rig"
            };

            for (int index = root.childCount - 1; index >= 0; index--)
            {
                Transform child = root.GetChild(index);
                for (int nameIndex = 0;
                     nameIndex < supersededNames.Length;
                     nameIndex++)
                {
                    if (child.name != supersededNames[nameIndex])
                    {
                        continue;
                    }

                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                    break;
                }
            }
        }

        private static void ValidatePersistentSessionComposition(
            Scene scene,
            CameraOutputSessionBinding output,
            SessionCameraOverrideBinding session,
            CameraRigComposer composer,
            Transform target)
        {
            List<CameraOutputSessionBinding> outputs =
                FindInScene<CameraOutputSessionBinding>(scene);
            List<SessionCameraOverrideBinding> overrides =
                FindInScene<SessionCameraOverrideBinding>(scene);

            if (outputs.Count != 1 ||
                overrides.Count != 1 ||
                !ReferenceEquals(outputs[0], output) ||
                !ReferenceEquals(overrides[0], session))
            {
                throw new InvalidOperationException(
                    "QA_UIGlobal Camera C9R repair did not leave exactly one persistent output and one Session override.");
            }

            if (output.OutputIdText != "camera.output.main" ||
                output.UnityCamera == null ||
                output.CinemachineBrain == null ||
                !ReferenceEquals(output.DefaultCameraRig, composer) ||
                output.UnityCamera.gameObject != output.gameObject ||
                output.CinemachineBrain.gameObject != output.gameObject)
            {
                throw new InvalidOperationException(
                    "QA_UIGlobal persistent Camera output is invalid after C9R repair.");
            }

            if (!ReferenceEquals(session.PersistentOutputSession, output) ||
                session.ScopeId != "qa.c9r.session.camera" ||
                session.RequestIdText != "qa.camera.request.c9r.session" ||
                !ReferenceEquals(session.RigComposer, composer) ||
                !ReferenceEquals(session.TargetSource, target) ||
                session.Precedence != 300 ||
                session.TieBreakerId != "session")
            {
                throw new InvalidOperationException(
                    "QA_UIGlobal Session Camera override is invalid after C9R repair.");
            }

            if (composer.TargetSourceKind != CameraTargetSourceKind.ExplicitTransform ||
                composer.TargetSourceBehaviour != null ||
                !ReferenceEquals(composer.ExplicitFollowTarget, target) ||
                !ReferenceEquals(composer.ExplicitLookAtTarget, target) ||
                !composer.ResolveConfiguredCameraTargets().IsSucceeded)
            {
                throw new InvalidOperationException(
                    "QA_UIGlobal Session Camera rig is invalid after C9R repair.");
            }

            CinemachineCamera[] localCameras =
                composer.GetComponentsInChildren<
                    CinemachineCamera>(
                    true);

            if (composer.CinemachineCamera == null ||
                localCameras.Length != 1 ||
                !ReferenceEquals(
                    localCameras[0],
                    composer.CinemachineCamera) ||
                composer.CinemachineCamera.gameObject.name !=
                    CameraName ||
                composer.CinemachineCamera.transform.parent !=
                    composer.transform)
            {
                throw new InvalidOperationException(
                    "QA_UIGlobal Camera C9R repair did not leave exactly one canonical local CinemachineCamera.");
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

            if (value == null)
            {
                if (item.propertyType != SerializedPropertyType.ObjectReference)
                {
                    throw new InvalidOperationException(
                        $"Serialized property '{property}' on '{target.GetType().Name}' " +
                        $"does not accept a null object reference. type='{item.propertyType}'.");
                }

                item.objectReferenceValue = null;
            }
            else switch (value)
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
