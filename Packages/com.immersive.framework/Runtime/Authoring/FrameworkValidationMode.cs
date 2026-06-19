namespace Immersive.Framework.Authoring
{
    /// <summary>
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
