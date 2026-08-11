using System;
using System.Collections.Generic;
using Immersive.Framework.ProgressionSave;

namespace ImmersiveFrameworkQA.ProgressionSave.Internal.Editor
{
    /// <summary>
    /// QA-only alternate backend used to prove that ProgressionSaveRuntime depends
    /// only on the core IProgressionSaveStore contract.
    ///
    /// Intentionally does NOT implement IProgressionSaveCatalog.
    /// </summary>
    internal sealed class QaInMemoryProgressionSaveStore
        : IProgressionSaveStore
    {
        private readonly Dictionary<
            ProgressionSaveSlotId,
            ProgressionSaveSlotRecord> _records =
            new Dictionary<
                ProgressionSaveSlotId,
                ProgressionSaveSlotRecord>();

        internal QaInMemoryProgressionSaveStore(string backendId)
        {
            BackendId =
                ProgressionSaveBackendId.From(backendId);
        }

        public ProgressionSaveBackendId BackendId { get; }

        internal QaProgressionSaveFault Fault { get; set; }

        public ProgressionSaveReadResult ReadSlot(
            ProgressionSaveSlotId slotId)
        {
            if (!slotId.IsValid)
            {
                throw new ArgumentException(
                    "QA Progression Save read requires a valid slot id.",
                    nameof(slotId));
            }

            if (Fault == QaProgressionSaveFault.BackendUnavailable)
            {
                return ProgressionSaveReadResult.BackendUnavailable(
                    slotId,
                    "QA in-memory backend unavailable.");
            }

            if (Fault == QaProgressionSaveFault.CorruptRead)
            {
                return ProgressionSaveReadResult.Corrupt(
                    slotId,
                    "QA in-memory backend injected corrupt read.");
            }

            if (Fault == QaProgressionSaveFault.FailedRead)
            {
                return ProgressionSaveReadResult.FailedResult(
                    slotId,
                    "QA in-memory backend injected failed read.");
            }

            return _records.TryGetValue(
                    slotId,
                    out ProgressionSaveSlotRecord record)
                ? ProgressionSaveReadResult.Found(
                    record,
                    "QA in-memory slot found.")
                : ProgressionSaveReadResult.Missing(
                    slotId,
                    "QA in-memory slot missing.");
        }

        public ProgressionSaveWriteResult WriteSlot(
            ProgressionSaveSlotRecord record)
        {
            if (!record.IsValid)
            {
                throw new ArgumentException(
                    "QA Progression Save write requires a valid record.",
                    nameof(record));
            }

            if (Fault == QaProgressionSaveFault.BackendUnavailable)
            {
                return ProgressionSaveWriteResult.BackendUnavailable(
                    record.SlotId,
                    "QA in-memory backend unavailable.");
            }

            if (Fault == QaProgressionSaveFault.RejectedWrite)
            {
                return ProgressionSaveWriteResult.Rejected(
                    record.SlotId,
                    "QA in-memory backend injected rejected write.");
            }

            if (Fault == QaProgressionSaveFault.FailedWrite)
            {
                return ProgressionSaveWriteResult.FailedResult(
                    record.SlotId,
                    "QA in-memory backend injected failed write.");
            }

            _records[record.SlotId] = record;

            return ProgressionSaveWriteResult.SlotWritten(
                record,
                "QA in-memory slot written.");
        }

        public ProgressionSaveDeleteResult DeleteSlot(
            ProgressionSaveSlotId slotId)
        {
            if (!slotId.IsValid)
            {
                throw new ArgumentException(
                    "QA Progression Save delete requires a valid slot id.",
                    nameof(slotId));
            }

            if (Fault == QaProgressionSaveFault.BackendUnavailable)
            {
                return ProgressionSaveDeleteResult.BackendUnavailable(
                    slotId,
                    "QA in-memory backend unavailable.");
            }

            if (Fault == QaProgressionSaveFault.RejectedDelete)
            {
                return ProgressionSaveDeleteResult.Rejected(
                    slotId,
                    "QA in-memory backend injected rejected delete.");
            }

            if (Fault == QaProgressionSaveFault.FailedDelete)
            {
                return ProgressionSaveDeleteResult.FailedResult(
                    slotId,
                    "QA in-memory backend injected failed delete.");
            }

            return _records.Remove(slotId)
                ? ProgressionSaveDeleteResult.Deleted(
                    slotId,
                    "QA in-memory slot deleted.")
                : ProgressionSaveDeleteResult.Missing(
                    slotId,
                    "QA in-memory slot already missing.");
        }
    }

    internal enum QaProgressionSaveFault
    {
        None = 0,
        BackendUnavailable = 10,
        CorruptRead = 20,
        FailedRead = 30,
        RejectedWrite = 40,
        FailedWrite = 50,
        RejectedDelete = 60,
        FailedDelete = 70
    }
}
