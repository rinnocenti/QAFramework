using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.Authoring
{
    /// <summary>
    /// API status: Experimental. Authoring profile for Activity-owned content declarations.
    /// F25A only defines the contract; later F25 cuts add plan/result, execution and release.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Activity Content Profile",
        menuName = "Immersive Framework/Activity Content Profile",
        order = 21)]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Activity content profile is declaration-only in F25A; scene composition execution is deferred.")]
    public sealed class ActivityContentProfileAsset : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Stable profile id used in diagnostics. If empty, the asset name is used.")]
        private string profileId = string.Empty;

        [SerializeField]
        [Tooltip("Scenes declared by this Activity. F25A validates declarations only; later F25 cuts load them additively when execution-ready.")]
        private ActivityContentSceneEntry[] scenes = Array.Empty<ActivityContentSceneEntry>();

        [SerializeField]
        [TextArea(2, 4)]
        [Tooltip("Optional authoring note for this Activity Content Profile. This has no runtime behavior.")]
        private string description = string.Empty;

        public string ProfileId
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(profileId))
                {
                    return profileId.Trim();
                }

                return !string.IsNullOrWhiteSpace(name) ? name : "ActivityContentProfile";
            }
        }

        public IReadOnlyList<ActivityContentSceneEntry> Scenes => scenes ?? Array.Empty<ActivityContentSceneEntry>();

        public int SceneCount => Scenes.Count;

        public bool HasScenes => SceneCount > 0;

        public string Description => description ?? string.Empty;
    }
}
