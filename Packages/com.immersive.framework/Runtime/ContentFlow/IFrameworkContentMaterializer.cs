namespace Immersive.Framework.ContentFlow
{
    /// <summary>
    /// Marker contract for objects that materialize content handles for a specific scope and kind.
    /// Concrete materializers define their own command/result types.
    /// </summary>
    public interface IFrameworkContentMaterializer
    {
        FrameworkContentScope Scope { get; }

        FrameworkContentKind Kind { get; }
    }
}
