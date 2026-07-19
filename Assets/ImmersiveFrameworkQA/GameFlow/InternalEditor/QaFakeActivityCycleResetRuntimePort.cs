using System.Threading.Tasks;
using Immersive.Framework.CycleReset;
namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor.ImmersiveFrameworkQA.GameFlow.InternalEditor
{
    internal sealed class QaFakeActivityCycleResetRuntimePort : IActivityCycleResetRuntimePort
    {
        internal CycleResetResult Result { get; set; }
        internal int CallCount { get; private set; }
        internal string LastSource { get; private set; }
        internal string LastReason { get; private set; }
        public Task<CycleResetResult> RequestActivityCycleResetAsync(string source, string reason) { CallCount++; LastSource=source; LastReason=reason; return Task.FromResult(Result); }
    }
}
