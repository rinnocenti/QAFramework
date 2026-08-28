using Immersive.Framework.PlayerParticipation;

namespace ImmersiveFrameworkQA.Player
{
    /// <summary>
    /// QA-only scoped Player Session consumer. It receives the official
    /// Framework-injected access port and exposes no extra behaviour.
    /// </summary>
    public sealed class PlayerQaScopedAccessProbe : PlayerSessionScopedAccessConsumer
    {
    }
}
