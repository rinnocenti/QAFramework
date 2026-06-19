namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Implement this on a component under a Route Content Binding root when scene-authored content
    /// needs to react to Route enter/exit without owning Route Lifecycle.
    /// </summary>
    public interface IRouteContentLifecycleReceiver
    {
        void OnRouteContentEntered(RouteContentLifecycleContext context);

        void OnRouteContentExited(RouteContentLifecycleContext context);
    }
}
