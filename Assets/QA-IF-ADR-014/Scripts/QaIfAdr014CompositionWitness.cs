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
                ObjectEntryDeclaration declaration,
                string objectEntryId,
                bool rootWasLive,
                bool declarationWasIntact,
                bool ownerSceneWasLoaded)
            {
                Root = root;
                Declaration = declaration;
                ObjectEntryId = objectEntryId;
                RootWasLive = rootWasLive;
                DeclarationWasIntact = declarationWasIntact;
                OwnerSceneWasLoaded = ownerSceneWasLoaded;
            }

            public GameObject Root { get; }
            public ObjectEntryDeclaration Declaration { get; }
            public string ObjectEntryId { get; }
            public bool RootWasLive { get; }
            public bool DeclarationWasIntact { get; }
            public bool OwnerSceneWasLoaded { get; }
        }

        private static readonly Dictionary<string, DestructionEvidence> Destructions =
            new Dictionary<string, DestructionEvidence>();

        private GameObject _root;
        private ObjectEntryDeclaration _declaration;
        private string _objectEntryId = string.Empty;
        private Scene _ownerScene;

        public static void ResetEvidence()
        {
            Destructions.Clear();
        }

        public static bool TryGetDestructionEvidence(
            string objectEntryId,
            out DestructionEvidence evidence)
        {
            return Destructions.TryGetValue(objectEntryId ?? string.Empty, out evidence);
        }

        private void OnEnable()
        {
            if (_root != null)
                return;

            _declaration = GetComponentInParent<ObjectEntryDeclaration>(true);
            if (_declaration == null || !_declaration.TryGetObjectEntryId(out ObjectEntryId id))
                return;

            _root = _declaration.gameObject;
            _objectEntryId = id.StableText;
            _ownerScene = _root.scene;
        }

        private void OnDestroy()
        {
            bool rootWasLive = _root != null;
            bool declarationWasIntact = rootWasLive && _declaration != null &&
                object.ReferenceEquals(_declaration, _root.GetComponent<ObjectEntryDeclaration>()) &&
                _declaration.TryGetObjectEntryId(out ObjectEntryId currentId) &&
                currentId.StableText == _objectEntryId;
            bool ownerSceneWasLoaded = _ownerScene.IsValid() && _ownerScene.isLoaded;

            if (!string.IsNullOrEmpty(_objectEntryId))
            {
                Destructions[_objectEntryId] = new DestructionEvidence(
                    _root,
                    _declaration,
                    _objectEntryId,
                    rootWasLive,
                    declarationWasIntact,
                    ownerSceneWasLoaded);
            }
        }
    }
}
