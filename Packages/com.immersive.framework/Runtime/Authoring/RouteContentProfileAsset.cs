using System;
using System.Collections.Generic;
using UnityEngine;

namespace Immersive.Framework.Authoring
{
    /// <summary>
    /// API status: Deferred / Planning-only. Additional scenes are declarations only in the active baseline.
    /// Authoring profile for Route-owned content declarations.
    /// The baseline cut only plans additional scenes; actual additive loading is introduced later.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Route Content Profile",
        menuName = "Immersive Framework/Route Content Profile",
        order = 11)]
    public sealed class RouteContentProfileAsset : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Stable profile id used in diagnostics. If empty, the asset name is used.")]
        private string profileId = string.Empty;

        [SerializeField]
        [Tooltip("Additional scenes declared by the Route. These are planned only in this cut; they are not loaded yet.")]
        private RouteContentSceneEntry[] additionalScenes = Array.Empty<RouteContentSceneEntry>();

        [SerializeField]
        [TextArea(2, 4)]
        [Tooltip("Optional authoring note for this Route Content Profile. This has no runtime behavior.")]
        private string description = string.Empty;

        public string ProfileId
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(profileId))
                {
                    return profileId.Trim();
                }

                return !string.IsNullOrWhiteSpace(name) ? name : "RouteContentProfile";
            }
        }

        public IReadOnlyList<RouteContentSceneEntry> AdditionalScenes => additionalScenes ?? Array.Empty<RouteContentSceneEntry>();

        public int AdditionalSceneCount => AdditionalScenes.Count;

        public bool HasAdditionalScenes => AdditionalSceneCount > 0;

        public string Description => description ?? string.Empty;
    }
}
