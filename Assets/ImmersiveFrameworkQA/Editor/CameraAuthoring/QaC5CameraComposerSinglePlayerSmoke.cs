using System;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Editor.CameraAuthoring;
using Immersive.Framework.PlayerAuthoring;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace ImmersiveFrameworkQA.Camera.Editor
{
    /// <summary>
    /// Canonical QA smoke for the C5 CameraComposer SinglePlayer MVP.
    ///
    /// The current QA shape is scene-first: the clean Camera Product Surface scene owns a fixture and
    /// typed references. This smoke validates and, when possible, repairs only QA harness references.
    /// It does not use Camera.main, does not resolve the product player by name and does not create a player.
    /// </summary>
    public static class QaC5CameraComposerSinglePlayerSmoke
    {
        private const string RootName = "QA_C5_CameraComposer_SinglePlayer_Smoke";
        private const string FixtureTypeName = "QaCameraProductSurfaceFixture";
        private const string NegativeObjectName = "QA_Negative_MissingPlayerComposer";
        private const string CameraRigObjectName = "QA_CameraRig";

        [MenuItem("Immersive Framework/QA/Camera/C5 CameraComposer SinglePlayer Smoke")]
        private static void Run()
        {
            RunForRegression();
        }

        public static bool RunForRegression()
        {
            SmokeEvidence evidence = ResolveEvidence();
            if (!evidence.Succeeded)
            {
                return Fail(evidence.FailureCode, evidence.FailureDetail);
            }

            ConfigurePlayerComposer(evidence.PlayerComposer, evidence.FollowTarget, evidence.LookAtTarget);
            ConfigureCameraComposer(evidence.CameraComposer, evidence.PlayerComposer, 10);
            ConfigureCameraComposer(evidence.NegativeComposer, null, 10);
            RepairFixtureReferences(evidence);

            CameraComposerApplyRebuildResult validation = CameraComposerApplyRebuildUtility.Validate(evidence.CameraComposer, true);
            Debug.Log($"[QA][C5 CameraComposer] Validate: succeeded='{validation.Succeeded}' status='{validation.Status}' blocked='{validation.BlockedCount}' issue='{validation.BlockingIssue}'", evidence.CameraComposer);

            CameraComposerApplyRebuildResult first = CameraComposerApplyRebuildUtility.ApplyOrRebuild(evidence.CameraComposer, true, false);
            Debug.Log($"[QA][C5 CameraComposer] First Apply/Rebuild: succeeded='{first.Succeeded}' created='{first.CreatedCount}' repaired='{first.RepairedCount}' alreadyValid='{first.AlreadyValidCount}' skipped='{first.SkippedCount}' blocked='{first.BlockedCount}' issue='{first.BlockingIssue}'", evidence.CameraComposer);

            CameraComposerApplyRebuildResult second = CameraComposerApplyRebuildUtility.ApplyOrRebuild(evidence.CameraComposer, true, false);
            Debug.Log($"[QA][C5 CameraComposer] Second Apply/Rebuild: succeeded='{second.Succeeded}' created='{second.CreatedCount}' repaired='{second.RepairedCount}' alreadyValid='{second.AlreadyValidCount}' skipped='{second.SkippedCount}' blocked='{second.BlockedCount}' issue='{second.BlockingIssue}'", evidence.CameraComposer);

            CameraComposerApplyRebuildResult negative = CameraComposerApplyRebuildUtility.Validate(evidence.NegativeComposer, true);
            Debug.Log($"[QA][C5 CameraComposer] Negative Missing PlayerComposer: succeeded='{negative.Succeeded}' status='{negative.Status}' blocked='{negative.BlockedCount}' issue='{negative.BlockingIssue}'", evidence.NegativeComposer);

            if (!validation.Succeeded)
            {
                return Fail("happy-path-validation-failed", validation.BlockingIssue);
            }

            if (!first.Succeeded || first.BlockedCount != 0)
            {
                return Fail("first-apply-failed", first.BlockingIssue);
            }

            if (!second.Succeeded || second.BlockedCount != 0)
            {
                return Fail("second-apply-failed", second.BlockingIssue);
            }

            if (second.CreatedCount != 0)
            {
                return Fail("second-apply-created-new-objects", second.CreatedCount.ToString());
            }

            if (second.AlreadyValidCount <= 0)
            {
                return Fail("second-apply-no-already-valid-evidence", second.AlreadyValidCount.ToString());
            }

            if (evidence.CameraComposer.LastResolvedFollowTarget != evidence.FollowTarget)
            {
                return Fail("follow-target-not-resolved", CreateTransformDetail(evidence.CameraComposer.LastResolvedFollowTarget, evidence.FollowTarget));
            }

            if (evidence.CameraComposer.LastResolvedLookAtTarget != evidence.LookAtTarget)
            {
                return Fail("look-at-target-not-resolved", CreateTransformDetail(evidence.CameraComposer.LastResolvedLookAtTarget, evidence.LookAtTarget));
            }

            if (evidence.CameraComposer.CinemachineCamera == null)
            {
                return Fail("cinemachine-rig-not-materialized", "cinemachine-camera-reference-missing");
            }

            if (evidence.CameraComposer.UnityCamera == null)
            {
                return Fail("cinemachine-rig-not-materialized", "unity-camera-reference-missing");
            }

            if (negative.Succeeded || negative.BlockedCount == 0)
            {
                return Fail("missing-player-composer-not-blocked", negative.BlockingIssue);
            }

            Selection.activeObject = evidence.Fixture != null ? evidence.Fixture : evidence.CameraComposer;
            Debug.Log("[QA][C5 CameraComposer] PASS. SinglePlayer CameraComposer resolves PlayerComposer anchors, materializes a Cinemachine rig, remains idempotent, and blocks missing PlayerComposer.", evidence.CameraComposer);
            return true;
        }

        private static SmokeEvidence ResolveEvidence()
        {
            var evidence = new SmokeEvidence
            {
                Fixture = FindFixture()
            };

            evidence.PlayerComposer = ResolveFromFixture<PlayerComposer>(
                evidence.Fixture,
                "playerComposer",
                "sourcePlayerComposer",
                "player");
            evidence.PlayerComposer = evidence.PlayerComposer != null
                ? evidence.PlayerComposer
                : FindSingleSceneObject<PlayerComposer>();

            if (evidence.PlayerComposer == null)
            {
                return SmokeEvidence.Fail("fixture-player-composer-not-resolved", "No PlayerComposer reference was found in the fixture or scene.");
            }

            evidence.FollowTarget = ResolveFromFixture<Transform>(
                evidence.Fixture,
                "cameraTarget",
                "followTarget",
                "resolvedFollowTarget");
            evidence.FollowTarget = evidence.FollowTarget != null
                ? evidence.FollowTarget
                : GetSerializedObjectReference<Transform>(evidence.PlayerComposer, "cameraTarget");

            evidence.LookAtTarget = ResolveFromFixture<Transform>(
                evidence.Fixture,
                "lookAtTarget",
                "lookAt",
                "resolvedLookAtTarget");
            evidence.LookAtTarget = evidence.LookAtTarget != null
                ? evidence.LookAtTarget
                : GetSerializedObjectReference<Transform>(evidence.PlayerComposer, "lookAtTarget");

            if (evidence.FollowTarget == null || evidence.LookAtTarget == null)
            {
                return SmokeEvidence.Fail(
                    "fixture-target-references-not-resolved",
                    $"follow='{FormatObject(evidence.FollowTarget)}' lookAt='{FormatObject(evidence.LookAtTarget)}'");
            }

            evidence.CameraComposer = ResolveFromFixture<CameraComposer>(
                evidence.Fixture,
                "cameraComposer",
                "happyPathCameraComposer",
                "composer");
            evidence.CameraComposer = evidence.CameraComposer != null
                ? evidence.CameraComposer
                : FindCameraComposerForPlayer(evidence.PlayerComposer);
            evidence.CameraComposer = evidence.CameraComposer != null
                ? evidence.CameraComposer
                : EnsureCameraComposerOnRig(evidence.Fixture, CameraRigObjectName);

            if (evidence.CameraComposer == null)
            {
                return SmokeEvidence.Fail("fixture-camera-composer-not-resolved", "No CameraComposer reference was found and QA_CameraRig could not be resolved.");
            }

            evidence.NegativeComposer = ResolveFromFixture<CameraComposer>(
                evidence.Fixture,
                "negativeCameraComposer",
                "negativeMissingPlayerComposer",
                "missingPlayerComposerCameraComposer",
                "negativeComposer");
            evidence.NegativeComposer = evidence.NegativeComposer != null
                ? evidence.NegativeComposer
                : FindNegativeCameraComposer(evidence.CameraComposer);
            evidence.NegativeComposer = evidence.NegativeComposer != null
                ? evidence.NegativeComposer
                : EnsureCameraComposerOnNamedObject(NegativeObjectName);

            if (evidence.NegativeComposer == null)
            {
                return SmokeEvidence.Fail("fixture-negative-camera-composer-not-resolved", "No negative CameraComposer reference was found and QA_Negative_MissingPlayerComposer could not be resolved.");
            }

            return evidence;
        }

        private static MonoBehaviour FindFixture()
        {
            MonoBehaviour[] behaviours = UnityObject.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || EditorUtility.IsPersistent(behaviour))
                {
                    continue;
                }

                Type type = behaviour.GetType();
                if (type.Name == FixtureTypeName || type.FullName != null && type.FullName.EndsWith("." + FixtureTypeName, StringComparison.Ordinal))
                {
                    return behaviour;
                }
            }

            return null;
        }

        private static T ResolveFromFixture<T>(MonoBehaviour fixture, params string[] preferredNames)
            where T : UnityObject
        {
            if (fixture == null)
            {
                return null;
            }

            T bySerializedProperty = ResolveFromSerializedObject<T>(new SerializedObject(fixture), preferredNames);
            if (bySerializedProperty != null)
            {
                return bySerializedProperty;
            }

            Type fixtureType = fixture.GetType();
            const System.Reflection.BindingFlags Flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            System.Reflection.FieldInfo[] fields = fixtureType.GetFields(Flags);
            for (int i = 0; i < preferredNames.Length; i++)
            {
                for (int j = 0; j < fields.Length; j++)
                {
                    if (!string.Equals(fields[j].Name, preferredNames[i], StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (fields[j].GetValue(fixture) is T value)
                    {
                        return value;
                    }
                }
            }

            return null;
        }

        private static T ResolveFromSerializedObject<T>(SerializedObject serialized, params string[] preferredNames)
            where T : UnityObject
        {
            for (int i = 0; i < preferredNames.Length; i++)
            {
                SerializedProperty preferred = serialized.FindProperty(preferredNames[i]);
                if (preferred != null && preferred.propertyType == SerializedPropertyType.ObjectReference && preferred.objectReferenceValue is T preferredValue)
                {
                    return preferredValue;
                }
            }

            SerializedProperty iterator = serialized.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.propertyType != SerializedPropertyType.ObjectReference)
                {
                    continue;
                }

                for (int i = 0; i < preferredNames.Length; i++)
                {
                    if (!string.Equals(iterator.name, preferredNames[i], StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (iterator.objectReferenceValue is T value)
                    {
                        return value;
                    }
                }
            }

            return null;
        }

        private static T FindSingleSceneObject<T>()
            where T : UnityObject
        {
            T[] objects = UnityObject.FindObjectsByType<T>(FindObjectsInactive.Include);
            T found = null;
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] == null || EditorUtility.IsPersistent(objects[i]))
                {
                    continue;
                }

                if (found != null)
                {
                    return null;
                }

                found = objects[i];
            }

            return found;
        }

        private static CameraComposer FindCameraComposerForPlayer(PlayerComposer playerComposer)
        {
            CameraComposer[] composers = UnityObject.FindObjectsByType<CameraComposer>(FindObjectsInactive.Include);
            for (int i = 0; i < composers.Length; i++)
            {
                CameraComposer composer = composers[i];
                if (composer == null || EditorUtility.IsPersistent(composer))
                {
                    continue;
                }

                if (composer.PlayerComposer == playerComposer)
                {
                    return composer;
                }
            }

            CameraComposer fallback = null;
            for (int i = 0; i < composers.Length; i++)
            {
                CameraComposer composer = composers[i];
                if (composer == null || EditorUtility.IsPersistent(composer))
                {
                    continue;
                }

                if (composer.gameObject.name.IndexOf("Negative", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                fallback = composer;
                break;
            }

            return fallback;
        }

        private static CameraComposer FindNegativeCameraComposer(CameraComposer happyComposer)
        {
            CameraComposer[] composers = UnityObject.FindObjectsByType<CameraComposer>(FindObjectsInactive.Include);
            CameraComposer unassigned = null;
            for (int i = 0; i < composers.Length; i++)
            {
                CameraComposer composer = composers[i];
                if (composer == null || composer == happyComposer || EditorUtility.IsPersistent(composer))
                {
                    continue;
                }

                if (composer.gameObject.name.IndexOf("Negative", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return composer;
                }

                if (composer.PlayerComposer == null && unassigned == null)
                {
                    unassigned = composer;
                }
            }

            return unassigned;
        }

        private static CameraComposer EnsureCameraComposerOnRig(MonoBehaviour fixture, string rigName)
        {
            Transform root = fixture != null ? fixture.transform.root : null;
            Transform rig = root != null ? FindChildRecursive(root, rigName) : null;
            if (rig == null)
            {
                GameObject byName = GameObject.Find(rigName);
                rig = byName != null ? byName.transform : null;
            }

            if (rig == null)
            {
                return null;
            }

            CameraComposer composer = rig.GetComponent<CameraComposer>();
            if (composer == null)
            {
                composer = Undo.AddComponent<CameraComposer>(rig.gameObject);
            }

            return composer;
        }

        private static CameraComposer EnsureCameraComposerOnNamedObject(string objectName)
        {
            GameObject go = GameObject.Find(objectName);
            if (go == null)
            {
                return null;
            }

            CameraComposer composer = go.GetComponent<CameraComposer>();
            if (composer == null)
            {
                composer = Undo.AddComponent<CameraComposer>(go);
            }

            return composer;
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == childName)
                {
                    return child;
                }

                Transform nested = FindChildRecursive(child, childName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private static T GetSerializedObjectReference<T>(UnityObject target, string propertyName)
            where T : UnityObject
        {
            if (target == null)
            {
                return null;
            }

            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
            {
                return null;
            }

            return property.objectReferenceValue as T;
        }

        private static void ConfigurePlayerComposer(PlayerComposer composer, Transform cameraTarget, Transform lookAtTarget)
        {
            var serialized = new SerializedObject(composer);
            serialized.Update();
            SetString(serialized, "actorId", "qa.player.actor");
            SetString(serialized, "playerSlotId", "qa.player.1");
            SetObject(serialized, "cameraTarget", cameraTarget);
            SetObject(serialized, "lookAtTarget", lookAtTarget);
            SetBool(serialized, "inputBindingRequired", false);
            SetBool(serialized, "cameraBindingRequired", true);
            SetBool(serialized, "createBindingsRootIfMissing", true);
            SetBool(serialized, "createAnchorsIfMissing", false);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(composer);
        }

        private static void ConfigureCameraComposer(CameraComposer composer, PlayerComposer playerComposer, int priority)
        {
            var serialized = new SerializedObject(composer);
            serialized.Update();
            SetEnum(serialized, "mode", CameraMode.SinglePlayerFollowCamera);
            SetEnum(serialized, "ownershipScope", CameraOwnershipScope.SinglePlayer);
            SetEnum(serialized, "targetSourceKind", CameraTargetSourceKind.PlayerComposer);
            SetObject(serialized, "playerComposer", playerComposer);
            SetEnum(serialized, "followRequirement", CameraTargetRequirement.Required);
            SetEnum(serialized, "lookAtRequirement", CameraTargetRequirement.Required);
            SetInt(serialized, "priority", priority);
            SetBool(serialized, "createUnityCameraIfMissing", true);
            SetBool(serialized, "createCinemachineCameraIfMissing", true);
            SetString(serialized, "unityCameraObjectName", "Unity Camera");
            SetString(serialized, "cinemachineCameraObjectName", "Cinemachine Camera");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(composer);
        }

        private static void RepairFixtureReferences(SmokeEvidence evidence)
        {
            if (evidence.Fixture == null)
            {
                return;
            }

            var serialized = new SerializedObject(evidence.Fixture);
            serialized.Update();
            SetObjectIfPropertyExists(serialized, evidence.PlayerComposer, "playerComposer", "sourcePlayerComposer", "player");
            SetObjectIfPropertyExists(serialized, evidence.CameraComposer, "cameraComposer", "happyPathCameraComposer", "composer");
            SetObjectIfPropertyExists(serialized, evidence.FollowTarget, "cameraTarget", "followTarget", "resolvedFollowTarget");
            SetObjectIfPropertyExists(serialized, evidence.LookAtTarget, "lookAtTarget", "lookAt", "resolvedLookAtTarget");
            SetObjectIfPropertyExists(serialized, evidence.NegativeComposer, "negativeCameraComposer", "negativeMissingPlayerComposer", "missingPlayerComposerCameraComposer", "negativeComposer");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(evidence.Fixture);
        }

        private static void SetObjectIfPropertyExists(SerializedObject serialized, UnityObject value, params string[] propertyNames)
        {
            for (int i = 0; i < propertyNames.Length; i++)
            {
                SerializedProperty property = serialized.FindProperty(propertyNames[i]);
                if (property != null && property.propertyType == SerializedPropertyType.ObjectReference)
                {
                    property.objectReferenceValue = value;
                    return;
                }
            }
        }

        private static void SetString(SerializedObject serialized, string propertyName, string value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.stringValue = value;
            }
        }

        private static void SetBool(SerializedObject serialized, string propertyName, bool value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
            }
        }

        private static void SetInt(SerializedObject serialized, string propertyName, int value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.intValue = value;
            }
        }

        private static void SetObject(SerializedObject serialized, string propertyName, UnityObject value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void SetEnum<TEnum>(SerializedObject serialized, string propertyName, TEnum value)
            where TEnum : Enum
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || property.propertyType != SerializedPropertyType.Enum)
            {
                return;
            }

            string enumName = value.ToString();
            for (int i = 0; i < property.enumNames.Length; i++)
            {
                if (string.Equals(property.enumNames[i], enumName, StringComparison.Ordinal))
                {
                    property.enumValueIndex = i;
                    return;
                }
            }

            int numericValue = Convert.ToInt32(value);
            if (numericValue >= 0 && numericValue < property.enumNames.Length)
            {
                property.enumValueIndex = numericValue;
                return;
            }

            property.intValue = numericValue;
        }

        private static bool Fail(string code, string detail)
        {
            Debug.LogError($"[QA][C5 CameraComposer] FAIL. code='{code}' detail='{detail}'");
            return false;
        }

        private static string FormatObject(UnityObject value)
        {
            return value != null ? value.name : "<null>";
        }

        private static string CreateTransformDetail(Transform actual, Transform expected)
        {
            return $"actual='{FormatObject(actual)}' expected='{FormatObject(expected)}'";
        }

        private sealed class SmokeEvidence
        {
            public MonoBehaviour Fixture;
            public PlayerComposer PlayerComposer;
            public CameraComposer CameraComposer;
            public CameraComposer NegativeComposer;
            public Transform FollowTarget;
            public Transform LookAtTarget;
            public string FailureCode;
            public string FailureDetail;

            public bool Succeeded => string.IsNullOrEmpty(FailureCode);

            public static SmokeEvidence Fail(string code, string detail)
            {
                return new SmokeEvidence
                {
                    FailureCode = code,
                    FailureDetail = detail
                };
            }
        }
    }
}
