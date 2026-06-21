namespace Immersive.Framework.ContentFlow
{
    /// <summary>
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
