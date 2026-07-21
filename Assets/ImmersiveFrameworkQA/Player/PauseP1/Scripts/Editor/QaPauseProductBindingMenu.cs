using UnityEditor;

namespace ImmersiveFrameworkQA.PauseP1.Editor
{
    internal static class QaPauseProductBindingMenu
    {
        private const string MenuPath =
            "Immersive Framework/QA/Setup/Pause/Create or Refresh Pause Product Binding QA";

        [MenuItem(MenuPath, false, 100)]
        private static void CreateOrRefresh()
        {
            QaPauseProductBindingSetup.CreateOrRefresh();
        }
    }
}
