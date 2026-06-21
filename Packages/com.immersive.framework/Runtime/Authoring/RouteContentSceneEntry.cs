using System;
using Immersive.Framework.ContentFlow;
using UnityEngine;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Authoring
{
    /// <summary>
    /// Authoring declaration for one additional scene owned by a Route content profile.
    /// This declaration is planning data only in the baseline cut; execution comes in a later route composition cut.
    /// </summary>
    [Serializable]
    [FrameworkApiStatus(FrameworkApiStatus.Deferred, "Route content profile is planning-only until route scene composition/release phases.")]
    public sealed class RouteContentSceneEntry
    {
        [SerializeField]
        [Tooltip("Stable content id within the Route Content Profile. If empty, the scene name is used.")]
        private string contentId = string.Empty;

        [SerializeField]
        [Tooltip("Project-relative path of the additional Unity scene declared by this route content profile. Managed by the Profile Inspector.")]
        private string scenePath = string.Empty;

        [SerializeField]
        [Tooltip("Cached human-readable scene name shown in framework diagnostics.")]
        private string sceneName = string.Empty;

        [SerializeField]
        [Tooltip("Whether this scene declaration is required once route scene composition execution is implemented.")]
        private FrameworkContentRequiredness requiredness = FrameworkContentRequiredness.Optional;

        public string ContentId
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(contentId))
                {
                    return contentId.Trim();
                }

                if (!string.IsNullOrWhiteSpace(SceneName))
                {
                    return SceneName;
                }

                return "route-scene";
            }
        }

        public string ScenePath => scenePath ?? string.Empty;

        public string SceneName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(sceneName))
                {
                    return sceneName.Trim();
                }

                if (!string.IsNullOrWhiteSpace(scenePath))
                {
                    var fileName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                    if (!string.IsNullOrWhiteSpace(fileName))
                    {
                        return fileName;
                    }
                }

                return string.Empty;
            }
        }

        public FrameworkContentRequiredness Requiredness => requiredness;

        public bool HasScene => !string.IsNullOrWhiteSpace(ScenePath) || !string.IsNullOrWhiteSpace(SceneName);
    }
}
