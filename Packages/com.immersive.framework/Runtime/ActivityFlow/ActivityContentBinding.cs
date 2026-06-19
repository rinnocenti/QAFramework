using UnityEngine;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Scene-authored binding between one GameObject and one Activity.
    /// Activity Flow uses this component to activate content for the active Activity.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Activity Content Binding")]
    public sealed class ActivityContentBinding : MonoBehaviour
    {
        [SerializeField]
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
