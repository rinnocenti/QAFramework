using System;
using System.Collections.Generic;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using ImmersiveFrameworkQA.Player.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Camera.Editor
{
    /// <summary>
    /// Canonical IF-ADR-032 QA baseline guard.
    ///
    /// Persistent Content is not Camera topology authority. This guard removes
    /// historical scene-owned Camera roots, verifies the persisted scene is free
    /// of legacy Camera authority, preserves the requested Player boot profile
    /// and leaves QA_Hub open for the next fresh Framework boot.
    /// </summary>
    internal static class QaCameraPersistentBaselineGuard
    {
        private const string Prefix =
            "[QA_CAMERA_BASELINE]";
        private const string GlobalScenePath =
            "Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scenes/QA_UIGlobal.unity";
        private const string HubScenePath =
            "Assets/ImmersiveFrameworkQA/Hub/Scenes/QA_Hub.unity";
        private const string PendingRestoreKey =
            "ImmersiveFrameworkQA.QA_CAMERA_BASELINE.PendingRestore";
        private const string PendingRestoreReasonKey =
            "ImmersiveFrameworkQA.QA_CAMERA_BASELINE.PendingRestoreReason";

        private static readonly string[] HistoricalCameraRootNames =
        {
            "QA ADR026 Camera Output A",
            "QA ADR026 Camera Output B",
            "QA C9R Session Camera Output",
            "QA CAMERA-028-D Player Camera Output Policy"
        };

        [InitializeOnLoadMethod]
        private static void Register()
        {
            EditorApplication.playModeStateChanged -=
                HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged +=
                HandlePlayModeStateChanged;

            if (!EditorApplication.isPlaying &&
                SessionState.GetBool(
                    PendingRestoreKey,
                    false))
            {
                EditorApplication.delayCall -=
                    RestorePending;
                EditorApplication.delayCall +=
                    RestorePending;
            }
        }

        [MenuItem(
            "Immersive Framework/QA/Setup/Camera/Restore Canonical Camera Baseline",
            priority = 205)]
        private static void RestoreFromMenu()
        {
            try
            {
                RestoreCanonicalBaseline();
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{Prefix} status='Failed' operation='ManualRestore' " +
                    $"exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.GetBaseException().Message)}'.");
                throw;
            }
        }

        internal static void PrepareAndVerify()
        {
            PrepareAndVerify(
                QaPlayerSessionBootProfile.ManagerProvisioned);
        }

        internal static void PrepareAndVerify(
            QaPlayerSessionBootProfile playerSessionBootProfile)
        {
            if (EditorApplication.isPlaying)
            {
                throw new InvalidOperationException(
                    "Camera canonical baseline can only be prepared and verified in Edit Mode.");
            }

            ApplyPlayerBootProfile(
                playerSessionBootProfile);
            QaCameraDefinitionAssetGuard.Ensure();

            Scene scene =
                EditorSceneManager.OpenScene(
                    GlobalScenePath,
                    OpenSceneMode.Single);

            RemoveHistoricalCameraAuthority(
                scene);

            EditorSceneManager.MarkSceneDirty(
                scene);
            EditorSceneManager.SaveScene(
                scene,
                GlobalScenePath);
            AssetDatabase.SaveAssets();

            VerifyPersistedBaseline();

            EditorSceneManager.OpenScene(
                HubScenePath,
                OpenSceneMode.Single);
        }

        internal static void RestoreCanonicalBaseline()
        {
            PrepareAndVerify(
                QaPlayerSessionBootProfile.ManagerProvisioned);

            SessionState.SetBool(
                PendingRestoreKey,
                false);
            SessionState.EraseString(
                PendingRestoreReasonKey);

            Debug.Log(
                $"{Prefix} status='Restored' topology='IF-ADR-032' " +
                "persistentCameraAuthority='0' hub='Restored'.");
        }

        internal static void RequestRestore(
            string reason)
        {
            SessionState.SetBool(
                PendingRestoreKey,
                true);
            SessionState.SetString(
                PendingRestoreReasonKey,
                reason ?? string.Empty);

            if (!EditorApplication.isPlaying)
            {
                EditorApplication.delayCall -=
                    RestorePending;
                EditorApplication.delayCall +=
                    RestorePending;
            }
        }

        private static void ApplyPlayerBootProfile(
            QaPlayerSessionBootProfile profile)
        {
            switch (profile)
            {
                case QaPlayerSessionBootProfile.ManagerProvisioned:
                    QaPlayerSessionBootProfileGuard
                        .UseManagerProvisioned();
                    return;

                case QaPlayerSessionBootProfile.SceneProvided:
                    QaPlayerSessionBootProfileGuard
                        .UseSceneProvided();
                    return;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(profile),
                        profile,
                        "Unsupported Player Session boot profile.");
            }
        }

        private static void RemoveHistoricalCameraAuthority(
            Scene scene)
        {
            var destroy =
                new HashSet<GameObject>();

            foreach (CameraOutputAuthoring output in
                FindInScene<CameraOutputAuthoring>(scene))
            {
                if (output != null)
                {
                    destroy.Add(
                        output.gameObject);
                }
            }

            foreach (GameObject root in
                scene.GetRootGameObjects())
            {
                if (root == null)
                {
                    continue;
                }

                for (int index = 0;
                     index < HistoricalCameraRootNames.Length;
                     index++)
                {
                    if (string.Equals(
                            root.name,
                            HistoricalCameraRootNames[index],
                            StringComparison.Ordinal))
                    {
                        destroy.Add(root);
                        break;
                    }
                }
            }

            foreach (GameObject candidate in destroy)
            {
                if (candidate != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        candidate);
                }
            }
        }

        private static void VerifyPersistedBaseline()
        {
            Scene scene =
                EditorSceneManager.OpenScene(
                    GlobalScenePath,
                    OpenSceneMode.Single);

            List<CameraOutputAuthoring> outputs =
                FindInScene<CameraOutputAuthoring>(
                    scene);
            if (outputs.Count != 0)
            {
                throw new InvalidOperationException(
                    "Persisted IF-ADR-032 QA baseline retained scene-owned Camera authority. " +
                    $"outputs='{outputs.Count}'.");
            }

            foreach (GameObject root in
                scene.GetRootGameObjects())
            {
                if (root == null)
                {
                    continue;
                }

                for (int index = 0;
                     index < HistoricalCameraRootNames.Length;
                     index++)
                {
                    if (string.Equals(
                            root.name,
                            HistoricalCameraRootNames[index],
                            StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"Persisted IF-ADR-032 QA baseline retained historical Camera root '{root.name}'.");
                    }
                }
            }

            int missingScripts = 0;
            GameObject firstMissing = null;
            foreach (GameObject root in
                scene.GetRootGameObjects())
            {
                if (root == null)
                {
                    continue;
                }

                foreach (Transform transform in
                    root.GetComponentsInChildren<Transform>(
                        true))
                {
                    GameObject candidate =
                        transform != null
                            ? transform.gameObject
                            : null;
                    if (candidate == null)
                    {
                        continue;
                    }

                    int count =
                        GameObjectUtility
                            .GetMonoBehavioursWithMissingScriptCount(
                                candidate);
                    if (count <= 0)
                    {
                        continue;
                    }

                    missingScripts += count;
                    firstMissing ??= candidate;
                }
            }

            if (missingScripts != 0)
            {
                throw new InvalidOperationException(
                    "Persisted IF-ADR-032 QA baseline contains missing MonoBehaviour scripts after legacy Camera cleanup. " +
                    $"count='{missingScripts}' first='{(firstMissing != null ? firstMissing.name : "<unknown>")}'.");
            }

            Debug.Log(
                $"{Prefix} status='Verified' topology='IF-ADR-032' " +
                "persistentCameraAuthority='0' missingScripts='0'.");
        }

        private static void HandlePlayModeStateChanged(
            PlayModeStateChange state)
        {
            if (state !=
                    PlayModeStateChange.EnteredEditMode ||
                !SessionState.GetBool(
                    PendingRestoreKey,
                    false))
            {
                return;
            }

            EditorApplication.delayCall -=
                RestorePending;
            EditorApplication.delayCall +=
                RestorePending;
        }

        private static void RestorePending()
        {
            if (EditorApplication.isPlaying ||
                !SessionState.GetBool(
                    PendingRestoreKey,
                    false))
            {
                return;
            }

            string reason =
                SessionState.GetString(
                    PendingRestoreReasonKey,
                    "unspecified");

            SessionState.SetBool(
                PendingRestoreKey,
                false);
            SessionState.EraseString(
                PendingRestoreReasonKey);

            try
            {
                RestoreCanonicalBaseline();
                Debug.Log(
                    $"{Prefix} status='RestoredAfterRun' reason='{Escape(reason)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{Prefix} status='RestoreFailed' reason='{Escape(reason)}' " +
                    $"exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.GetBaseException().Message)}'.");
            }
        }

        private static List<T> FindInScene<T>(
            Scene scene)
            where T : Component
        {
            var results =
                new List<T>();

            foreach (GameObject root in
                scene.GetRootGameObjects())
            {
                if (root == null)
                {
                    continue;
                }

                results.AddRange(
                    root.GetComponentsInChildren<T>(
                        true));
            }

            return results;
        }

        private static string Escape(
            string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", " ")
                .Replace("\n", " ");
        }
    }
}
