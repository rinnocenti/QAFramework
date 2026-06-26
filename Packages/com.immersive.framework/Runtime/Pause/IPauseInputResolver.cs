using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Resolves normalized Pause input signals into framework-facing Pause intent.
    /// Implementations must not poll concrete devices, own Input System assets, mutate Pause state, change Time.timeScale,
    /// show UI, execute gameplay commands or become gameplay adapters.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F23D Pause Input resolver boundary; no concrete input package binding.")]
    public interface IPauseInputResolver
    {
        /// <summary>Human-readable resolver name for diagnostics.</summary>
        string ResolverName { get; }

        /// <summary>Returns true when this resolver can process the supplied normalized Pause input signal.</summary>
        bool Supports(PauseInputSignal signal);

        /// <summary>Resolves a normalized signal into a Pause request, menu command, ignore/reject/failure result.</summary>
        PauseInputResolutionResult Resolve(PauseInputSignal signal, string requestId);
    }
}
