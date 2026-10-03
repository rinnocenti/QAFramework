using Immersive.Audio.Authoring;
using Immersive.Framework.Audio;
using UnityEngine;

namespace Immersive.QaFramework.New005
{
    [CreateAssetMenu(menuName = "QA Framework/QA-NEW-005 Audio Evidence", fileName = "QaNew005AudioEvidence")]
    public sealed class QaNew005AudioEvidence : ScriptableObject
    {
        public int RouteContentEnteredCount { get; private set; }
        public int RouteContentExitedCount { get; private set; }
        public int RouteContentDetachedCount { get; private set; }
        public int RouteConsumersBound { get; private set; }
        public int RouteConsumersDestroyedWhileBound { get; private set; }
        public bool RouteDirectorAttached { get; private set; }
        public bool RouteContentEntered { get; private set; }
        public bool RouteContentExited { get; private set; }
        public bool RouteProviderConfirmed { get; private set; }
        public bool RouteLastOperationObserved { get; private set; }
        public FrameworkBgmOperationOutcome RouteLastOperationOutcome { get; private set; }
        public string LastRouteOperationResult { get; private set; } = "NotObserved";

        public int ActivityContentEnteredCount { get; private set; }
        public int ActivityContentExitedCount { get; private set; }
        public int ActivityContentDetachedCount { get; private set; }
        public int ActivityConsumersBound { get; private set; }
        public int ActivityConsumersDestroyedWhileBound { get; private set; }
        public bool ActivityDirectorAttached { get; private set; }
        public bool ActivityContentEntered { get; private set; }
        public bool ActivityContentExited { get; private set; }
        public bool ActivityProviderConfirmed { get; private set; }
        public bool ActivityLastOperationObserved { get; private set; }
        public FrameworkBgmOperationOutcome ActivityLastOperationOutcome { get; private set; }
        public string LastActivityOperationResult { get; private set; } = "NotObserved";

        public void ResetEvidence()
        {
            RouteContentEnteredCount = RouteContentExitedCount = RouteContentDetachedCount = 0;
            RouteConsumersBound = RouteConsumersDestroyedWhileBound = 0;
            RouteDirectorAttached = RouteContentEntered = RouteContentExited = RouteProviderConfirmed = false;
            RouteLastOperationObserved = false;
            RouteLastOperationOutcome = default;
            LastRouteOperationResult = "NotObserved";
            ActivityContentEnteredCount = ActivityContentExitedCount = ActivityContentDetachedCount = 0;
            ActivityConsumersBound = ActivityConsumersDestroyedWhileBound = 0;
            ActivityDirectorAttached = ActivityContentEntered = ActivityContentExited = ActivityProviderConfirmed = false;
            ActivityLastOperationObserved = false;
            ActivityLastOperationOutcome = default;
            LastActivityOperationResult = "NotObserved";
        }

        public bool RecordRouteEnter(RouteBgmAuthoring authoring)
        {
            RouteContentEnteredCount++;
            RouteContentEntered = true;
            RouteDirectorAttached = authoring != null && authoring.Director != null;
            if (RouteDirectorAttached) RouteConsumersBound++;
            FrameworkBgmOperationResult result = authoring != null ? authoring.LastOperationResult : default;
            bool operationObserved = authoring != null && authoring.HasRouteContentContext && authoring.IsRouteContentActive;
            RouteLastOperationObserved = operationObserved;
            RouteLastOperationOutcome = result.Outcome;
            RouteProviderConfirmed = IsProviderConfirmed(result, authoring != null ? authoring.Director : null, operationObserved);
            LastRouteOperationResult = FormatOperation(result, operationObserved);
            return RouteDirectorAttached;
        }

        public void RecordRouteExit()
        {
            RouteContentExitedCount++;
            RouteContentExited = true;
        }

        public void RecordRouteDetached(bool wasCountedBound)
        {
            if (!wasCountedBound) return;
            RouteContentDetachedCount++;
            RouteConsumersBound = Mathf.Max(0, RouteConsumersBound - 1);
        }

        public void RecordRouteDestroyedWhileBound(bool wasCountedBound)
        {
            if (!wasCountedBound) return;
            RouteConsumersDestroyedWhileBound++;
            RouteConsumersBound = Mathf.Max(0, RouteConsumersBound - 1);
        }

        public bool RecordActivityEnter(ActivityBgmAuthoring authoring)
        {
            ActivityContentEnteredCount++;
            ActivityContentEntered = true;
            ActivityDirectorAttached = authoring != null && authoring.Director != null;
            if (ActivityDirectorAttached) ActivityConsumersBound++;
            FrameworkBgmOperationResult result = authoring != null ? authoring.LastOperationResult : default;
            bool operationObserved = authoring != null && authoring.HasActivityContentContext && authoring.IsActivityContentActive;
            ActivityLastOperationObserved = operationObserved;
            ActivityLastOperationOutcome = result.Outcome;
            ActivityProviderConfirmed = IsProviderConfirmed(result, authoring != null ? authoring.Director : null, operationObserved);
            LastActivityOperationResult = FormatOperation(result, operationObserved);
            return ActivityDirectorAttached;
        }

        public void RecordActivityExit()
        {
            ActivityContentExitedCount++;
            ActivityContentExited = true;
        }

        public void RecordActivityDetached(bool wasCountedBound)
        {
            if (!wasCountedBound) return;
            ActivityContentDetachedCount++;
            ActivityConsumersBound = Mathf.Max(0, ActivityConsumersBound - 1);
        }

        public void RecordActivityDestroyedWhileBound(bool wasCountedBound)
        {
            if (!wasCountedBound) return;
            ActivityConsumersDestroyedWhileBound++;
            ActivityConsumersBound = Mathf.Max(0, ActivityConsumersBound - 1);
        }

        private static bool IsProviderConfirmed(
            FrameworkBgmOperationResult result,
            FrameworkBgmDirector director,
            bool operationObserved)
        {
            return operationObserved && director != null && result.IsProviderConfirmed &&
                   ReferenceEquals(result.ConfirmedCue, director.ConfirmedBgm) &&
                   result.ConfirmedExplicitSilence == director.ConfirmedExplicitSilence;
        }

        private static string FormatOperation(FrameworkBgmOperationResult result, bool observed)
        {
            if (!observed) return "NotObserved";
            return $"operation='{result.Operation}' outcome='{result.Outcome}' requestedCue='{CueId(result.RequestedCue)}' confirmedCue='{CueId(result.ConfirmedCue)}' explicitSilence='{result.ConfirmedExplicitSilence.ToString().ToLowerInvariant()}' reason='{Sanitize(result.Reason)}'";
        }

        private static string CueId(AudioBgmCueAsset cue) =>
            cue != null ? cue.CueIdValue : "<null>";

        private static string Sanitize(string value) =>
            (value ?? string.Empty).Replace("'", "\\'").Replace("\r", "\\r").Replace("\n", "\\n");
    }
}
