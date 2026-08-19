using UnityEngine;

namespace ImmersiveFrameworkQA.Audio
{
    /// <summary>
    /// QA-only lifetime owner used to prove the same topology provided by Framework Persistent
    /// Content: the AudioRuntimeHost + FrameworkBgmDirector authority survives transient Route
    /// scene unloads. This component is not package/framework runtime architecture.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FrameworkBgmQaPersistentAuthority : MonoBehaviour
    {
        private void Awake()
        {
            if (transform.parent != null)
            {
                Debug.LogError(
                    "[FRAMEWORK_BGM_QA] Persistent authority must be a scene root before DontDestroyOnLoad.",
                    this);
                return;
            }

            DontDestroyOnLoad(gameObject);
        }
    }
}
