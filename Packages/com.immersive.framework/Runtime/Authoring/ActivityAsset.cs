using UnityEngine;

namespace Immersive.Framework.Authoring
{
    /// <summary>
    /// Public authoring asset for a gameplay activity.
    /// This first cut only identifies the activity; content, actors, input, camera, save and pause are added later.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Activity",
        menuName = "Immersive Framework/Activity",
        order = 20)]
    public sealed class ActivityAsset : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Human-readable activity name shown in framework diagnostics. If empty, the asset name is used.")]
        private string activityName = "Activity";

        [SerializeField]
        [TextArea(2, 4)]
        [Tooltip("Optional authoring note for the activity. This has no runtime behavior yet.")]
        private string description = string.Empty;

        public string ActivityName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(activityName))
                {
                    return activityName.Trim();
                }

                return !string.IsNullOrWhiteSpace(name) ? name : "Activity";
            }
        }

        public string Description => description ?? string.Empty;
    }
}
