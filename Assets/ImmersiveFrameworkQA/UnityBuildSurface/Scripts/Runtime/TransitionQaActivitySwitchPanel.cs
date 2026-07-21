using Immersive.Framework.GameFlow;
using UnityEngine;

namespace ImmersiveFrameworkQA.UnityBuildSurface
{
    /// <summary>
    /// QA-only IMGUI panel used by Unity Build Surface fixtures to request activity switches and clear activity.
    /// This component is not a transition surface, not gameplay UI and not part of the framework product API.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Unity Build Surface/Transition QA Activity Switch Panel")]
    public sealed class TransitionQaActivitySwitchPanel : MonoBehaviour
    {
        [Header("QA Targets")]
        [SerializeField] private ActivityRequestTrigger primaryActivityTrigger;
        [SerializeField] private ActivityRequestTrigger alternateActivityTrigger;
        [SerializeField] private ActivityRequestTrigger clearActivityTrigger;

        [Header("Panel")]
        [SerializeField] private string title = "Transition Activity QA";
        [SerializeField] private string primaryButtonLabel = "Request Primary Activity";
        [SerializeField] private string alternateButtonLabel = "Request Alternate Activity";
        [SerializeField] private string clearButtonLabel = "Clear Activity";
        [SerializeField] private bool showPanel = true;
        [SerializeField] private Rect panelRect = new Rect(16f, 150f, 420f, 190f);

        public ActivityRequestTrigger PrimaryActivityTrigger
        {
            get => primaryActivityTrigger;
            set => primaryActivityTrigger = value;
        }

        public ActivityRequestTrigger AlternateActivityTrigger
        {
            get => alternateActivityTrigger;
            set => alternateActivityTrigger = value;
        }

        public ActivityRequestTrigger ClearActivityTrigger
        {
            get => clearActivityTrigger;
            set => clearActivityTrigger = value;
        }

        public void Configure(
            ActivityRequestTrigger primaryTrigger,
            ActivityRequestTrigger alternateTrigger,
            ActivityRequestTrigger clearTrigger,
            string nextTitle,
            string nextPrimaryButtonLabel,
            string nextAlternateButtonLabel,
            string nextClearButtonLabel)
        {
            primaryActivityTrigger = primaryTrigger;
            alternateActivityTrigger = alternateTrigger;
            clearActivityTrigger = clearTrigger;
            title = string.IsNullOrWhiteSpace(nextTitle) ? "Transition Activity QA" : nextTitle;
            primaryButtonLabel = string.IsNullOrWhiteSpace(nextPrimaryButtonLabel) ? "Request Primary Activity" : nextPrimaryButtonLabel;
            alternateButtonLabel = string.IsNullOrWhiteSpace(nextAlternateButtonLabel) ? "Request Alternate Activity" : nextAlternateButtonLabel;
            clearButtonLabel = string.IsNullOrWhiteSpace(nextClearButtonLabel) ? "Clear Activity" : nextClearButtonLabel;
        }

        public void RequestPrimaryActivity()
        {
            RequestActivity(primaryActivityTrigger, "primary");
        }

        public void RequestAlternateActivity()
        {
            RequestActivity(alternateActivityTrigger, "alternate");
        }

        public void ClearActivity()
        {
            if (clearActivityTrigger == null)
            {
                Debug.LogWarning("[Immersive Framework QA] Transition QA activity clear failed. ActivityRequestTrigger is missing.", this);
                return;
            }

            clearActivityTrigger.ClearActivity();
        }

        private void RequestActivity(ActivityRequestTrigger trigger, string label)
        {
            if (trigger == null)
            {
                Debug.LogWarning($"[Immersive Framework QA] Transition QA activity request failed. {label} ActivityRequestTrigger is missing.", this);
                return;
            }

            trigger.RequestActivity();
        }

        private void OnGUI()
        {
            if (!showPanel)
            {
                return;
            }

            panelRect = ClampToScreen(GUI.Window(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this), panelRect, DrawWindow, title));
        }

        private void DrawWindow(int windowId)
        {
            DrawButton(alternateActivityTrigger, alternateButtonLabel, RequestAlternateActivity);
            DrawButton(primaryActivityTrigger, primaryButtonLabel, RequestPrimaryActivity);
            DrawButton(clearActivityTrigger, clearButtonLabel, ClearActivity);

            GUI.enabled = true;
            DrawStatus("Alternate", alternateActivityTrigger);
            DrawStatus("Primary", primaryActivityTrigger);
            DrawStatus("Clear", clearActivityTrigger);

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }

        private static Rect ClampToScreen(Rect rect)
        {
            float width = Mathf.Max(280f, rect.width);
            float height = Mathf.Max(150f, rect.height);
            float maxX = Mathf.Max(0f, Screen.width - width);
            float maxY = Mathf.Max(0f, Screen.height - height);
            return new Rect(Mathf.Clamp(rect.x, 0f, maxX), Mathf.Clamp(rect.y, 0f, maxY), width, height);
        }

        private static void DrawButton(ActivityRequestTrigger trigger, string label, System.Action action)
        {
            GUI.enabled = trigger != null && !trigger.IsRequestInFlight;
            if (GUILayout.Button(label, GUILayout.Height(30f)))
            {
                action?.Invoke();
            }
        }

        private static void DrawStatus(string label, ActivityRequestTrigger trigger)
        {
            if (trigger == null)
            {
                GUILayout.Label($"{label}: Missing trigger");
                return;
            }

            var suffix = trigger.LastRequestClearedActivity ? " clear" : string.Empty;
            GUILayout.Label($"{label}: {trigger.LastOutcome}{suffix}");
        }
    }
}
