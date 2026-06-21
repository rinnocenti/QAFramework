namespace Immersive.Framework.ContentFlow
{
    /// <summary>
    /// API status: Experimental. Do not treat this as a stable materialization or contribution contract before F1/F5/F8.
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
