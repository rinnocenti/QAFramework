using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Authoring
{
    /// <summary>
    /// API status: Experimental. Declares how Activity-owned content scenes will be loaded once Activity scene composition is implemented.
    /// F25A only defines the contract; execution is introduced by later F25 cuts.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Activity content scene load modes are declaration-only in F25A; execution is deferred to Activity scene composition.")]
    public enum ActivityContentSceneLoadMode
    {
        Additive = 0
    }
}
