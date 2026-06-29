using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.UnityInput
{
    /// <summary>
    /// API status: Experimental. Unity-facing declaration that a scene/authored object is an input target.
    /// This component is declarative only: it does not enable input, switch action maps, spawn players or bind commands.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Input/Unity Input Target Declaration")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F29A Unity Input target declaration component; no InputMode behavior.")]
    public sealed class UnityInputTargetDeclaration : MonoBehaviour
    {
        [SerializeField] private UnityInputTargetRole targetRole = UnityInputTargetRole.GlobalUiPause;
        [SerializeField] private string targetId = "qa.input.target.global-ui-pause";
        [SerializeField] private string displayName = "QA Unity Input Target";
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private string reason = "unity.input.target.declaration";

        public UnityInputTargetRole TargetRole => targetRole;

        public string TargetIdText => targetId.NormalizeText();

        public string DisplayName => displayName.NormalizeTextOrFallback(name);

        public PlayerInput PlayerInput => playerInput;

        public bool HasPlayerInputReference => playerInput != null;

        public string Reason => reason.NormalizeText();

        public bool TryCreateDescriptor(
            string source,
            out UnityInputTargetDescriptor descriptor,
            out UnityInputTargetSetIssue issue)
        {
            descriptor = default;
            issue = default;
            string normalizedSource = source.NormalizeTextOrFallback(nameof(UnityInputTargetDeclaration));

            if (!Enum.IsDefined(typeof(UnityInputTargetRole), targetRole) || targetRole == UnityInputTargetRole.Unknown)
            {
                issue = UnityInputTargetSetIssue.BlockingIssue(
                    UnityInputTargetSetIssueKind.InvalidTargetRole,
                    targetRole,
                    TargetIdText,
                    normalizedSource,
                    "Unity Input target declaration has an invalid role.");
                return false;
            }

            string normalizedTargetId = targetId.NormalizeText();
            if (string.IsNullOrWhiteSpace(normalizedTargetId))
            {
                issue = UnityInputTargetSetIssue.BlockingIssue(
                    UnityInputTargetSetIssueKind.InvalidTargetId,
                    targetRole,
                    string.Empty,
                    normalizedSource,
                    "Unity Input target declaration has an empty target id.");
                return false;
            }

            try
            {
                descriptor = new UnityInputTargetDescriptor(
                    UnityInputTargetId.From(normalizedTargetId),
                    targetRole,
                    HasPlayerInputReference,
                    DisplayName,
                    gameObject.scene.IsValid() ? gameObject.scene.name : string.Empty,
                    gameObject.name,
                    normalizedSource,
                    Reason);
                return true;
            }
            catch (Exception exception)
            {
                issue = UnityInputTargetSetIssue.BlockingIssue(
                    UnityInputTargetSetIssueKind.InvalidDeclaration,
                    targetRole,
                    normalizedTargetId,
                    normalizedSource,
                    exception.Message);
                return false;
            }
        }


        internal void ConfigureForDiagnostics(
            UnityInputTargetRole role,
            string id,
            string label,
            PlayerInput inputReference,
            string declarationReason)
        {
            targetRole = role;
            targetId = id.NormalizeText();
            displayName = label.NormalizeTextOrFallback(name);
            playerInput = inputReference;
            reason = declarationReason.NormalizeText();
        }

        private void Reset()
        {
            playerInput = GetComponent<PlayerInput>();
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = name;
            }
        }
    }
}
