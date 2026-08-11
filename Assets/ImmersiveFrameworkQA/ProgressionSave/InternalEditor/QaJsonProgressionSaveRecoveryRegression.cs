using System;
using System.IO;
using System.Linq;
using Immersive.Framework.ProgressionSave;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.ProgressionSave.Internal.Editor
{
    public static class QaJsonProgressionSaveRecoveryRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Progression Save/" +
            "Run ADR-018 JSON Recovery";

        private const string Prefix =
            "[ADR018_QA_JSON_RECOVERY]";

        private const int ExpectedCaseCount = 18;

        private const int TransactionFormatVersion = 1;
        private const int OperationWrite = 10;
        private const int OperationDelete = 20;

        private const string ManifestFileName = "manifest.json";
        private const string SlotDirectoryName = "slots";

        private const string TransactionDirectoryName = ".transaction";
        private const string IntentFileName = "intent.json";
        private const string SlotStageFileName = "slot.stage.json";
        private const string ManifestStageFileName = "manifest.stage.json";

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun()
        {
            return !EditorApplication.isPlaying;
        }

        [MenuItem(MenuPath)]
        private static void Run()
        {
            string runRoot =
                Path.GetFullPath(
                    Path.Combine(
                        "Library",
                        "ImmersiveFrameworkQA",
                        "ADR018B",
                        Guid.NewGuid().ToString("N")));

            var cases =
                new CaseCounter(
                    ExpectedCaseCount);

            try
            {
                ValidateNormalWrite(
                    runRoot,
                    cases);

                ValidateNormalDelete(
                    runRoot,
                    cases);

                ValidateWriteRecoveryBeforeApply(
                    runRoot,
                    cases);

                ValidateWriteRecoveryAfterSlotApply(
                    runRoot,
                    cases);

                ValidateWriteRecoveryAfterManifestApply(
                    runRoot,
                    cases);

                ValidateDeleteRecoveryBeforeApply(
                    runRoot,
                    cases);

                ValidateDeleteRecoveryAfterSlotDelete(
                    runRoot,
                    cases);

                ValidateDeleteRecoveryAfterManifestApply(
                    runRoot,
                    cases);

                ValidateUncommittedStagingDiscard(
                    runRoot,
                    cases);

                ValidateCorruptIntentFailClosedRead(
                    runRoot,
                    cases);

                ValidateCorruptSlotStageFailClosed(
                    runRoot,
                    cases);

                ValidateCorruptManifestStageFailClosed(
                    runRoot,
                    cases);

                ValidateCorruptTransactionBlocksWrite(
                    runRoot,
                    cases);

                ValidateCorruptTransactionBlocksDelete(
                    runRoot,
                    cases);

                ValidateCorruptTransactionBlocksManifestRead(
                    runRoot,
                    cases);

                ValidateAlreadyAppliedReplayIsIdempotent(
                    runRoot,
                    cases);

                ValidateUnsupportedIntentVersionFailClosed(
                    runRoot,
                    cases);

                ValidateCleanMissingState(
                    runRoot,
                    cases);

                cases.RequireComplete();

                Debug.Log(
                    $"{Prefix} status='Passed' " +
                    $"cases='{ExpectedCaseCount}' " +
                    $"writeRecovery='3/3' " +
                    $"deleteRecovery='3/3' " +
                    $"uncommittedStaging='Discarded' " +
                    $"failClosed='6/6' " +
                    $"idempotentReplay='Passed' " +
                    $"normalWriteDelete='Passed' " +
                    $"transactionResidue='None' " +
                    $"backend='JsonProgressionSaveStore'.");
            }
            finally
            {
                DeleteDirectory(
                    runRoot);
            }
        }

        private static void ValidateNormalWrite(
            string runRoot,
            CaseCounter cases)
        {
            string root =
                ScenarioRoot(
                    runRoot,
                    "normal-write");

            ProgressionSaveSlotId slot =
                Slot("normal-write");

            ProgressionSaveSlotRecord record =
                Record(
                    slot,
                    "normal-write-a",
                    "alpha");

            var store =
                Store(
                    root,
                    "qa.json.normal-write");

            ProgressionSaveWriteResult write =
                store.WriteSlot(record);

            ProgressionSaveReadResult read =
                store.ReadSlot(slot);

            ProgressionSaveManifestReadResult manifest =
                store.ReadManifest();

            Require(
                write.Status ==
                    ProgressionSaveWriteStatus.Written &&
                read.Status ==
                    ProgressionSaveReadStatus.Found &&
                read.Record == record &&
                ManifestMatchesRecord(
                    manifest,
                    record) &&
                !HasTransaction(root),
                "Normal JSON write did not commit slot+manifest cleanly.");

            cases.Complete();
        }

        private static void ValidateNormalDelete(
            string runRoot,
            CaseCounter cases)
        {
            string root =
                ScenarioRoot(
                    runRoot,
                    "normal-delete");

            ProgressionSaveSlotId slot =
                Slot("normal-delete");

            ProgressionSaveSlotRecord record =
                Record(
                    slot,
                    "normal-delete-a",
                    "alpha");

            JsonProgressionSaveStore store =
                Seed(
                    root,
                    "qa.json.normal-delete",
                    record);

            ProgressionSaveDeleteResult delete =
                store.DeleteSlot(slot);

            ProgressionSaveReadResult read =
                store.ReadSlot(slot);

            ProgressionSaveManifestReadResult manifest =
                store.ReadManifest();

            Require(
                delete.Status ==
                    ProgressionSaveDeleteStatus.Deleted &&
                read.Status ==
                    ProgressionSaveReadStatus.Missing &&
                manifest.Status ==
                    ProgressionSaveReadStatus.Found &&
                manifest.HasManifest &&
                !manifest.Manifest.ContainsSlot(slot) &&
                !HasTransaction(root),
                "Normal JSON delete did not commit slot+manifest cleanly.");

            cases.Complete();
        }

        private static void ValidateWriteRecoveryBeforeApply(
            string runRoot,
            CaseCounter cases)
        {
            WriteRecoveryScenario(
                runRoot,
                "write-before-apply",
                applySlot: false,
                applyManifest: false,
                cases: cases);
        }

        private static void ValidateWriteRecoveryAfterSlotApply(
            string runRoot,
            CaseCounter cases)
        {
            WriteRecoveryScenario(
                runRoot,
                "write-after-slot",
                applySlot: true,
                applyManifest: false,
                cases: cases);
        }

        private static void ValidateWriteRecoveryAfterManifestApply(
            string runRoot,
            CaseCounter cases)
        {
            WriteRecoveryScenario(
                runRoot,
                "write-after-manifest",
                applySlot: false,
                applyManifest: true,
                cases: cases);
        }

        private static void WriteRecoveryScenario(
            string runRoot,
            string scenario,
            bool applySlot,
            bool applyManifest,
            CaseCounter cases)
        {
            string root =
                ScenarioRoot(
                    runRoot,
                    scenario);

            string producerRoot =
                root + "-producer";

            ProgressionSaveSlotId slot =
                Slot(scenario);

            ProgressionSaveSlotRecord oldRecord =
                Record(
                    slot,
                    $"{scenario}-old",
                    "old");

            ProgressionSaveSlotRecord newRecord =
                Record(
                    slot,
                    $"{scenario}-new",
                    "new");

            JsonProgressionSaveStore store =
                Seed(
                    root,
                    $"qa.json.{scenario}",
                    oldRecord);

            PrepareWriteTransaction(
                root,
                producerRoot,
                slot,
                newRecord);

            if (applySlot)
            {
                ApplySlotStage(
                    root);
            }

            if (applyManifest)
            {
                ApplyManifestStage(
                    root);
            }

            ProgressionSaveReadResult read =
                store.ReadSlot(slot);

            ProgressionSaveManifestReadResult manifest =
                store.ReadManifest();

            Require(
                read.Status ==
                    ProgressionSaveReadStatus.Found &&
                read.Record == newRecord &&
                ManifestMatchesRecord(
                    manifest,
                    newRecord) &&
                !HasTransaction(root) &&
                ContainsRecoveryDiagnostic(
                    read.Message,
                    manifest.Message),
                $"{scenario}: committed Write transaction did not converge to the new record.");

            cases.Complete();

            DeleteDirectory(
                producerRoot);
        }

        private static void ValidateDeleteRecoveryBeforeApply(
            string runRoot,
            CaseCounter cases)
        {
            DeleteRecoveryScenario(
                runRoot,
                "delete-before-apply",
                deleteSlot: false,
                applyManifest: false,
                cases: cases);
        }

        private static void ValidateDeleteRecoveryAfterSlotDelete(
            string runRoot,
            CaseCounter cases)
        {
            DeleteRecoveryScenario(
                runRoot,
                "delete-after-slot",
                deleteSlot: true,
                applyManifest: false,
                cases: cases);
        }

        private static void ValidateDeleteRecoveryAfterManifestApply(
            string runRoot,
            CaseCounter cases)
        {
            DeleteRecoveryScenario(
                runRoot,
                "delete-after-manifest",
                deleteSlot: false,
                applyManifest: true,
                cases: cases);
        }

        private static void DeleteRecoveryScenario(
            string runRoot,
            string scenario,
            bool deleteSlot,
            bool applyManifest,
            CaseCounter cases)
        {
            string root =
                ScenarioRoot(
                    runRoot,
                    scenario);

            string producerRoot =
                root + "-producer";

            ProgressionSaveSlotId slot =
                Slot(scenario);

            ProgressionSaveSlotRecord record =
                Record(
                    slot,
                    $"{scenario}-record",
                    "delete");

            JsonProgressionSaveStore store =
                Seed(
                    root,
                    $"qa.json.{scenario}",
                    record);

            string canonicalSlotPath =
                SingleSlotPath(
                    root);

            PrepareDeleteTransaction(
                root,
                producerRoot,
                slot,
                record);

            if (deleteSlot &&
                File.Exists(canonicalSlotPath))
            {
                File.Delete(
                    canonicalSlotPath);
            }

            if (applyManifest)
            {
                ApplyManifestStage(
                    root);
            }

            ProgressionSaveReadResult read =
                store.ReadSlot(slot);

            ProgressionSaveManifestReadResult manifest =
                store.ReadManifest();

            Require(
                read.Status ==
                    ProgressionSaveReadStatus.Missing &&
                manifest.Status ==
                    ProgressionSaveReadStatus.Found &&
                manifest.HasManifest &&
                !manifest.Manifest.ContainsSlot(slot) &&
                !File.Exists(canonicalSlotPath) &&
                !HasTransaction(root) &&
                ContainsRecoveryDiagnostic(
                    read.Message,
                    manifest.Message),
                $"{scenario}: committed Delete transaction did not converge to deleted state.");

            cases.Complete();

            DeleteDirectory(
                producerRoot);
        }

        private static void ValidateUncommittedStagingDiscard(
            string runRoot,
            CaseCounter cases)
        {
            string root =
                ScenarioRoot(
                    runRoot,
                    "uncommitted");

            string producerRoot =
                root + "-producer";

            ProgressionSaveSlotId slot =
                Slot("uncommitted");

            ProgressionSaveSlotRecord oldRecord =
                Record(
                    slot,
                    "uncommitted-old",
                    "old");

            ProgressionSaveSlotRecord stagedRecord =
                Record(
                    slot,
                    "uncommitted-new",
                    "new");

            JsonProgressionSaveStore store =
                Seed(
                    root,
                    "qa.json.uncommitted",
                    oldRecord);

            PrepareWriteStagesWithoutIntent(
                root,
                producerRoot,
                stagedRecord);

            ProgressionSaveReadResult read =
                store.ReadSlot(slot);

            ProgressionSaveManifestReadResult manifest =
                store.ReadManifest();

            Require(
                read.Status ==
                    ProgressionSaveReadStatus.Found &&
                read.Record == oldRecord &&
                ManifestMatchesRecord(
                    manifest,
                    oldRecord) &&
                !HasTransaction(root) &&
                read.Message.IndexOf(
                    "Discarded uncommitted",
                    StringComparison.Ordinal) >= 0,
                "Uncommitted transaction staging was not discarded without changing canonical state.");

            cases.Complete();

            DeleteDirectory(
                producerRoot);
        }

        private static void ValidateCorruptIntentFailClosedRead(
            string runRoot,
            CaseCounter cases)
        {
            string root =
                ScenarioRoot(
                    runRoot,
                    "corrupt-intent-read");

            ProgressionSaveSlotId slot =
                Slot("corrupt-intent-read");

            ProgressionSaveSlotRecord oldRecord =
                Record(
                    slot,
                    "corrupt-intent-old",
                    "old");

            JsonProgressionSaveStore store =
                Seed(
                    root,
                    "qa.json.corrupt-intent-read",
                    oldRecord);

            CreateCorruptIntent(
                root);

            ProgressionSaveReadResult blocked =
                store.ReadSlot(slot);

            Require(
                blocked.Status ==
                    ProgressionSaveReadStatus.Failed &&
                HasTransaction(root),
                "Corrupt committed intent did not fail closed for slot read.");

            RemoveTransaction(root);

            ProgressionSaveReadResult canonical =
                store.ReadSlot(slot);

            Require(
                canonical.Status ==
                    ProgressionSaveReadStatus.Found &&
                canonical.Record == oldRecord,
                "Corrupt intent read case mutated canonical slot before failing.");

            cases.Complete();
        }

        private static void ValidateCorruptSlotStageFailClosed(
            string runRoot,
            CaseCounter cases)
        {
            string root =
                ScenarioRoot(
                    runRoot,
                    "corrupt-slot-stage");

            string producerRoot =
                root + "-producer";

            ProgressionSaveSlotId slot =
                Slot("corrupt-slot-stage");

            ProgressionSaveSlotRecord oldRecord =
                Record(
                    slot,
                    "corrupt-slot-old",
                    "old");

            ProgressionSaveSlotRecord newRecord =
                Record(
                    slot,
                    "corrupt-slot-new",
                    "new");

            JsonProgressionSaveStore store =
                Seed(
                    root,
                    "qa.json.corrupt-slot-stage",
                    oldRecord);

            PrepareWriteTransaction(
                root,
                producerRoot,
                slot,
                newRecord);

            File.WriteAllText(
                TransactionSlotStagePath(root),
                "{ not-valid-json");

            ProgressionSaveReadResult blocked =
                store.ReadSlot(slot);

            Require(
                blocked.Status ==
                    ProgressionSaveReadStatus.Failed &&
                HasTransaction(root),
                "Corrupt committed slot stage did not fail closed.");

            RemoveTransaction(root);

            ProgressionSaveReadResult canonical =
                store.ReadSlot(slot);

            ProgressionSaveManifestReadResult manifest =
                store.ReadManifest();

            Require(
                canonical.Status ==
                    ProgressionSaveReadStatus.Found &&
                canonical.Record == oldRecord &&
                ManifestMatchesRecord(
                    manifest,
                    oldRecord),
                "Corrupt slot stage mutated canonical data before validation.");

            cases.Complete();

            DeleteDirectory(
                producerRoot);
        }

        private static void ValidateCorruptManifestStageFailClosed(
            string runRoot,
            CaseCounter cases)
        {
            string root =
                ScenarioRoot(
                    runRoot,
                    "corrupt-manifest-stage");

            string producerRoot =
                root + "-producer";

            ProgressionSaveSlotId slot =
                Slot("corrupt-manifest-stage");

            ProgressionSaveSlotRecord oldRecord =
                Record(
                    slot,
                    "corrupt-manifest-old",
                    "old");

            ProgressionSaveSlotRecord newRecord =
                Record(
                    slot,
                    "corrupt-manifest-new",
                    "new");

            JsonProgressionSaveStore store =
                Seed(
                    root,
                    "qa.json.corrupt-manifest-stage",
                    oldRecord);

            PrepareWriteTransaction(
                root,
                producerRoot,
                slot,
                newRecord);

            File.WriteAllText(
                TransactionManifestStagePath(root),
                "{ not-valid-json");

            ProgressionSaveManifestReadResult blocked =
                store.ReadManifest();

            Require(
                blocked.Status ==
                    ProgressionSaveReadStatus.Failed &&
                HasTransaction(root),
                "Corrupt committed manifest stage did not fail closed.");

            RemoveTransaction(root);

            ProgressionSaveReadResult canonical =
                store.ReadSlot(slot);

            ProgressionSaveManifestReadResult manifest =
                store.ReadManifest();

            Require(
                canonical.Status ==
                    ProgressionSaveReadStatus.Found &&
                canonical.Record == oldRecord &&
                ManifestMatchesRecord(
                    manifest,
                    oldRecord),
                "Corrupt manifest stage mutated canonical data before validation.");

            cases.Complete();

            DeleteDirectory(
                producerRoot);
        }

        private static void ValidateCorruptTransactionBlocksWrite(
            string runRoot,
            CaseCounter cases)
        {
            string root =
                ScenarioRoot(
                    runRoot,
                    "blocks-write");

            ProgressionSaveSlotId slot =
                Slot("blocks-write");

            ProgressionSaveSlotRecord oldRecord =
                Record(
                    slot,
                    "blocks-write-old",
                    "old");

            ProgressionSaveSlotRecord newRecord =
                Record(
                    slot,
                    "blocks-write-new",
                    "new");

            JsonProgressionSaveStore store =
                Seed(
                    root,
                    "qa.json.blocks-write",
                    oldRecord);

            CreateCorruptIntent(
                root);

            ProgressionSaveWriteResult blocked =
                store.WriteSlot(
                    newRecord);

            Require(
                blocked.Status ==
                    ProgressionSaveWriteStatus.Failed &&
                HasTransaction(root),
                "Write bypassed a corrupt committed transaction.");

            RemoveTransaction(root);

            ProgressionSaveReadResult canonical =
                store.ReadSlot(slot);

            Require(
                canonical.Status ==
                    ProgressionSaveReadStatus.Found &&
                canonical.Record == oldRecord,
                "Blocked Write mutated canonical state.");

            cases.Complete();
        }

        private static void ValidateCorruptTransactionBlocksDelete(
            string runRoot,
            CaseCounter cases)
        {
            string root =
                ScenarioRoot(
                    runRoot,
                    "blocks-delete");

            ProgressionSaveSlotId slot =
                Slot("blocks-delete");

            ProgressionSaveSlotRecord record =
                Record(
                    slot,
                    "blocks-delete-record",
                    "old");

            JsonProgressionSaveStore store =
                Seed(
                    root,
                    "qa.json.blocks-delete",
                    record);

            CreateCorruptIntent(
                root);

            ProgressionSaveDeleteResult blocked =
                store.DeleteSlot(slot);

            Require(
                blocked.Status ==
                    ProgressionSaveDeleteStatus.Failed &&
                HasTransaction(root),
                "Delete bypassed a corrupt committed transaction.");

            RemoveTransaction(root);

            ProgressionSaveReadResult canonical =
                store.ReadSlot(slot);

            Require(
                canonical.Status ==
                    ProgressionSaveReadStatus.Found &&
                canonical.Record == record,
                "Blocked Delete mutated canonical state.");

            cases.Complete();
        }

        private static void ValidateCorruptTransactionBlocksManifestRead(
            string runRoot,
            CaseCounter cases)
        {
            string root =
                ScenarioRoot(
                    runRoot,
                    "blocks-manifest");

            ProgressionSaveSlotId slot =
                Slot("blocks-manifest");

            ProgressionSaveSlotRecord record =
                Record(
                    slot,
                    "blocks-manifest-record",
                    "old");

            JsonProgressionSaveStore store =
                Seed(
                    root,
                    "qa.json.blocks-manifest",
                    record);

            CreateCorruptIntent(
                root);

            ProgressionSaveManifestReadResult blocked =
                store.ReadManifest();

            Require(
                blocked.Status ==
                    ProgressionSaveReadStatus.Failed &&
                HasTransaction(root),
                "Manifest read bypassed a corrupt committed transaction.");

            RemoveTransaction(root);

            ProgressionSaveManifestReadResult canonical =
                store.ReadManifest();

            Require(
                ManifestMatchesRecord(
                    canonical,
                    record),
                "Blocked manifest read mutated canonical manifest.");

            cases.Complete();
        }

        private static void ValidateAlreadyAppliedReplayIsIdempotent(
            string runRoot,
            CaseCounter cases)
        {
            string root =
                ScenarioRoot(
                    runRoot,
                    "idempotent");

            string producerRoot =
                root + "-producer";

            ProgressionSaveSlotId slot =
                Slot("idempotent");

            ProgressionSaveSlotRecord oldRecord =
                Record(
                    slot,
                    "idempotent-old",
                    "old");

            ProgressionSaveSlotRecord newRecord =
                Record(
                    slot,
                    "idempotent-new",
                    "new");

            JsonProgressionSaveStore store =
                Seed(
                    root,
                    "qa.json.idempotent",
                    oldRecord);

            PrepareWriteTransaction(
                root,
                producerRoot,
                slot,
                newRecord);

            ApplySlotStage(root);
            ApplyManifestStage(root);

            ProgressionSaveReadResult first =
                store.ReadSlot(slot);

            ProgressionSaveReadResult second =
                store.ReadSlot(slot);

            ProgressionSaveManifestReadResult manifest =
                store.ReadManifest();

            Require(
                first.Status ==
                    ProgressionSaveReadStatus.Found &&
                first.Record == newRecord &&
                second.Status ==
                    ProgressionSaveReadStatus.Found &&
                second.Record == newRecord &&
                ManifestMatchesRecord(
                    manifest,
                    newRecord) &&
                !HasTransaction(root),
                "Replaying an already-applied committed transaction was not idempotent.");

            cases.Complete();

            DeleteDirectory(
                producerRoot);
        }

        private static void ValidateUnsupportedIntentVersionFailClosed(
            string runRoot,
            CaseCounter cases)
        {
            string root =
                ScenarioRoot(
                    runRoot,
                    "unsupported-intent");

            ProgressionSaveSlotId slot =
                Slot("unsupported-intent");

            ProgressionSaveSlotRecord record =
                Record(
                    slot,
                    "unsupported-intent-record",
                    "old");

            JsonProgressionSaveStore store =
                Seed(
                    root,
                    "qa.json.unsupported-intent",
                    record);

            Directory.CreateDirectory(
                TransactionDirectory(root));

            WriteIntent(
                root,
                version: 999,
                operation: OperationDelete,
                slot: slot,
                hasSlotStage: false,
                hasManifestStage: false);

            ProgressionSaveReadResult blocked =
                store.ReadSlot(slot);

            Require(
                blocked.Status ==
                    ProgressionSaveReadStatus.Failed &&
                HasTransaction(root),
                "Unsupported committed transaction version did not fail closed.");

            RemoveTransaction(root);

            ProgressionSaveReadResult canonical =
                store.ReadSlot(slot);

            Require(
                canonical.Status ==
                    ProgressionSaveReadStatus.Found &&
                canonical.Record == record,
                "Unsupported intent version mutated canonical state.");

            cases.Complete();
        }

        private static void ValidateCleanMissingState(
            string runRoot,
            CaseCounter cases)
        {
            string root =
                ScenarioRoot(
                    runRoot,
                    "clean-missing");

            ProgressionSaveSlotId slot =
                Slot("clean-missing");

            var store =
                Store(
                    root,
                    "qa.json.clean-missing");

            ProgressionSaveReadResult read =
                store.ReadSlot(slot);

            ProgressionSaveManifestReadResult manifest =
                store.ReadManifest();

            ProgressionSaveDeleteResult delete =
                store.DeleteSlot(slot);

            Require(
                read.Status ==
                    ProgressionSaveReadStatus.Missing &&
                manifest.Status ==
                    ProgressionSaveReadStatus.Missing &&
                delete.Status ==
                    ProgressionSaveDeleteStatus.Missing &&
                !HasTransaction(root),
                "Clean empty backend no longer preserves Missing semantics.");

            cases.Complete();
        }

        private static JsonProgressionSaveStore Seed(
            string root,
            string backendId,
            ProgressionSaveSlotRecord record)
        {
            DeleteDirectory(root);

            JsonProgressionSaveStore store =
                Store(
                    root,
                    backendId);

            ProgressionSaveWriteResult write =
                store.WriteSlot(record);

            Require(
                write.Status ==
                    ProgressionSaveWriteStatus.Written &&
                !HasTransaction(root),
                $"Seed write failed for '{root}'.");

            return store;
        }

        private static JsonProgressionSaveStore Store(
            string root,
            string backendId)
        {
            return new JsonProgressionSaveStore(
                root,
                ProgressionSaveBackendId.From(
                    backendId));
        }

        private static ProgressionSaveSlotId Slot(
            string scenario)
        {
            return ProgressionSaveSlotId.From(
                $"qa.adr018b.{scenario}");
        }

        private static ProgressionSaveSlotRecord Record(
            ProgressionSaveSlotId slot,
            string recordValue,
            string payloadText)
        {
            long now =
                DateTime.UtcNow.Ticks;

            return new ProgressionSaveSlotRecord(
                slot,
                ProgressionSaveRecordId.From(
                    $"qa.adr018b.{recordValue}"),
                ProgressionSavePayload.FromText(
                    payloadText,
                    "text/plain"),
                now,
                now,
                $"QA {recordValue}",
                "ADR018-QA",
                recordValue);
        }

        private static void PrepareWriteTransaction(
            string targetRoot,
            string producerRoot,
            ProgressionSaveSlotId slot,
            ProgressionSaveSlotRecord newRecord)
        {
            PrepareWriteStagesWithoutIntent(
                targetRoot,
                producerRoot,
                newRecord);

            WriteIntent(
                targetRoot,
                TransactionFormatVersion,
                OperationWrite,
                slot,
                hasSlotStage: true,
                hasManifestStage: true);
        }

        private static void PrepareWriteStagesWithoutIntent(
            string targetRoot,
            string producerRoot,
            ProgressionSaveSlotRecord newRecord)
        {
            DeleteDirectory(
                producerRoot);

            JsonProgressionSaveStore producer =
                Store(
                    producerRoot,
                    "qa.json.producer.write");

            ProgressionSaveWriteResult write =
                producer.WriteSlot(
                    newRecord);

            Require(
                write.Status ==
                    ProgressionSaveWriteStatus.Written,
                "Producer Write transaction could not create staged reference data.");

            Directory.CreateDirectory(
                TransactionDirectory(
                    targetRoot));

            File.Copy(
                SingleSlotPath(
                    producerRoot),
                TransactionSlotStagePath(
                    targetRoot),
                overwrite: true);

            File.Copy(
                ManifestPath(
                    producerRoot),
                TransactionManifestStagePath(
                    targetRoot),
                overwrite: true);
        }

        private static void PrepareDeleteTransaction(
            string targetRoot,
            string producerRoot,
            ProgressionSaveSlotId slot,
            ProgressionSaveSlotRecord record)
        {
            DeleteDirectory(
                producerRoot);

            JsonProgressionSaveStore producer =
                Seed(
                    producerRoot,
                    "qa.json.producer.delete",
                    record);

            ProgressionSaveDeleteResult delete =
                producer.DeleteSlot(slot);

            Require(
                delete.Status ==
                    ProgressionSaveDeleteStatus.Deleted,
                "Producer Delete transaction could not create empty manifest stage.");

            Directory.CreateDirectory(
                TransactionDirectory(
                    targetRoot));

            File.Copy(
                ManifestPath(
                    producerRoot),
                TransactionManifestStagePath(
                    targetRoot),
                overwrite: true);

            WriteIntent(
                targetRoot,
                TransactionFormatVersion,
                OperationDelete,
                slot,
                hasSlotStage: false,
                hasManifestStage: true);
        }

        private static void ApplySlotStage(
            string root)
        {
            string canonical =
                SingleSlotPath(
                    root);

            File.Copy(
                TransactionSlotStagePath(
                    root),
                canonical,
                overwrite: true);
        }

        private static void ApplyManifestStage(
            string root)
        {
            File.Copy(
                TransactionManifestStagePath(
                    root),
                ManifestPath(root),
                overwrite: true);
        }

        private static void CreateCorruptIntent(
            string root)
        {
            Directory.CreateDirectory(
                TransactionDirectory(root));

            File.WriteAllText(
                IntentPath(root),
                "{ not-valid-json");
        }

        private static void WriteIntent(
            string root,
            int version,
            int operation,
            ProgressionSaveSlotId slot,
            bool hasSlotStage,
            bool hasManifestStage)
        {
            var dto =
                new TransactionIntentDto
                {
                    version = version,
                    operation = operation,
                    slotId = slot.Value.Value,
                    hasSlotStage = hasSlotStage,
                    hasManifestStage = hasManifestStage
                };

            File.WriteAllText(
                IntentPath(root),
                JsonUtility.ToJson(
                    dto,
                    prettyPrint: true));
        }

        private static bool ManifestMatchesRecord(
            ProgressionSaveManifestReadResult manifestRead,
            ProgressionSaveSlotRecord record)
        {
            if (!manifestRead.HasManifest ||
                !manifestRead.Manifest.TryGetEntry(
                    record.SlotId,
                    out ProgressionSaveManifestEntry entry))
            {
                return false;
            }

            return entry.RecordId ==
                    record.RecordId &&
                entry.DisplayName ==
                    record.DisplayName &&
                entry.CreatedUtcTicks ==
                    record.CreatedUtcTicks &&
                entry.UpdatedUtcTicks ==
                    record.UpdatedUtcTicks &&
                entry.PayloadFormat ==
                    record.Payload.Format &&
                entry.PayloadByteCount ==
                    record.Payload.ByteCount &&
                entry.Source ==
                    record.Source &&
                entry.Reason ==
                    record.Reason;
        }

        private static bool ContainsRecoveryDiagnostic(
            params string[] messages)
        {
            return messages.Any(
                message =>
                    !string.IsNullOrWhiteSpace(message) &&
                    message.IndexOf(
                        "Recovered committed JSON transaction",
                        StringComparison.Ordinal) >= 0);
        }

        private static string ScenarioRoot(
            string runRoot,
            string scenario)
        {
            return Path.Combine(
                runRoot,
                scenario);
        }

        private static string ManifestPath(
            string root)
        {
            return Path.Combine(
                root,
                ManifestFileName);
        }

        private static string TransactionDirectory(
            string root)
        {
            return Path.Combine(
                root,
                TransactionDirectoryName);
        }

        private static string IntentPath(
            string root)
        {
            return Path.Combine(
                TransactionDirectory(root),
                IntentFileName);
        }

        private static string TransactionSlotStagePath(
            string root)
        {
            return Path.Combine(
                TransactionDirectory(root),
                SlotStageFileName);
        }

        private static string TransactionManifestStagePath(
            string root)
        {
            return Path.Combine(
                TransactionDirectory(root),
                ManifestStageFileName);
        }

        private static bool HasTransaction(
            string root)
        {
            return Directory.Exists(
                TransactionDirectory(root));
        }

        private static string SingleSlotPath(
            string root)
        {
            string slotDirectory =
                Path.Combine(
                    root,
                    SlotDirectoryName);

            Require(
                Directory.Exists(slotDirectory),
                $"Expected slot directory is missing: '{slotDirectory}'.");

            string[] files =
                Directory.GetFiles(
                    slotDirectory,
                    "*.json",
                    SearchOption.TopDirectoryOnly);

            Require(
                files.Length == 1,
                $"Expected exactly one slot JSON file under '{slotDirectory}', actual='{files.Length}'.");

            return files[0];
        }

        private static void RemoveTransaction(
            string root)
        {
            DeleteDirectory(
                TransactionDirectory(root));
        }

        private static void DeleteDirectory(
            string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(
                    path,
                    recursive: true);
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

        [Serializable]
        private sealed class TransactionIntentDto
        {
            public int version;
            public int operation;
            public string slotId;
            public bool hasSlotStage;
            public bool hasManifestStage;
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
                    $"ADR-018 JSON Recovery QA case-count mismatch. " +
                    $"completed='{_completed}' expected='{_expected}'.");
            }
        }
    }
}
