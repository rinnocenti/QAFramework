using UnityEngine;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Loading;

namespace Immersive.Framework.Authoring
{
    /// <summary>
    /// API status: Experimental. Policy for the Unity transition surface wired through a Game Application.
    /// NoneConfigured keeps Transition explicit NoOp; Required instantiates the configured prefab and fails explicitly when the surface is missing or invalid.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F24C Transition surface policy for GameApplication authoring.")]
    public enum TransitionSurfacePolicy
    {
        NoneConfigured = 0,
        Required = 1
    }

    /// <summary>
    /// API status: Experimental. Public authoring root retained as the baseline entry point before F1 identity/status hardening.
    /// Public root asset for an Immersive game/application.
    /// Keep this asset small: it should grow only when a real framework cut needs a new decision.
    /// </summary>
    [CreateAssetMenu(
        fileName = "GameApplication",
        menuName = "Immersive Framework/Game Application",
        order = 0)]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    public sealed class GameApplicationAsset : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Human-readable name shown in framework diagnostics. If empty, the asset name is used.")]
        private string applicationName = "Game Application";

        [SerializeField]
        [Tooltip("First route requested by the Game Flow after framework boot. The Route declares the Primary Scene loaded by Scene Lifecycle.")]
        private RouteAsset startupRoute;

        [SerializeField]
        [Tooltip("Controls whether this Game Application uses an explicit Unity transition surface. NoneConfigured keeps Transition as an explicit NoOp. Required instantiates the configured prefab under the persistent FrameworkRuntimeHost and fails explicitly if the surface is missing or invalid.")]
        private TransitionSurfacePolicy transitionSurfacePolicy = TransitionSurfacePolicy.NoneConfigured;

        [SerializeField]
        [Tooltip("Prefab for the app/session-scoped transition surface. The prefab should include a Canvas, a UI panel and UnityFadeCurtainEffectAdapter. It is instantiated under the persistent FrameworkRuntimeHost when the policy is Required.")]
        private GameObject transitionSurfacePrefab;

        [SerializeField]
        [Tooltip("Controls whether this Game Application uses an explicit Unity loading surface. NoneConfigured keeps loading as an explicit NoOp. Optional uses the prefab if present and skips explicitly when absent. Required instantiates the configured prefab under the persistent FrameworkRuntimeHost and fails explicitly if the surface is missing or invalid.")]
        private LoadingSurfacePolicy loadingSurfacePolicy = LoadingSurfacePolicy.NoneConfigured;

        [SerializeField]
        [Tooltip("Prefab for the app/session-scoped loading surface. The prefab should include a Canvas, a loading panel and UnityLoadingSurfaceAdapter. It is instantiated under the persistent FrameworkRuntimeHost when the policy is Optional or Required and a prefab is assigned.")]
        private GameObject loadingSurfacePrefab;

        [SerializeField]
        [Tooltip("Controls validation and diagnostics severity. Required configuration fails in every mode; Strict promotes warnings, Standard keeps them, Release suppresses info diagnostics.")]
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

        public TransitionSurfacePolicy TransitionSurfacePolicyValue => transitionSurfacePolicy;

        public GameObject TransitionSurfacePrefab => transitionSurfacePrefab;

        public LoadingSurfacePolicy LoadingSurfacePolicyValue => loadingSurfacePolicy;

        public GameObject LoadingSurfacePrefab => loadingSurfacePrefab;

        public FrameworkValidationMode ValidationMode => validationMode;
    }
}
