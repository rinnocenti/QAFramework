using System.Collections.Generic;
using Immersive.Framework.ObjectEntry;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.QaFramework.IfAdr014.C4
{
    public sealed class QaIfAdr014C4PartWitness : MonoBehaviour
    {
        public sealed class DestructionEvidence
        {
            internal DestructionEvidence(
                string prefabId,
                GameObject root,
                GameObject part,
                ObjectEntryDeclaration declaration,
                ObjectEntryId objectEntryId,
                bool rootWasLive,
                bool declarationWasIntact,
                bool ownerSceneWasLoaded,
                SceneHandle ownerSceneHandle)
            {
                PrefabId = prefabId;
                Root = root;
                Part = part;
                Declaration = declaration;
                ObjectEntryId = objectEntryId;
                RootWasLive = rootWasLive;
                DeclarationWasIntact = declarationWasIntact;
                OwnerSceneWasLoaded = ownerSceneWasLoaded;
                OwnerSceneHandle = ownerSceneHandle;
            }

            public string PrefabId { get; }
            public GameObject Root { get; }
            public GameObject Part { get; }
            public ObjectEntryDeclaration Declaration { get; }
            public ObjectEntryId ObjectEntryId { get; }
            public bool PartIsPhysicallyDestroyed => Part == null;
            public bool RootWasLive { get; }
            public bool DeclarationWasIntact { get; }
            public bool OwnerSceneWasLoaded { get; }
            public SceneHandle OwnerSceneHandle { get; }
        }

        private static readonly List<DestructionEvidence> Destructions =
            new List<DestructionEvidence>();

        [SerializeField] private string prefabId = string.Empty;

        private GameObject _root;
        private ObjectEntryDeclaration _declaration;
        private ObjectEntryId _objectEntryId;
        private Scene _ownerScene;
        private bool _ownerSceneWasLoadedAtEnable;

        public string PrefabId => prefabId;
        public Scene OwnerScene => _ownerScene;
        public bool OwnerSceneWasLoadedAtEnable => _ownerSceneWasLoadedAtEnable;

        public static void ResetEvidence()
        {
            Destructions.Clear();
        }

        public static DestructionEvidence[] GetDestructionEvidence(
            ObjectEntryId objectEntryId,
            GameObject expectedRoot)
        {
            var evidence = new List<DestructionEvidence>();
            if (!objectEntryId.IsValid || object.ReferenceEquals(expectedRoot, null))
                return evidence.ToArray();

            foreach (DestructionEvidence candidate in Destructions)
            {
                if (candidate.ObjectEntryId == objectEntryId &&
                    object.ReferenceEquals(candidate.Root, expectedRoot))
                    evidence.Add(candidate);
            }

            return evidence.ToArray();
        }

        private void OnEnable()
        {
            if (_root != null)
                return;

            _declaration = GetComponentInParent<ObjectEntryDeclaration>(true);
            if (_declaration == null ||
                !_declaration.TryGetObjectEntryId(out _objectEntryId))
                return;

            _root = _declaration.gameObject;
            _ownerScene = _root.scene;
            _ownerSceneWasLoadedAtEnable = _ownerScene.IsValid() && _ownerScene.isLoaded;
        }

        private void OnDestroy()
        {
            bool rootWasLive = _root != null;
            bool declarationWasIntact = rootWasLive &&
                _declaration != null &&
                object.ReferenceEquals(
                    _declaration,
                    _root.GetComponent<ObjectEntryDeclaration>()) &&
                _declaration.TryGetObjectEntryId(out ObjectEntryId currentId) &&
                currentId == _objectEntryId;

            if (_objectEntryId.IsValid)
            {
                Destructions.Add(new DestructionEvidence(
                    prefabId,
                    _root,
                    gameObject,
                    _declaration,
                    _objectEntryId,
                    rootWasLive,
                    declarationWasIntact,
                    _ownerScene.IsValid() && _ownerScene.isLoaded,
                    _ownerScene.IsValid() ? _ownerScene.handle : SceneHandle.None));
            }
        }
    }
}
