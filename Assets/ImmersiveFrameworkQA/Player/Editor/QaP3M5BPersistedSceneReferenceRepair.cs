using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Repairs P3M5B scene references after persistence.
    ///
    /// The original fixture validated references before saving, but PlayerActorDeclaration was
    /// referenced as a stripped component on a prefab instance. In the affected Unity import state,
    /// that reference could deserialize as Unity fake-null in Play Mode even though the YAML still
    /// contained a fileID. This repair unpacks the scene Actor, rebinds every explicit reference,
    /// saves, reopens and validates the persisted scene shape.
    /// </summary>
    public static class QaP3M5BPersistedSceneReferenceRepair
    {
        private enum ExpectedSceneShape
        {
            Valid = 0,
            MissingActor = 10,
            MismatchedEvidence = 20
        }

        private readonly struct SceneSpecification
        {
            internal SceneSpecification(string path, ExpectedSceneShape shape)
            {
                Path = path;
                Shape = shape;
            }

            internal string Path { get; }
            internal ExpectedSceneShape Shape { get; }
        }

        private const string MenuPath =
            "Immersive Framework/QA/Player/P3M5B Repair Persisted Scene References";

        private static readonly SceneSpecification[] SceneSpecifications =
        {
            new SceneSpecification(
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.RouteAPrimaryScenePath,
                ExpectedSceneShape.Valid),
            new SceneSpecification(
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.RouteAActivityScenePath,
                ExpectedSceneShape.Valid),
            new SceneSpecification(
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.RouteBActivityScenePath,
                ExpectedSceneShape.Valid),
            new SceneSpecification(
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.DuplicateSlotScenePath,
                ExpectedSceneShape.Valid),
            new SceneSpecification(
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.MissingActorScenePath,
                ExpectedSceneShape.MissingActor),
            new SceneSpecification(
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.MismatchedProfileScenePath,
                ExpectedSceneShape.MismatchedEvidence),
            new SceneSpecification(
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.ReusedHostScenePath,
                ExpectedSceneShape.Valid)
        };

        private static bool ValidateRepair()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        public static void Repair()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError(
                    "[P3M5B_PERSISTED_SCENE_REFERENCE_REPAIR] " +
                    "status='RejectedPlayMode' message='Exit Play Mode before repairing the fixture.'.");
                return;
            }

            try
            {
                PlayerSlotProfile[] slots = ResolveConfiguredSlots();
                GameObject actorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    QaP3M5BRouteTransitionAndNegativeMatrixSetup.ActorPrefabPath);
                ActorProfile actorProfile = AssetDatabase.LoadAssetAtPath<ActorProfile>(
                    QaP3M5BRouteTransitionAndNegativeMatrixSetup.ActorProfilePath);
                ActorProfile alternateProfile = AssetDatabase.LoadAssetAtPath<ActorProfile>(
                    QaP3M5BRouteTransitionAndNegativeMatrixSetup.AlternateActorProfilePath);

                Require(actorPrefab != null, "P3M5B Actor prefab is missing.");
                Require(actorProfile != null, "P3M5B primary Actor Profile is missing.");
                Require(alternateProfile != null, "P3M5B alternate Actor Profile is missing.");

                int repairedSurfaceCount = 0;
                int unpackedActorCount = 0;
                for (int index = 0; index < SceneSpecifications.Length; index++)
                {
                    SceneSpecification specification = SceneSpecifications[index];
                    RepairScene(
                        specification,
                        slots,
                        actorPrefab,
                        actorProfile,
                        alternateProfile,
                        ref repairedSurfaceCount,
                        ref unpackedActorCount);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[P3M5B_PERSISTED_SCENE_REFERENCE_REPAIR] " +
                    "status='Applied' " +
                    $"scenes='{SceneSpecifications.Length}' " +
                    $"surfaces='{repairedSurfaceCount}' " +
                    $"unpackedActors='{unpackedActorCount}' " +
                    "postSaveValidation='Passed'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3M5B_PERSISTED_SCENE_REFERENCE_REPAIR] " +
                    $"status='Failed' exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}'.");
                throw;
            }
        }

        private static void RepairScene(
            SceneSpecification specification,
            PlayerSlotProfile[] slots,
            GameObject actorPrefab,
            ActorProfile actorProfile,
            ActorProfile alternateProfile,
            ref int repairedSurfaceCount,
            ref int unpackedActorCount)
        {
            Require(
                AssetDatabase.LoadAssetAtPath<SceneAsset>(specification.Path) != null,
                $"P3M5B scene '{specification.Path}' is missing.");

            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene scene = default;
            bool opened = false;
            try
            {
                scene = EditorSceneManager.OpenScene(
                    specification.Path,
                    OpenSceneMode.Additive);
                opened = true;

                List<SceneLocalPlayerAdmissionAuthoring> admissions =
                    FindSceneComponents<SceneLocalPlayerAdmissionAuthoring>(scene);
                List<LocalPlayerHostAuthoring> hosts =
                    FindSceneComponents<LocalPlayerHostAuthoring>(scene);
                List<PlayerActorDeclaration> actors =
                    FindSceneComponents<PlayerActorDeclaration>(scene);

                Require(
                    admissions.Count > 0,
                    $"P3M5B scene '{specification.Path}' has no Scene Local Player Admission surface.");

                admissions.Sort((left, right) =>
                    string.Compare(left.name, right.name, StringComparison.Ordinal));

                for (int index = 0; index < admissions.Count; index++)
                {
                    SceneLocalPlayerAdmissionAuthoring admission = admissions[index];
                    string label = RemoveSuffix(admission.name, " Admission");

                    PlayerSlotProfile slot = ResolveSlot(
                        admission,
                        label,
                        slots);
                    LocalPlayerHostAuthoring host = ResolveHost(
                        admission,
                        label,
                        hosts);
                    PlayerActorDeclaration actor = specification.Shape ==
                        ExpectedSceneShape.MissingActor
                            ? null
                            : ResolveActor(
                                admission,
                                label,
                                host,
                                actors);

                    if (actor != null)
                    {
                        GameObject prefabInstanceRoot =
                            PrefabUtility.GetOutermostPrefabInstanceRoot(actor.gameObject);
                        if (prefabInstanceRoot != null)
                        {
                            PrefabUtility.UnpackPrefabInstance(
                                prefabInstanceRoot,
                                PrefabUnpackMode.Completely,
                                InteractionMode.AutomatedAction);
                            unpackedActorCount++;

                            actor = ResolveActor(
                                null,
                                label,
                                host,
                                FindSceneComponents<PlayerActorDeclaration>(scene));
                        }

                        SceneLogicalPlayerActorEvidence evidence =
                            actor.GetComponent<SceneLogicalPlayerActorEvidence>();
                        if (evidence == null)
                        {
                            evidence =
                                actor.gameObject.AddComponent<SceneLogicalPlayerActorEvidence>();
                        }

                        ActorProfile evidenceProfile = specification.Shape ==
                            ExpectedSceneShape.MismatchedEvidence
                                ? alternateProfile
                                : actorProfile;
                        evidence.EditorSetEvidence(
                            evidenceProfile,
                            actorPrefab,
                            $"P3M5B persisted reference repair for '{actor.name}'.");
                        EditorUtility.SetDirty(evidence);
                    }

                    SetObject(admission, "playerSlotProfile", slot);
                    SetObject(admission, "localPlayerHost", host);
                    SetObject(admission, "actorProfile", actorProfile);
                    SetObject(admission, "sceneLogicalPlayerActor", actor);
                    SetEnum(admission, "admissionTiming", "OnActivityEnter");
                    repairedSurfaceCount++;
                }

                Require(
                    EditorSceneManager.SaveScene(scene, specification.Path, false),
                    $"Could not save repaired P3M5B scene '{specification.Path}'.");

                EditorSceneManager.CloseScene(scene, true);
                opened = false;

                scene = EditorSceneManager.OpenScene(
                    specification.Path,
                    OpenSceneMode.Additive);
                opened = true;
                ValidatePersistedScene(scene, specification);
            }
            finally
            {
                if (opened && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }

                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActiveScene);
                }
            }
        }

        private static void ValidatePersistedScene(
            Scene scene,
            SceneSpecification specification)
        {
            List<SceneLocalPlayerAdmissionAuthoring> admissions =
                FindSceneComponents<SceneLocalPlayerAdmissionAuthoring>(scene);
            Require(
                admissions.Count > 0,
                $"Reopened P3M5B scene '{specification.Path}' has no admission surfaces.");

            for (int index = 0; index < admissions.Count; index++)
            {
                SceneLocalPlayerAdmissionAuthoring admission = admissions[index];
                string references = ReferenceDiagnostic(admission);

                if (specification.Shape == ExpectedSceneShape.MissingActor)
                {
                    Require(
                        admission.PlayerSlotProfile != null &&
                        admission.LocalPlayerHost != null &&
                        admission.ActorProfile != null &&
                        admission.SceneLogicalPlayerActor == null,
                        $"Missing-Actor scene did not preserve its exact negative shape. {references}");
                    continue;
                }

                Require(
                    admission.PlayerSlotProfile != null &&
                    admission.LocalPlayerHost != null &&
                    admission.ActorProfile != null &&
                    admission.SceneLogicalPlayerActor != null,
                    $"P3M5B persisted surface '{admission.name}' contains a missing or fake-null reference. {references}");

                bool valid = admission.TryValidateRuntimeEvidence(out string issue);
                if (specification.Shape == ExpectedSceneShape.MismatchedEvidence)
                {
                    Require(
                        !valid &&
                        issue.IndexOf(
                            "evidence does not match",
                            StringComparison.OrdinalIgnoreCase) >= 0,
                        $"Mismatched-evidence scene no longer has the expected negative shape. issue='{issue}' {references}");
                    continue;
                }

                Require(
                    valid,
                    $"P3M5B persisted surface '{admission.name}' is invalid after reopen. issue='{issue}' {references}");
            }
        }

        private static PlayerSlotProfile[] ResolveConfiguredSlots()
        {
            ImmersiveFrameworkSettingsAsset settings =
                Resources.Load<ImmersiveFrameworkSettingsAsset>(
                    ImmersiveFrameworkSettingsAsset.ResourcesPath);
            Require(
                settings != null && settings.ActiveGameApplication != null,
                "P3M5B requires the QA Active Game Application.");

            var slots = new PlayerSlotProfile[2];
            for (int index = 0; index < slots.Length; index++)
            {
                Require(
                    settings.ActiveGameApplication.TryGetLocalPlayerSlot(
                        index,
                        out slots[index]) &&
                    slots[index] != null,
                    "P3M5B requires two configured Local Player Slots.");
            }

            return slots;
        }

        private static PlayerSlotProfile ResolveSlot(
            SceneLocalPlayerAdmissionAuthoring admission,
            string label,
            PlayerSlotProfile[] slots)
        {
            if (admission != null && admission.PlayerSlotProfile != null)
            {
                return admission.PlayerSlotProfile;
            }

            return label.IndexOf("Slot 2", StringComparison.OrdinalIgnoreCase) >= 0
                ? slots[1]
                : slots[0];
        }

        private static LocalPlayerHostAuthoring ResolveHost(
            SceneLocalPlayerAdmissionAuthoring admission,
            string label,
            IReadOnlyList<LocalPlayerHostAuthoring> hosts)
        {
            if (admission != null && admission.LocalPlayerHost != null)
            {
                return admission.LocalPlayerHost;
            }

            LocalPlayerHostAuthoring exact = FindByName(
                hosts,
                label + " Host");
            if (exact != null)
            {
                return exact;
            }

            if (hosts.Count == 1)
            {
                return hosts[0];
            }

            if (label.IndexOf("Reused Host", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                LocalPlayerHostAuthoring shared = FindContainingName(
                    hosts,
                    "Shared Host");
                if (shared != null)
                {
                    return shared;
                }
            }

            throw new InvalidOperationException(
                $"Could not resolve Local Player Host for admission label '{label}'.");
        }

        private static PlayerActorDeclaration ResolveActor(
            SceneLocalPlayerAdmissionAuthoring admission,
            string label,
            LocalPlayerHostAuthoring host,
            IReadOnlyList<PlayerActorDeclaration> actors)
        {
            if (admission != null && admission.SceneLogicalPlayerActor != null)
            {
                return admission.SceneLogicalPlayerActor;
            }

            if (host != null && host.ActorMount != null)
            {
                PlayerActorDeclaration mounted =
                    host.ActorMount.GetComponentInChildren<PlayerActorDeclaration>(true);
                if (mounted != null)
                {
                    return mounted;
                }
            }

            PlayerActorDeclaration exact = FindByName(
                actors,
                label + " Actor");
            if (exact != null)
            {
                return exact;
            }

            if (actors.Count == 1)
            {
                return actors[0];
            }

            if (label.IndexOf("Reused Host", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                PlayerActorDeclaration shared = FindContainingName(
                    actors,
                    "Shared Actor");
                if (shared != null)
                {
                    return shared;
                }
            }

            throw new InvalidOperationException(
                $"Could not resolve Scene Logical Player Actor for admission label '{label}'.");
        }

        private static List<T> FindSceneComponents<T>(Scene scene)
            where T : Component
        {
            var values = new List<T>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                values.AddRange(
                    roots[rootIndex].GetComponentsInChildren<T>(true));
            }

            return values;
        }

        private static T FindByName<T>(
            IReadOnlyList<T> values,
            string expectedName)
            where T : Component
        {
            for (int index = 0; index < values.Count; index++)
            {
                T value = values[index];
                if (value != null &&
                    string.Equals(
                        value.name,
                        expectedName,
                        StringComparison.Ordinal))
                {
                    return value;
                }
            }

            return null;
        }

        private static T FindContainingName<T>(
            IReadOnlyList<T> values,
            string fragment)
            where T : Component
        {
            for (int index = 0; index < values.Count; index++)
            {
                T value = values[index];
                if (value != null &&
                    value.name.IndexOf(
                        fragment,
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return value;
                }
            }

            return null;
        }

        private static string ReferenceDiagnostic(
            SceneLocalPlayerAdmissionAuthoring admission)
        {
            return
                $"slot='{ObjectName(admission?.PlayerSlotProfile)}' " +
                $"host='{ObjectName(admission?.LocalPlayerHost)}' " +
                $"profile='{ObjectName(admission?.ActorProfile)}' " +
                $"actor='{ObjectName(admission?.SceneLogicalPlayerActor)}'.";
        }

        private static string ObjectName(UnityEngine.Object value)
        {
            return value != null ? value.name : "<null-or-destroyed>";
        }

        private static string RemoveSuffix(string value, string suffix)
        {
            if (string.IsNullOrEmpty(value) ||
                string.IsNullOrEmpty(suffix) ||
                !value.EndsWith(suffix, StringComparison.Ordinal))
            {
                return value ?? string.Empty;
            }

            return value.Substring(0, value.Length - suffix.Length);
        }

        private static void SetObject(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            Require(
                property != null,
                $"Missing object property '{propertyName}' on '{target.GetType().Name}'.");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetEnum(
            UnityEngine.Object target,
            string propertyName,
            string enumName)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            Require(
                property != null,
                $"Missing enum property '{propertyName}' on '{target.GetType().Name}'.");

            string[] names = property.enumNames;
            for (int index = 0; index < names.Length; index++)
            {
                if (string.Equals(names[index], enumName, StringComparison.Ordinal))
                {
                    property.enumValueIndex = index;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(target);
                    return;
                }
            }

            throw new InvalidOperationException(
                $"Enum value '{enumName}' is unavailable for '{property.propertyPath}'.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "\\'")
                    .Replace("\r", " ")
                    .Replace("\n", " ");
        }
    }
}
