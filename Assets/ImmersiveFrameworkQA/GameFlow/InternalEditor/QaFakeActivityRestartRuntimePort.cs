using System.Threading.Tasks;
using Immersive.Framework.ActivityRestart;
using Immersive.Framework.Authoring;
using Immersive.Framework.Reset;
namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    internal sealed class QaFakeActivityRestartRuntimePort : IActivityRestartRuntimePort
    {
        internal ActivityRestartResult Result { get; set; }
        internal int CallCount { get; private set; }
        internal ActivityAsset LastTargetActivity { get; private set; }
        internal bool LastUseCurrentActivityWhenTargetMissing { get; private set; }
        internal bool LastRequireTargetActivityIsCurrent { get; private set; }
        internal ResetSelectionConfig LastResetSelection { get; private set; }
        internal string LastSource { get; private set; }
        internal string LastReason { get; private set; }

        public Task<ActivityRestartRuntimeResult> RequestActivityRestartAsync(ActivityAsset targetActivity, bool useCurrentActivityWhenTargetMissing, bool requireTargetActivityIsCurrent, ResetSelectionConfig resetSelection, string source, string reason)
        {
            CallCount++;
            LastTargetActivity = targetActivity;
            LastUseCurrentActivityWhenTargetMissing = useCurrentActivityWhenTargetMissing;
            LastRequireTargetActivityIsCurrent = requireTargetActivityIsCurrent;
            LastResetSelection = resetSelection;
            LastSource = source;
            LastReason = reason;
            return Task.FromResult(ActivityRestartRuntimeResult.From(Result));
        }
    }
}
