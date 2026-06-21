using Immersive.Framework.Authoring;
using UnityEngine;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// API status: Deferred until F3. This type is retained as frozen transitional source, not as active baseline API.
    /// Optional base class for MonoBehaviours under a Route Content Binding root.
    /// Prefer this for scene-authored Route content that needs lifecycle callbacks
    /// without manually implementing IRouteContentLifecycleReceiver.
    /// </summary>
    public abstract class RouteContentBehaviour : MonoBehaviour, IRouteContentLifecycleReceiver
    {
        public bool IsRouteContentActive { get; private set; }

        public bool HasRouteContentContext { get; private set; }

        public RouteContentLifecycleContext LastRouteContentContext { get; private set; }

        public RouteAsset ActiveRoute => IsRouteContentActive
            ? LastRouteContentContext.Route
            : null;

        public RouteContentBinding RouteContentBinding => LastRouteContentContext.Binding;

        public string LifecycleSource => HasRouteContentContext
            ? LastRouteContentContext.Source
            : string.Empty;

        public string LifecycleReason => HasRouteContentContext
            ? LastRouteContentContext.Reason
            : string.Empty;

        void IRouteContentLifecycleReceiver.OnRouteContentEntered(RouteContentLifecycleContext context)
        {
            HasRouteContentContext = true;
            LastRouteContentContext = context;
            IsRouteContentActive = true;

            OnRouteContentEntered(context);
        }

        void IRouteContentLifecycleReceiver.OnRouteContentExited(RouteContentLifecycleContext context)
        {
            HasRouteContentContext = true;
            LastRouteContentContext = context;
            IsRouteContentActive = false;

            OnRouteContentExited(context);
        }

        /// <summary>
        /// Called after this Route content root has entered the active Route scope.
        /// </summary>
        protected virtual void OnRouteContentEntered(RouteContentLifecycleContext context)
        {
        }

        /// <summary>
        /// Called before this Route content root leaves the active Route scope.
        /// </summary>
        protected virtual void OnRouteContentExited(RouteContentLifecycleContext context)
        {
        }
    }
}
