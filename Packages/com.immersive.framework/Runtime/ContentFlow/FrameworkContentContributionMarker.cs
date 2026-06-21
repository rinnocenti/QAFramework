using UnityEngine;

namespace Immersive.Framework.ContentFlow
{
    /// <summary>
    /// API status: Experimental. Do not treat this as a stable materialization or contribution contract before F1/F5/F8.
    /// Generic authored contribution marker for route/activity content.
    /// This is discovery data only; it does not materialize, activate, or release objects.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FrameworkContentContributionMarker : MonoBehaviour, IFrameworkContentContribution
    {
        [SerializeField]
        [Tooltip("Lifecycle scope that owns this contribution.")]
        private FrameworkContentScope contributionScope = FrameworkContentScope.Route;

        [SerializeField]
        [Tooltip("Stable contribution id within its owning content scope. If empty, the GameObject name is used.")]
        private string contributionId = string.Empty;

        [SerializeField]
        [Tooltip("Domain-specific contribution kind. Examples: Camera, Audio, Surface, Actor, Reset.")]
        private string contributionKind = "Generic";

        [SerializeField]
        [Tooltip("Whether this contribution is required by its future consumer. No consumer is implemented in this baseline cut.")]
        private FrameworkContentRequiredness requiredness = FrameworkContentRequiredness.Optional;

        public FrameworkContentScope ContributionScope => contributionScope;

        public string ContributionId
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(contributionId))
                {
                    return contributionId.Trim();
                }

                return !string.IsNullOrWhiteSpace(name) ? name : "ContentContribution";
            }
        }

        public string ContributionKind
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(contributionKind))
                {
                    return contributionKind.Trim();
                }

                return "Generic";
            }
        }

        public FrameworkContentRequiredness Requiredness => requiredness;
    }
}
