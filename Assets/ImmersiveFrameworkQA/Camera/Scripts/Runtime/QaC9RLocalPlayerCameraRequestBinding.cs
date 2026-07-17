using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using UnityEngine;

namespace ImmersiveFrameworkQA.Camera
{
    /// <summary>
    /// QA-only synthetic LocalPlayer request source used to prove Camera arbitration.
    /// It intentionally does not prove Player admission, Slot allocation or Actor readiness.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class QaC9RLocalPlayerCameraRequestBinding :
        MonoBehaviour,
        ICameraOutputSessionConsumer
    {
        [SerializeField] private string ownerId;
        [SerializeField] private string eligibilityScopeId;
        [SerializeField] private string requestId;
        [SerializeField] private CameraOutputSessionBinding outputSession;
        [SerializeField] private CameraRigComposer rigComposer;
        [SerializeField] private int precedence = 50;
        [SerializeField] private string tieBreakerId;
        [SerializeField] private bool eligibleOnEnable = true;
        [SerializeField] private bool releaseOnDisable = true;
        [SerializeField] private bool logDiagnostics = true;
        [SerializeField] private bool isLocallyEligible;
        [SerializeField] private string lastStatus = "NotEligible";
        [SerializeField] private string lastDiagnostic;

        private LocalPlayerCameraRequestPublisher publisher;

        public string EligibilityScopeId => Normalize(eligibilityScopeId);
        public string RequestIdText => Normalize(requestId);
        public bool IsLocallyEligible => isLocallyEligible;
        public bool IsPublished => publisher != null && publisher.IsPublished;
        public string LastStatus => Normalize(lastStatus);
        public string LastDiagnostic => Normalize(lastDiagnostic);

        private void OnEnable()
        {
            if (eligibleOnEnable)
            {
                SetEligible(true);
            }
        }

        private void OnDisable()
        {
            if (releaseOnDisable)
            {
                SetEligible(false);
            }
        }

        public bool SetEligible(bool eligible)
        {
            return eligible ? TryPublish() : TryRelease();
        }

        public bool TryPublish()
        {
            if (publisher != null && publisher.IsPublished)
            {
                isLocallyEligible = true;
                SetDiagnostic("Preserved", "Synthetic Player request is already published.", false);
                return true;
            }

            if (!TryValidate(out string issue))
            {
                isLocallyEligible = false;
                SetDiagnostic("Blocked", issue, true);
                return false;
            }

            if (outputSession == null)
            {
                isLocallyEligible = true;
                SetDiagnostic("AwaitingOutputSession", "Synthetic Player request is waiting for output injection.", false);
                return true;
            }

            if (!outputSession.TryGetSession(out CameraOutputSession session, out issue))
            {
                isLocallyEligible = false;
                SetDiagnostic("Blocked", issue, true);
                return false;
            }

            CameraTargetResolveResult targets = rigComposer.ResolveCameraTargets(
                rigComposer.FollowRequirement,
                rigComposer.LookAtRequirement);
            if (!targets.IsSucceeded)
            {
                isLocallyEligible = false;
                SetDiagnostic("Blocked", targets.BlockingIssue, true);
                return false;
            }

            CameraRequestCreateResult requestResult =
                CameraRequestCreateResult.Create(
                    new CameraRequestId(RequestIdText),
                    session.OutputId,
                    new CameraRequestOwner(
                        CameraRequestOwnerKind.LocalPlayer,
                        Normalize(ownerId)),
                    new CameraRequestLifetime(
                        CameraRequestLifetimeKind.LocalPlayerEligibility,
                        EligibilityScopeId),
                    CameraRigReference.FromComposer(rigComposer),
                    targets.Source,
                    new CameraRequestPolicy(
                        precedence,
                        Normalize(tieBreakerId)),
                    CameraRequestReleaseCondition.ExplicitRelease,
                    nameof(QaC9RLocalPlayerCameraRequestBinding),
                    "QA-only synthetic LocalPlayer request for Camera arbitration.");
            if (!requestResult.IsSucceeded)
            {
                isLocallyEligible = false;
                SetDiagnostic("Blocked", requestResult.BlockingIssue, true);
                return false;
            }

            CameraRequestPublisherCreateResult creation =
                LocalPlayerCameraRequestPublisher.Create(
                    session,
                    requestResult.Request);
            if (!creation.Succeeded)
            {
                isLocallyEligible = false;
                SetDiagnostic("Blocked", creation.DiagnosticSummary, true);
                return false;
            }

            publisher = creation.Publisher as LocalPlayerCameraRequestPublisher;
            if (publisher == null)
            {
                isLocallyEligible = false;
                SetDiagnostic("Blocked", "Unexpected synthetic publisher type.", true);
                return false;
            }

            CameraRequestPublisherResult published = publisher.Publish();
            if (!published.Succeeded)
            {
                publisher = null;
                isLocallyEligible = false;
                SetDiagnostic("Blocked", published.DiagnosticSummary, true);
                return false;
            }

            isLocallyEligible = true;
            SetDiagnostic("Published", "Synthetic Player request published.", false);
            return true;
        }

        public bool TryRelease()
        {
            if (publisher == null)
            {
                isLocallyEligible = false;
                SetDiagnostic("Preserved", "Synthetic Player request is already released.", false);
                return true;
            }

            CameraRequestPublisherResult released = publisher.Release();
            if (!released.Succeeded)
            {
                SetDiagnostic("Blocked", released.DiagnosticSummary, true);
                return false;
            }

            publisher = null;
            isLocallyEligible = false;
            SetDiagnostic("Released", "Synthetic Player request released.", false);
            return true;
        }

        private bool TryValidate(out string issue)
        {
            if (string.IsNullOrEmpty(Normalize(ownerId)))
            {
                issue = "Synthetic Player request requires ownerId.";
                return false;
            }
            if (string.IsNullOrEmpty(EligibilityScopeId))
            {
                issue = "Synthetic Player request requires eligibilityScopeId.";
                return false;
            }
            if (string.IsNullOrEmpty(RequestIdText))
            {
                issue = "Synthetic Player request requires requestId.";
                return false;
            }
            if (string.IsNullOrEmpty(Normalize(tieBreakerId)))
            {
                issue = "Synthetic Player request requires tieBreakerId.";
                return false;
            }
            if (rigComposer == null || rigComposer.TargetSource == null)
            {
                issue = "Synthetic Player request requires a typed CameraRigComposer target source.";
                return false;
            }

            CameraTargetResolveResult targets = rigComposer.ResolveCameraTargets(
                rigComposer.FollowRequirement,
                rigComposer.LookAtRequirement);
            if (!targets.IsSucceeded)
            {
                issue = targets.BlockingIssue;
                return false;
            }

            issue = string.Empty;
            return true;
        }

        void ICameraOutputSessionConsumer.AttachOutputSession(
            CameraOutputSessionBinding binding)
        {
            outputSession = binding;
            if (binding == null)
            {
                SetDiagnostic("Blocked", "Synthetic Player output injection is missing.", true);
                return;
            }

            if (isLocallyEligible && !IsPublished)
            {
                TryPublish();
            }
        }

        void ICameraOutputSessionConsumer.DetachOutputSession(string reason)
        {
            TryRelease();
            outputSession = null;
            SetDiagnostic("OutputDetached", $"Output detached. reason='{Normalize(reason)}'.", false);
        }

        private void SetDiagnostic(string status, string diagnostic, bool error)
        {
            lastStatus = Normalize(status);
            lastDiagnostic = Normalize(diagnostic);
            if (!logDiagnostics)
            {
                return;
            }

            string message =
                $"[QA][C9R Synthetic Local Player Camera] status='{lastStatus}' diagnostic='{lastDiagnostic}'.";
            if (error)
            {
                Debug.LogError(message, this);
            }
            else
            {
                Debug.Log(message, this);
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
        }
    }
}
