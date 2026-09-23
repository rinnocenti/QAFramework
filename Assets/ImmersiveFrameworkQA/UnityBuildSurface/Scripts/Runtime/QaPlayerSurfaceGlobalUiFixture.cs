using Immersive.Framework.PlayerParticipation;
using UnityEngine;

namespace ImmersiveFrameworkQA.UnityBuildSurface
{
    /// <summary>
    /// Scene-local QA evidence for the persistent UIGlobal provisioning host and
    /// loading surface. Route-owned Player commands deliberately do not live
    /// here because this composition is retained as DontDestroyOnLoad.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu(
        "Immersive Framework QA/Player Surface/Global UI Fixture")]
    public sealed class QaPlayerSurfaceGlobalUiFixture : MonoBehaviour
    {
        [SerializeField] private LocalPlayerProvisioningAuthoring provisioningAuthoring;
        [SerializeField]
        private QaLoadingSurfaceVisibilityHoldAdapter loadingSurface;

        public LocalPlayerProvisioningAuthoring ProvisioningAuthoring => provisioningAuthoring;
        public QaLoadingSurfaceVisibilityHoldAdapter LoadingSurface =>
            loadingSurface;

        public void Configure(
            LocalPlayerProvisioningAuthoring authoredProvisioning,
            QaLoadingSurfaceVisibilityHoldAdapter authoredLoadingSurface)
        {
            provisioningAuthoring = authoredProvisioning;
            loadingSurface = authoredLoadingSurface;
        }

        public bool TryValidateAuthoredSurface(out string issue)
        {
            if (provisioningAuthoring == null ||
                provisioningAuthoring.gameObject != gameObject ||
                provisioningAuthoring.gameObject.scene != gameObject.scene)
            {
                issue =
                    "UIGlobal QA fixture requires Local Player Provisioning authoring on the same GameObject.";
                return false;
            }

            if (loadingSurface == null ||
                loadingSurface.gameObject.scene != gameObject.scene)
            {
                issue =
                    "UIGlobal QA fixture requires its scene-local Loading Surface adapter.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

    }
}
