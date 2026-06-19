using UnityEngine;

namespace Immersive.Framework.Authoring
{
    /// <summary>
    /// Public root asset for an Immersive game/application.
    /// Keep this asset small: it should grow only when a real framework cut needs a new decision.
    /// </summary>
    [CreateAssetMenu(
        fileName = "GameApplication",
        menuName = "Immersive Framework/Game Application",
        order = 0)]
    public sealed class GameApplicationAsset : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Human-readable name shown in framework diagnostics. If empty, the asset name is used.")]
        private string applicationName = "Game Application";

        [SerializeField]
        [Tooltip("First route requested by the Game Flow after framework boot. The Route declares the Primary Scene loaded by Scene Lifecycle.")]
        private RouteAsset startupRoute;

        [SerializeField]
        [Tooltip("Controls validation and diagnostics severity. Required configuration must still fail in every mode.")]
        private FrameworkValidationMode validationMode = FrameworkValidationMode.Standard;

        public string ApplicationName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(applicationName))
                {
                    return applicationName.Trim();
                }

                return !string.IsNullOrWhiteSpace(name) ? name : "Game Application";
            }
        }

        public RouteAsset StartupRoute => startupRoute;

        public FrameworkValidationMode ValidationMode => validationMode;
    }
}
