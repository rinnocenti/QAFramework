namespace Immersive.Framework.Authoring
{
    /// <summary>
    /// Editor-only startup behavior for entering Play Mode while developing scenes.
    /// Player/runtime builds always use FrameworkStartup.
    /// </summary>
    public enum FrameworkEditorPlayModeStartup
    {
        FrameworkStartup = 0,
        CurrentSceneOnly = 1
    }
}
