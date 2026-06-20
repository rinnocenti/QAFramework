using UnityEngine;
using UnityEngine.Events;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Scene-authored UnityEvent bridge for Activity content lifecycle.
    /// Use this under an ActivityContentBinding root when no custom gameplay script is needed.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Activity Content Lifecycle Events")]
    public sealed class ActivityContentLifecycleEvents : ActivityContentBehaviour
    {
        [Header("Activity Content Lifecycle")]
        [SerializeField] private UnityEvent entered = new UnityEvent();
        [SerializeField] private UnityEvent exited = new UnityEvent();

        public UnityEvent Entered => entered;

        public UnityEvent Exited => exited;

        protected override void OnActivityContentEntered(ActivityContentLifecycleContext context)
        {
            entered?.Invoke();
        }

        protected override void OnActivityContentExited(ActivityContentLifecycleContext context)
        {
            exited?.Invoke();
        }
    }
}
