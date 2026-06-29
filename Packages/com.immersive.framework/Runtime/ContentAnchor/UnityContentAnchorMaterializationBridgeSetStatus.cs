using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Status vocabulary for the authored, explicit ContentAnchor materialization bridge set.
    /// This set batches bridge submissions without adding Route/Activity lifecycle automation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9R-F authored ContentAnchor bridge set proof status; explicit batch submit/release only, no automatic lifecycle wiring.")]
    public enum UnityContentAnchorMaterializationBridgeSetStatus
    {
        Unknown = 0,
        SucceededMaterializedAll = 10,
        SucceededReleasedAll = 20,
        SucceededReleaseNoContent = 30,
        FailedNoBridges = 100,
        FailedNullBridge = 110,
        FailedDuplicateBridge = 120,
        FailedBridgeMaterializationPreflight = 125,
        FailedDuplicateMaterializationKey = 126,
        FailedBridgeMaterialization = 130,
        FailedBridgeRelease = 140
    }
}
