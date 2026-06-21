using UnityEngine;
using Immersive.Framework.Authoring;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Scene-authored local visibility adapter between one GameObject and one Activity.
    /// Activity Flow uses this component to toggle local scene-authored content for the active Activity.
    /// It is not canonical Activity materialization.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Activity Local Visibility Adapter")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    public sealed class ActivityContentBinding : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Activity that owns this local visibility adapter. This only toggles this GameObject when that Activity is active; it is not canonical Activity materialization.")]
        private ActivityAsset activity;

        public ActivityAsset Activity => activity;

        internal bool IsSceneBinding => gameObject.scene.IsValid() && gameObject.scene.isLoaded;

        internal string ObjectName => gameObject != null ? gameObject.name : "<missing>";

        internal string SceneName => gameObject != null && gameObject.scene.IsValid()
            ? gameObject.scene.name
            : "<no-scene>";

        internal bool SetContentActive(bool active)
        {
            if (gameObject.activeSelf == active)
            {
                return false;
            }

            gameObject.SetActive(active);
            return true;
        }
    }
}
