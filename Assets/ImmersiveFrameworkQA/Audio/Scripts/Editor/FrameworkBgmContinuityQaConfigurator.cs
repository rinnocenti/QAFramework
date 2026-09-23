using Immersive.Audio.Authoring;
using Immersive.Audio.Unity.Hosts;
using Immersive.Framework.Audio;
using ImmersiveFrameworkQA.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Audio.Editor
{
    /// <summary>
    /// BGM-CONTINUITY-1 topology pass applied after the base Framework BGM fixture builder.
    ///
    /// The Framework owns execution. QA_UIGlobal is the GameApplication Persistent Content scene,
    /// therefore the BGM playback/intent authority belongs there. QA_Audio and QA_AudioRouteB are
    /// transient Route scenes and contain only Route/Activity intent bindings and QA surfaces.
    ///
    /// The persistent AudioRuntimeHost is promoted from the freshly configured canonical QA Route
    /// fixture instead of being independently re-created. This preserves the exact AudioDefaults
    /// and playback-root configuration already validated by FrameworkBgmQaSceneBuilder.
    /// </summary>
    internal static class FrameworkBgmContinuityQaConfigurator
    {
        private const string GlobalScenePath =
            "Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scenes/QA_UIGlobal.unity";

        private const string CanonicalScenePath =
            "Assets/ImmersiveFrameworkQA/Audio/Scenes/QA_Audio.unity";

        private const string AlternateScenePath =
            "Assets/ImmersiveFrameworkQA/Audio/Scenes/QA_AudioRouteB.unity";

        private const string AlternateRouteAssetPath =
            "Assets/ImmersiveFrameworkQA/Audio/Routes/QA_FrameworkBgmRouteB.asset";

        private const string RetainActivityAssetPath =
            "Assets/ImmersiveFrameworkQA/Audio/Activities/QA_FrameworkBgmRetainPreviousActivity.asset";

        private const string DefaultsPath =
            "Assets/ImmersiveFrameworkQA/Audio/ScriptableObjects/QA_AudioDefaults.asset";

        private const string SessionAuthorityName =
            "QA_FrameworkBgm_SessionAuthority";

        private const string LegacyTransientAuthorityName =
            "QA_FrameworkBgmAuthority_Persistent";

        private const string HostName =
            "QA_FrameworkBgm_AudioRuntimeHost";

        private const string CanonicalRouteRootName =
            "QA_FrameworkBgmRoot_Canonical";

        private const string AlternateRouteRootName =
            "QA_FrameworkBgmRoot_Alternate";

        private const string CanonicalPanelName =
            "QA_FrameworkBgmPanel_Canonical";

        private const string AlternatePanelName =
            "QA_FrameworkBgmPanel_Alternate";

        internal static bool Configure()
        {
            string expectedDefaultsGuid =
                AssetDatabase.AssetPathToGUID(DefaultsPath);

            if (string.IsNullOrWhiteSpace(expectedDefaultsGuid))
            {
                Debug.LogError(
                    $"[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] Audio defaults are missing. asset='{DefaultsPath}'. Run the base Audio QA builder first.");
                return false;
            }

            bool canonicalAndSessionValid =
                ConfigureCanonicalRouteAndPromoteSessionAuthority(expectedDefaultsGuid);

            bool persistedSessionValid =
                canonicalAndSessionValid &&
                ValidatePersistedSessionAuthority(expectedDefaultsGuid);

            bool alternateRouteStartupValid =
                ConfigureAlternateRouteStartupActivity();

            bool alternateValid =
                alternateRouteStartupValid &&
                ConfigureAlternateRouteScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            bool valid =
                canonicalAndSessionValid &&
                persistedSessionValid &&
                alternateValid;

            Debug.Log(
                $"[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] status='{(valid ? "Applied" : "Failed")}' " +
                "sessionAuthorityScene='QA_UIGlobal' " +
                "sessionHostSource='QA_AudioValidatedHost' " +
                $"sessionDefaults='{(persistedSessionValid ? "Resolved" : "Invalid")}' " +
                "entry='GameApplication->QA_Hub->RouteRequest' " +
                "canonicalRoute='QA_Audio' transientRoute='QA_AudioRouteB' " +
                "routeBStartupActivity='RetainPreviousNoRequest' " +
                "routeBIntent='NoRequest'.");

            return valid;
        }

        private static bool ConfigureCanonicalRouteAndPromoteSessionAuthority(
            string expectedDefaultsGuid)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GlobalScenePath) == null)
            {
                Debug.LogError(
                    $"[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] Persistent Content scene is missing. scene='{GlobalScenePath}'.");
                return false;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(CanonicalScenePath) == null)
            {
                Debug.LogError(
                    $"[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] Canonical Audio Route scene is missing. scene='{CanonicalScenePath}'.");
                return false;
            }

            Scene global =
                EditorSceneManager.OpenScene(
                    GlobalScenePath,
                    OpenSceneMode.Single);

            Scene canonical =
                EditorSceneManager.OpenScene(
                    CanonicalScenePath,
                    OpenSceneMode.Additive);

            GameObject routeRoot =
                FindInScene(canonical, CanonicalRouteRootName);

            if (routeRoot == null)
            {
                Debug.LogError(
                    "[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] Canonical Route root is missing. Run the base Audio QA builder first.");
                return false;
            }

            Transform routeHostTransform =
                routeRoot.transform.Find(HostName);

            if (routeHostTransform == null)
            {
                Debug.LogError(
                    "[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] Canonical validated AudioRuntimeHost is missing. Run the base Audio QA builder first.",
                    routeRoot);
                return false;
            }

            GameObject hostObject =
                routeHostTransform.gameObject;

            AudioRuntimeHost host =
                hostObject.GetComponent<AudioRuntimeHost>();

            if (host == null)
            {
                Debug.LogError(
                    "[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] Canonical validated AudioRuntimeHost component is missing.",
                    hostObject);
                return false;
            }

            if (!TryValidateDefaults(
                    host,
                    expectedDefaultsGuid,
                    out string canonicalDefaultsPath))
            {
                Debug.LogError(
                    "[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] Canonical validated AudioRuntimeHost does not reference the expected AudioDefaultsAsset. " +
                    $"expectedPath='{DefaultsPath}' actualPath='{canonicalDefaultsPath}'.",
                    host);
                return false;
            }

            GameObject legacyGlobal =
                FindRoot(global, LegacyTransientAuthorityName);

            if (legacyGlobal != null)
            {
                Object.DestroyImmediate(legacyGlobal);
            }

            GameObject authority =
                EnsureRoot(global, SessionAuthorityName);

            RemoveExistingSessionHost(authority, hostObject);

            // A GameObject must be root-level before moving between scenes.
            hostObject.transform.SetParent(null, false);
            SceneManager.MoveGameObjectToScene(hostObject, global);
            hostObject.transform.SetParent(authority.transform, false);

            FrameworkBgmDirector director =
                EnsureComponent<FrameworkBgmDirector>(authority);

            bool valid = true;

            valid &= SetBool(host, "composeOnAwake", true);
            valid &= SetBool(host, "ensurePersistentListener", false);

            valid &= SetObject(director, "audioRuntimeHost", host);
            valid &= SetBool(director, "logTransitions", true);

            FrameworkBgmDirector routeLocalDirector =
                routeRoot.GetComponent<FrameworkBgmDirector>();

            if (routeLocalDirector != null)
            {
                Object.DestroyImmediate(routeLocalDirector);
            }

            valid &= ClearBindingDirectorReferences(canonical);

            RouteBgmAuthoring routeBinding =
                routeRoot.GetComponent<RouteBgmAuthoring>();

            if (routeBinding == null)
            {
                Debug.LogError(
                    "[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] Canonical RouteBgmAuthoring is missing.",
                    routeRoot);
                valid = false;
            }
            else
            {
                valid &= SetInt(
                    routeBinding,
                    "policy",
                    (int)FrameworkBgmRoutePolicy.PlayOwn);
                valid &= SetInt(
                    routeBinding,
                    "routePolicySerializationVersion",
                    1);
                valid &= SetObject(routeBinding, "director", null);
            }

            FrameworkBgmQaPanel panel =
                FindInScene(canonical, CanonicalPanelName)?
                    .GetComponent<FrameworkBgmQaPanel>();

            if (panel == null)
            {
                Debug.LogError(
                    "[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] Canonical FrameworkBgmQaPanel is missing.");
                valid = false;
            }
            else
            {
                valid &= SetObject(panel, "director", null);
                valid &= SetObject(panel, "routeBgmBinding", routeBinding);
            }

            valid &= ValidateSessionAuthorityInMemory(
                global,
                authority,
                host,
                director,
                expectedDefaultsGuid);

            valid &= ValidateTransientSceneHasNoBgmAuthority(
                canonical,
                "Canonical");

            EditorSceneManager.MarkSceneDirty(global);
            EditorSceneManager.MarkSceneDirty(canonical);

            if (!EditorSceneManager.SaveScene(global, GlobalScenePath))
            {
                Debug.LogError(
                    $"[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] Could not save Persistent Content scene. scene='{GlobalScenePath}'.");
                valid = false;
            }

            if (!EditorSceneManager.SaveScene(canonical, CanonicalScenePath))
            {
                Debug.LogError(
                    $"[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] Could not save canonical Audio Route scene. scene='{CanonicalScenePath}'.");
                valid = false;
            }

            return valid;
        }

        private static bool ValidatePersistedSessionAuthority(
            string expectedDefaultsGuid)
        {
            Scene scene =
                EditorSceneManager.OpenScene(
                    GlobalScenePath,
                    OpenSceneMode.Single);

            GameObject authority =
                FindRoot(scene, SessionAuthorityName);

            if (authority == null)
            {
                Debug.LogError(
                    "[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] Persisted Session BGM authority is missing after reopening QA_UIGlobal.");
                return false;
            }

            AudioRuntimeHost host =
                authority.GetComponentInChildren<AudioRuntimeHost>(true);

            FrameworkBgmDirector director =
                authority.GetComponent<FrameworkBgmDirector>();

            bool defaultsValid =
                TryValidateDefaults(
                    host,
                    expectedDefaultsGuid,
                    out string persistedDefaultsPath);

            bool valid =
                host != null &&
                director != null &&
                defaultsValid;

            if (!valid)
            {
                Debug.LogError(
                    "[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] Persisted Session BGM authority failed reopen validation. " +
                    $"host='{FormatObject(host)}' defaultsPath='{persistedDefaultsPath}' " +
                    $"expectedDefaultsPath='{DefaultsPath}' director='{FormatObject(director)}'.",
                    authority);
                return false;
            }

            SerializedObject directorSerialized =
                new SerializedObject(director);

            SerializedProperty hostProperty =
                directorSerialized.FindProperty("audioRuntimeHost");

            if (hostProperty == null ||
                !ReferenceEquals(
                    hostProperty.objectReferenceValue,
                    host))
            {
                Debug.LogError(
                    "[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] Persisted FrameworkBgmDirector does not reference the persisted AudioRuntimeHost after reopening QA_UIGlobal.",
                    director);
                return false;
            }

            return true;
        }

        private static bool ConfigureAlternateRouteStartupActivity()
        {
            Object alternateRoute =
                AssetDatabase.LoadMainAssetAtPath(
                    AlternateRouteAssetPath);

            Object retainActivity =
                AssetDatabase.LoadMainAssetAtPath(
                    RetainActivityAssetPath);

            if (alternateRoute == null ||
                retainActivity == null)
            {
                Debug.LogError(
                    "[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] Route B startup Activity fixture is missing. " +
                    $"route='{AlternateRouteAssetPath}' retainActivity='{RetainActivityAssetPath}'.");
                return false;
            }

            bool valid =
                SetObject(
                    alternateRoute,
                    "startupActivity",
                    retainActivity);

            if (!valid)
            {
                return false;
            }

            var serialized =
                new SerializedObject(alternateRoute);

            SerializedProperty startupActivity =
                serialized.FindProperty(
                    "startupActivity");

            if (startupActivity == null ||
                !ReferenceEquals(
                    startupActivity.objectReferenceValue,
                    retainActivity))
            {
                Debug.LogError(
                    "[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] Route B startup Activity was not persisted as Retain Previous.",
                    alternateRoute);
                return false;
            }

            EditorUtility.SetDirty(
                alternateRoute);

            return true;
        }

        private static bool ConfigureAlternateRouteScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(AlternateScenePath) == null)
            {
                Debug.LogError(
                    $"[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] Alternate Audio Route scene is missing. scene='{AlternateScenePath}'.");
                return false;
            }

            Scene scene =
                EditorSceneManager.OpenScene(
                    AlternateScenePath,
                    OpenSceneMode.Single);

            GameObject routeRoot =
                FindInScene(scene, AlternateRouteRootName);

            if (routeRoot == null)
            {
                Debug.LogError(
                    "[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] Alternate Route root is missing. Run the base Audio QA builder first.");
                return false;
            }

            RemoveTransientAuthority(scene, routeRoot);

            bool valid =
                ClearBindingDirectorReferences(scene);

            RouteBgmAuthoring routeBinding =
                routeRoot.GetComponent<RouteBgmAuthoring>();

            if (routeBinding == null)
            {
                Debug.LogError(
                    "[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] Alternate RouteBgmAuthoring is missing.",
                    routeRoot);
                return false;
            }

            // Route B explicitly preserves the previously confirmed presentation across Route A
            // exit, scene unload and Route B entry.
            valid &= SetObject(routeBinding, "routeBgm", null);
            valid &= SetInt(
                routeBinding,
                "policy",
                (int)FrameworkBgmRoutePolicy.PreserveCurrent);
            valid &= SetInt(
                routeBinding,
                "routePolicySerializationVersion",
                1);
            valid &= SetObject(routeBinding, "director", null);

            FrameworkBgmQaPanel panel =
                FindInScene(scene, AlternatePanelName)?
                    .GetComponent<FrameworkBgmQaPanel>();

            if (panel != null)
            {
                valid &= SetObject(panel, "director", null);
                valid &= SetObject(panel, "routeBgmBinding", routeBinding);
                valid &= SetObject(panel, "expectedRouteBgm", null);
            }

            valid &= ValidateTransientSceneHasNoBgmAuthority(
                scene,
                "Alternate");

            EditorSceneManager.MarkSceneDirty(scene);

            if (!EditorSceneManager.SaveScene(
                    scene,
                    AlternateScenePath))
            {
                Debug.LogError(
                    $"[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] Could not save alternate Audio Route scene. scene='{AlternateScenePath}'.");
                return false;
            }

            return valid;
        }

        private static void RemoveExistingSessionHost(
            GameObject authority,
            GameObject promotedHost)
        {
            AudioRuntimeHost[] existingHosts =
                authority.GetComponentsInChildren<AudioRuntimeHost>(true);

            for (int index =
                     existingHosts.Length - 1;
                 index >= 0;
                 index--)
            {
                AudioRuntimeHost candidate =
                    existingHosts[index];

                if (candidate == null ||
                    candidate.gameObject == promotedHost)
                {
                    continue;
                }

                Object.DestroyImmediate(
                    candidate.gameObject);
            }

            Transform namedHost =
                authority.transform.Find(HostName);

            if (namedHost != null &&
                namedHost.gameObject != promotedHost)
            {
                Object.DestroyImmediate(
                    namedHost.gameObject);
            }
        }

        private static void RemoveTransientAuthority(
            Scene scene,
            GameObject routeRoot)
        {
            FrameworkBgmDirector routeLocalDirector =
                routeRoot.GetComponent<FrameworkBgmDirector>();

            if (routeLocalDirector != null)
            {
                Object.DestroyImmediate(
                    routeLocalDirector);
            }

            Transform host =
                routeRoot.transform.Find(HostName);

            if (host != null)
            {
                Object.DestroyImmediate(
                    host.gameObject);
            }

            GameObject legacyAuthority =
                FindRoot(scene, LegacyTransientAuthorityName);

            if (legacyAuthority != null)
            {
                Object.DestroyImmediate(
                    legacyAuthority);
            }
        }

        private static bool ClearBindingDirectorReferences(
            Scene scene)
        {
            bool valid = true;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                RouteBgmAuthoring[] routeBindings =
                    root.GetComponentsInChildren<RouteBgmAuthoring>(true);

                for (int index = 0;
                     index < routeBindings.Length;
                     index++)
                {
                    valid &= SetObject(
                        routeBindings[index],
                        "director",
                        null);
                }

                ActivityBgmAuthoring[] activityBindings =
                    root.GetComponentsInChildren<ActivityBgmAuthoring>(true);

                for (int index = 0;
                     index < activityBindings.Length;
                     index++)
                {
                    valid &= SetObject(
                        activityBindings[index],
                        "director",
                        null);
                }
            }

            return valid;
        }

        private static bool ValidateSessionAuthorityInMemory(
            Scene scene,
            GameObject authority,
            AudioRuntimeHost host,
            FrameworkBgmDirector director,
            string expectedDefaultsGuid)
        {
            int authorityCount = 0;
            int hostCount = 0;
            int directorCount = 0;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == SessionAuthorityName)
                {
                    authorityCount++;
                }

                AudioRuntimeHost[] hosts =
                    root.GetComponentsInChildren<AudioRuntimeHost>(true);

                for (int index = 0;
                     index < hosts.Length;
                     index++)
                {
                    if (hosts[index] != null &&
                        hosts[index].gameObject.name == HostName)
                    {
                        hostCount++;
                    }
                }

                FrameworkBgmDirector[] directors =
                    root.GetComponentsInChildren<FrameworkBgmDirector>(true);

                for (int index = 0;
                     index < directors.Length;
                     index++)
                {
                    if (directors[index] != null &&
                        directors[index].gameObject == authority)
                    {
                        directorCount++;
                    }
                }
            }

            bool defaultsValid =
                TryValidateDefaults(
                    host,
                    expectedDefaultsGuid,
                    out string actualDefaultsPath);

            bool valid =
                authorityCount == 1 &&
                hostCount == 1 &&
                directorCount == 1 &&
                host != null &&
                director != null &&
                defaultsValid &&
                host.transform.IsChildOf(authority.transform) &&
                director.gameObject == authority;

            if (!valid)
            {
                Debug.LogError(
                    "[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] Persistent BGM authority composition is invalid. " +
                    $"authorityCount='{authorityCount}' hostCount='{hostCount}' directorCount='{directorCount}' " +
                    $"defaultsPath='{actualDefaultsPath}' expectedDefaultsPath='{DefaultsPath}'.",
                    authority);
            }

            return valid;
        }

        private static bool ValidateTransientSceneHasNoBgmAuthority(
            Scene scene,
            string label)
        {
            int directors = 0;
            int frameworkHosts = 0;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                directors +=
                    root.GetComponentsInChildren<FrameworkBgmDirector>(true).Length;

                AudioRuntimeHost[] hosts =
                    root.GetComponentsInChildren<AudioRuntimeHost>(true);

                for (int index = 0;
                     index < hosts.Length;
                     index++)
                {
                    if (hosts[index] != null &&
                        hosts[index].gameObject.name == HostName)
                    {
                        frameworkHosts++;
                    }
                }
            }

            bool valid =
                directors == 0 &&
                frameworkHosts == 0;

            if (!valid)
            {
                Debug.LogError(
                    "[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] Transient Audio Route scene still owns BGM authority. " +
                    $"label='{label}' directors='{directors}' frameworkHosts='{frameworkHosts}'.");
            }

            return valid;
        }

        private static GameObject EnsureRoot(
            Scene scene,
            string name)
        {
            GameObject existing =
                FindRoot(scene, name);

            if (existing != null)
            {
                return existing;
            }

            var root =
                new GameObject(name);

            SceneManager.MoveGameObjectToScene(
                root,
                scene);

            return root;
        }

        private static GameObject FindRoot(
            Scene scene,
            string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }

            return null;
        }

        private static GameObject FindInScene(
            Scene scene,
            string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform match =
                    FindInChildren(
                        root.transform,
                        name);

                if (match != null)
                {
                    return match.gameObject;
                }
            }

            return null;
        }

        private static Transform FindInChildren(
            Transform root,
            string name)
        {
            if (root.name == name)
            {
                return root;
            }

            for (int index = 0;
                 index < root.childCount;
                 index++)
            {
                Transform match =
                    FindInChildren(
                        root.GetChild(index),
                        name);

                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static T EnsureComponent<T>(
            GameObject gameObject)
            where T : Component
        {
            T existing =
                gameObject.GetComponent<T>();

            return existing != null
                ? existing
                : gameObject.AddComponent<T>();
        }

        private static bool SetObject(
            Object target,
            string propertyName,
            Object value)
        {
            if (target == null)
            {
                Debug.LogError(
                    $"[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] Cannot set serialized object property on null target. property='{propertyName}'.");
                return false;
            }

            var serialized =
                new SerializedObject(target);

            serialized.Update();

            SerializedProperty property =
                serialized.FindProperty(propertyName);

            if (property == null)
            {
                Debug.LogError(
                    $"[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] Serialized property missing. target='{target.GetType().Name}' property='{propertyName}'.",
                    target);
                return false;
            }

            property.objectReferenceValue =
                value;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
            return true;
        }

        private static bool SetBool(
            Object target,
            string propertyName,
            bool value)
        {
            if (target == null)
            {
                Debug.LogError(
                    $"[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] Cannot set serialized bool property on null target. property='{propertyName}'.");
                return false;
            }

            var serialized =
                new SerializedObject(target);

            serialized.Update();

            SerializedProperty property =
                serialized.FindProperty(propertyName);

            if (property == null)
            {
                Debug.LogError(
                    $"[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] Serialized property missing. target='{target.GetType().Name}' property='{propertyName}'.",
                    target);
                return false;
            }

            property.boolValue =
                value;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
            return true;
        }

        private static bool SetInt(
            Object target,
            string propertyName,
            int value)
        {
            if (target == null)
            {
                Debug.LogError(
                    $"[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] Cannot set serialized integer property on null target. property='{propertyName}'.");
                return false;
            }

            var serialized = new SerializedObject(target);
            serialized.Update();

            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError(
                    $"[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] Serialized property missing. target='{target.GetType().Name}' property='{propertyName}'.",
                    target);
                return false;
            }

            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
            return true;
        }

        private static bool TryValidateDefaults(
            AudioRuntimeHost host,
            string expectedDefaultsGuid,
            out string actualDefaultsPath)
        {
            actualDefaultsPath = string.Empty;

            if (host == null)
            {
                return false;
            }

            AudioDefaultsAsset currentDefaults =
                host.Defaults;

            if (currentDefaults == null)
            {
                return false;
            }

            actualDefaultsPath =
                AssetDatabase.GetAssetPath(currentDefaults);

            if (string.IsNullOrWhiteSpace(actualDefaultsPath))
            {
                return false;
            }

            string actualGuid =
                AssetDatabase.AssetPathToGUID(actualDefaultsPath);

            return string.Equals(
                actualGuid,
                expectedDefaultsGuid,
                System.StringComparison.Ordinal);
        }

        private static string FormatObject(
            Object value)
        {
            return value != null
                ? value.name
                : "<null>";
        }
    }
}
