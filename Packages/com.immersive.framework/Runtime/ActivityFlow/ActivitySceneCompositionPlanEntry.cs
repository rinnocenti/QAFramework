using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.ContentFlow;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Side-effect-free planning record for one scene declared by an Activity Content Profile.
    /// This is not a loaded scene handle and it does not authorize release/unload.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F25B Activity scene composition plan entry; execution and release are deferred.")]
    internal readonly struct ActivitySceneCompositionPlanEntry
    {
        public ActivitySceneCompositionPlanEntry(
            FrameworkContentIdentity contentIdentity,
            string contentId,
            string sceneName,
            string scenePath,
            FrameworkContentRequiredness requiredness,
            ActivityContentSceneLoadMode loadMode,
            ActivityContentReleasePolicy releasePolicy,
            int executionOrder,
            bool hasExplicitContentId)
        {
            ContentIdentity = contentIdentity;
            ContentId = Normalize(contentId);
            SceneName = Normalize(sceneName);
            ScenePath = Normalize(scenePath);
            Requiredness = requiredness;
            LoadMode = loadMode;
            ReleasePolicy = releasePolicy;
            ExecutionOrder = executionOrder;
            HasExplicitContentId = hasExplicitContentId;
        }

        public FrameworkContentIdentity ContentIdentity { get; }

        public string ContentId { get; }

        public string SceneName { get; }

        public string ScenePath { get; }

        public FrameworkContentScope Scope => FrameworkContentScope.Activity;

        public FrameworkContentKind Kind => FrameworkContentKind.Scene;

        public FrameworkContentRequiredness Requiredness { get; }

        public ActivityContentSceneLoadMode LoadMode { get; }

        public ActivityContentReleasePolicy ReleasePolicy { get; }

        public int ExecutionOrder { get; }

        public bool HasExplicitContentId { get; }

        public bool HasContentIdentity => ContentIdentity.IsValid;

        public bool HasScene => !string.IsNullOrWhiteSpace(SceneName) || !string.IsNullOrWhiteSpace(ScenePath);

        public bool IsExecutionReady => HasScene
            && HasContentIdentity
            && HasExplicitContentId
            && LoadMode == ActivityContentSceneLoadMode.Additive;

        public string ToDiagnosticString()
        {
            var scene = !string.IsNullOrWhiteSpace(SceneName) ? SceneName : ScenePath;
            if (string.IsNullOrWhiteSpace(scene))
            {
                scene = "<missing>";
            }

            var identity = HasContentIdentity ? ContentIdentity.StableText : "<missing>";
            var contentId = !string.IsNullOrWhiteSpace(ContentId) ? ContentId : "<missing>";
            return $"identity='{identity}' id='{contentId}' scene='{scene}' requiredness='{Requiredness}' loadMode='{LoadMode}' releasePolicy='{ReleasePolicy}' order='{ExecutionOrder}' explicitId='{HasExplicitContentId}' executionReady='{IsExecutionReady}'";
        }

        public static ActivitySceneCompositionPlanEntry FromEntry(
            ActivityContentSceneEntry entry,
            int declarationIndex,
            string activityOwnerId)
        {
            var explicitContentId = entry != null ? Normalize(entry.ExplicitContentId) : string.Empty;
            var sceneName = entry != null ? Normalize(entry.SceneName) : string.Empty;
            var scenePath = entry != null ? Normalize(entry.ScenePath) : string.Empty;
            var requiredness = entry != null ? entry.Requiredness : FrameworkContentRequiredness.Optional;
            var loadMode = entry != null ? entry.LoadMode : ActivityContentSceneLoadMode.Additive;
            var releasePolicy = entry != null ? entry.ReleasePolicy : ActivityContentReleasePolicy.ReleaseOnActivityExit;
            var hasExplicitContentId = !string.IsNullOrWhiteSpace(explicitContentId);
            FrameworkContentIdentity identity = default;
            if (hasExplicitContentId)
            {
                identity = FrameworkContentIdentity.FromOwnerValue(
                    FrameworkContentScope.Activity,
                    FrameworkContentKind.Scene,
                    activityOwnerId,
                    explicitContentId);
            }

            return new ActivitySceneCompositionPlanEntry(
                identity,
                explicitContentId,
                sceneName,
                scenePath,
                requiredness,
                loadMode,
                releasePolicy,
                declarationIndex,
                hasExplicitContentId);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
