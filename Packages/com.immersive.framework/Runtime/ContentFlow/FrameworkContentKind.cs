namespace Immersive.Framework.ContentFlow
{
    /// <summary>
    /// Declares the concrete materialization family represented by a content handle.
    /// </summary>
    public enum FrameworkContentKind
    {
        Unknown = 0,
        Scene = 10,
        Prefab = 20,
        SceneAuthored = 30,
        RuntimeRoot = 40,
        SurfaceSlot = 50,
        RuntimeSpawned = 60
    }
}
