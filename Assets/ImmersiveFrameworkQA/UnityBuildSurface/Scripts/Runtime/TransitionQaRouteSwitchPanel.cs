using Immersive.Framework.GameFlow;
using UnityEngine;

namespace ImmersiveFrameworkQA.UnityBuildSurface
{
    /// <summary>
    /// QA-only IMGUI panel used by Unity Build Surface fixtures to request the paired transition route.
    /// This component is not a transition surface, not gameplay UI and not part of the framework product API.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Unity Build Surface/Transition QA Route Switch Panel")]
    public sealed class TransitionQaRouteSwitchPanel : MonoBehaviour
    {
        [Header("QA Target")]
        [SerializeField] private RouteRequestTrigger routeRequestTrigger;

        [Header("Panel")]
        [SerializeField] private string title = "Transition QA";
        [SerializeField] private string buttonLabel = "Request Other Route";
        [SerializeField] private bool showPanel = true;
        [SerializeField] private Rect panelRect = new Rect(16f, 16f, 360f, 120f);

        public RouteRequestTrigger RouteRequestTrigger
        {
            get => routeRequestTrigger;
            set => routeRequestTrigger = value;
        }

        public void Configure(RouteRequestTrigger trigger, string nextTitle, string nextButtonLabel)
        {
            routeRequestTrigger = trigger;
            title = string.IsNullOrWhiteSpace(nextTitle) ? "Transition QA" : nextTitle;
            buttonLabel = string.IsNullOrWhiteSpace(nextButtonLabel) ? "Request Other Route" : nextButtonLabel;
        }

        [ContextMenu("Request Target Route")]
        public void RequestTargetRoute()
        {
            if (routeRequestTrigger == null)
            {
                Debug.LogWarning("[Immersive Framework QA] Transition QA route switch failed. RouteRequestTrigger is missing.", this);
                return;
            }

            routeRequestTrigger.RequestRoute();
        }

        private void OnGUI()
        {
            if (!showPanel)
            {
                return;
            }

            GUILayout.BeginArea(panelRect, GUI.skin.box);
            GUILayout.Label(title, GUI.skin.label);

            GUI.enabled = routeRequestTrigger != null && !routeRequestTrigger.IsRequestInFlight;
            if (GUILayout.Button(buttonLabel, GUILayout.Height(36f)))
            {
                RequestTargetRoute();
            }

            GUI.enabled = true;

            if (routeRequestTrigger == null)
            {
                GUILayout.Label("RouteRequestTrigger: Missing");
            }
            else
            {
                GUILayout.Label($"Last Outcome: {routeRequestTrigger.LastOutcome}");
                if (!string.IsNullOrWhiteSpace(routeRequestTrigger.LastMessage))
                {
                    GUILayout.Label(routeRequestTrigger.LastMessage);
                }
            }

            GUILayout.EndArea();
        }
    }
}
