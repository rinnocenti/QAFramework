using System;
using System.Collections.Generic;
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
            ValidSingle = 0,
            DuplicateSlot = 10,
            MissingActor = 20,
            MismatchedEvidence = 30,
            ReusedHost = 40
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
                ExpectedShape.ValidSingle,
                1),
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
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.ReusedHostScenePath,
                ExpectedShape.ReusedHost,
                2)
        };

        private static bool ValidateRun()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

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

                    case ExpectedShape.ReusedHost:
                        ValidateAllSurfaces(admissions, expectValidEvidence: true);
                        Require(
                            ReferenceEquals(
                                admissions[0].LocalPlayerHost,
                                admissions[1].LocalPlayerHost),
                            "P3M5B reused-Host scene no longer reuses one Local Player Host.");
                        Require(
                            !ReferenceEquals(
                                admissions[0].PlayerSlotProfile,
                                admissions[1].PlayerSlotProfile),
                            "P3M5B reused-Host scene unexpectedly duplicates one Player Slot Profile.");
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
            Require(
                admission.PlayerSlotProfile != null &&
                admission.LocalPlayerHost != null &&
                admission.ActorProfile != null &&
                admission.SceneLogicalPlayerActor != null,
                $"P3M5B admission '{admission.name}' contains a missing or Unity fake-null " +
                $"reference. {ReferenceDiagnostic(admission)}");
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
