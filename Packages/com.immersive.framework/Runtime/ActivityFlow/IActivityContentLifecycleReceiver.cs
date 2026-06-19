namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Implement this on a component under an Activity Content Binding root when scene-authored content
    /// needs to react to Activity enter/exit without owning Activity Flow.
    /// </summary>
    public interface IActivityContentLifecycleReceiver
    {
        void OnActivityContentEntered(ActivityContentLifecycleContext context);

        void OnActivityContentExited(ActivityContentLifecycleContext context);
    }
}
