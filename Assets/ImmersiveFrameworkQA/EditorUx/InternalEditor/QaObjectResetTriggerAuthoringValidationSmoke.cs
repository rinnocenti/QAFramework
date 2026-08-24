using System;
using Immersive.Framework.Actors;
using Immersive.Framework.Editor.Authoring;
using Immersive.Framework.ObjectReset;
using Immersive.Framework.Reset.Unity;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.EditorUx.Internal.Editor
{
    internal static class QaObjectResetTriggerAuthoringValidationSmoke
    {
        [MenuItem("Immersive Framework/QA/Regressions/Editor UX/Run Object Reset Trigger Authoring Validation Smoke", priority = 261)]
        private static void Run()
        {
            VerifyAdapterWithNoResolvedId();
            VerifyActorDerivedAdapterWithNoRegistration();
            VerifyMissingTarget();
            VerifyDirectId();
            VerifyBothTargetsUseCurrentRuntimePrecedence();
            VerifySerializedObjectUpdate();
            Debug.Log("[QA_OBJECT_RESET_TRIGGER_AUTHORING] status='Passed' cases='adapter-empty-id,actor-derived,missing,direct-id,both,serialized-update'.");
        }

        private static void VerifyAdapterWithNoResolvedId()
        {
            using (var fixture = new Fixture())
            {
                UnityResetSubjectAdapter adapter = fixture.Root.AddComponent<UnityResetSubjectAdapter>();
                fixture.Configure(adapter, string.Empty);
                ObjectResetTargetAuthoringValidationResult result = fixture.Validate();
                Require(result.Status == ObjectResetTargetAuthoringValidationStatus.ValidAdapterReference && result.IsValid, "An assigned adapter with no resolved runtime ID must be valid authoring.");
                Require(!adapter.SubjectId.IsValid, "Fixture requires an unresolved adapter Subject ID.");
            }
        }

        private static void VerifyActorDerivedAdapterWithNoRegistration()
        {
            using (var fixture = new Fixture())
            {
                PlayerActorDeclaration actor = fixture.Root.AddComponent<PlayerActorDeclaration>();
                UnityResetSubjectAdapter adapter = fixture.Root.AddComponent<UnityResetSubjectAdapter>();
                var adapterSerialized = new SerializedObject(adapter);
                adapterSerialized.FindProperty("sourcePlayerActor").objectReferenceValue = actor;
                adapterSerialized.ApplyModifiedPropertiesWithoutUndo();
                fixture.Configure(adapter, string.Empty);
                ObjectResetTargetAuthoringValidationResult result = fixture.Validate();
                Require(result.Status == ObjectResetTargetAuthoringValidationStatus.ValidAdapterReference && !adapter.IsRegistered && !adapter.SubjectId.IsValid, "An actor-derived unregistered adapter must remain valid authoring.");
            }
        }

        private static void VerifyMissingTarget()
        {
            using (var fixture = new Fixture())
            {
                ObjectResetTargetAuthoringValidationResult result = fixture.Validate();
                Require(result.Status == ObjectResetTargetAuthoringValidationStatus.MissingTarget && !result.IsValid, "A target with neither adapter nor ID must be incomplete.");
            }
        }

        private static void VerifyDirectId()
        {
            using (var fixture = new Fixture())
            {
                fixture.Configure(null, "qa.reset.subject");
                Require(fixture.Validate().Status == ObjectResetTargetAuthoringValidationStatus.ValidAuthoredSubjectId, "A valid direct Reset Subject ID must be valid authoring.");
                fixture.Configure(null, "   ");
                Require(fixture.Validate().Status == ObjectResetTargetAuthoringValidationStatus.MissingTarget, "Whitespace-only direct Reset Subject ID must be incomplete.");
            }
        }

        private static void VerifyBothTargetsUseCurrentRuntimePrecedence()
        {
            using (var fixture = new Fixture())
            {
                UnityResetSubjectAdapter adapter = fixture.Root.AddComponent<UnityResetSubjectAdapter>();
                fixture.Configure(adapter, "qa.reset.direct-fallback");
                ObjectResetTargetAuthoringValidationResult result = fixture.Validate();
                Require(result.Status == ObjectResetTargetAuthoringValidationStatus.ValidAdapterReference && result.HasDirectId, "Current runtime policy accepts an adapter and keeps direct ID as its unresolved-adapter fallback.");
            }
        }

        private static void VerifySerializedObjectUpdate()
        {
            using (var fixture = new Fixture())
            {
                UnityResetSubjectAdapter adapter = fixture.Root.AddComponent<UnityResetSubjectAdapter>();
                fixture.Configure(adapter, string.Empty);
                Require(fixture.Validate().IsValid, "Validation must observe the adapter immediately after SerializedObject.ApplyModifiedProperties.");
            }
        }

        private sealed class Fixture : IDisposable
        {
            internal Fixture()
            {
                Root = new GameObject("QA_ObjectResetTriggerAuthoring");
                Trigger = Root.AddComponent<ObjectResetTrigger>();
            }

            internal GameObject Root { get; }
            private ObjectResetTrigger Trigger { get; }

            internal void Configure(UnityResetSubjectAdapter adapter, string id)
            {
                var serialized = new SerializedObject(Trigger);
                SerializedProperty target = serialized.FindProperty("targetSubject");
                target.FindPropertyRelative("subjectAdapter").objectReferenceValue = adapter;
                target.FindPropertyRelative("subjectId").stringValue = id;
                serialized.ApplyModifiedProperties();
            }

            internal ObjectResetTargetAuthoringValidationResult Validate()
            {
                var serialized = new SerializedObject(Trigger);
                return ObjectResetTargetAuthoringValidator.Validate(serialized.FindProperty("targetSubject"));
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(Root);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
