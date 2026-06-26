using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Result vocabulary for Pause consuming canonical Content Anchor declarations.
    /// This reports contract preparation only; it does not materialize UI, bind input or change Pause state.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F23B Pause Content Anchor consumer status vocabulary; no materialization.")]
    public enum PauseContentAnchorConsumerStatus
    {
        /// <summary>Invalid default value. Results must always use an explicit status.</summary>
        Unknown = 0,

        /// <summary>A matching Content Anchor was found and a canonical ContentAnchorBindingRequest was prepared.</summary>
        Prepared = 10,

        /// <summary>The requested optional Content Anchor was not available and no binding request was produced.</summary>
        SkippedOptionalMissing = 20,

        /// <summary>The requested required Content Anchor was not available.</summary>
        RejectedMissingRequired = 100,

        /// <summary>A candidate anchor existed but did not match the explicit Pause request identity/kind.</summary>
        RejectedMismatchedAnchor = 110,

        /// <summary>The consumer failed while preparing the request contract.</summary>
        Failed = 200
    }
}
