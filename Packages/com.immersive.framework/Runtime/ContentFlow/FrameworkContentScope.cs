namespace Immersive.Framework.ContentFlow
{
    /// <summary>
    /// Declares the lifecycle scope that owns a materialized or discovered content handle.
    /// </summary>
    public enum FrameworkContentScope
    {
        Unknown = 0,
        Session = 10,
        Route = 20,
        Activity = 30,
        Surface = 40,
        RuntimeSpawned = 50
    }
}
