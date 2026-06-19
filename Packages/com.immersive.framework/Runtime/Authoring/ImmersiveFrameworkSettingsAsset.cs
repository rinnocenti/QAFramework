using UnityEngine;

namespace Immersive.Framework.Authoring
{
    /// <summary>
    /// Project-level backing asset for Immersive Framework settings.
    /// Users should edit this through Project Settings > Immersive Framework.
    /// </summary>
    public sealed class ImmersiveFrameworkSettingsAsset : ScriptableObject
    {
        public const string ResourcesPath = "ImmersiveFrameworkSettings";

        [SerializeField]
        private GameApplicationAsset activeGameApplication;

        [SerializeField]
        private FrameworkEditorPlayModeStartup editorPlayModeStartup = FrameworkEditorPlayModeStartup.FrameworkStartup;

        public GameApplicationAsset ActiveGameApplication => activeGameApplication;

        public FrameworkEditorPlayModeStartup EditorPlayModeStartup => editorPlayModeStartup;
    }
}
