using UnityEngine;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Authoring
{
    /// <summary>
    /// API status: Experimental. Public authoring asset retained for the baseline before F1/F4 identity and activity-state hardening.
    /// Public authoring asset for a gameplay activity.
    /// This first cut only identifies the activity; content, actors, input, camera, save and pause are added later.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Activity",
        menuName = "Immersive Framework/Activity",
        order = 20)]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    public sealed class ActivityAsset : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Human-readable activity name shown in framework diagnostics. If empty, the asset name is used.")]
        private string activityName = "Activity";

        [SerializeField]
        [TextArea(2, 4)]
        [Tooltip("Optional authoring note for the activity. This has no runtime behavior yet.")]
        private string description = string.Empty;

        [SerializeField]
        [Tooltip("Optional Activity Content Profile. F25A declares Activity-owned scenes only; scene composition execution and release are added in later F25 cuts.")]
        private ActivityContentProfileAsset activityContentProfile;

        [SerializeField]
        [Tooltip("Defines whether Activity requests should use the session TransitionSurface. Seamless is the default. FadeWithLoading is reserved until Activity scene/content loading execution exists. Route transitions remain mandatory elsewhere.")]
        private ActivityVisualTransitionMode visualTransitionMode = ActivityVisualTransitionMode.Seamless;

        public string ActivityName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(activityName))
                {
                    return activityName.Trim();
                }

                return !string.IsNullOrWhiteSpace(name) ? name : "Activity";
            }
        }

        public string Description => description ?? string.Empty;

        public ActivityContentProfileAsset ActivityContentProfile => activityContentProfile;

        public bool HasActivityContentProfile => activityContentProfile != null;

        public bool HasActivityContentScenes => activityContentProfile != null && activityContentProfile.HasScenes;

        public ActivityVisualTransitionMode VisualTransitionMode
        {
            get
            {
                return System.Enum.IsDefined(typeof(ActivityVisualTransitionMode), visualTransitionMode)
                    ? visualTransitionMode
                    : ActivityVisualTransitionMode.Seamless;
            }
        }
    }
}
