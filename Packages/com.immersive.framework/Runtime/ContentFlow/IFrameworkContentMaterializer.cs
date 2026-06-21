using Immersive.Framework.ApiStatus;
namespace Immersive.Framework.ContentFlow
{
    /// <summary>
    /// API status: Experimental. Do not treat this as a stable materialization or contribution contract before F1/F5/F8.
    /// Marker contract for objects that materialize content handles for a specific scope and kind.
    /// Concrete materializers define their own command/result types.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    public interface IFrameworkContentMaterializer
    {
        FrameworkContentScope Scope { get; }

        FrameworkContentKind Kind { get; }
    }
}
