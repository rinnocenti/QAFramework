namespace Immersive.Framework.ContentFlow
{
    /// <summary>
    /// API status: Experimental. Do not treat this as a stable materialization or contribution contract before F1/F5/F8.
    /// Minimal authored/runtime contribution contract exposed by materialized content.
    /// Contributions are consumed by subsystem-specific adapters in later cuts.
    /// </summary>
    public interface IFrameworkContentContribution
    {
        FrameworkContentScope ContributionScope { get; }

        string ContributionId { get; }

        string ContributionKind { get; }

        FrameworkContentRequiredness Requiredness { get; }
    }
}
