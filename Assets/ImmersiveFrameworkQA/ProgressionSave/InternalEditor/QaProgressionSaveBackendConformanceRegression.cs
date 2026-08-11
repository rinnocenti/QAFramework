using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Immersive.Framework.ProgressionSave;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.ProgressionSave.Internal.Editor
{
    public static class QaProgressionSaveBackendConformanceRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Progression Save/" +
            "Run ADR-018 Backend Conformance";

        private const string Prefix =
            "[ADR018_QA_BACKEND_CONFORMANCE]";

        private const int ContractCaseCount = 9;
        private const int PerBackendCoreCaseCount = 13;
        private const int CatalogCaseCount = 5;
        private const int NegativeProjectionCaseCount = 7;

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun()
        {
            return !EditorApplication.isPlaying;
        }

        [MenuItem(MenuPath)]
        private static void Run()
        {
            var contractCases =
                new CaseCounter(ContractCaseCount);
            ValidateContractShape(contractCases);

            string jsonRoot =
                Path.GetFullPath(
                    Path.Combine(
                        "Library",
                        "ImmersiveFrameworkQA",
                        "ADR018",
                        Guid.NewGuid().ToString("N")));

            try
            {
                var jsonStore =
                    new JsonProgressionSaveStore(
                        jsonRoot,
                        ProgressionSaveBackendId.From(
                            "qa.json"));

                var memoryStore =
                    new QaInMemoryProgressionSaveStore(
                        "qa.memory");

                var jsonCases =
                    new CaseCounter(PerBackendCoreCaseCount);
                BackendSuiteEvidence jsonEvidence =
                    RunCoreSuite(
                        "json",
                        jsonStore,
                        jsonCases);
                jsonCases.RequireComplete();

                var memoryCases =
                    new CaseCounter(PerBackendCoreCaseCount);
                BackendSuiteEvidence memoryEvidence =
                    RunCoreSuite(
                        "memory",
                        memoryStore,
                        memoryCases);
                memoryCases.RequireComplete();

                Require(
                    jsonEvidence.SemanticFingerprint ==
                    memoryEvidence.SemanticFingerprint,
                    "ADR-018 core runtime semantics differ between JSON and alternate backend.");

                var catalogCases =
                    new CaseCounter(CatalogCaseCount);
                ValidateCatalogBoundary(
                    jsonStore,
                    memoryStore,
                    catalogCases);
                catalogCases.RequireComplete();

                var negativeCases =
                    new CaseCounter(
                        NegativeProjectionCaseCount);
                ValidateNegativeProjection(
                    memoryStore,
                    negativeCases);
                negativeCases.RequireComplete();

                contractCases.RequireComplete();

                Debug.Log(
                    $"{Prefix} status='Passed' " +
                    $"contractCases='{ContractCaseCount}' " +
                    $"jsonCoreCases='{PerBackendCoreCaseCount}' " +
                    $"alternateCoreCases='{PerBackendCoreCaseCount}' " +
                    $"catalogCases='{CatalogCaseCount}' " +
                    $"negativeCases='{NegativeProjectionCaseCount}' " +
                    $"jsonBackend='{jsonStore.BackendId.StableText}' " +
                    $"alternateBackend='{memoryStore.BackendId.StableText}' " +
                    $"alternateCatalog='False' " +
                    $"consumerRuntime='ProgressionSaveRuntime' " +
                    $"semanticFingerprint='{jsonEvidence.SemanticFingerprint}'.");
            }
            finally
            {
                if (Directory.Exists(jsonRoot))
                {
                    Directory.Delete(
                        jsonRoot,
                        recursive: true);
                }
            }
        }

        private static void ValidateContractShape(
            CaseCounter cases)
        {
            Type storeType =
                typeof(IProgressionSaveStore);

            PropertyInfo[] storeProperties =
                storeType.GetProperties();
            MethodInfo[] storeMethods =
                storeType
                    .GetMethods()
                    .Where(method => !method.IsSpecialName)
                    .ToArray();

            Require(
                storeProperties.Length == 1 &&
                storeProperties[0].Name == "BackendId" &&
                storeProperties[0].PropertyType ==
                    typeof(ProgressionSaveBackendId),
                "ADR-018 core store must expose only BackendId as a property.");
            cases.Complete();

            string[] expectedMethods =
            {
                "DeleteSlot",
                "ReadSlot",
                "WriteSlot"
            };

            string[] actualMethods =
                storeMethods
                    .Select(method => method.Name)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .ToArray();

            Require(
                actualMethods.SequenceEqual(
                    expectedMethods),
                $"ADR-018 core store method shape mismatch. actual='{string.Join(",", actualMethods)}'.");
            cases.Complete();

            Require(
                storeType.GetMethod("ReadManifest") == null &&
                storeType.GetMethod("WriteManifest") == null &&
                storeType.GetMethod("ContainsSlot") == null,
                "ADR-018 core store still exposes catalog/manifest maintenance.");
            cases.Complete();

            Type catalogType =
                typeof(IProgressionSaveCatalog);

            MethodInfo[] catalogMethods =
                catalogType
                    .GetMethods()
                    .Where(method => !method.IsSpecialName)
                    .ToArray();

            Require(
                catalogType.GetProperties().Length == 0 &&
                catalogMethods.Length == 1 &&
                catalogMethods[0].Name == "ReadManifest" &&
                catalogMethods[0].ReturnType ==
                    typeof(ProgressionSaveManifestReadResult),
                "ADR-018 optional catalog contract must expose only ReadManifest.");
            cases.Complete();

            Require(
                typeof(IProgressionSaveStore)
                    .IsAssignableFrom(
                        typeof(JsonProgressionSaveStore)),
                "Built-in JSON backend does not implement the core store contract.");
            cases.Complete();

            Require(
                typeof(IProgressionSaveCatalog)
                    .IsAssignableFrom(
                        typeof(JsonProgressionSaveStore)),
                "Built-in JSON backend does not implement the optional catalog capability.");
            cases.Complete();

            Require(
                typeof(IProgressionSaveStore)
                    .IsAssignableFrom(
                        typeof(QaInMemoryProgressionSaveStore)),
                "QA alternate backend does not implement the core store contract.");
            cases.Complete();

            Require(
                !typeof(IProgressionSaveCatalog)
                    .IsAssignableFrom(
                        typeof(QaInMemoryProgressionSaveStore)),
                "QA alternate backend must prove catalog is optional.");
            cases.Complete();

            Type internalManifestWriteResult =
                typeof(IProgressionSaveStore)
                    .Assembly
                    .GetType(
                        "Immersive.Framework.ProgressionSave." +
                        "ProgressionSaveManifestWriteResult");

            Require(
                internalManifestWriteResult != null &&
                internalManifestWriteResult.IsNotPublic,
                "Manifest write result must not remain a public backend contract surface.");
            cases.Complete();
        }

        private static BackendSuiteEvidence RunCoreSuite(
            string label,
            IProgressionSaveStore store,
            CaseCounter cases)
        {
            Require(
                store != null &&
                store.BackendId.IsValid,
                $"{label}: backend identity is invalid.");
            cases.Complete();

            var runtime =
                new ProgressionSaveRuntime(store);

            Require(
                ReferenceEquals(
                    runtime.Store,
                    store) &&
                runtime.BackendId == store.BackendId,
                $"{label}: runtime did not retain the explicitly injected backend.");
            cases.Complete();

            ProgressionSaveSlotId slot =
                ProgressionSaveSlotId.From(
                    $"qa.{label}.primary");
            ProgressionSaveMoment moment =
                ProgressionSaveMoment.Manual(
                    $"qa.{label}.moment",
                    "ADR018-QA",
                    "backend-conformance");

            ProgressionSaveRequestResult initialMissing =
                runtime.Request(
                    ProgressionSaveRequest.Load(
                        $"qa.{label}.load-missing",
                        slot,
                        moment,
                        "ADR018-QA",
                        "initial-missing"));

            Require(
                initialMissing.Status ==
                    ProgressionSaveRequestStatus.Missing &&
                initialMissing.BackendId ==
                    store.BackendId,
                $"{label}: initial missing-load semantics failed.");
            cases.Complete();

            ProgressionSavePayload payloadA =
                ProgressionSavePayload.FromText(
                    "alpha",
                    "text/plain");

            ProgressionSaveRequestResult saveA =
                runtime.Request(
                    ProgressionSaveRequest.Save(
                        $"qa.{label}.save-a",
                        slot,
                        ProgressionSaveRecordId.From(
                            $"qa.{label}.record-a"),
                        payloadA,
                        "QA Slot A",
                        moment,
                        "ADR018-QA",
                        "save-a"));

            Require(
                saveA.Status ==
                    ProgressionSaveRequestStatus.Saved &&
                saveA.HasRecord &&
                saveA.Record.Payload == payloadA,
                $"{label}: save A semantics failed.");
            cases.Complete();

            Require(
                saveA.Record.SlotId == slot &&
                saveA.Record.RecordId ==
                    ProgressionSaveRecordId.From(
                        $"qa.{label}.record-a") &&
                saveA.Record.DisplayName ==
                    "QA Slot A",
                $"{label}: saved record identity/projection failed.");
            cases.Complete();

            ProgressionSaveRequestResult loadA =
                runtime.Request(
                    ProgressionSaveRequest.Load(
                        $"qa.{label}.load-a",
                        slot,
                        moment,
                        "ADR018-QA",
                        "load-a"));

            Require(
                loadA.Status ==
                    ProgressionSaveRequestStatus.Loaded &&
                loadA.HasRecord &&
                loadA.Record == saveA.Record,
                $"{label}: load A roundtrip failed.");
            cases.Complete();

            ProgressionSavePayload payloadB =
                ProgressionSavePayload.FromText(
                    "beta",
                    "text/plain");

            ProgressionSaveRequestResult saveB =
                runtime.Request(
                    ProgressionSaveRequest.Save(
                        $"qa.{label}.save-b",
                        slot,
                        ProgressionSaveRecordId.From(
                            $"qa.{label}.record-b"),
                        payloadB,
                        "QA Slot B",
                        moment,
                        "ADR018-QA",
                        "save-b"));

            Require(
                saveB.Status ==
                    ProgressionSaveRequestStatus.Saved &&
                saveB.HasRecord &&
                saveB.Record.Payload == payloadB,
                $"{label}: overwrite save semantics failed.");
            cases.Complete();

            ProgressionSaveRequestResult loadB =
                runtime.Request(
                    ProgressionSaveRequest.Load(
                        $"qa.{label}.load-b",
                        slot,
                        moment,
                        "ADR018-QA",
                        "load-b"));

            Require(
                loadB.Status ==
                    ProgressionSaveRequestStatus.Loaded &&
                loadB.Record == saveB.Record &&
                loadB.Record != saveA.Record,
                $"{label}: latest-record load semantics failed.");
            cases.Complete();

            ProgressionSaveRequestResult deleted =
                runtime.Request(
                    ProgressionSaveRequest.Delete(
                        $"qa.{label}.delete",
                        slot,
                        moment,
                        "ADR018-QA",
                        "delete"));

            Require(
                deleted.Status ==
                    ProgressionSaveRequestStatus.Deleted &&
                deleted.BackendId ==
                    store.BackendId,
                $"{label}: delete semantics failed.");
            cases.Complete();

            ProgressionSaveRequestResult missingAfterDelete =
                runtime.Request(
                    ProgressionSaveRequest.Load(
                        $"qa.{label}.load-after-delete",
                        slot,
                        moment,
                        "ADR018-QA",
                        "load-after-delete"));

            Require(
                missingAfterDelete.Status ==
                    ProgressionSaveRequestStatus.Missing,
                $"{label}: load-after-delete must be Missing.");
            cases.Complete();

            ProgressionSaveRequestResult deleteMissing =
                runtime.Request(
                    ProgressionSaveRequest.Delete(
                        $"qa.{label}.delete-missing",
                        slot,
                        moment,
                        "ADR018-QA",
                        "delete-missing"));

            Require(
                deleteMissing.Status ==
                    ProgressionSaveRequestStatus.Missing,
                $"{label}: repeated delete must be Missing.");
            cases.Complete();

            Require(
                saveA.BackendId == store.BackendId &&
                loadA.BackendId == store.BackendId &&
                saveB.BackendId == store.BackendId &&
                loadB.BackendId == store.BackendId &&
                deleted.BackendId == store.BackendId,
                $"{label}: runtime result backend identity projection failed.");
            cases.Complete();

            const string fingerprint =
                "Missing>Saved>Loaded>Saved>Loaded>Deleted>Missing>Missing";

            Require(
                BuildFingerprint(
                    initialMissing,
                    saveA,
                    loadA,
                    saveB,
                    loadB,
                    deleted,
                    missingAfterDelete,
                    deleteMissing) ==
                    fingerprint,
                $"{label}: semantic fingerprint mismatch.");
            cases.Complete();

            return new BackendSuiteEvidence(
                fingerprint);
        }

        private static void ValidateCatalogBoundary(
            JsonProgressionSaveStore jsonStore,
            QaInMemoryProgressionSaveStore memoryStore,
            CaseCounter cases)
        {
            Require(
                jsonStore is IProgressionSaveCatalog,
                "Built-in JSON backend must expose the optional catalog.");
            cases.Complete();

            Require(
                !((object)memoryStore is IProgressionSaveCatalog),
                "Core-only alternate backend unexpectedly exposes catalog.");
            cases.Complete();

            IProgressionSaveCatalog catalog =
                jsonStore;

            ProgressionSaveManifestReadResult initial =
                catalog.ReadManifest();

            Require(
                initial.Status ==
                    ProgressionSaveReadStatus.Found &&
                initial.HasManifest &&
                initial.Manifest.Count == 0,
                "JSON catalog after core-suite cleanup must be a valid empty manifest.");
            cases.Complete();

            ProgressionSaveSlotId slot =
                ProgressionSaveSlotId.From(
                    "qa.json.catalog");

            var runtime =
                new ProgressionSaveRuntime(jsonStore);

            ProgressionSaveMoment moment =
                ProgressionSaveMoment.Manual(
                    "qa.json.catalog.moment",
                    "ADR018-QA",
                    "catalog");

            ProgressionSaveRequestResult save =
                runtime.Request(
                    ProgressionSaveRequest.Save(
                        "qa.json.catalog.save",
                        slot,
                        ProgressionSaveRecordId.From(
                            "qa.json.catalog.record"),
                        ProgressionSavePayload.FromText(
                            "catalog",
                            "text/plain"),
                        "Catalog QA",
                        moment,
                        "ADR018-QA",
                        "catalog-save"));

            ProgressionSaveManifestReadResult afterSave =
                catalog.ReadManifest();

            Require(
                save.Status ==
                    ProgressionSaveRequestStatus.Saved &&
                afterSave.HasManifest &&
                afterSave.Manifest.ContainsSlot(slot),
                "JSON optional catalog did not project the saved slot.");
            cases.Complete();

            ProgressionSaveRequestResult delete =
                runtime.Request(
                    ProgressionSaveRequest.Delete(
                        "qa.json.catalog.delete",
                        slot,
                        moment,
                        "ADR018-QA",
                        "catalog-delete"));

            ProgressionSaveManifestReadResult afterDelete =
                catalog.ReadManifest();

            Require(
                delete.Status ==
                    ProgressionSaveRequestStatus.Deleted &&
                afterDelete.HasManifest &&
                !afterDelete.Manifest.ContainsSlot(slot),
                "JSON optional catalog did not remove the deleted slot.");
            cases.Complete();
        }

        private static void ValidateNegativeProjection(
            QaInMemoryProgressionSaveStore store,
            CaseCounter cases)
        {
            var runtime =
                new ProgressionSaveRuntime(store);

            ProgressionSaveSlotId slot =
                ProgressionSaveSlotId.From(
                    "qa.memory.negative");
            ProgressionSaveMoment moment =
                ProgressionSaveMoment.Manual(
                    "qa.memory.negative.moment",
                    "ADR018-QA",
                    "negative");

            store.Fault =
                QaProgressionSaveFault.BackendUnavailable;

            ProgressionSaveRequestResult unavailableSave =
                runtime.Request(
                    ProgressionSaveRequest.Save(
                        "qa.memory.unavailable.save",
                        slot,
                        ProgressionSaveRecordId.From(
                            "qa.memory.unavailable.record"),
                        ProgressionSavePayload.FromText(
                            "unavailable",
                            "text/plain"),
                        "Unavailable",
                        moment,
                        "ADR018-QA",
                        "unavailable-save"));

            Require(
                unavailableSave.Status ==
                    ProgressionSaveRequestStatus.BackendUnavailable,
                "BackendUnavailable write was not projected by ProgressionSaveRuntime.");
            cases.Complete();

            ProgressionSaveRequestResult unavailableLoad =
                runtime.Request(
                    ProgressionSaveRequest.Load(
                        "qa.memory.unavailable.load",
                        slot,
                        moment,
                        "ADR018-QA",
                        "unavailable-load"));

            Require(
                unavailableLoad.Status ==
                    ProgressionSaveRequestStatus.BackendUnavailable,
                "BackendUnavailable read was not projected by ProgressionSaveRuntime.");
            cases.Complete();

            ProgressionSaveRequestResult unavailableDelete =
                runtime.Request(
                    ProgressionSaveRequest.Delete(
                        "qa.memory.unavailable.delete",
                        slot,
                        moment,
                        "ADR018-QA",
                        "unavailable-delete"));

            Require(
                unavailableDelete.Status ==
                    ProgressionSaveRequestStatus.BackendUnavailable,
                "BackendUnavailable delete was not projected by ProgressionSaveRuntime.");
            cases.Complete();

            store.Fault =
                QaProgressionSaveFault.CorruptRead;

            ProgressionSaveRequestResult corrupt =
                runtime.Request(
                    ProgressionSaveRequest.Load(
                        "qa.memory.corrupt.load",
                        slot,
                        moment,
                        "ADR018-QA",
                        "corrupt-load"));

            Require(
                corrupt.Status ==
                    ProgressionSaveRequestStatus.Corrupt,
                "Corrupt read was not projected by ProgressionSaveRuntime.");
            cases.Complete();

            store.Fault =
                QaProgressionSaveFault.FailedWrite;

            ProgressionSaveRequestResult failedWrite =
                runtime.Request(
                    ProgressionSaveRequest.Save(
                        "qa.memory.failed.write",
                        slot,
                        ProgressionSaveRecordId.From(
                            "qa.memory.failed.record"),
                        ProgressionSavePayload.FromText(
                            "failed",
                            "text/plain"),
                        "Failed",
                        moment,
                        "ADR018-QA",
                        "failed-write"));

            Require(
                failedWrite.Status ==
                    ProgressionSaveRequestStatus.Failed,
                "Failed write was not projected by ProgressionSaveRuntime.");
            cases.Complete();

            store.Fault =
                QaProgressionSaveFault.RejectedWrite;

            ProgressionSaveRequestResult rejectedWrite =
                runtime.Request(
                    ProgressionSaveRequest.Save(
                        "qa.memory.rejected.write",
                        slot,
                        ProgressionSaveRecordId.From(
                            "qa.memory.rejected.record"),
                        ProgressionSavePayload.FromText(
                            "rejected",
                            "text/plain"),
                        "Rejected",
                        moment,
                        "ADR018-QA",
                        "rejected-write"));

            Require(
                rejectedWrite.Status ==
                    ProgressionSaveRequestStatus.Rejected,
                "Rejected write was not projected by ProgressionSaveRuntime.");
            cases.Complete();

            store.Fault =
                QaProgressionSaveFault.FailedDelete;

            ProgressionSaveRequestResult failedDelete =
                runtime.Request(
                    ProgressionSaveRequest.Delete(
                        "qa.memory.failed.delete",
                        slot,
                        moment,
                        "ADR018-QA",
                        "failed-delete"));

            Require(
                failedDelete.Status ==
                    ProgressionSaveRequestStatus.Failed,
                "Failed delete was not projected by ProgressionSaveRuntime.");
            cases.Complete();

            store.Fault =
                QaProgressionSaveFault.None;
        }

        private static string BuildFingerprint(
            params ProgressionSaveRequestResult[] results)
        {
            return string.Join(
                ">",
                results.Select(
                    result => result.Status.ToString()));
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

        private readonly struct BackendSuiteEvidence
        {
            internal BackendSuiteEvidence(
                string semanticFingerprint)
            {
                SemanticFingerprint =
                    semanticFingerprint;
            }

            internal string SemanticFingerprint { get; }
        }

        private sealed class CaseCounter
        {
            private readonly int _expected;
            private int _completed;

            internal CaseCounter(int expected)
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
                    $"ADR-018 QA case-count mismatch. " +
                    $"completed='{_completed}' expected='{_expected}'.");
            }
        }
    }
}
