using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.PlayerAssignment.Internal.Editor
{
    /// <summary>
    /// Edit Mode technical smoke for IF-SESSION-CONFIG-05B. It verifies the
    /// creation-time Profile selection input without materializing a runtime
    /// host: default selection, complete explicit replacement, invalid
    /// explicit failure, and absence of field-level merging.
    /// </summary>
    internal static class QaIfSessionConfig05BPlayerSessionProfileOverrideSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Player/" +
            "Run IF-SESSION-CONFIG-05B Session Profile Override Smoke";

        private const string LogPrefix =
            "[IF_SESSION_CONFIG_05B_PLAYER_SESSION_PROFILE_OVERRIDE]";

        private static readonly string[] CaseIds =
        {
            "01-no-override-uses-default",
            "02-explicit-override-replaces-default",
            "03-invalid-explicit-override-does-not-fallback",
            "04-explicit-override-does-not-field-merge"
        };

        [MenuItem(MenuPath)]
        internal static void Run()
        {
            var created = new List<UnityEngine.Object>();
            var results = new List<CaseResult>(CaseIds.Length);

            try
            {
                Require(
                    !EditorApplication.isPlayingOrWillChangePlaymode,
                    "IF-SESSION-CONFIG-05B smoke must run in Edit Mode.");

                results.Add(RunCase(
                    CaseIds[0],
                    () => Case01NoOverrideUsesDefault(created)));
                results.Add(RunCase(
                    CaseIds[1],
                    () => Case02ExplicitOverrideReplacesDefault(created)));
                results.Add(RunCase(
                    CaseIds[2],
                    () => Case03InvalidExplicitOverrideDoesNotFallback(created)));
                results.Add(RunCase(
                    CaseIds[3],
                    () => Case04ExplicitOverrideDoesNotFieldMerge(created)));

                Require(
                    results.Count == CaseIds.Length,
                    "IF-SESSION-CONFIG-05B case count changed unexpectedly.");

                bool allPassed = true;
                var summary = new StringBuilder();
                for (int index = 0; index < results.Count; index++)
                {
                    CaseResult result = results[index];
                    allPassed &= result.Passed;
                    if (index > 0)
                    {
                        summary.Append(';');
                    }

                    summary.Append(result.CaseId)
                        .Append('=')
                        .Append(result.Passed ? "PASS" : "FAIL");
                    if (!result.Passed)
                    {
                        summary.Append('(')
                            .Append(Escape(result.Detail))
                            .Append(')');
                    }
                }

                if (!allPassed)
                {
                    Debug.LogError(
                        $"{LogPrefix} status='FAIL' cases='{results.Count}' results='{summary}'.");
                    throw new InvalidOperationException(
                        "IF-SESSION-CONFIG-05B smoke failed. " + summary);
                }

                Debug.Log(
                    $"{LogPrefix} status='PASS' cases='{results.Count}' results='{summary}'.");
            }
            finally
            {
                for (int index = created.Count - 1; index >= 0; index--)
                {
                    if (created[index] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(created[index]);
                    }
                }
            }
        }

        private static void Case01NoOverrideUsesDefault(
            ICollection<UnityEngine.Object> created)
        {
            PlayerSlotProfile defaultSlot = CreateSlot(created, "qa.05b.default", "Default");
            PlayerSessionProfile defaultProfile = CreateSession(
                created,
                "Default Profile",
                new[] { defaultSlot },
                1,
                false,
                PlayerHostProvisioningMode.ManagerProvisioned,
                PlayerActorResolutionPolicy.LeaveUnresolved);
            GameApplicationAsset application = CreateApplication(
                created,
                defaultProfile);

            bool enabled = PlayerSessionCreationConfigurationResolver.TryResolve(
                application,
                null,
                out PlayerSessionInitializationResult result);

            Require(enabled, "Enabled Player Session was reported as absent.");
            RequireSucceeded(result, "Default resolution");
            RequireEffective(
                result.Configuration,
                defaultSlot,
                1,
                false,
                PlayerHostProvisioningMode.ManagerProvisioned,
                PlayerActorResolutionPolicy.LeaveUnresolved,
                "Default resolution");
        }

        private static void Case02ExplicitOverrideReplacesDefault(
            ICollection<UnityEngine.Object> created)
        {
            PlayerSlotProfile defaultSlot = CreateSlot(created, "qa.05b.default", "Default");
            PlayerSlotProfile overrideSlot = CreateSlot(created, "qa.05b.override", "Override");
            PlayerSessionProfile defaultProfile = CreateSession(
                created,
                "Default Profile",
                new[] { defaultSlot },
                1,
                false,
                PlayerHostProvisioningMode.ManagerProvisioned,
                PlayerActorResolutionPolicy.LeaveUnresolved);
            PlayerSessionProfile explicitProfile = CreateSession(
                created,
                "Explicit Profile",
                new[] { overrideSlot },
                1,
                true,
                PlayerHostProvisioningMode.SceneProvided,
                PlayerActorResolutionPolicy.ResolveConfiguredDefault);
            GameApplicationAsset application = CreateApplication(
                created,
                defaultProfile);

            bool enabled = PlayerSessionCreationConfigurationResolver.TryResolve(
                application,
                explicitProfile,
                out PlayerSessionInitializationResult result);

            Require(enabled, "Enabled Player Session was reported as absent.");
            RequireSucceeded(result, "Explicit override resolution");
            RequireEffective(
                result.Configuration,
                overrideSlot,
                1,
                true,
                PlayerHostProvisioningMode.SceneProvided,
                PlayerActorResolutionPolicy.ResolveConfiguredDefault,
                "Explicit override resolution");
            Require(
                !result.Configuration.Slots[0].PlayerSlotId.Equals(defaultSlot.PlayerSlotId),
                "Explicit override retained the default Slot.");
        }

        private static void Case03InvalidExplicitOverrideDoesNotFallback(
            ICollection<UnityEngine.Object> created)
        {
            PlayerSlotProfile slot = CreateSlot(created, "qa.05b.invalid", "Invalid");
            PlayerSessionProfile defaultProfile = CreateSession(
                created,
                "Valid Default Profile",
                new[] { slot },
                1,
                true,
                PlayerHostProvisioningMode.ManagerProvisioned,
                PlayerActorResolutionPolicy.LeaveUnresolved);
            PlayerSessionProfile invalidExplicitProfile = CreateSession(
                created,
                "Invalid Explicit Profile",
                new[] { slot },
                1,
                true,
                PlayerHostProvisioningMode.SceneProvided,
                PlayerActorResolutionPolicy.LeaveUnresolved);
            var invalidSerialized = new SerializedObject(invalidExplicitProfile);
            invalidSerialized.FindProperty("initialCapacity").intValue = 2;
            invalidSerialized.ApplyModifiedPropertiesWithoutUndo();
            GameApplicationAsset application = CreateApplication(
                created,
                defaultProfile);

            bool enabled = PlayerSessionCreationConfigurationResolver.TryResolve(
                application,
                invalidExplicitProfile,
                out PlayerSessionInitializationResult result);

            Require(enabled, "Enabled Player Session was reported as absent.");
            Require(result != null && result.Failed, "Invalid explicit Profile resolved successfully.");
            Require(
                result.Failure == PlayerSessionInitializationFailure.InvalidPlayerSessionProfile,
                "Invalid explicit Profile did not report InvalidPlayerSessionProfile. " +
                $"actual='{result.Failure}' message='{result.Message}'.");
            Require(
                result.Configuration == null,
                "Invalid explicit Profile fell back to the default configuration.");
        }

        private static void Case04ExplicitOverrideDoesNotFieldMerge(
            ICollection<UnityEngine.Object> created)
        {
            PlayerSlotProfile defaultP1 = CreateSlot(created, "qa.05b.default.p1", "Default P1");
            PlayerSlotProfile defaultP2 = CreateSlot(created, "qa.05b.default.p2", "Default P2");
            PlayerSlotProfile overrideP1 = CreateSlot(created, "qa.05b.override.p1", "Override P1");
            PlayerSessionProfile defaultProfile = CreateSession(
                created,
                "Default Profile",
                new[] { defaultP1, defaultP2 },
                2,
                false,
                PlayerHostProvisioningMode.ManagerProvisioned,
                PlayerActorResolutionPolicy.LeaveUnresolved);
            PlayerSessionProfile explicitProfile = CreateSession(
                created,
                "Explicit Profile",
                new[] { overrideP1 },
                1,
                true,
                PlayerHostProvisioningMode.SceneProvided,
                PlayerActorResolutionPolicy.ResolveConfiguredDefault);
            GameApplicationAsset application = CreateApplication(
                created,
                defaultProfile);

            PlayerSessionCreationConfigurationResolver.TryResolve(
                application,
                explicitProfile,
                out PlayerSessionInitializationResult result);

            RequireSucceeded(result, "No-merge resolution");
            RequireEffective(
                result.Configuration,
                overrideP1,
                1,
                true,
                PlayerHostProvisioningMode.SceneProvided,
                PlayerActorResolutionPolicy.ResolveConfiguredDefault,
                "No-merge resolution");
            Require(
                result.Configuration.SupportedSlotCount != 2,
                "Explicit Profile inherited default Supported Slots.");
        }

        private static CaseResult RunCase(string caseId, Action body)
        {
            try
            {
                body();
                Debug.Log($"{LogPrefix} case='{caseId}' status='PASS'.");
                return new CaseResult(caseId, true, string.Empty);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{LogPrefix} case='{caseId}' status='FAIL' message='{Escape(exception.Message)}'.");
                return new CaseResult(caseId, false, exception.Message);
            }
        }

        private static GameApplicationAsset CreateApplication(
            ICollection<UnityEngine.Object> created,
            PlayerSessionProfile defaultProfile)
        {
            var application = ScriptableObject.CreateInstance<GameApplicationAsset>();
            application.name = "QA IF-SESSION-CONFIG-05B Game Application";
            created.Add(application);
            var serialized = new SerializedObject(application);
            serialized.FindProperty("playerSessionEnabled").boolValue = true;
            serialized.FindProperty("defaultPlayerSessionProfile").objectReferenceValue =
                defaultProfile;
            serialized.FindProperty("playerActorSelectionDuplicatePolicy").intValue =
                (int)PlayerActorSelectionDuplicatePolicy.AllowDuplicates;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return application;
        }

        private static PlayerSessionProfile CreateSession(
            ICollection<UnityEngine.Object> created,
            string name,
            PlayerSlotProfile[] slots,
            int capacity,
            bool joiningOpen,
            PlayerHostProvisioningMode provisioningMode,
            PlayerActorResolutionPolicy actorResolutionPolicy)
        {
            PlayerProvisioningProfile provisioning =
                ScriptableObject.CreateInstance<PlayerProvisioningProfile>();
            provisioning.name = name + " Provisioning";
            created.Add(provisioning);
            var provisioningSerialized = new SerializedObject(provisioning);
            provisioningSerialized.FindProperty("defaultHostProvisioning").intValue =
                (int)provisioningMode;
            provisioningSerialized.FindProperty("actorResolutionPolicy").intValue =
                (int)actorResolutionPolicy;
            provisioningSerialized.ApplyModifiedPropertiesWithoutUndo();

            var session = ScriptableObject.CreateInstance<PlayerSessionProfile>();
            session.name = name;
            created.Add(session);
            var sessionSerialized = new SerializedObject(session);
            SerializedProperty supportedSlots =
                sessionSerialized.FindProperty("supportedSlots");
            supportedSlots.arraySize = slots.Length;
            for (int index = 0; index < slots.Length; index++)
            {
                supportedSlots.GetArrayElementAtIndex(index).objectReferenceValue =
                    slots[index];
            }

            sessionSerialized.FindProperty("initialCapacity").intValue = capacity;
            sessionSerialized.FindProperty("initialJoiningOpen").boolValue = joiningOpen;
            sessionSerialized.FindProperty("playerProvisioningProfile").objectReferenceValue =
                provisioning;
            sessionSerialized.ApplyModifiedPropertiesWithoutUndo();
            return session;
        }

        private static PlayerSlotProfile CreateSlot(
            ICollection<UnityEngine.Object> created,
            string slotId,
            string displayName)
        {
            var slot = ScriptableObject.CreateInstance<PlayerSlotProfile>();
            slot.name = displayName;
            created.Add(slot);
            var serialized = new SerializedObject(slot);
            serialized.FindProperty("playerSlotId").stringValue = slotId;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return slot;
        }

        private static void RequireEffective(
            EffectivePlayerSessionConfiguration configuration,
            PlayerSlotProfile expectedSlot,
            int expectedCapacity,
            bool expectedJoiningOpen,
            PlayerHostProvisioningMode expectedProvisioning,
            PlayerActorResolutionPolicy expectedActorResolution,
            string label)
        {
            Require(configuration != null, label + ": configuration is null.");
            Require(
                configuration.SupportedSlotCount == 1 &&
                configuration.Slots[0].PlayerSlotId.Equals(expectedSlot.PlayerSlotId),
                label + ": effective Slots did not come exclusively from the selected Profile.");
            Require(
                configuration.InitialCapacity == expectedCapacity,
                label + ": Initial Capacity was not selected intact.");
            Require(
                configuration.InitialJoiningOpen == expectedJoiningOpen,
                label + ": Initial Joining was not selected intact.");
            Require(
                configuration.Slots[0].HostProvisioningMode == expectedProvisioning,
                label + ": Host Provisioning was not selected intact.");
            Require(
                configuration.ActorResolutionPolicy == expectedActorResolution,
                label + ": Actor Resolution Policy was not selected intact.");
        }

        private static void RequireSucceeded(
            PlayerSessionInitializationResult result,
            string label)
        {
            Require(
                result != null && result.Succeeded && result.Configuration != null,
                $"{label}: expected success. failure='{result?.Failure}' message='{result?.Message}'.");
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
                : value.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
        }

        private readonly struct CaseResult
        {
            internal CaseResult(string caseId, bool passed, string detail)
            {
                CaseId = caseId;
                Passed = passed;
                Detail = detail ?? string.Empty;
            }

            internal string CaseId { get; }

            internal bool Passed { get; }

            internal string Detail { get; }
        }
    }
}
