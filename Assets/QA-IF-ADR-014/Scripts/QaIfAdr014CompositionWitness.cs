using System.Collections.Generic;
using Immersive.Framework.ObjectEntry;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.QaFramework.IfAdr014
{
    public sealed class QaIfAdr014CompositionWitness : MonoBehaviour
    {
        public sealed class DestructionEvidence
        {
            internal DestructionEvidence(
                GameObject root,
                GameObject part,
                ObjectEntryDeclaration declaration,
                ObjectEntryId objectEntryId,
                bool partWasSubordinate,
                bool rootWasLive,
                bool declarationWasIntact,
                bool ownerSceneWasLoaded)
            {
                Root = root;
                Part = part;
                Declaration = declaration;
                ObjectEntryId = objectEntryId;
                PartWasSubordinate = partWasSubordinate;
                RootWasLive = rootWasLive;
                DeclarationWasIntact = declarationWasIntact;
                OwnerSceneWasLoaded = ownerSceneWasLoaded;
            }

            public GameObject Root { get; }
            public GameObject Part { get; }
            public ObjectEntryDeclaration Declaration { get; }
            public ObjectEntryId ObjectEntryId { get; }
            public bool PartWasSubordinate { get; }
            public bool PartWasDestroyed => Part == null;
            public bool RootWasLive { get; }
            public bool DeclarationWasIntact { get; }
            public bool OwnerSceneWasLoaded { get; }
        }

        private static readonly List<DestructionEvidence> Destructions =
            new List<DestructionEvidence>();

        private GameObject _root;
        private GameObject _part;
        private ObjectEntryDeclaration _declaration;
        private ObjectEntryId _objectEntryId;
        private bool _partWasSubordinate;
        private Scene _ownerScene;

        public static void ResetEvidence()
        {
            Destructions.Clear();
        }

        public static bool TryGetDestructionEvidence(
            ObjectEntryId objectEntryId,
            GameObject expectedRoot,
            out DestructionEvidence evidence)
        {
            evidence = null;
            if (!objectEntryId.IsValid || object.ReferenceEquals(expectedRoot, null))
                return false;

            for (int index = Destructions.Count - 1; index >= 0; index--)
            {
                DestructionEvidence candidate = Destructions[index];
                if (candidate.ObjectEntryId == objectEntryId &&
                    object.ReferenceEquals(candidate.Root, expectedRoot))
                {
                    evidence = candidate;
                    return true;
                }
            }

            return false;
        }

        private void OnEnable()
        {
            if (_root != null)
                return;

            _declaration = GetComponentInParent<ObjectEntryDeclaration>(true);
            if (_declaration == null || !_declaration.TryGetObjectEntryId(out ObjectEntryId id))
                return;

            _root = _declaration.gameObject;
            _part = gameObject;
            _objectEntryId = id;
            _partWasSubordinate = !object.ReferenceEquals(_part, _root) &&
                _part.transform.IsChildOf(_root.transform);
            _ownerScene = _root.scene;
        }

        private void OnDestroy()
        {
            bool rootWasLive = _root != null;
            bool declarationWasIntact = rootWasLive && _declaration != null &&
                object.ReferenceEquals(_declaration, _root.GetComponent<ObjectEntryDeclaration>()) &&
                _declaration.TryGetObjectEntryId(out ObjectEntryId currentId) &&
                currentId == _objectEntryId;
            bool ownerSceneWasLoaded = _ownerScene.IsValid() && _ownerScene.isLoaded;

            if (_objectEntryId.IsValid)
            {
                Destructions.Add(new DestructionEvidence(
                    _root,
                    _part,
                    _declaration,
                    _objectEntryId,
                    _partWasSubordinate,
                    rootWasLive,
                    declarationWasIntact,
                    ownerSceneWasLoaded));
            }
        }
    }
}
