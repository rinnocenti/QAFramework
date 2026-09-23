using System;
using Immersive.Framework.ProgressionSave;

namespace ImmersiveFrameworkQA.ProgressionSave.Internal.Editor
{
    internal enum QaProgressionSaveProviderMode
    {
        SuccessMemory = 0,
        InvalidConfiguration = 10,
        CreateFailure = 20,
        NullStore = 30,
        InvalidBackendId = 40,
        ThrowOnCreate = 50
    }

    internal sealed class QaProgressionSaveStoreProviderAsset :
        ProgressionSaveStoreProviderAsset
    {
        internal QaProgressionSaveProviderMode Mode { get; set; }

        internal int ValidateCount { get; private set; }

        internal int CreateCount { get; private set; }

        public override bool TryValidate(out string issue)
        {
            ValidateCount++;

            if (Mode ==
                QaProgressionSaveProviderMode.InvalidConfiguration)
            {
                issue =
                    "QA provider configuration is intentionally invalid.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        public override bool TryCreateStore(
            ProgressionSaveStoreCreationContext context,
            out IProgressionSaveStore store,
            out string issue)
        {
            CreateCount++;

            switch (Mode)
            {
                case QaProgressionSaveProviderMode.SuccessMemory:
                    store =
                        new QaInMemoryProgressionSaveStore(
                            "qa.composition.custom");
                    issue = string.Empty;
                    return true;

                case QaProgressionSaveProviderMode.CreateFailure:
                    store = null;
                    issue =
                        "QA provider intentionally rejected store creation.";
                    return false;

                case QaProgressionSaveProviderMode.NullStore:
                    store = null;
                    issue =
                        "QA provider intentionally returned success with a null store.";
                    return true;

                case QaProgressionSaveProviderMode.InvalidBackendId:
                    store =
                        new QaInvalidBackendProgressionSaveStore();
                    issue = string.Empty;
                    return true;

                case QaProgressionSaveProviderMode.ThrowOnCreate:
                    throw new InvalidOperationException(
                        "QA provider intentionally threw during creation.");

                case QaProgressionSaveProviderMode.InvalidConfiguration:
                    store = null;
                    issue =
                        "Invalid QA provider should not reach creation.";
                    return false;

                default:
                    store = null;
                    issue =
                        $"Unsupported QA provider mode '{Mode}'.";
                    return false;
            }
        }
    }

    internal sealed class QaInvalidBackendProgressionSaveStore :
        IProgressionSaveStore
    {
        public ProgressionSaveBackendId BackendId => default;

        public ProgressionSaveReadResult ReadSlot(
            ProgressionSaveSlotId slotId)
        {
            throw new InvalidOperationException(
                "Invalid-backend QA store must never be used.");
        }

        public ProgressionSaveWriteResult WriteSlot(
            ProgressionSaveSlotRecord record)
        {
            throw new InvalidOperationException(
                "Invalid-backend QA store must never be used.");
        }

        public ProgressionSaveDeleteResult DeleteSlot(
            ProgressionSaveSlotId slotId)
        {
            throw new InvalidOperationException(
                "Invalid-backend QA store must never be used.");
        }
    }
}
