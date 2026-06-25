using System;
using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.ObjectEntry
{
    /// <summary>
    /// API status: Experimental. Passive scene-authored declaration for a logical object entry.
    /// It does not bind a Unity GameObject as a runtime object, reset anything, spawn anything or create Player/Actor semantics.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Object Entry/Object Entry Declaration")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F13C passive scene-authored declaration for Object Entry.")]
    public sealed class ObjectEntryDeclaration : MonoBehaviour
    {
        [Header("Object Entry")]
        [SerializeField] private string objectEntryId;
        [SerializeField] private ObjectEntryScope scope = ObjectEntryScope.Activity;
        [SerializeField] private ObjectEntryRequiredness requiredness = ObjectEntryRequiredness.Required;
        [SerializeField] private string displayName;

        public string ObjectEntryIdText => objectEntryId;

        public ObjectEntryScope Scope => scope;

        public ObjectEntryRequiredness Requiredness => requiredness;

        public string DisplayName => displayName;

        public bool HasObjectEntryId => !string.IsNullOrWhiteSpace(objectEntryId);

        public bool TryCreateDescriptor(out ObjectEntryDescriptor descriptor, out string issue)
        {
            descriptor = default;
            issue = string.Empty;

            if (string.IsNullOrWhiteSpace(objectEntryId))
            {
                issue = "Missing Object Entry Id.";
                return false;
            }

            if (!Enum.IsDefined(typeof(ObjectEntryScope), scope) || scope == ObjectEntryScope.Unspecified)
            {
                issue = "Object Entry Scope must be explicit.";
                return false;
            }

            if (!Enum.IsDefined(typeof(ObjectEntryRequiredness), requiredness) || requiredness == ObjectEntryRequiredness.Unspecified)
            {
                issue = "Object Entry Requiredness must be explicit.";
                return false;
            }

            try
            {
                descriptor = new ObjectEntryDescriptor(
                    ObjectEntryId.From(objectEntryId.Trim()),
                    scope,
                    ObjectEntrySourceKind.SceneAuthored,
                    requiredness,
                    ResolveDisplayName());
                return true;
            }
            catch (Exception exception) when (exception is ArgumentException || exception is ArgumentOutOfRangeException)
            {
                issue = exception.Message;
                return false;
            }
        }

        public ObjectEntryDescriptor CreateDescriptor()
        {
            if (TryCreateDescriptor(out var descriptor, out var issue))
            {
                return descriptor;
            }

            throw new InvalidOperationException(issue);
        }

        private string ResolveDisplayName()
        {
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return displayName.Trim();
            }

            return gameObject != null && !string.IsNullOrWhiteSpace(gameObject.name)
                ? gameObject.name.Trim()
                : objectEntryId.Trim();
        }
    }
}
