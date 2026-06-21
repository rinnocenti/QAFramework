using UnityEngine;
using UnityEngine.Events;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// API status: Deferred until F3. This type is retained as frozen transitional source, not as active baseline API.
    /// Scene-authored UnityEvent bridge for Route content lifecycle.
    /// Use this under a RouteContentBinding root when no custom gameplay script is needed.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public sealed class RouteContentLifecycleEvents : RouteContentBehaviour
    {
        [Header("Route Content Lifecycle")]
        [SerializeField] private UnityEvent entered = new UnityEvent();
        [SerializeField] private UnityEvent exited = new UnityEvent();

        public UnityEvent Entered => entered;

        public UnityEvent Exited => exited;

        protected override void OnRouteContentEntered(RouteContentLifecycleContext context)
        {
            entered?.Invoke();
        }

        protected override void OnRouteContentExited(RouteContentLifecycleContext context)
        {
            exited?.Invoke();
        }
    }
}
