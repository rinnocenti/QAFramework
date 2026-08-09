using Immersive.Framework.PlayerParticipation;
using UnityEngine;

namespace ImmersiveFrameworkQA.UnityBuildSurface
{
    /// <summary>
    /// Scene-local QA evidence for the public Actor Selection authoring that
    /// belongs to the persistent UIGlobal composition. This fixture exposes no
    /// Player authority and is intentionally authored on the same GameObject as
    /// the referenced public authoring.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu(
        "Immersive Framework QA/Player Surface/Global UI Fixture")]
    public sealed class QaPlayerSurfaceGlobalUiFixture : MonoBehaviour
    {
        [SerializeField]
        private LocalPlayerActorSelectionRequestAuthoring
            actorSelectionRequestAuthoring;

        public LocalPlayerActorSelectionRequestAuthoring
            ActorSelectionRequestAuthoring => actorSelectionRequestAuthoring;

        public void Configure(
            LocalPlayerActorSelectionRequestAuthoring actorSelectionAuthoring)
        {
            actorSelectionRequestAuthoring = actorSelectionAuthoring;
        }

        public bool TryValidateAuthoredSurface(out string issue)
        {
            if (actorSelectionRequestAuthoring == null)
            {
                issue =
                    "UIGlobal QA fixture is missing Local Player Actor Selection Request authoring.";
                return false;
            }

            if (actorSelectionRequestAuthoring.gameObject != gameObject)
            {
                issue =
                    "UIGlobal QA fixture must reference Actor Selection authoring on the same GameObject.";
                return false;
            }

            if (actorSelectionRequestAuthoring.gameObject.scene != gameObject.scene)
            {
                issue =
                    "UIGlobal QA fixture references Actor Selection authoring from another Scene.";
                return false;
            }

            if (!actorSelectionRequestAuthoring.TryValidateConfiguration(
                    out string actorSelectionIssue))
            {
                issue =
                    "UIGlobal QA fixture references an invalid Actor Selection authoring. " +
                    actorSelectionIssue;
                return false;
            }

            issue = string.Empty;
            return true;
        }
    }
}
