using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ContentFlow
{
    /// <summary>
    /// API status: Experimental. Do not treat this as a stable materialization or contribution contract before F1/F5/F8.
    /// Immutable runtime identity for content materialized or discovered by the framework.
    /// A handle records ownership and release intent; it does not expose a service locator.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    public readonly struct FrameworkContentHandle
    {
        public FrameworkContentHandle(
            string contentId,
            FrameworkContentScope scope,
            FrameworkContentKind kind,
            FrameworkContentRequiredness requiredness,
            string ownerId,
            string ownerName,
            string resourceName,
            string resourcePath,
            bool active,
            string source,
            string reason,
            string message)
        {
            ContentId = Normalize(contentId);
            Scope = scope;
            Kind = kind;
            Requiredness = requiredness;
            OwnerId = Normalize(ownerId);
            OwnerName = Normalize(ownerName);
            ResourceName = Normalize(resourceName);
            ResourcePath = Normalize(resourcePath);
            Active = active;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public string ContentId { get; }

        public FrameworkContentScope Scope { get; }

        public FrameworkContentKind Kind { get; }

        public FrameworkContentRequiredness Requiredness { get; }

        public string OwnerId { get; }

        public string OwnerName { get; }

        public string ResourceName { get; }

        public string ResourcePath { get; }

        public bool Active { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool HasContentId => !string.IsNullOrWhiteSpace(ContentId);

        public bool HasResource => !string.IsNullOrWhiteSpace(ResourceName) || !string.IsNullOrWhiteSpace(ResourcePath);

        public string ToDiagnosticString()
        {
            var resource = !string.IsNullOrWhiteSpace(ResourceName) ? ResourceName : ResourcePath;
            if (string.IsNullOrWhiteSpace(resource))
            {
                resource = "<none>";
            }

            return $"id='{ContentId}' scope='{Scope}' kind='{Kind}' requiredness='{Requiredness}' owner='{OwnerName}' resource='{resource}' active='{Active}'";
        }

        public static FrameworkContentHandle RoutePrimaryScene(
            string ownerId,
            string ownerName,
            string sceneName,
            string scenePath,
            bool active,
            string source,
            string reason,
            string message)
        {
            var normalizedOwnerId = Normalize(ownerId);
            var normalizedSceneName = Normalize(sceneName);
            var contentId = !string.IsNullOrWhiteSpace(normalizedOwnerId) && !string.IsNullOrWhiteSpace(normalizedSceneName)
                ? $"route:{normalizedOwnerId}:primary-scene:{normalizedSceneName}"
                : $"route:primary-scene:{Guid.NewGuid():N}";

            return new FrameworkContentHandle(
                contentId,
                FrameworkContentScope.Route,
                FrameworkContentKind.Scene,
                FrameworkContentRequiredness.Required,
                normalizedOwnerId,
                ownerName,
                sceneName,
                scenePath,
                active,
                source,
                reason,
                message);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
