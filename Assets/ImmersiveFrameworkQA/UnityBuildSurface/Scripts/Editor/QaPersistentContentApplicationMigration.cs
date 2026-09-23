using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Immersive.Framework.Authoring;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.UnityBuildSurface.Editor
{
    internal static class QaPersistentContentApplicationMigration
    {
        private const string CanonicalScenePath =
            "Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scenes/QA_UIGlobal.unity";

        private const string MigrateMenuPath =
            "Immersive Framework/QA/Setup/Persistent Content/Migrate Positive Game Applications";

        private const string ValidateMenuPath =
            "Immersive Framework/QA/Setup/Persistent Content/Validate Game Applications";

        private const string LogPrefix =
            "[QA_PERSISTENT_CONTENT_MIGRATION]";

        private const string MigrationScriptPath =
            "Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scripts/Editor/QaPersistentContentApplicationMigration.cs";

        private static readonly string[] NegativeMarkers =
        {
            "/Negative/",
            "/Negatives/",
            "/Invalid/",
            "/Missing/",
            "Negative",
            "MissingPersistent",
            "Missing Persistent",
            "InvalidPersistent",
            "Invalid Persistent",
            "NoPersistent",
            "No Persistent",
            "WithoutPersistent",
            "Without Persistent",
            "IncompletePersistent",
            "Incomplete Persistent",
            "MissingContent",
            "Missing Content",
            "NoContent",
            "No Content"
        };

        private static readonly string[] TemporaryMarkers =
        {
            "/Temp/",
            "/Temporary/",
            "/__Temp/",
            "/__Temporary/",
            "/Library/",
            "/Packages/"
        };

        private static readonly string[] LegacySerializedTokens =
        {
            "globalUiScenePolicy:",
            "globalUiScenePath:",
            "globalUiSceneName:"
        };

        private static readonly string[] LegacyCodeTokens =
        {
            "\"globalUiScenePolicy\"",
            "\"globalUiScenePath\"",
            "\"globalUiSceneName\"",
            "GlobalUiScenePolicy"
        };

        [MenuItem(MigrateMenuPath, priority = 300)]
        private static void MigrateMenu()
        {
            if (!TryLoadCanonicalScene(
                    out SceneAsset canonicalScene,
                    out string sceneIssue))
            {
                Debug.LogError(
                    $"{LogPrefix} FAIL. {sceneIssue}");
                return;
            }

            EnsureSceneInBuildSettings(CanonicalScenePath);

            string[] applicationPaths =
                FindGameApplicationPaths();
            var migrated = new List<string>();
            var current = new List<string>();
            var custom = new List<string>();
            var negatives = new List<string>();
            var unresolved = new List<string>();

            AssetDatabase.StartAssetEditing();
            try
            {
                for (int index = 0;
                     index < applicationPaths.Length;
                     index++)
                {
                    string path = applicationPaths[index];
                    GameApplicationAsset application =
                        AssetDatabase.LoadAssetAtPath<
                            GameApplicationAsset>(path);
                    if (application == null)
                    {
                        unresolved.Add(
                            $"{path}: could not load GameApplicationAsset.");
                        continue;
                    }

                    if (IsTemporaryPath(path))
                    {
                        continue;
                    }

                    if (IsIntentionalNegative(
                            path,
                            application))
                    {
                        // Preserve the negative contract, but mark it dirty so an
                        // explicit reserialize removes obsolete serialized fields.
                        EditorUtility.SetDirty(application);
                        negatives.Add(path);
                        continue;
                    }

                    UnityEngine.Object existingScene =
                        application.PersistentContent != null
                            ? application.PersistentContent.ContainerScene
                            : null;

                    if (existingScene != null)
                    {
                        string existingPath =
                            AssetDatabase.GetAssetPath(
                                existingScene);
                        if (string.Equals(
                                existingPath,
                                CanonicalScenePath,
                                StringComparison.Ordinal))
                        {
                            current.Add(path);
                        }
                        else
                        {
                            custom.Add(
                                $"{path}: {existingPath}");
                        }

                        continue;
                    }

                    if (!TryConfigureApplication(
                            application,
                            canonicalScene,
                            out string configureIssue))
                    {
                        unresolved.Add(
                            $"{path}: {configureIssue}");
                        continue;
                    }

                    migrated.Add(path);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            // Reserialize only GameApplication assets. This removes the obsolete
            // globalUiScenePolicy/path/name fields while preserving negative fixtures.
            AssetDatabase.ForceReserializeAssets(
                applicationPaths.ToList());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            QaPersistentContentValidationReport report =
                ValidateAllInternal();

            string message =
                $"{LogPrefix} migration completed. " +
                $"applications='{applicationPaths.Length}' " +
                $"migrated='{migrated.Count}' " +
                $"alreadyCurrent='{current.Count}' " +
                $"customCurrent='{custom.Count}' " +
                $"preservedNegative='{negatives.Count}' " +
                $"unresolved='{unresolved.Count}' " +
                $"validationErrors='{report.ErrorCount}' " +
                $"validationWarnings='{report.WarningCount}'.";

            if (unresolved.Count > 0 ||
                report.ErrorCount > 0)
            {
                Debug.LogError(
                    message +
                    BuildDetails(
                        "unresolved",
                        unresolved) +
                    report.ToDetailText());
                return;
            }

            if (custom.Count > 0 ||
                report.WarningCount > 0)
            {
                Debug.LogWarning(
                    message +
                    BuildDetails(
                        "custom",
                        custom) +
                    report.ToDetailText());
                return;
            }

            Debug.Log(message);
        }

        [MenuItem(ValidateMenuPath, priority = 301)]
        private static void ValidateMenu()
        {
            QaPersistentContentValidationReport report =
                ValidateAllInternal();

            string message =
                $"{LogPrefix} validation completed. " +
                $"applications='{report.ApplicationCount}' " +
                $"positive='{report.PositiveCount}' " +
                $"negative='{report.NegativeCount}' " +
                $"errors='{report.ErrorCount}' " +
                $"warnings='{report.WarningCount}'.";

            if (report.ErrorCount > 0)
            {
                Debug.LogError(
                    message +
                    report.ToDetailText());
                return;
            }

            if (report.WarningCount > 0)
            {
                Debug.LogWarning(
                    message +
                    report.ToDetailText());
                return;
            }

            Debug.Log(
                $"{message} status='PASS'.");
        }

        internal static bool TryConfigureApplication(
            GameApplicationAsset application,
            SceneAsset persistentContentScene,
            out string issue)
        {
            if (application == null)
            {
                issue =
                    "Game Application is missing.";
                return false;
            }

            if (persistentContentScene == null)
            {
                issue =
                    "Persistent Content Scene is missing.";
                return false;
            }

            var serialized =
                new SerializedObject(application);
            SerializedProperty persistentContent =
                serialized.FindProperty(
                    "persistentContent");
            if (persistentContent == null)
            {
                issue =
                    "GameApplicationAsset does not expose the expected 'persistentContent' property.";
                return false;
            }

            SerializedProperty containerScene =
                persistentContent.FindPropertyRelative(
                    "containerScene");
            if (containerScene == null)
            {
                issue =
                    "PersistentContentComposition does not expose the expected 'containerScene' property.";
                return false;
            }

            if (containerScene.objectReferenceValue != null &&
                !ReferenceEquals(
                    containerScene.objectReferenceValue,
                    persistentContentScene))
            {
                issue =
                    $"Game Application already references a different Persistent Content Scene: " +
                    $"'{AssetDatabase.GetAssetPath(containerScene.objectReferenceValue)}'.";
                return false;
            }

            containerScene.objectReferenceValue =
                persistentContentScene;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(application);

            issue = string.Empty;
            return true;
        }

        internal static void EnsureSceneInBuildSettings(
            string scenePath)
        {
            SceneAsset scene =
                AssetDatabase.LoadAssetAtPath<
                    SceneAsset>(scenePath);
            if (scene == null)
            {
                return;
            }

            var scenes =
                new List<EditorBuildSettingsScene>(
                    EditorBuildSettings.scenes);
            int firstIndex = -1;

            for (int index = scenes.Count - 1;
                 index >= 0;
                 index--)
            {
                EditorBuildSettingsScene entry =
                    scenes[index];
                if (entry == null ||
                    !string.Equals(
                        NormalizePath(entry.path),
                        NormalizePath(scenePath),
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (firstIndex < 0)
                {
                    firstIndex = index;
                    scenes[index] =
                        new EditorBuildSettingsScene(
                            scenePath,
                            true);
                }
                else
                {
                    scenes.RemoveAt(index);
                    firstIndex--;
                }
            }

            if (firstIndex < 0)
            {
                scenes.Add(
                    new EditorBuildSettingsScene(
                        scenePath,
                        true));
            }

            EditorBuildSettings.scenes =
                scenes.ToArray();
        }

        private static QaPersistentContentValidationReport
            ValidateAllInternal()
        {
            string[] applicationPaths =
                FindGameApplicationPaths();
            var errors = new List<string>();
            var warnings = new List<string>();
            int positiveCount = 0;
            int negativeCount = 0;

            bool canonicalSceneAvailable =
                TryLoadCanonicalScene(
                    out SceneAsset canonicalScene,
                    out string canonicalIssue);
            if (!canonicalSceneAvailable)
            {
                errors.Add(canonicalIssue);
            }
            else if (!IsSceneEnabledInBuildSettings(
                         CanonicalScenePath))
            {
                errors.Add(
                    $"Canonical Persistent Content Scene is not enabled in Build Settings: '{CanonicalScenePath}'.");
            }

            for (int index = 0;
                 index < applicationPaths.Length;
                 index++)
            {
                string path = applicationPaths[index];
                if (IsTemporaryPath(path))
                {
                    continue;
                }

                GameApplicationAsset application =
                    AssetDatabase.LoadAssetAtPath<
                        GameApplicationAsset>(path);
                if (application == null)
                {
                    errors.Add(
                        $"{path}: could not load GameApplicationAsset.");
                    continue;
                }

                bool negative =
                    IsIntentionalNegative(
                        path,
                        application);
                if (negative)
                {
                    negativeCount++;
                }
                else
                {
                    positiveCount++;
                }

                string rawText =
                    TryReadText(path);
                for (int tokenIndex = 0;
                     tokenIndex < LegacySerializedTokens.Length;
                     tokenIndex++)
                {
                    string token =
                        LegacySerializedTokens[tokenIndex];
                    if (rawText.IndexOf(
                            token,
                            StringComparison.Ordinal) >= 0)
                    {
                        errors.Add(
                            $"{path}: obsolete serialized field remains: '{token.TrimEnd(':')}'.");
                    }
                }

                UnityEngine.Object scene =
                    application.PersistentContent != null
                        ? application.PersistentContent.ContainerScene
                        : null;

                if (negative)
                {
                    if (scene != null)
                    {
                        warnings.Add(
                            $"{path}: negative fixture now has a Persistent Content Scene and may no longer prove its intended failure.");
                    }

                    continue;
                }

                if (scene == null)
                {
                    errors.Add(
                        $"{path}: positive Game Application has no Persistent Content Content Scene.");
                    continue;
                }

                string scenePath =
                    AssetDatabase.GetAssetPath(scene);
                if (string.IsNullOrWhiteSpace(scenePath))
                {
                    errors.Add(
                        $"{path}: Persistent Content Scene reference has no asset path.");
                    continue;
                }

                if (!IsSceneEnabledInBuildSettings(
                        scenePath))
                {
                    errors.Add(
                        $"{path}: Persistent Content Scene is not enabled in Build Settings: '{scenePath}'.");
                }

                if (canonicalSceneAvailable &&
                    ReferenceEquals(
                        scene,
                        canonicalScene))
                {
                    continue;
                }

                warnings.Add(
                    $"{path}: uses a custom Persistent Content Scene '{scenePath}'. It was preserved and requires its own topology validation.");
            }

            ValidateLegacyCodeTokens(errors);

            return new QaPersistentContentValidationReport(
                applicationPaths.Length,
                positiveCount,
                negativeCount,
                errors,
                warnings);
        }

        private static void ValidateLegacyCodeTokens(
            ICollection<string> errors)
        {
            string[] scriptGuids =
                AssetDatabase.FindAssets(
                    "t:MonoScript",
                    new[]
                    {
                        "Assets/ImmersiveFrameworkQA",
                        "Assets/_Project"
                    });

            for (int guidIndex = 0;
                 guidIndex < scriptGuids.Length;
                 guidIndex++)
            {
                string path =
                    AssetDatabase.GUIDToAssetPath(
                        scriptGuids[guidIndex]);
                if (string.IsNullOrWhiteSpace(path) ||
                    !File.Exists(path))
                {
                    continue;
                }

                if (string.Equals(
                        NormalizePath(path),
                        NormalizePath(MigrationScriptPath),
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string text =
                    TryReadText(path);
                for (int tokenIndex = 0;
                     tokenIndex < LegacyCodeTokens.Length;
                     tokenIndex++)
                {
                    string token =
                        LegacyCodeTokens[tokenIndex];
                    if (text.IndexOf(
                            token,
                            StringComparison.Ordinal) < 0)
                    {
                        continue;
                    }

                    errors.Add(
                        $"{path}: obsolete Persistent Content authoring token remains: '{token}'.");
                }
            }
        }

        private static string[] FindGameApplicationPaths()
        {
            return AssetDatabase.FindAssets(
                    "t:GameApplicationAsset",
                    new[]
                    {
                        "Assets"
                    })
                .Select(
                    AssetDatabase.GUIDToAssetPath)
                .Where(
                    path =>
                        !string.IsNullOrWhiteSpace(path))
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .OrderBy(
                    path => path,
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static bool IsIntentionalNegative(
            string path,
            GameApplicationAsset application)
        {
            string applicationName =
                application != null
                    ? application.ApplicationName
                    : string.Empty;
            string assetName =
                application != null
                    ? application.name
                    : string.Empty;
            string combined =
                $"{NormalizePath(path)}|{applicationName}|{assetName}";

            for (int index = 0;
                 index < NegativeMarkers.Length;
                 index++)
            {
                if (combined.IndexOf(
                        NegativeMarkers[index],
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsTemporaryPath(
            string path)
        {
            string normalized =
                NormalizePath(path);
            for (int index = 0;
                 index < TemporaryMarkers.Length;
                 index++)
            {
                if (normalized.IndexOf(
                        TemporaryMarkers[index],
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryLoadCanonicalScene(
            out SceneAsset scene,
            out string issue)
        {
            scene =
                AssetDatabase.LoadAssetAtPath<
                    SceneAsset>(CanonicalScenePath);
            if (scene == null)
            {
                issue =
                    $"Canonical Persistent Content Scene is missing: '{CanonicalScenePath}'.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private static bool IsSceneEnabledInBuildSettings(
            string scenePath)
        {
            string normalized =
                NormalizePath(scenePath);
            int count = 0;

            EditorBuildSettingsScene[] scenes =
                EditorBuildSettings.scenes;
            for (int index = 0;
                 index < scenes.Length;
                 index++)
            {
                EditorBuildSettingsScene entry =
                    scenes[index];
                if (entry != null &&
                    entry.enabled &&
                    string.Equals(
                        NormalizePath(entry.path),
                        normalized,
                        StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                }
            }

            return count == 1;
        }

        private static string TryReadText(
            string path)
        {
            try
            {
                return File.Exists(path)
                    ? File.ReadAllText(path)
                    : string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private static string BuildDetails(
            string label,
            IReadOnlyList<string> values)
        {
            if (values == null ||
                values.Count == 0)
            {
                return string.Empty;
            }

            return
                Environment.NewLine +
                $"{label}:" +
                Environment.NewLine +
                string.Join(
                    Environment.NewLine,
                    values.Select(
                        value => $"  - {value}"));
        }

        private static string NormalizePath(
            string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? string.Empty
                : path.Replace(
                    Path.DirectorySeparatorChar,
                    '/');
        }
    }

    internal sealed class QaPersistentContentValidationReport
    {
        private readonly IReadOnlyList<string> _errors;
        private readonly IReadOnlyList<string> _warnings;

        internal QaPersistentContentValidationReport(
            int applicationCount,
            int positiveCount,
            int negativeCount,
            IReadOnlyList<string> errors,
            IReadOnlyList<string> warnings)
        {
            ApplicationCount = applicationCount;
            PositiveCount = positiveCount;
            NegativeCount = negativeCount;
            _errors =
                errors ??
                Array.Empty<string>();
            _warnings =
                warnings ??
                Array.Empty<string>();
        }

        internal int ApplicationCount { get; }

        internal int PositiveCount { get; }

        internal int NegativeCount { get; }

        internal int ErrorCount =>
            _errors.Count;

        internal int WarningCount =>
            _warnings.Count;

        internal string ToDetailText()
        {
            string errors =
                BuildSection(
                    "errors",
                    _errors);
            string warnings =
                BuildSection(
                    "warnings",
                    _warnings);
            return errors + warnings;
        }

        private static string BuildSection(
            string label,
            IReadOnlyList<string> values)
        {
            if (values == null ||
                values.Count == 0)
            {
                return string.Empty;
            }

            return
                Environment.NewLine +
                $"{label}:" +
                Environment.NewLine +
                string.Join(
                    Environment.NewLine,
                    values.Select(
                        value => $"  - {value}"));
        }
    }
}
