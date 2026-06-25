using System;
using System.Collections.Generic;
using System.Linq;
using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.ObjectEntry
{
    /// <summary>
    /// API status: Experimental. Passive source that converts scene-authored ObjectEntryDeclaration components into an ObjectEntrySet.
    /// It does not bind GameObjects, materialize prefabs, spawn actors, register services, perform reset or create lifecycle authority.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Passive Object Entry declaration source introduced by F13D.")]
    public sealed class ObjectEntryDeclarationSource
    {
        public ObjectEntryDeclarationSource(bool includeInactiveDeclarations = true)
        {
            IncludeInactiveDeclarations = includeInactiveDeclarations;
        }

        public bool IncludeInactiveDeclarations { get; }

        public ObjectEntryDeclarationSourceResult CollectLoadedSceneDeclarations()
        {
            var declarations = UnityEngine.Object.FindObjectsByType<ObjectEntryDeclaration>(
                IncludeInactiveDeclarations ? FindObjectsInactive.Include : FindObjectsInactive.Exclude);
            return Collect(declarations, "loaded-scenes");
        }

        public ObjectEntryDeclarationSourceResult Collect(
            IEnumerable<ObjectEntryDeclaration> declarations,
            string source = null)
        {
            var materializedDeclarations = declarations == null
                ? Array.Empty<ObjectEntryDeclaration>()
                : declarations.Where(declaration => declaration != null).ToArray();

            var descriptors = new List<ObjectEntryDescriptor>(materializedDeclarations.Length);
            var issues = new List<ObjectEntryIssue>();
            int rejected = 0;

            for (int i = 0; i < materializedDeclarations.Length; i++)
            {
                var declaration = materializedDeclarations[i];
                if (!declaration.TryCreateDescriptor(out var descriptor, out var issue))
                {
                    rejected++;
                    issues.Add(ObjectEntryIssue.Error(
                        ObjectEntryIssueKind.InvalidRequest,
                        FormatDeclarationIssue(declaration, issue)));
                    continue;
                }

                descriptors.Add(descriptor);
            }

            ObjectEntrySet set;
            try
            {
                set = new ObjectEntrySet(descriptors);
            }
            catch (ArgumentException exception)
            {
                set = ObjectEntrySet.Empty();
                issues.Add(ObjectEntryIssue.Error(
                    ObjectEntryIssueKind.DuplicateIdentity,
                    $"Object Entry declaration source '{ResolveSource(source)}' rejected duplicate identity. {exception.Message}"));
            }

            return new ObjectEntryDeclarationSourceResult(
                set,
                materializedDeclarations.Length,
                descriptors.Count,
                rejected,
                issues);
        }

        private static string FormatDeclarationIssue(ObjectEntryDeclaration declaration, string issue)
        {
            string objectName = declaration != null && declaration.gameObject != null
                ? declaration.gameObject.name
                : "<missing>";
            string sceneName = declaration != null && declaration.gameObject != null && declaration.gameObject.scene.IsValid()
                ? declaration.gameObject.scene.name
                : "<no-scene>";
            string message = string.IsNullOrWhiteSpace(issue) ? "Invalid Object Entry declaration." : issue.Trim();
            return $"ObjectEntryDeclaration object='{objectName}' scene='{sceneName}' issue='{message}'.";
        }

        private static string ResolveSource(string source)
        {
            return string.IsNullOrWhiteSpace(source) ? nameof(ObjectEntryDeclarationSource) : source.Trim();
        }
    }
}
