using System;
using System.Collections.Generic;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Edit Mode preflight for the serialized P3M5B scene matrix.
    ///
    /// This validator checks the data after Unity has saved and reopened each scene. It therefore
    /// detects Unity fake-null references that an in-memory setup assertion cannot detect.
    /// </summary>
    internal static class QaP3M5BPersistedFixturePreflight
    {
        private enum ExpectedShape
        {
            Empty = 0,
            ValidSingle = 10,
            DuplicateSlot = 20,
            MissingActor = 30,
            MismatchedEvidence = 40
        }

        private readonly struct SceneSpecification
        {
            internal SceneSpecification(
                string path,
                ExpectedShape shape,
                int expectedAdmissionCount)
            {
                Path = path;
                Shape = shape;
                ExpectedAdmissionCount = expectedAdmissionCount;
            }

            internal string Path { get; }

            internal ExpectedShape Shape { get; }

            internal int ExpectedAdmissionCount { get; }
        }

        internal readonly struct PreflightResult
        {
            internal PreflightResult(int sceneCount, int admissionCount)
            {
                SceneCount = sceneCount;
                AdmissionCount = admissionCount;
            }

            internal int SceneCount { get; }

            internal int AdmissionCount { get; }
        }

        private const string MenuPath =
            "Immersive Framework/QA/Player/P3M5B Validate Persisted Fixture";

        private static readonly SceneSpecification[] Specifications =
        {
            new SceneSpecification(
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.RouteAPrimaryScenePath,
                ExpectedShape.Empty,
                0),
            new SceneSpecification(
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.RouteBPrimaryScenePath,
                ExpectedShape.Empty,
                0),
            new SceneSpecification(
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.RouteAActivityScenePath,
                ExpectedShape.ValidSingle,
                1),
            new SceneSpecification(
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.RouteBActivityScenePath,
                ExpectedShape.ValidSingle,
                1),
            new SceneSpecification(
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.DuplicateSlotScenePath,
                ExpectedShape.DuplicateSlot,
                2),
            new SceneSpecification(
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.MissingActorScenePath,
                ExpectedShape.MissingActor,
                1),
            new SceneSpecification(
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.MismatchedProfileScenePath,
                ExpectedShape.MismatchedEvidence,
                1),
            new SceneSpecification(
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.UndeclaredSurfaceScenePath,
                ExpectedShape.ValidSingle,
                1)
        };

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        [MenuItem(MenuPath)]
        private static void Run()
        {
            try
            {
                PreflightResult result = ValidateOrThrow();
                Debug.Log(
                    "[P3M5B_PERSISTED_FIXTURE_PREFLIGHT] " +
                    $"status='Passed' scenes='{result.SceneCount}' " +
                    $"admissions='{result.AdmissionCount}' fakeNullReferences='0' " +
                    "negativeShapes='Preserved'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3M5B_PERSISTED_FIXTURE_PREFLIGHT] " +
                    $"status='Failed' exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}'.");
                throw;
            }
        }

        internal static PreflightResult ValidateOrThrow()
        {
            ValidateDeclarationTopology();

            int sceneCount = 0;
            int admissionCount = 0;

            for (int index = 0; index < Specifications.Length; index++)
            {
                SceneSpecification specification = Specifications[index];
                admissionCount += ValidateScene(specification);
                sceneCount++;
            }

            return new PreflightResult(sceneCount, admissionCount);
        }

        private static void ValidateDeclarationTopology()
        {
            RouteAsset routeA = LoadRequiredAsset<RouteAsset>(
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.RouteAPath);
            RouteAsset routeB = LoadRequiredAsset<RouteAsset>(
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.RouteBPath);
            ActivityAsset routeAActivity = LoadRequiredAsset<ActivityAsset>(
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.RouteAActivityPath);
            ActivityAsset routeBActivity = LoadRequiredAsset<ActivityAsset>(
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.RouteBActivityPath);
            ActivityAsset duplicateSlotActivity = LoadRequiredAsset<ActivityAsset>(
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.DuplicateSlotActivityPath);
            ActivityAsset missingActorActivity = LoadRequiredAsset<ActivityAsset>(
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.MissingActorActivityPath);
            ActivityAsset mismatchedProfileActivity = LoadRequiredAsset<ActivityAsset>(
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.MismatchedProfileActivityPath);
            ActivityAsset undeclaredActivity = LoadRequiredAsset<ActivityAsset>(
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.UndeclaredSurfaceActivityPath);

            ValidateRouteActivityTopology(
                routeA,
                routeAActivity,
                "qa.p3m5b.route.a",
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.RouteAActivityId,
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.RouteAPrimaryScenePath,
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.RouteAActivityScenePath,
                "Route A");
            ValidateRouteActivityTopology(
                routeB,
                routeBActivity,
                "qa.p3m5b.route.b",
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.RouteBActivityId,
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.RouteBPrimaryScenePath,
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.RouteBActivityScenePath,
                "Route B");

            ValidateActivitySceneTopology(
                duplicateSlotActivity,
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.DuplicateSlotActivityId,
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.DuplicateSlotScenePath,
                "duplicate-Slot Activity");
            ValidateActivitySceneTopology(
                missingActorActivity,
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.MissingActorActivityId,
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.MissingActorScenePath,
                "missing-Actor Activity");
            ValidateActivitySceneTopology(
                mismatchedProfileActivity,
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.MismatchedProfileActivityId,
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.MismatchedProfileScenePath,
                "mismatched-profile Activity");

            Require(
                undeclaredActivity.HasValidActivityId &&
                string.Equals(
                    undeclaredActivity.ActivityId.StableText,
                    QaP3M5BRouteTransitionAndNegativeMatrixSetup.UndeclaredSurfaceActivityId,
                    StringComparison.Ordinal),
                "P3M5B undeclared-surface Activity has a missing or unexpected Activity ID.");
            Require(
                !undeclaredActivity.HasActivityContentProfile,
                "P3M5B undeclared-surface Activity unexpectedly owns an Activity Content Profile.");
            Require(
                !ReferenceEquals(routeA.StartupActivity, undeclaredActivity) &&
                !ReferenceEquals(routeB.StartupActivity, undeclaredActivity),
                "P3M5B undeclared-surface Activity unexpectedly participates in a tested Route.");
        }

        private static void ValidateRouteActivityTopology(
            RouteAsset route,
            ActivityAsset expectedActivity,
            string expectedRouteId,
            string expectedActivityId,
            string expectedPrimaryScenePath,
            string expectedActivityScenePath,
            string label)
        {
            Require(
                route.HasValidRouteId &&
                string.Equals(
                    route.RouteId.StableText,
                    expectedRouteId,
                    StringComparison.Ordinal),
                $"P3M5B {label} has a missing or unexpected Route ID.");
            Require(
                ReferenceEquals(route.StartupActivity, expectedActivity),
                $"P3M5B {label} does not reference its canonical startup Activity.");
            Require(
                SameAssetPath(route.PrimaryScenePath, expectedPrimaryScenePath),
                $"P3M5B {label} primary scene changed. expected='{expectedPrimaryScenePath}' " +
                $"actual='{route.PrimaryScenePath}'.");
            Require(
                !SameAssetPath(expectedPrimaryScenePath, expectedActivityScenePath),
                $"P3M5B {label} primary scene and Activity Player scene must be distinct.");

            ValidateActivitySceneTopology(
                expectedActivity,
                expectedActivityId,
                expectedActivityScenePath,
                $"{label} startup Activity");
        }

        private static void ValidateActivitySceneTopology(
            ActivityAsset activity,
            string expectedActivityId,
            string expectedScenePath,
            string label)
        {
            Require(
                activity.HasValidActivityId &&
                string.Equals(
                    activity.ActivityId.StableText,
                    expectedActivityId,
                    StringComparison.Ordinal),
                $"P3M5B {label} has a missing or unexpected Activity ID.");
            Require(
                activity.TryGetPlayerParticipationProjectionDescriptor(
                    out _,
                    out string participationIssue),
                $"P3M5B {label} has an invalid Player participation projection. " +
                $"issue='{participationIssue}'.");
            Require(
                activity.PlayerParticipationProjectionMode ==
                    ActivityParticipationProjectionMode.ExplicitSlots &&
                activity.PlayerParticipationZeroParticipantPolicy ==
                    ActivityParticipationZeroParticipantPolicy.Rejected &&
                activity.PlayerParticipationExplicitSlotProfiles.Count == 1 &&
                activity.PlayerParticipationExplicitSlotProfiles[0] != null &&
                activity.PlayerParticipationRequirementLevel ==
                    PlayerParticipationRequirementLevel.LogicalActorsPrepared,
                $"P3M5B {label} does not preserve the canonical explicit-Slot " +
                "LogicalActorsPrepared participation contract.");
            Require(
                activity.ActivityContentProfile != null,
                $"P3M5B {label} has no Activity Content Profile.");

            IReadOnlyList<ActivityContentSceneEntry> scenes =
                activity.ActivityContentProfile.Scenes;
            Require(
                scenes.Count == 1,
                $"P3M5B {label} must declare exactly one scene, but declares " +
                $"'{scenes.Count}'.");
            Require(
                scenes[0] != null &&
                scenes[0].HasExplicitContentId &&
                SameAssetPath(scenes[0].ScenePath, expectedScenePath),
                $"P3M5B {label} does not declare its canonical scene. " +
                $"expected='{expectedScenePath}' " +
                $"actual='{(scenes[0] != null ? scenes[0].ScenePath : "<missing>")}'.");
        }

        private static T LoadRequiredAsset<T>(string path)
            where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Require(asset != null, $"P3M5B asset '{path}' is missing or has the wrong type.");
            return asset;
        }

        private static bool SameAssetPath(string left, string right)
        {
            return string.Equals(
                NormalizeAssetPath(left),
                NormalizeAssetPath(right),
                StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeAssetPath(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().Replace('\\', '/');
        }

        private static int ValidateScene(SceneSpecification specification)
        {
            Require(
                AssetDatabase.LoadAssetAtPath<SceneAsset>(specification.Path) != null,
                $"P3M5B scene '{specification.Path}' is missing.");

            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(specification.Path);
            bool openedByPreflight = !scene.IsValid() || !scene.isLoaded;

            try
            {
                if (openedByPreflight)
                {
                    scene = EditorSceneManager.OpenScene(
                        specification.Path,
                        OpenSceneMode.Additive);
                }

                List<SceneLocalPlayerAdmissionAuthoring> admissions =
                    FindSceneComponents<SceneLocalPlayerAdmissionAuthoring>(scene);
                admissions.Sort((left, right) =>
                    string.Compare(left.name, right.name, StringComparison.Ordinal));

                Require(
                    admissions.Count == specification.ExpectedAdmissionCount,
                    $"P3M5B scene '{specification.Path}' expected " +
                    $"'{specification.ExpectedAdmissionCount}' admission surfaces but found " +
                    $"'{admissions.Count}'.");

                switch (specification.Shape)
                {
                    case ExpectedShape.Empty:
                        break;

                    case ExpectedShape.ValidSingle:
                        ValidateAllSurfaces(admissions, expectValidEvidence: true);
                        break;

                    case ExpectedShape.DuplicateSlot:
                        ValidateAllSurfaces(admissions, expectValidEvidence: true);
                        Require(
                            ReferenceEquals(
                                admissions[0].PlayerSlotProfile,
                                admissions[1].PlayerSlotProfile),
                            "P3M5B duplicate-Slot scene no longer duplicates one Player Slot Profile.");
                        Require(
                            !ReferenceEquals(
                                admissions[0].LocalPlayerHost,
                                admissions[1].LocalPlayerHost),
                            "P3M5B duplicate-Slot scene unexpectedly reuses one Local Player Host.");
                        break;

                    case ExpectedShape.MissingActor:
                        ValidateMissingActor(admissions[0]);
                        break;

                    case ExpectedShape.MismatchedEvidence:
                        ValidateMismatchedEvidence(admissions[0]);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(specification),
                            specification.Shape,
                            "P3M5B preflight requires an explicit scene shape.");
                }

                return admissions.Count;
            }
            finally
            {
                if (openedByPreflight && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }

                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActiveScene);
                }
            }
        }

        private static void ValidateAllSurfaces(
            IReadOnlyList<SceneLocalPlayerAdmissionAuthoring> admissions,
            bool expectValidEvidence)
        {
            for (int index = 0; index < admissions.Count; index++)
            {
                SceneLocalPlayerAdmissionAuthoring admission = admissions[index];
                ValidateRequiredReferences(admission);

                bool valid = admission.TryValidateRuntimeEvidence(out string issue);
                Require(
                    valid == expectValidEvidence,
                    $"P3M5B admission '{admission.name}' returned an unexpected evidence result. " +
                    $"expectedValid='{expectValidEvidence}' actualValid='{valid}' " +
                    $"issue='{issue}' {ReferenceDiagnostic(admission)}");
            }
        }

        private static void ValidateMissingActor(
            SceneLocalPlayerAdmissionAuthoring admission)
        {
            Require(admission != null, "P3M5B missing-Actor admission is missing.");
            ValidateSameRootHost(admission);
            Require(
                admission.PlayerSlotProfile != null &&
                admission.LocalPlayerHost != null &&
                admission.ActorProfile != null &&
                admission.SceneLogicalPlayerActor == null,
                "P3M5B missing-Actor scene did not preserve its exact negative shape. " +
                ReferenceDiagnostic(admission));
        }

        private static void ValidateMismatchedEvidence(
            SceneLocalPlayerAdmissionAuthoring admission)
        {
            ValidateRequiredReferences(admission);
            bool valid = admission.TryValidateRuntimeEvidence(out string issue);
            Require(
                !valid &&
                issue.IndexOf(
                    "evidence does not match",
                    StringComparison.OrdinalIgnoreCase) >= 0,
                "P3M5B mismatched-evidence scene did not preserve its expected negative shape. " +
                $"valid='{valid}' issue='{issue}' {ReferenceDiagnostic(admission)}");
        }

        private static void ValidateRequiredReferences(
            SceneLocalPlayerAdmissionAuthoring admission)
        {
            Require(admission != null, "P3M5B admission surface is missing.");
            ValidateSameRootHost(admission);
            Require(
                admission.PlayerSlotProfile != null &&
                admission.LocalPlayerHost != null &&
                admission.ActorProfile != null &&
                admission.SceneLogicalPlayerActor != null,
                $"P3M5B admission '{admission.name}' contains a missing or Unity fake-null " +
                $"reference. {ReferenceDiagnostic(admission)}");
        }

        private static void ValidateSameRootHost(
            SceneLocalPlayerAdmissionAuthoring admission)
        {
            LocalPlayerHostAuthoring host = admission.LocalPlayerHost;
            Require(
                host != null &&
                ReferenceEquals(admission.gameObject, host.gameObject),
                $"P3M5B admission '{admission.name}' does not own its same-root Local Player Host.");
            Require(
                host.PlayerInput != null &&
                ReferenceEquals(host.gameObject, host.PlayerInput.gameObject),
                $"P3M5B admission '{admission.name}' has no same-root PlayerInput evidence.");
            Require(
                host.ActorMount != null &&
                host.ActorMount.IsChildOf(host.transform),
                $"P3M5B admission '{admission.name}' has no Actor Mount under its same-root Host.");
        }

        private static List<T> FindSceneComponents<T>(Scene scene)
            where T : Component
        {
            var results = new List<T>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                results.AddRange(roots[index].GetComponentsInChildren<T>(true));
            }

            return results;
        }

        private static string ReferenceDiagnostic(
            SceneLocalPlayerAdmissionAuthoring admission)
        {
            if (admission == null)
            {
                return "admission='<missing>'.";
            }

            return
                $"admission='{admission.name}' " +
                $"slot='{ObjectState(admission.PlayerSlotProfile)}' " +
                $"host='{ObjectState(admission.LocalPlayerHost)}' " +
                $"actorProfile='{ObjectState(admission.ActorProfile)}' " +
                $"sceneActor='{ObjectState(admission.SceneLogicalPlayerActor)}'.";
        }

        private static string ObjectState(UnityEngine.Object value)
        {
            return value != null ? value.name : "<missing-or-fake-null>";
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
