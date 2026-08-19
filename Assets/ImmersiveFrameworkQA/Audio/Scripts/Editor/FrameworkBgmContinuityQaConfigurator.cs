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
    /// BGM-CONTINUITY-1 topology pass applied after the legacy Framework BGM fixture builder.
    /// It separates persistent playback authority from transient Route/Activity bindings and makes
    /// Route B intentionally publish no Route BGM request.
    /// </summary>
    internal static class FrameworkBgmContinuityQaConfigurator
    {
        private const string CanonicalScenePath = "Assets/ImmersiveFrameworkQA/Audio/Scenes/QA_Audio.unity";
        private const string AlternateScenePath = "Assets/ImmersiveFrameworkQA/Audio/Scenes/QA_AudioRouteB.unity";
        private const string AuthorityName = "QA_FrameworkBgmAuthority_Persistent";
        private const string HostName = "QA_FrameworkBgm_AudioRuntimeHost";
        private const string CanonicalRouteRootName = "QA_FrameworkBgmRoot_Canonical";
        private const string AlternateRouteRootName = "QA_FrameworkBgmRoot_Alternate";

        internal static bool Configure()
        {
            Scene canonical = EditorSceneManager.OpenScene(CanonicalScenePath, OpenSceneMode.Single);
            bool canonicalValid = ConfigureCanonical(canonical);
            EditorSceneManager.MarkSceneDirty(canonical);
            EditorSceneManager.SaveScene(canonical, CanonicalScenePath);

            Scene alternate = EditorSceneManager.OpenScene(AlternateScenePath, OpenSceneMode.Single);
            bool alternateValid = ConfigureAlternate(alternate);
            EditorSceneManager.MarkSceneDirty(alternate);
            EditorSceneManager.SaveScene(alternate, AlternateScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            bool valid = canonicalValid && alternateValid;
            Debug.Log(
                $"[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] status='{(valid ? "Applied" : "Failed")}' " +
                $"authorityScene='QA_Audio' transientRoute='QA_AudioRouteB' routeBIntent='NoRequest'.");
            return valid;
        }

        private static bool ConfigureCanonical(Scene scene)
        {
            GameObject routeRoot = FindInScene(scene, CanonicalRouteRootName);
            GameObject hostObject = routeRoot != null
                ? routeRoot.transform.Find(HostName)?.gameObject
                : null;
            if (routeRoot == null || hostObject == null)
            {
                Debug.LogError("[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] Canonical route root or AudioRuntimeHost is missing. Run the base Audio QA builder first.");
                return false;
            }

            GameObject authority = FindRoot(scene, AuthorityName);
            if (authority == null)
            {
                authority = new GameObject(AuthorityName);
                SceneManager.MoveGameObjectToScene(authority, scene);
            }

            EnsureComponent<FrameworkBgmQaPersistentAuthority>(authority);

            Transform previousAuthorityHost = authority.transform.Find(HostName);
            if (previousAuthorityHost != null && previousAuthorityHost.gameObject != hostObject)
            {
                Object.DestroyImmediate(previousAuthorityHost.gameObject);
            }

            hostObject.transform.SetParent(authority.transform, false);

            AudioRuntimeHost host = hostObject.GetComponent<AudioRuntimeHost>();
            if (host == null)
            {
                Debug.LogError("[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] Canonical AudioRuntimeHost component is missing.", hostObject);
                return false;
            }

            FrameworkBgmDirector routeLocalDirector = routeRoot.GetComponent<FrameworkBgmDirector>();
            FrameworkBgmDirector director = EnsureComponent<FrameworkBgmDirector>(authority);
            bool valid = SetObject(director, "audioRuntimeHost", host)
                & SetBool(director, "logTransitions", true);

            valid &= ClearBindingDirectorReferences(scene);

            FrameworkBgmQaPanel panel = FindInScene(scene, "QA_FrameworkBgmPanel_Canonical")?.GetComponent<FrameworkBgmQaPanel>();
            if (panel == null)
            {
                Debug.LogError("[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] Canonical FrameworkBgmQaPanel is missing.");
                valid = false;
            }
            else
            {
                valid &= SetObject(panel, "director", director);
            }

            if (routeLocalDirector != null && routeLocalDirector != director)
            {
                Object.DestroyImmediate(routeLocalDirector);
            }

            FrameworkRouteBgmBinding routeBinding = routeRoot.GetComponent<FrameworkRouteBgmBinding>();
            if (routeBinding == null)
            {
                Debug.LogError("[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] Canonical FrameworkRouteBgmBinding is missing.", routeRoot);
                valid = false;
            }
            else
            {
                valid &= SetObject(routeBinding, "director", null);
            }

            return valid;
        }

        private static bool ConfigureAlternate(Scene scene)
        {
            GameObject routeRoot = FindInScene(scene, AlternateRouteRootName);
            if (routeRoot == null)
            {
                Debug.LogError("[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] Alternate Route root is missing. Run the base Audio QA builder first.");
                return false;
            }

            bool valid = ClearBindingDirectorReferences(scene);

            FrameworkBgmDirector routeLocalDirector = routeRoot.GetComponent<FrameworkBgmDirector>();
            if (routeLocalDirector != null)
            {
                Object.DestroyImmediate(routeLocalDirector);
            }

            GameObject hostObject = FindInScene(scene, HostName);
            if (hostObject != null)
            {
                Object.DestroyImmediate(hostObject);
            }

            FrameworkRouteBgmBinding routeBinding = routeRoot.GetComponent<FrameworkRouteBgmBinding>();
            if (routeBinding == null)
            {
                Debug.LogError("[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] Alternate FrameworkRouteBgmBinding is missing.", routeRoot);
                return false;
            }

            // Route B deliberately has no BGM opinion. Switching from canonical Route A to this
            // Route must preserve the currently confirmed BGM.
            valid &= SetObject(routeBinding, "routeBgm", null);
            valid &= SetObject(routeBinding, "director", null);

            FrameworkBgmQaPanel panel = FindInScene(scene, "QA_FrameworkBgmPanel_Alternate")?.GetComponent<FrameworkBgmQaPanel>();
            if (panel != null)
            {
                valid &= SetObject(panel, "director", null);
                valid &= SetObject(panel, "expectedRouteBgm", null);
            }

            return valid;
        }

        private static bool ClearBindingDirectorReferences(Scene scene)
        {
            bool valid = true;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                FrameworkRouteBgmBinding[] routeBindings = root.GetComponentsInChildren<FrameworkRouteBgmBinding>(true);
                for (int i = 0; i < routeBindings.Length; i++)
                {
                    valid &= SetObject(routeBindings[i], "director", null);
                }

                FrameworkActivityBgmBinding[] activityBindings = root.GetComponentsInChildren<FrameworkActivityBgmBinding>(true);
                for (int i = 0; i < activityBindings.Length; i++)
                {
                    valid &= SetObject(activityBindings[i], "director", null);
                }
            }

            return valid;
        }

        private static GameObject FindRoot(Scene scene, string name)
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

        private static GameObject FindInScene(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform match = FindInChildren(root.transform, name);
                if (match != null)
                {
                    return match.gameObject;
                }
            }

            return null;
        }

        private static Transform FindInChildren(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform match = FindInChildren(root.GetChild(i), name);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static T EnsureComponent<T>(GameObject gameObject) where T : Component
        {
            T existing = gameObject.GetComponent<T>();
            return existing != null ? existing : gameObject.AddComponent<T>();
        }

        private static bool SetObject(Object target, string propertyName, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError(
                    $"[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] Serialized property missing. target='{target.GetType().Name}' property='{propertyName}'.",
                    target);
                return false;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
            return true;
        }

        private static bool SetBool(Object target, string propertyName, bool value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError(
                    $"[FRAMEWORK_BGM_CONTINUITY_QA_SETUP] Serialized property missing. target='{target.GetType().Name}' property='{propertyName}'.",
                    target);
                return false;
            }

            property.boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
            return true;
        }
    }
}
