using System;
using Immersive.Framework.Authoring;
using Immersive.Framework.ProgressionSave;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.ProgressionSave.Internal.Editor
{
    public static class QaProgressionSaveProductCompositionRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Progression Save/" +
            "Run ADR-018 Product Composition";

        private const string Prefix =
            "[ADR018_QA_PRODUCT_COMPOSITION]";

        private const int ExpectedCaseCount = 12;
        private const int ExpectedNegativeCaseCount = 7;

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun()
        {
            return !EditorApplication.isPlaying;
        }

        [MenuItem(MenuPath)]
        private static void Run()
        {
            var cases =
                new CaseCounter(
                    ExpectedCaseCount);

            int negativePassed = 0;

            ValidateDisabledComposition(cases);
            ValidateBuiltInComposition(cases);
            ValidateCustomComposition(cases);

            negativePassed +=
                ValidateMissingProfileRejected(cases);
            negativePassed +=
                ValidateMissingProviderRejected(cases);
            negativePassed +=
                ValidateInvalidProviderRejected(cases);
            negativePassed +=
                ValidateProviderCreateFailureRejected(cases);
            negativePassed +=
                ValidateProviderNullStoreRejected(cases);
            negativePassed +=
                ValidateProviderInvalidBackendRejected(cases);
            negativePassed +=
                ValidateProviderExceptionRejected(cases);

            ValidateBuiltInSelectionDoesNotInvokeCustomProvider(cases);
            ValidateRuntimeUsesSelectedCustomBackend(cases);

            cases.RequireComplete();

            Require(
                negativePassed ==
                    ExpectedNegativeCaseCount,
                $"ADR-018 composition negative-case mismatch. " +
                $"passed='{negativePassed}' expected='{ExpectedNegativeCaseCount}'.");

            Debug.Log(
                $"{Prefix} status='Passed' " +
                $"cases='{ExpectedCaseCount}' " +
                $"disabled='Passed' " +
                $"builtIn='Passed' " +
                $"custom='Passed' " +
                $"negative='{negativePassed}/{ExpectedNegativeCaseCount}' " +
                $"noFallback='Passed' " +
                $"selectionIsolation='Passed' " +
                $"runtimeRequest='Passed' " +
                $"composition='ProgressionSaveApplicationComposition'.");
        }

        private static void ValidateDisabledComposition(
            CaseCounter cases)
        {
            GameApplicationAsset application =
                CreateApplication(
                    "QA Disabled",
                    enabled: false,
                    profile: null);

            try
            {
                ProgressionSaveApplicationCompositionResult result =
                    ProgressionSaveApplicationComposition.Resolve(
                        application);

                Require(
                    result.Succeeded &&
                    result.Status ==
                        ProgressionSaveApplicationCompositionStatus.Disabled &&
                    !result.Configured &&
                    !result.HasRuntime &&
                    !result.HasProfile,
                    "Disabled Game Application did not resolve to explicit Disabled composition.");

                cases.Complete();
            }
            finally
            {
                Destroy(application);
            }
        }

        private static void ValidateBuiltInComposition(
            CaseCounter cases)
        {
            ProgressionSaveProfile profile =
                CreateProfile(
                    ProgressionSaveBackendSelection.BuiltInJson,
                    null);

            GameApplicationAsset application =
                CreateApplication(
                    "QA Built In",
                    enabled: true,
                    profile);

            try
            {
                ProgressionSaveApplicationCompositionResult result =
                    ProgressionSaveApplicationComposition.Resolve(
                        application);

                Require(
                    result.Succeeded &&
                    result.Configured &&
                    result.Status ==
                        ProgressionSaveApplicationCompositionStatus.Ready &&
                    result.Profile == profile &&
                    result.Runtime != null &&
                    result.Runtime.Store is JsonProgressionSaveStore &&
                    result.BackendId.IsValid &&
                    result.Runtime.BackendId ==
                        result.BackendId,
                    "Built-in JSON Profile did not resolve to a ready JSON-backed runtime.");

                cases.Complete();
            }
            finally
            {
                Destroy(application);
                Destroy(profile);
            }
        }

        private static void ValidateCustomComposition(
            CaseCounter cases)
        {
            QaProgressionSaveStoreProviderAsset provider =
                CreateProvider(
                    QaProgressionSaveProviderMode.SuccessMemory);

            ProgressionSaveProfile profile =
                CreateProfile(
                    ProgressionSaveBackendSelection.CustomProvider,
                    provider);

            GameApplicationAsset application =
                CreateApplication(
                    "QA Custom",
                    enabled: true,
                    profile);

            try
            {
                ProgressionSaveApplicationCompositionResult result =
                    ProgressionSaveApplicationComposition.Resolve(
                        application);

                Require(
                    result.Succeeded &&
                    result.Configured &&
                    result.Runtime != null &&
                    result.Runtime.Store is
                        QaInMemoryProgressionSaveStore &&
                    result.BackendId.StableText ==
                        "ProgressionSave:qa.composition.custom" &&
                    provider.CreateCount == 1,
                    "Custom Provider did not materialize the selected alternate backend.");

                cases.Complete();
            }
            finally
            {
                Destroy(application);
                Destroy(profile);
                Destroy(provider);
            }
        }

        private static int ValidateMissingProfileRejected(
            CaseCounter cases)
        {
            GameApplicationAsset application =
                CreateApplication(
                    "QA Missing Profile",
                    enabled: true,
                    profile: null);

            try
            {
                ProgressionSaveApplicationCompositionResult result =
                    ProgressionSaveApplicationComposition.Resolve(
                        application);

                Require(
                    result.Failed &&
                    result.Status ==
                        ProgressionSaveApplicationCompositionStatus.Rejected &&
                    !result.HasRuntime &&
                    !result.HasProfile,
                    "Enabled application without Profile was not rejected.");

                cases.Complete();
                return 1;
            }
            finally
            {
                Destroy(application);
            }
        }

        private static int ValidateMissingProviderRejected(
            CaseCounter cases)
        {
            ProgressionSaveProfile profile =
                CreateProfile(
                    ProgressionSaveBackendSelection.CustomProvider,
                    null);

            GameApplicationAsset application =
                CreateApplication(
                    "QA Missing Provider",
                    enabled: true,
                    profile);

            try
            {
                ProgressionSaveApplicationCompositionResult result =
                    ProgressionSaveApplicationComposition.Resolve(
                        application);

                Require(
                    result.Failed &&
                    !result.HasRuntime &&
                    result.HasProfile &&
                    result.Message.IndexOf(
                        "no provider",
                        StringComparison.OrdinalIgnoreCase) >= 0,
                    "Custom Provider selection without provider asset was not rejected.");

                cases.Complete();
                return 1;
            }
            finally
            {
                Destroy(application);
                Destroy(profile);
            }
        }

        private static int ValidateInvalidProviderRejected(
            CaseCounter cases)
        {
            QaProgressionSaveStoreProviderAsset provider =
                CreateProvider(
                    QaProgressionSaveProviderMode.InvalidConfiguration);

            ProgressionSaveProfile profile =
                CreateProfile(
                    ProgressionSaveBackendSelection.CustomProvider,
                    provider);

            GameApplicationAsset application =
                CreateApplication(
                    "QA Invalid Provider",
                    enabled: true,
                    profile);

            try
            {
                ProgressionSaveApplicationCompositionResult result =
                    ProgressionSaveApplicationComposition.Resolve(
                        application);

                Require(
                    result.Failed &&
                    !result.HasRuntime &&
                    provider.ValidateCount > 0 &&
                    provider.CreateCount == 0,
                    "Invalid custom provider was not rejected before materialization.");

                cases.Complete();
                return 1;
            }
            finally
            {
                Destroy(application);
                Destroy(profile);
                Destroy(provider);
            }
        }

        private static int ValidateProviderCreateFailureRejected(
            CaseCounter cases)
        {
            QaProgressionSaveStoreProviderAsset provider =
                CreateProvider(
                    QaProgressionSaveProviderMode.CreateFailure);

            ProgressionSaveProfile profile =
                CreateProfile(
                    ProgressionSaveBackendSelection.CustomProvider,
                    provider);

            GameApplicationAsset application =
                CreateApplication(
                    "QA Create Failure",
                    enabled: true,
                    profile);

            try
            {
                ProgressionSaveApplicationCompositionResult result =
                    ProgressionSaveApplicationComposition.Resolve(
                        application);

                Require(
                    result.Failed &&
                    !result.HasRuntime &&
                    provider.CreateCount == 1 &&
                    result.Message.IndexOf(
                        "No fallback backend was used",
                        StringComparison.Ordinal) >= 0,
                    "Custom provider creation failure did not reject explicitly without fallback.");

                cases.Complete();
                return 1;
            }
            finally
            {
                Destroy(application);
                Destroy(profile);
                Destroy(provider);
            }
        }

        private static int ValidateProviderNullStoreRejected(
            CaseCounter cases)
        {
            QaProgressionSaveStoreProviderAsset provider =
                CreateProvider(
                    QaProgressionSaveProviderMode.NullStore);

            ProgressionSaveProfile profile =
                CreateProfile(
                    ProgressionSaveBackendSelection.CustomProvider,
                    provider);

            GameApplicationAsset application =
                CreateApplication(
                    "QA Null Store",
                    enabled: true,
                    profile);

            try
            {
                ProgressionSaveApplicationCompositionResult result =
                    ProgressionSaveApplicationComposition.Resolve(
                        application);

                Require(
                    result.Failed &&
                    !result.HasRuntime &&
                    provider.CreateCount == 1 &&
                    result.Message.IndexOf(
                        "produced no store",
                        StringComparison.OrdinalIgnoreCase) >= 0,
                    "Provider success with null store was not rejected.");

                cases.Complete();
                return 1;
            }
            finally
            {
                Destroy(application);
                Destroy(profile);
                Destroy(provider);
            }
        }

        private static int ValidateProviderInvalidBackendRejected(
            CaseCounter cases)
        {
            QaProgressionSaveStoreProviderAsset provider =
                CreateProvider(
                    QaProgressionSaveProviderMode.InvalidBackendId);

            ProgressionSaveProfile profile =
                CreateProfile(
                    ProgressionSaveBackendSelection.CustomProvider,
                    provider);

            GameApplicationAsset application =
                CreateApplication(
                    "QA Invalid Backend",
                    enabled: true,
                    profile);

            try
            {
                ProgressionSaveApplicationCompositionResult result =
                    ProgressionSaveApplicationComposition.Resolve(
                        application);

                Require(
                    result.Failed &&
                    !result.HasRuntime &&
                    provider.CreateCount == 1 &&
                    result.Message.IndexOf(
                        "invalid BackendId",
                        StringComparison.OrdinalIgnoreCase) >= 0,
                    "Provider store with invalid BackendId was not rejected.");

                cases.Complete();
                return 1;
            }
            finally
            {
                Destroy(application);
                Destroy(profile);
                Destroy(provider);
            }
        }

        private static int ValidateProviderExceptionRejected(
            CaseCounter cases)
        {
            QaProgressionSaveStoreProviderAsset provider =
                CreateProvider(
                    QaProgressionSaveProviderMode.ThrowOnCreate);

            ProgressionSaveProfile profile =
                CreateProfile(
                    ProgressionSaveBackendSelection.CustomProvider,
                    provider);

            GameApplicationAsset application =
                CreateApplication(
                    "QA Provider Exception",
                    enabled: true,
                    profile);

            try
            {
                ProgressionSaveApplicationCompositionResult result =
                    ProgressionSaveApplicationComposition.Resolve(
                        application);

                Require(
                    result.Failed &&
                    !result.HasRuntime &&
                    provider.CreateCount == 1 &&
                    result.Message.IndexOf(
                        "No fallback backend was used",
                        StringComparison.Ordinal) >= 0,
                    "Provider exception did not become explicit rejected composition.");

                cases.Complete();
                return 1;
            }
            finally
            {
                Destroy(application);
                Destroy(profile);
                Destroy(provider);
            }
        }

        private static void ValidateBuiltInSelectionDoesNotInvokeCustomProvider(
            CaseCounter cases)
        {
            QaProgressionSaveStoreProviderAsset provider =
                CreateProvider(
                    QaProgressionSaveProviderMode.ThrowOnCreate);

            ProgressionSaveProfile profile =
                CreateProfile(
                    ProgressionSaveBackendSelection.BuiltInJson,
                    provider);

            GameApplicationAsset application =
                CreateApplication(
                    "QA Built In Isolation",
                    enabled: true,
                    profile);

            try
            {
                ProgressionSaveApplicationCompositionResult result =
                    ProgressionSaveApplicationComposition.Resolve(
                        application);

                Require(
                    result.Succeeded &&
                    result.Runtime.Store is JsonProgressionSaveStore &&
                    provider.ValidateCount == 0 &&
                    provider.CreateCount == 0,
                    "Built-in selection consulted the stale/unselected Custom Provider.");

                cases.Complete();
            }
            finally
            {
                Destroy(application);
                Destroy(profile);
                Destroy(provider);
            }
        }

        private static void ValidateRuntimeUsesSelectedCustomBackend(
            CaseCounter cases)
        {
            QaProgressionSaveStoreProviderAsset provider =
                CreateProvider(
                    QaProgressionSaveProviderMode.SuccessMemory);

            ProgressionSaveProfile profile =
                CreateProfile(
                    ProgressionSaveBackendSelection.CustomProvider,
                    provider);

            GameApplicationAsset application =
                CreateApplication(
                    "QA Runtime Request",
                    enabled: true,
                    profile);

            try
            {
                ProgressionSaveApplicationCompositionResult composition =
                    ProgressionSaveApplicationComposition.Resolve(
                        application);

                Require(
                    composition.Succeeded &&
                    composition.Runtime != null,
                    "Runtime request case could not compose custom backend.");

                ProgressionSaveSlotId slot =
                    ProgressionSaveSlotId.From(
                        "qa.composition.runtime.slot");

                ProgressionSaveMoment moment =
                    ProgressionSaveMoment.Manual(
                        "qa.composition.runtime.moment",
                        "ADR018-QA",
                        "composition-runtime");

                ProgressionSaveRequestResult save =
                    composition.Runtime.Request(
                        ProgressionSaveRequest.Save(
                            "qa.composition.runtime.save",
                            slot,
                            ProgressionSaveRecordId.From(
                                "qa.composition.runtime.record"),
                            ProgressionSavePayload.FromText(
                                "composition",
                                "text/plain"),
                            "Composition QA",
                            moment,
                            "ADR018-QA",
                            "composition-runtime"));

                ProgressionSaveRequestResult load =
                    composition.Runtime.Request(
                        ProgressionSaveRequest.Load(
                            "qa.composition.runtime.load",
                            slot,
                            moment,
                            "ADR018-QA",
                            "composition-runtime"));

                Require(
                    save.Status ==
                        ProgressionSaveRequestStatus.Saved &&
                    load.Status ==
                        ProgressionSaveRequestStatus.Loaded &&
                    save.BackendId ==
                        composition.BackendId &&
                    load.BackendId ==
                        composition.BackendId &&
                    composition.BackendId.StableText ==
                        "ProgressionSave:qa.composition.custom",
                    "Composed ProgressionSaveRuntime did not execute requests through the selected custom backend.");

                cases.Complete();
            }
            finally
            {
                Destroy(application);
                Destroy(profile);
                Destroy(provider);
            }
        }

        private static GameApplicationAsset CreateApplication(
            string applicationName,
            bool enabled,
            ProgressionSaveProfile profile)
        {
            var application =
                ScriptableObject.CreateInstance<GameApplicationAsset>();

            var serialized =
                new SerializedObject(application);

            serialized.FindProperty(
                    "applicationName")
                .stringValue =
                    applicationName;

            serialized.FindProperty(
                    "progressionSaveEnabled")
                .boolValue =
                    enabled;

            serialized.FindProperty(
                    "defaultProgressionSaveProfile")
                .objectReferenceValue =
                    profile;

            serialized.ApplyModifiedPropertiesWithoutUndo();

            return application;
        }

        private static ProgressionSaveProfile CreateProfile(
            ProgressionSaveBackendSelection selection,
            ProgressionSaveStoreProviderAsset provider)
        {
            var profile =
                ScriptableObject.CreateInstance<ProgressionSaveProfile>();

            var serialized =
                new SerializedObject(profile);

            serialized.FindProperty(
                    "backend")
                .intValue =
                    (int)selection;

            serialized.FindProperty(
                    "customProvider")
                .objectReferenceValue =
                    provider;

            serialized.ApplyModifiedPropertiesWithoutUndo();

            return profile;
        }

        private static QaProgressionSaveStoreProviderAsset CreateProvider(
            QaProgressionSaveProviderMode mode)
        {
            var provider =
                ScriptableObject.CreateInstance<
                    QaProgressionSaveStoreProviderAsset>();

            provider.Mode = mode;
            return provider;
        }

        private static void Destroy(
            UnityEngine.Object value)
        {
            if (value != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    value);
            }
        }

        private static void Require(
            bool condition,
            string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(
                    message);
            }
        }

        private sealed class CaseCounter
        {
            private readonly int _expected;
            private int _completed;

            internal CaseCounter(
                int expected)
            {
                _expected = expected;
            }

            internal void Complete()
            {
                _completed++;
            }

            internal void RequireComplete()
            {
                Require(
                    _completed == _expected,
                    $"ADR-018 Product Composition QA case-count mismatch. " +
                    $"completed='{_completed}' expected='{_expected}'.");
            }
        }
    }
}
