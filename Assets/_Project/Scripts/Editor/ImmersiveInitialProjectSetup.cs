using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
namespace Project.Editor
{
    /// <summary>
    /// Setup inicial do projeto consumidor.
    /// Este arquivo prepara pastas e ferramentas antes da chegada do Immersive Framework.
    /// Não cria lifecycle, runtime bootstrap, GameLifecycleSettings ou estrutura interna do framework.
    /// </summary>
    [InitializeOnLoad]
    public static class ImmersiveInitialProjectSetup
    {
        private const string MenuRoot = "Tools/Initial Project Setup/";
        private const string ProjectRootFolder = "Assets/_Project";
        private const string ExternalRootFolder = "Assets/_External";
        private const string SandboxRootFolder = "Assets/_Sandbox";
        private const string DocumentationRootFolder = "Assets/_Documentation";

        private static readonly string[] InitialFolders =
        {
            ProjectRootFolder,
            ProjectRootFolder + "/Art",
            ProjectRootFolder + "/Audio",
            ProjectRootFolder + "/Audio/Music",
            ProjectRootFolder + "/Audio/SFX",
            ProjectRootFolder + "/Materials",
            ProjectRootFolder + "/Prefabs",
            ProjectRootFolder + "/Scenes",
            ProjectRootFolder + "/Scenes/Boot",
            ProjectRootFolder + "/Scenes/Menu",
            ProjectRootFolder + "/Scenes/Gameplay",
            ProjectRootFolder + "/Scenes/Sandbox",
            ProjectRootFolder + "/ScriptableObjects",
            ProjectRootFolder + "/Scripts",
            ProjectRootFolder + "/Scripts/Runtime",
            ProjectRootFolder + "/Scripts/Editor",
            ProjectRootFolder + "/Scripts/UI",
            ProjectRootFolder + "/Settings",
            ProjectRootFolder + "/UI",
            ProjectRootFolder + "/VFX",

            ExternalRootFolder,
            ExternalRootFolder + "/Plugins",
            ExternalRootFolder + "/Tools",
            ExternalRootFolder + "/LocalPackages",

            SandboxRootFolder,
            SandboxRootFolder + "/Scenes",
            SandboxRootFolder + "/Prefabs",
            SandboxRootFolder + "/Materials",
            SandboxRootFolder + "/ScriptableObjects",

            DocumentationRootFolder,
            DocumentationRootFolder + "/ADRs",
            DocumentationRootFolder + "/Notes",
            DocumentationRootFolder + "/Setup"
        };

        private static readonly PackageSpec[] UnityCorePackages =
        {
            new("unity.inputsystem", "Unity Input System", "com.unity.inputsystem", "com.unity.inputsystem"),
            new("unity.addressables", "Unity Addressables", "com.unity.addressables", "com.unity.addressables"),
            new("unity.cinemachine", "Unity Cinemachine", "com.unity.cinemachine", "com.unity.cinemachine"),
            new("unity.ai.navigation", "Unity AI Navigation", "com.unity.ai.navigation", "com.unity.ai.navigation")
        };

        private static readonly PackageSpec[] InitialGitTools =
        {
            new("git.unityutils", "Unity Utility Library", "https://github.com/adammyhre/Unity-Utils.git", "com.gitamend.unityutils"),
            new("git.improvedtimers", "Improved Timers", "https://github.com/adammyhre/Unity-Improved-Timers.git", "com.gitamend.improvedtimers")
        };

        private static readonly PackageSpec[] ImmersiveCorePackages =
        {
            new("immersive.foundation", "Immersive Foundation", "https://github.com/ImmersiveGames/com.immersive.foundation.git#v0.1.0", "com.immersive.foundation"),
            new("immersive.logging", "Immersive Logging", "https://github.com/ImmersiveGames/com.immersive.logging.git#v0.1.0", "com.immersive.logging"),
            new("immersive.pooling", "Immersive Pooling", "https://github.com/ImmersiveGames/com.immersive.pooling.git#v0.1.0", "com.immersive.pooling")
        };

        private static readonly PackageSpec[] OptionalUnityPackages =
        {
            new("unity.localization", "Unity Localization", "com.unity.localization", "com.unity.localization"),
            new("unity.test-framework", "Unity Test Framework", "com.unity.test-framework", "com.unity.test-framework"),
            new("unity.netcode.gameobjects", "Netcode for GameObjects", "com.unity.netcode.gameobjects", "com.unity.netcode.gameobjects"),
            new("unity.multiplayer.tools", "Unity Multiplayer Tools", "com.unity.multiplayer.tools", "com.unity.multiplayer.tools")
        };

        static ImmersiveInitialProjectSetup()
        {
            EditorApplication.delayCall += PackageInstaller.ResumeQueuedInstallIfNeeded;
        }

        [MenuItem(MenuRoot + "Run Safe Setup")]
        public static void RunSafeSetup()
        {
            CreateFolderStructure();
            CreateProjectAssemblyDefinitions();
            PackageInstaller.Enqueue(UnityCorePackages.Concat(InitialGitTools));
        }

        [MenuItem(MenuRoot + "Create Folder Structure")]
        public static void CreateFolderStructure()
        {
            foreach (string folder in InitialFolders)
            {
                ProjectFolders.EnsureFolder(folder);
            }

            AssetDatabase.Refresh();
            Debug.Log("[Initial Project Setup] Folder structure is ready.");
        }

        [MenuItem(MenuRoot + "Create Project Assembly Definitions")]
        public static void CreateProjectAssemblyDefinitions()
        {
            ProjectFolders.EnsureFolder(ProjectRootFolder + "/Scripts/Runtime");
            ProjectFolders.EnsureFolder(ProjectRootFolder + "/Scripts/Editor");

            AssemblyDefinitions.CreateIfMissing(
                ProjectRootFolder + "/Scripts/Runtime/Project.Runtime.asmdef",
                "Project.Runtime",
                "Project",
                Array.Empty<string>(),
                Array.Empty<string>());

            AssemblyDefinitions.CreateIfMissing(
                ProjectRootFolder + "/Scripts/Editor/Project.Editor.asmdef",
                "Project.Editor",
                "Project.Editor",
                new[] { "Project.Runtime" },
                new[] { "Editor" });

            AssetDatabase.Refresh();
            Debug.Log("[Initial Project Setup] Project assembly definitions are ready.");
        }

        [MenuItem(MenuRoot + "Packages/Install Unity Core Packages")]
        public static void InstallUnityCorePackages()
        {
            PackageInstaller.Enqueue(UnityCorePackages);
        }

        [MenuItem(MenuRoot + "Packages/Install Initial Git Tools")]
        public static void InstallInitialGitTools()
        {
            PackageInstaller.Enqueue(InitialGitTools);
        }

        [MenuItem(MenuRoot + "Packages/Install Immersive Core Packages")]
        public static void InstallImmersiveCorePackages()
        {
            PackageInstaller.Enqueue(ImmersiveCorePackages);
        }

        [MenuItem(MenuRoot + "Packages/Install Optional Unity Packages")]
        public static void InstallOptionalUnityPackages()
        {
            PackageInstaller.Enqueue(OptionalUnityPackages);
        }

        [MenuItem(MenuRoot + "Packages/Clear Package Install Queue")]
        public static void ClearPackageInstallQueue()
        {
            PackageInstaller.ClearQueue();
        }

        [MenuItem(MenuRoot + "Local Packages/Import DOTween Unitypackage")]
        public static void ImportDotweenPackage()
        {
            LocalUnityPackages.ImportUnityPackageWithPicker("DOTween");
        }

        [MenuItem(MenuRoot + "Local Packages/Import Better Hierarchy Unitypackage")]
        public static void ImportBetterHierarchyPackage()
        {
            LocalUnityPackages.ImportUnityPackageWithPicker("Better Hierarchy");
        }

        [MenuItem(MenuRoot + "Local Packages/Import Any Unitypackage")]
        public static void ImportAnyUnityPackage()
        {
            LocalUnityPackages.ImportUnityPackageWithPicker("Unity Package");
        }

        [MenuItem(MenuRoot + "Local Packages/Open Local Packages Folder")]
        public static void OpenLocalPackagesFolder()
        {
            ProjectFolders.EnsureFolder(ExternalRootFolder + "/LocalPackages");
            EditorUtility.RevealInFinder(ProjectFolders.ToFullPath(ExternalRootFolder + "/LocalPackages"));
        }

        [MenuItem(MenuRoot + "Validate Setup")]
        public static void ValidateSetup()
        {
            SetupValidator.ValidateFolders(InitialFolders);
            PackageStatusReporter.ReportKnownPackages(UnityCorePackages.Concat(InitialGitTools));
        }

        [MenuItem(MenuRoot + "Validate Immersive Packages")]
        public static void ValidateImmersivePackages()
        {
            PackageStatusReporter.ReportKnownPackages(ImmersiveCorePackages);
        }

        private readonly struct PackageSpec
        {
            public string Key { get; }
            public string DisplayName { get; }
            public string Request { get; }
            public string PackageName { get; }

            public PackageSpec(string key, string displayName, string request, string packageName)
            {
                Key = key;
                DisplayName = displayName;
                Request = request;
                PackageName = packageName;
            }
        }

        private static class ProjectFolders
        {
            public static void EnsureFolder(string assetFolderPath)
            {
                if (string.IsNullOrWhiteSpace(assetFolderPath))
                {
                    Debug.LogError("[Initial Project Setup] Folder path is empty.");
                    return;
                }

                string normalizedPath = NormalizeAssetPath(assetFolderPath);
                if (!normalizedPath.StartsWith("Assets", StringComparison.Ordinal))
                {
                    Debug.LogError($"[Initial Project Setup] Folder must be inside Assets: {assetFolderPath}");
                    return;
                }

                string[] parts = normalizedPath.Split('/');
                string current = parts[0];

                for (int i = 1; i < parts.Length; i++)
                {
                    string next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                    {
                        AssetDatabase.CreateFolder(current, parts[i]);
                    }

                    current = next;
                }
            }

            public static string ToFullPath(string assetPath)
            {
                string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
                if (string.IsNullOrWhiteSpace(projectRoot))
                {
                    throw new InvalidOperationException("Unable to resolve Unity project root.");
                }

                string normalized = NormalizeAssetPath(assetPath).Replace('/', Path.DirectorySeparatorChar);
                return Path.Combine(projectRoot, normalized);
            }

            private static string NormalizeAssetPath(string path)
            {
                return path.Replace('\\', '/').Trim('/');
            }
        }

        private static class AssemblyDefinitions
        {
            public static void CreateIfMissing(
                string assetPath,
                string assemblyName,
                string rootNamespace,
                IReadOnlyList<string> references,
                IReadOnlyList<string> includePlatforms)
            {
                if (File.Exists(ProjectFolders.ToFullPath(assetPath)))
                {
                    return;
                }

                string folder = assetPath[..assetPath.LastIndexOf('/')];
                ProjectFolders.EnsureFolder(folder);

                string json = BuildAsmdefJson(assemblyName, rootNamespace, references, includePlatforms);
                File.WriteAllText(ProjectFolders.ToFullPath(assetPath), json);
            }

            private static string BuildAsmdefJson(
                string assemblyName,
                string rootNamespace,
                IReadOnlyList<string> references,
                IReadOnlyList<string> includePlatforms)
            {
                string referenceJson = ToJsonStringArray(references);
                string platformJson = ToJsonStringArray(includePlatforms);

                return "{\n" +
                       $"    \"name\": \"{assemblyName}\",\n" +
                       $"    \"rootNamespace\": \"{rootNamespace}\",\n" +
                       $"    \"references\": {referenceJson},\n" +
                       $"    \"includePlatforms\": {platformJson},\n" +
                       "    \"excludePlatforms\": [],\n" +
                       "    \"allowUnsafeCode\": false,\n" +
                       "    \"overrideReferences\": false,\n" +
                       "    \"precompiledReferences\": [],\n" +
                       "    \"autoReferenced\": true,\n" +
                       "    \"defineConstraints\": [],\n" +
                       "    \"versionDefines\": [],\n" +
                       "    \"noEngineReferences\": false\n" +
                       "}\n";
            }

            private static string ToJsonStringArray(IReadOnlyList<string> values)
            {
                if (values == null || values.Count == 0)
                {
                    return "[]";
                }

                return "[" + string.Join(", ", values.Select(value => $"\"{value}\"")) + "]";
            }
        }

        private static class LocalUnityPackages
        {
            public static void ImportUnityPackageWithPicker(string displayName)
            {
                ProjectFolders.EnsureFolder(ExternalRootFolder + "/LocalPackages");

                string defaultFolder = ProjectFolders.ToFullPath(ExternalRootFolder + "/LocalPackages");
                string packagePath = EditorUtility.OpenFilePanel($"Select {displayName} .unitypackage", defaultFolder, "unitypackage");
                if (string.IsNullOrWhiteSpace(packagePath))
                {
                    Debug.Log($"[Initial Project Setup] Import cancelled: {displayName}.");
                    return;
                }

                if (!packagePath.EndsWith(".unitypackage", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogError($"[Initial Project Setup] Selected file is not a .unitypackage: {packagePath}");
                    return;
                }

                AssetDatabase.ImportPackage(packagePath, false);
                Debug.Log($"[Initial Project Setup] Import requested: {displayName} ({Path.GetFileName(packagePath)}).");
            }
        }

        private static class PackageInstaller
        {
            private const string QueueSessionKey = "ImmersiveInitialProjectSetup.PackageQueue";
            private const string ActiveSessionKey = "ImmersiveInitialProjectSetup.ActivePackage";

            private static readonly Dictionary<string, PackageSpec> SpecsByKey =
                UnityCorePackages
                    .Concat(InitialGitTools)
                    .Concat(ImmersiveCorePackages)
                    .Concat(OptionalUnityPackages)
                    .ToDictionary(spec => spec.Key, spec => spec);

            private static ListRequest _listRequest;
            private static AddRequest _addRequest;
            private static bool _isInstalling;

            public static void Enqueue(IEnumerable<PackageSpec> packageSpecs)
            {
                List<string> queue = LoadQueue();
                int addedCount = 0;

                foreach (PackageSpec spec in packageSpecs)
                {
                    if (queue.Contains(spec.Key))
                    {
                        continue;
                    }

                    queue.Add(spec.Key);
                    addedCount++;
                }

                SaveQueue(queue);
                Debug.Log($"[Initial Project Setup] Package install queue updated. Added={addedCount}, Pending={queue.Count}.");
                ResumeQueuedInstallIfNeeded();
            }

            public static void ClearQueue()
            {
                SessionState.EraseString(QueueSessionKey);
                SessionState.EraseString(ActiveSessionKey);
                _isInstalling = false;
                Debug.Log("[Initial Project Setup] Package install queue cleared.");
            }

            public static void ResumeQueuedInstallIfNeeded()
            {
                if (_isInstalling)
                {
                    return;
                }

                List<string> queue = LoadQueue();
                if (queue.Count == 0)
                {
                    return;
                }

                _isInstalling = true;
                _listRequest = Client.List(false, true);
                EditorApplication.update += ContinueAfterPackageList;
            }

            private static void ContinueAfterPackageList()
            {
                if (!_listRequest.IsCompleted)
                {
                    return;
                }

                EditorApplication.update -= ContinueAfterPackageList;

                if (_listRequest.Status >= StatusCode.Failure)
                {
                    _isInstalling = false;
                    Debug.LogError($"[Initial Project Setup] Failed to list installed packages: {_listRequest.Error.message}");
                    return;
                }

                HashSet<string> installedPackageNames = new(_listRequest.Result.Select(package => package.name));
                List<string> queue = LoadQueue();

                while (queue.Count > 0)
                {
                    string key = queue[0];
                    if (!SpecsByKey.TryGetValue(key, out PackageSpec spec))
                    {
                        Debug.LogWarning($"[Initial Project Setup] Unknown package key removed from queue: {key}");
                        queue.RemoveAt(0);
                        SaveQueue(queue);
                        continue;
                    }

                    if (installedPackageNames.Contains(spec.PackageName))
                    {
                        Debug.Log($"[Initial Project Setup] Already installed: {spec.DisplayName} ({spec.PackageName}).");
                        queue.RemoveAt(0);
                        SaveQueue(queue);
                        continue;
                    }

                    StartPackageInstall(spec);
                    return;
                }

                SaveQueue(queue);
                SessionState.EraseString(ActiveSessionKey);
                _isInstalling = false;
                Debug.Log("[Initial Project Setup] Package install queue completed.");
            }

            private static void StartPackageInstall(PackageSpec spec)
            {
                SessionState.SetString(ActiveSessionKey, spec.Key);
                Debug.Log($"[Initial Project Setup] Installing package: {spec.DisplayName} ({spec.Request}).");
                _addRequest = Client.Add(spec.Request);
                EditorApplication.update += ContinueAfterPackageAdd;
            }

            private static void ContinueAfterPackageAdd()
            {
                if (!_addRequest.IsCompleted)
                {
                    return;
                }

                EditorApplication.update -= ContinueAfterPackageAdd;

                string activeKey = SessionState.GetString(ActiveSessionKey, string.Empty);
                List<string> queue = LoadQueue();
                RemoveActiveFromQueue(activeKey, queue);
                SaveQueue(queue);
                SessionState.EraseString(ActiveSessionKey);

                if (_addRequest.Status == StatusCode.Success)
                {
                    Debug.Log($"[Initial Project Setup] Installed: {_addRequest.Result.packageId}.");
                }
                else if (_addRequest.Status >= StatusCode.Failure)
                {
                    Debug.LogError($"[Initial Project Setup] Package install failed: {_addRequest.Error.message}");
                }

                _isInstalling = false;
                ResumeQueuedInstallIfNeeded();
            }

            private static void RemoveActiveFromQueue(string activeKey, List<string> queue)
            {
                if (string.IsNullOrWhiteSpace(activeKey))
                {
                    if (queue.Count > 0)
                    {
                        queue.RemoveAt(0);
                    }

                    return;
                }

                int index = queue.IndexOf(activeKey);
                if (index >= 0)
                {
                    queue.RemoveAt(index);
                }
            }

            private static List<string> LoadQueue()
            {
                string raw = SessionState.GetString(QueueSessionKey, string.Empty);
                return raw
                    .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(value => value.Trim())
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct()
                    .ToList();
            }

            private static void SaveQueue(IReadOnlyList<string> queue)
            {
                if (queue == null || queue.Count == 0)
                {
                    SessionState.EraseString(QueueSessionKey);
                    return;
                }

                SessionState.SetString(QueueSessionKey, string.Join("\n", queue));
            }
        }

        private static class SetupValidator
        {
            public static void ValidateFolders(IEnumerable<string> folders)
            {
                List<string> missingFolders = folders
                    .Where(folder => !AssetDatabase.IsValidFolder(folder))
                    .ToList();

                if (missingFolders.Count == 0)
                {
                    Debug.Log("[Initial Project Setup] Folder validation passed.");
                    return;
                }

                Debug.LogWarning("[Initial Project Setup] Missing folders:\n" + string.Join("\n", missingFolders));
            }
        }

        private static class PackageStatusReporter
        {
            private static IReadOnlyList<PackageSpec> _knownPackages;
            private static ListRequest _request;

            public static void ReportKnownPackages(IEnumerable<PackageSpec> knownPackages)
            {
                _knownPackages = knownPackages.ToArray();
                _request = Client.List(false, true);
                EditorApplication.update += ContinueAfterPackageList;
            }

            private static void ContinueAfterPackageList()
            {
                if (!_request.IsCompleted)
                {
                    return;
                }

                EditorApplication.update -= ContinueAfterPackageList;

                if (_request.Status >= StatusCode.Failure)
                {
                    Debug.LogError($"[Initial Project Setup] Package validation failed: {_request.Error.message}");
                    return;
                }

                HashSet<string> installedPackageNames = new(_request.Result.Select(package => package.name));
                List<string> missingPackages = _knownPackages
                    .Where(spec => !installedPackageNames.Contains(spec.PackageName))
                    .Select(spec => $"{spec.DisplayName} ({spec.PackageName})")
                    .ToList();

                if (missingPackages.Count == 0)
                {
                    Debug.Log("[Initial Project Setup] Package validation passed for known initial packages.");
                    return;
                }

                Debug.LogWarning("[Initial Project Setup] Missing initial packages:\n" + string.Join("\n", missingPackages));
            }
        }
    }
}
