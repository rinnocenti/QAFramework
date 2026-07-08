namespace ImmersiveFrameworkQA.Hub.Editor
{
    /// <summary>
    /// Compatibility bridge for the earlier F49D QA fix attempt.
    /// The actual PlayerEntry passive scene builder lives in ImmersiveFrameworkQA.Player.Editor.
    /// </summary>
    public static class QaPlayerEntryPassiveSceneBuilder
    {
        public static void CreateOrRefreshPlayerEntryPassiveScene()
        {
            ImmersiveFrameworkQA.Player.Editor.QaPlayerEntryPassiveSceneBuilder.CreateOrRefreshPlayerEntryPassiveScene();
        }

        public static void OpenPlayerEntryPassiveScene()
        {
            ImmersiveFrameworkQA.Player.Editor.QaPlayerEntryPassiveSceneBuilder.OpenPlayerEntryPassiveScene();
        }
    }
}
