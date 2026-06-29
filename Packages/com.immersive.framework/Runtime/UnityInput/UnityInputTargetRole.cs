using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.UnityInput
{
    /// <summary>
    /// API status: Experimental. Canonical role for an explicitly declared Unity Input target.
    /// This enum does not represent Unity action maps, controls, devices, player actors or gameplay commands.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F29A Unity Input target role vocabulary; declaration only.")]
    public enum UnityInputTargetRole
    {
        Unknown = 0,

        /// <summary>
        /// Input target responsible for global UI and Pause intent continuity.
        /// It keeps PauseToggle, Cancel and UI navigation conceptually separate from gameplay commands.
        /// </summary>
        GlobalUiPause = 10,

        /// <summary>
        /// Input target that may later drive gameplay commands after InputMode and Activity ownership exist.
        /// </summary>
        GameplayCommands = 20
    }
}
