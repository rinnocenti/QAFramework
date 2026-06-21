namespace Immersive.Framework.Authoring
{
    /// <summary>
    /// API status: Experimental until F1 defines concrete Strict/Standard/Release semantics.
    /// Public authoring mode that controls validation and diagnostics severity.
    /// This must not enable silent fallback for required configuration.
    /// </summary>
    public enum FrameworkValidationMode
    {
        Strict = 0,
        Standard = 1,
        Release = 2
    }
}
