using System;
using System.Collections.Generic;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Reset;
using Immersive.Framework.Reset.Unity;
using Immersive.Framework.RuntimeContent;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    public static class QaH2210ResetRegistrationRuntimeBindingSmoke
    {
        private const string LogPrefix =
            "[H2210_RESET_REGISTRATION_RUNTIME_BINDING_SMOKE]";
        private const string RuntimeSource =
            nameof(UnityResetSubjectAdapter);

        private static bool ValidateRun() => EditorApplication.isPlaying;

        public static void Run()
        {
            RunInternal();
        }

        public static void RunInternal()
        {
            var completed = new List<string>();
            var objects = new List<UnityObject>();
            var adapters = new List<UnityResetSubjectAdapter>();
            var ports = new List<RecordingResetRegistrationRuntimePort>();

            try
            {
                Require(
                    EditorApplication.isPlaying,
                    "H2.2.10 vertical smoke requires Play Mode.");
                Require(
                    global::ImmersiveFrameworkQA.GameFlow.Internal.Editor.ImmersiveFrameworkQA.GameFlow.InternalEditor.QaH2FrameworkReadiness.TryResolveUniqueHost(
                        out FrameworkRuntimeHost host) &&
                    host != null,
                    "H2.2.10 vertical smoke requires FrameworkRuntimeHost.");

                IResetRegistrationRuntimePort hostRuntime = host;
                Require(
                    hostRuntime != null,
                    "FrameworkRuntimeHost did not expose Reset registration runtime port.");
                completed.Add("runtime-port-available");

                RunBindingCompositionCases(
                    objects,
                    adapters,
                    ports,
                    completed);

                UnityResetSubjectAdapter unbound = CreateAdapter(
                    "H2210 Unbound Adapter",
                    UnityResetSubjectIdGenerationMode.AuthoredStableId,
                    "qa.h2210.unbound",
                    string.Empty,
                    ResetSubjectScope.Activity,
                    includeParticipant: false,
                    objects,
                    adapters);
                bool unboundRegistered =
                    unbound.RegisterWithCurrentHost("unbound");
                Require(
                    !unboundRegistered &&
                    !unbound.IsRegistered &&
                    !unbound.HasResetRegistrationRuntimeBinding &&
                    !unbound.SubjectHandle.IsValid &&
                    unbound.ResetRegistrationRuntimeBindingDiagnostic
                        .Contains("not bound"),
                    BuildAdapterDiagnostic(unbound));
                completed.Add(
                    "unbound-adapter-does-not-fallback-to-current-host");

                var missingOwnerPort =
                    new RecordingResetRegistrationRuntimePort
                    {
                        OwnerAvailable = false
                    };
                ports.Add(missingOwnerPort);
                UnityResetSubjectAdapter missingOwner = CreateAdapter(
                    "H2210 Missing Owner Adapter",
                    UnityResetSubjectIdGenerationMode.AuthoredStableId,
                    "qa.h2210.missing-owner",
                    string.Empty,
                    ResetSubjectScope.Activity,
                    includeParticipant: false,
                    objects,
                    adapters);
                Require(
                    missingOwner.TryBindResetRegistrationRuntime(
                        missingOwnerPort,
                        out string missingOwnerBindingIssue),
                    missingOwnerBindingIssue);
                bool missingOwnerRegistered =
                    missingOwner.RegisterWithCurrentHost("missing-owner");
                Require(
                    !missingOwnerRegistered &&
                    !missingOwner.IsRegistered &&
                    missingOwnerPort.ResolveOwnerCallCount == 1 &&
                    missingOwnerPort.RegisterSubjectCallCount == 0 &&
                    missingOwnerPort.RegisterRuntimeSubjectCallCount == 0 &&
                    missingOwnerPort.Registry.SubjectCount == 0,
                    BuildPortDiagnostic(missingOwnerPort));
                completed.Add(
                    "missing-owner-fails-before-registration");

                var authoredPort =
                    new RecordingResetRegistrationRuntimePort();
                ports.Add(authoredPort);
                UnityResetSubjectAdapter authored = CreateAdapter(
                    "H2210 Authored Adapter",
                    UnityResetSubjectIdGenerationMode.AuthoredStableId,
                    "qa.h2210.authored",
                    string.Empty,
                    ResetSubjectScope.Activity,
                    includeParticipant: true,
                    objects,
                    adapters);
                Require(
                    authored.TryBindResetRegistrationRuntime(
                        authoredPort,
                        out string authoredBindingIssue),
                    authoredBindingIssue);
                bool authoredRegistered =
                    authored.RegisterWithCurrentHost("authored-register");
                Require(
                    authoredRegistered &&
                    authored.IsRegistered &&
                    authored.SubjectHandle.IsSubject &&
                    authored.SubjectId.StableText ==
                        "qa.h2210.authored" &&
                    authored.Subject.Scope == ResetSubjectScope.Activity &&
                    authored.Subject.Origin ==
                        ResetSubjectOrigin.SceneAuthored &&
                    authored.Subject.Owner ==
                        authoredPort.LastResolvedOwner &&
                    authoredPort.RegisterSubjectCallCount == 1 &&
                    authoredPort.LastSource == RuntimeSource &&
                    authoredPort.LastReason == "authored-register",
                    BuildAdapterAndPortDiagnostic(
                        authored,
                        authoredPort));
                completed.Add(
                    "authored-subject-registration-preserves-owner-source-and-reason");

                Require(
                    authored.RegisteredParticipantCount == 1 &&
                    authoredPort.RegisterParticipantCallCount == 1 &&
                    authoredPort.Registry.SubjectCount == 1 &&
                    authoredPort.Registry.ParticipantCount == 1 &&
                    authoredPort.LastParticipantSource == RuntimeSource &&
                    authoredPort.LastParticipantReason ==
                        "authored-register",
                    BuildAdapterAndPortDiagnostic(
                        authored,
                        authoredPort));
                completed.Add(
                    "inactive-child-resettable-participant-registered");

                bool authoredCleared =
                    authored.ClearRegistration("authored-clear");
                Require(
                    authoredCleared &&
                    !authored.IsRegistered &&
                    authored.HasResetRegistrationRuntimeBinding &&
                    authored.RegisteredParticipantCount == 0 &&
                    authoredPort.UnregisterCallCount == 1 &&
                    authoredPort.LastUnregisterSource == RuntimeSource &&
                    authoredPort.LastUnregisterReason ==
                        "authored-clear" &&
                    authoredPort.Registry.SubjectCount == 0 &&
                    authoredPort.Registry.ParticipantCount == 0,
                    BuildAdapterAndPortDiagnostic(
                        authored,
                        authoredPort));
                completed.Add(
                    "subject-unregister-cascades-participants-and-preserves-binding");

                var runtimePort =
                    new RecordingResetRegistrationRuntimePort();
                ports.Add(runtimePort);
                UnityResetSubjectAdapter runtimeAdapter = CreateAdapter(
                    "H2210 Runtime Adapter",
                    UnityResetSubjectIdGenerationMode.RuntimeInstanceId,
                    string.Empty,
                    "qa.h2210.runtime",
                    ResetSubjectScope.Runtime,
                    includeParticipant: false,
                    objects,
                    adapters);
                Require(
                    runtimeAdapter.TryBindResetRegistrationRuntime(
                        runtimePort,
                        out string runtimeBindingIssue),
                    runtimeBindingIssue);
                Require(
                    runtimeAdapter.RegisterWithCurrentHost(
                        "runtime-first"),
                    BuildAdapterAndPortDiagnostic(
                        runtimeAdapter,
                        runtimePort));
                string firstRuntimeId =
                    runtimeAdapter.SubjectId.StableText;
                Require(
                    firstRuntimeId == "qa.h2210.runtime#1" &&
                    runtimeAdapter.Subject.Origin ==
                        ResetSubjectOrigin.RuntimeRegistered &&
                    runtimeAdapter.ClearRegistration(
                        "runtime-first-clear"),
                    BuildAdapterAndPortDiagnostic(
                        runtimeAdapter,
                        runtimePort));
                Require(
                    runtimeAdapter.HasResetRegistrationRuntimeBinding &&
                    runtimeAdapter.RegisterWithCurrentHost(
                        "runtime-second"),
                    BuildAdapterAndPortDiagnostic(
                        runtimeAdapter,
                        runtimePort));
                string secondRuntimeId =
                    runtimeAdapter.SubjectId.StableText;
                Require(
                    secondRuntimeId == "qa.h2210.runtime#2" &&
                    runtimePort.RegisterRuntimeSubjectCallCount == 2 &&
                    runtimePort.UnregisterCallCount == 1 &&
                    runtimeAdapter.ClearRegistration(
                        "runtime-second-clear") &&
                    runtimePort.UnregisterCallCount == 2 &&
                    runtimePort.Registry.SubjectCount == 0,
                    BuildAdapterAndPortDiagnostic(
                        runtimeAdapter,
                        runtimePort));
                completed.Add(
                    "runtime-id-generation-remains-monotonic-across-reregistration");

                var lifetimePort =
                    new RecordingResetRegistrationRuntimePort();
                var foreignPort =
                    new RecordingResetRegistrationRuntimePort();
                ports.Add(lifetimePort);
                ports.Add(foreignPort);
                UnityResetSubjectAdapter lifetime = CreateAdapter(
                    "H2210 Lifetime Adapter",
                    UnityResetSubjectIdGenerationMode.AuthoredStableId,
                    "qa.h2210.lifetime",
                    string.Empty,
                    ResetSubjectScope.Route,
                    includeParticipant: false,
                    objects,
                    adapters);
                Require(
                    lifetime.TryBindResetRegistrationRuntime(
                        lifetimePort,
                        out string lifetimeBindingIssue),
                    lifetimeBindingIssue);
                Require(
                    lifetime.RegisterWithCurrentHost(
                        "lifetime-register"),
                    BuildAdapterAndPortDiagnostic(
                        lifetime,
                        lifetimePort));
                bool foreignBound =
                    lifetime.TryBindResetRegistrationRuntime(
                        foreignPort,
                        out string foreignBindingIssue);
                Require(
                    !foreignBound &&
                    foreignBindingIssue.Contains("different port") &&
                    lifetime.ClearRegistration(
                        "lifetime-clear") &&
                    lifetimePort.UnregisterCallCount == 1 &&
                    foreignPort.UnregisterCallCount == 0 &&
                    lifetimePort.Registry.SubjectCount == 0,
                    $"foreignIssue='{foreignBindingIssue}' " +
                    BuildAdapterAndPortDiagnostic(
                        lifetime,
                        lifetimePort));
                completed.Add(
                    "divergent-rebind-cannot-redirect-unregister-authority");

                var duplicatePort =
                    new RecordingResetRegistrationRuntimePort();
                ports.Add(duplicatePort);
                UnityResetSubjectAdapter duplicateA = CreateAdapter(
                    "H2210 Duplicate Adapter A",
                    UnityResetSubjectIdGenerationMode.AuthoredStableId,
                    "qa.h2210.duplicate",
                    string.Empty,
                    ResetSubjectScope.Activity,
                    includeParticipant: false,
                    objects,
                    adapters);
                UnityResetSubjectAdapter duplicateB = CreateAdapter(
                    "H2210 Duplicate Adapter B",
                    UnityResetSubjectIdGenerationMode.AuthoredStableId,
                    "qa.h2210.duplicate",
                    string.Empty,
                    ResetSubjectScope.Activity,
                    includeParticipant: false,
                    objects,
                    adapters);
                Require(
                    duplicateA.TryBindResetRegistrationRuntime(
                        duplicatePort,
                        out string duplicateABindingIssue),
                    duplicateABindingIssue);
                Require(
                    duplicateB.TryBindResetRegistrationRuntime(
                        duplicatePort,
                        out string duplicateBBindingIssue),
                    duplicateBBindingIssue);
                Require(
                    duplicateA.RegisterWithCurrentHost(
                        "duplicate-first"),
                    BuildAdapterAndPortDiagnostic(
                        duplicateA,
                        duplicatePort));
                bool duplicateRegistered =
                    duplicateB.RegisterWithCurrentHost(
                        "duplicate-second");
                Require(
                    !duplicateRegistered &&
                    !duplicateB.IsRegistered &&
                    !duplicateB.SubjectHandle.IsValid &&
                    duplicatePort.RegisterSubjectCallCount == 2 &&
                    duplicatePort.Registry.SubjectCount == 1 &&
                    duplicatePort.Registry.ParticipantCount == 0,
                    BuildAdapterAndPortDiagnostic(
                        duplicateB,
                        duplicatePort));
                Require(
                    duplicateA.ClearRegistration(
                        "duplicate-cleanup") &&
                    duplicatePort.Registry.SubjectCount == 0,
                    BuildAdapterAndPortDiagnostic(
                        duplicateA,
                        duplicatePort));
                completed.Add(
                    "duplicate-authored-subject-fails-without-partial-registration");

                for (int index = 0; index < adapters.Count; index++)
                {
                    if (adapters[index] != null &&
                        adapters[index].IsRegistered)
                    {
                        adapters[index].ClearRegistration(
                            "final-cleanup");
                    }

                    Require(
                        adapters[index] == null ||
                        !adapters[index].IsRegistered,
                        $"Adapter index '{index}' retained registration.");
                }

                for (int index = 0; index < ports.Count; index++)
                {
                    Require(
                        ports[index].Registry.SubjectCount == 0 &&
                        ports[index].Registry.ParticipantCount == 0,
                        $"Port index '{index}' retained registrations. " +
                        BuildPortDiagnostic(ports[index]));
                }
                completed.Add("no-reset-registration-remains");

                Debug.Log(
                    $"{LogPrefix} status='Passed' cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{LogPrefix} status='Failed' message='{exception.Message}'.");
                throw;
            }
            finally
            {
                for (int index = adapters.Count - 1;
                     index >= 0;
                     index--)
                {
                    if (adapters[index] != null &&
                        adapters[index].IsRegistered)
                    {
                        adapters[index].ClearRegistration(
                            "finally-cleanup");
                    }
                }

                for (int index = objects.Count - 1;
                     index >= 0;
                     index--)
                {
                    if (objects[index] != null)
                    {
                        UnityObject.Destroy(objects[index]);
                    }
                }
            }
        }

        private static void RunBindingCompositionCases(
            ICollection<UnityObject> objects,
            ICollection<UnityResetSubjectAdapter> adapters,
            ICollection<RecordingResetRegistrationRuntimePort> ports,
            ICollection<string> completed)
        {
            var port = new RecordingResetRegistrationRuntimePort();
            var divergentPort =
                new RecordingResetRegistrationRuntimePort();
            ports.Add(port);
            ports.Add(divergentPort);

            GameObject empty = CreateInactiveRoot(
                "H2210 Binding Empty",
                objects);
            UnityResetSubjectAdapterBindingResult missingRuntime =
                UnityResetSubjectAdapterBinding.TryBind(
                    new[] { empty },
                    null);
            Require(
                !missingRuntime.Succeeded &&
                missingRuntime.Status ==
                    "RejectedMissingResetRegistrationRuntime" &&
                missingRuntime.RootCount == 1 &&
                missingRuntime.AdapterCount == 0,
                missingRuntime.Message);

            UnityResetSubjectAdapterBindingResult absent =
                UnityResetSubjectAdapterBinding.TryBind(
                    new[] { empty, empty },
                    port);
            Require(
                absent.Succeeded &&
                absent.Status == "OptionalAbsent" &&
                absent.RootCount == 1 &&
                absent.AdapterCount == 0,
                absent.Message);

            GameObject root = CreateInactiveRoot(
                "H2210 Binding Root",
                objects);
            GameObject child = new GameObject(
                "H2210 Binding Child");
            child.transform.SetParent(root.transform, false);
            child.SetActive(false);
            UnityResetSubjectAdapter adapter =
                child.AddComponent<UnityResetSubjectAdapter>();
            ConfigureAdapter(
                adapter,
                UnityResetSubjectIdGenerationMode.AuthoredStableId,
                "qa.h2210.binding",
                string.Empty,
                ResetSubjectScope.Activity,
                includeParticipant: false);
            adapters.Add(adapter);

            UnityResetSubjectAdapterBindingResult bound =
                UnityResetSubjectAdapterBinding.TryBind(
                    new[] { root, root, child },
                    port);
            Require(
                bound.Succeeded &&
                bound.Status == "Bound" &&
                bound.RootCount == 2 &&
                bound.AdapterCount == 1 &&
                bound.BoundCount == 1 &&
                bound.IdempotentCount == 0 &&
                bound.RejectedCount == 0 &&
                adapter.HasResetRegistrationRuntimeBinding,
                bound.Message);

            UnityResetSubjectAdapterBindingResult idempotent =
                UnityResetSubjectAdapterBinding.TryBind(
                    new[] { root },
                    port);
            Require(
                idempotent.Succeeded &&
                idempotent.AdapterCount == 1 &&
                idempotent.BoundCount == 0 &&
                idempotent.IdempotentCount == 1 &&
                idempotent.RejectedCount == 0,
                idempotent.Message);

            UnityResetSubjectAdapterBindingResult divergent =
                UnityResetSubjectAdapterBinding.TryBind(
                    new[] { root },
                    divergentPort);
            Require(
                !divergent.Succeeded &&
                divergent.Status == "RejectedAdapterBinding" &&
                divergent.AdapterCount == 1 &&
                divergent.RejectedCount == 1,
                divergent.Message);

            completed.Add(
                "explicit-root-binding-missing-optional-idempotent-and-divergent-cases");
        }

        private static UnityResetSubjectAdapter CreateAdapter(
            string name,
            UnityResetSubjectIdGenerationMode idGeneration,
            string subjectId,
            string runtimePrefix,
            ResetSubjectScope scope,
            bool includeParticipant,
            ICollection<UnityObject> objects,
            ICollection<UnityResetSubjectAdapter> adapters)
        {
            GameObject root = CreateInactiveRoot(name, objects);
            UnityResetSubjectAdapter adapter =
                root.AddComponent<UnityResetSubjectAdapter>();
            ConfigureAdapter(
                adapter,
                idGeneration,
                subjectId,
                runtimePrefix,
                scope,
                includeParticipant);

            if (includeParticipant)
            {
                GameObject participantObject = new GameObject(
                    name + " Inactive Participant");
                participantObject.transform.SetParent(
                    root.transform,
                    false);
                participantObject.SetActive(false);
                participantObject.AddComponent<
                    QaH2210UnityResettable>();
            }

            adapters.Add(adapter);
            return adapter;
        }

        private static void ConfigureAdapter(
            UnityResetSubjectAdapter adapter,
            UnityResetSubjectIdGenerationMode idGeneration,
            string subjectId,
            string runtimePrefix,
            ResetSubjectScope scope,
            bool includeParticipant)
        {
            adapter.ConfigureForQa(
                qaRegisterOnEnable: false,
                qaUnregisterOnDisable: true,
                qaRetryUntilRuntimeAvailable: true,
                qaIdGeneration: idGeneration,
                qaSubjectId: subjectId,
                qaRuntimeSubjectIdPrefix: runtimePrefix,
                qaScope: scope,
                qaDisplayName: adapter.gameObject.name,
                qaDiagnosticTag: "qa.h2210",
                qaParticipantDiscovery:
                    UnityResetParticipantDiscoveryMode.Children,
                qaIncludeInactiveParticipants: true,
                qaIncludeUnityResettableComponents:
                    includeParticipant);
        }

        private static GameObject CreateInactiveRoot(
            string name,
            ICollection<UnityObject> objects)
        {
            var root = new GameObject(name);
            root.SetActive(false);
            objects.Add(root);
            return root;
        }

        private static string BuildAdapterDiagnostic(
            UnityResetSubjectAdapter adapter)
        {
            return
                $"adapter='{adapter?.name ?? "<null>"}' " +
                $"bound='{adapter?.HasResetRegistrationRuntimeBinding}' " +
                $"registered='{adapter?.IsRegistered}' " +
                $"bindingStatus='{adapter?.ResetRegistrationRuntimeBindingStatus}' " +
                $"bindingDiagnostic='{adapter?.ResetRegistrationRuntimeBindingDiagnostic}' " +
                $"participantCount='{adapter?.RegisteredParticipantCount}'.";
        }

        private static string BuildAdapterAndPortDiagnostic(
            UnityResetSubjectAdapter adapter,
            RecordingResetRegistrationRuntimePort port)
        {
            return BuildAdapterDiagnostic(adapter) + " " +
                   BuildPortDiagnostic(port);
        }

        private static string BuildPortDiagnostic(
            RecordingResetRegistrationRuntimePort port)
        {
            return
                $"subjects='{port?.Registry.SubjectCount}' " +
                $"participants='{port?.Registry.ParticipantCount}' " +
                $"resolveCalls='{port?.ResolveOwnerCallCount}' " +
                $"subjectCalls='{port?.RegisterSubjectCallCount}' " +
                $"runtimeSubjectCalls='{port?.RegisterRuntimeSubjectCallCount}' " +
                $"participantCalls='{port?.RegisterParticipantCallCount}' " +
                $"unregisterCalls='{port?.UnregisterCallCount}' " +
                $"lastSource='{port?.LastSource}' " +
                $"lastReason='{port?.LastReason}'.";
        }

        private static void Require(
            bool value,
            string message)
        {
            if (!value)
            {
                throw new InvalidOperationException(message);
            }
        }

        private sealed class RecordingResetRegistrationRuntimePort :
            IResetRegistrationRuntimePort
        {
            internal ResetRegistry Registry { get; } =
                new ResetRegistry();

            internal bool OwnerAvailable { get; set; } = true;

            internal int ResolveOwnerCallCount { get; private set; }

            internal int RegisterSubjectCallCount { get; private set; }

            internal int RegisterRuntimeSubjectCallCount { get; private set; }

            internal int RegisterParticipantCallCount { get; private set; }

            internal int UnregisterCallCount { get; private set; }

            internal RuntimeContentOwner LastResolvedOwner {
                get;
                private set;
            }

            internal string LastSource { get; private set; } =
                string.Empty;

            internal string LastReason { get; private set; } =
                string.Empty;

            internal string LastParticipantSource {
                get;
                private set;
            } = string.Empty;

            internal string LastParticipantReason {
                get;
                private set;
            } = string.Empty;

            internal string LastUnregisterSource {
                get;
                private set;
            } = string.Empty;

            internal string LastUnregisterReason {
                get;
                private set;
            } = string.Empty;

            public bool TryResolveCurrentResetOwner(
                ResetSubjectScope scope,
                out RuntimeContentOwner owner,
                out string issue)
            {
                ResolveOwnerCallCount++;
                if (!OwnerAvailable)
                {
                    owner = default;
                    issue =
                        "QA Reset registration runtime has no owner available.";
                    return false;
                }

                switch (scope)
                {
                    case ResetSubjectScope.Route:
                        owner = RuntimeContentOwner.Route(
                            "qa.h2210.route",
                            "H2210 Route Owner");
                        break;

                    case ResetSubjectScope.Activity:
                        owner = RuntimeContentOwner.Activity(
                            "qa.h2210.activity",
                            "H2210 Activity Owner");
                        break;

                    case ResetSubjectScope.Runtime:
                        owner = RuntimeContentOwner.Transient(
                            "qa.h2210.runtime-owner",
                            "H2210 Runtime Owner");
                        break;

                    default:
                        owner = default;
                        issue =
                            $"QA Reset registration runtime does not support scope '{scope}'.";
                        return false;
                }

                LastResolvedOwner = owner;
                issue = string.Empty;
                return true;
            }

            public ResetRegistryOperationResult RegisterResetSubject(
                ResetSubject subject,
                UnityObject owner,
                string source,
                string reason)
            {
                RegisterSubjectCallCount++;
                LastSource = source ?? string.Empty;
                LastReason = reason ?? string.Empty;
                return Registry.RegisterSubject(
                    subject,
                    owner,
                    source,
                    reason);
            }

            public ResetRegistryOperationResult RegisterRuntimeResetSubject(
                string authoredPrefix,
                ResetSubjectScope scope,
                RuntimeContentOwner owner,
                UnityObject ownerObject,
                string displayName,
                string diagnosticTag,
                string source,
                string reason)
            {
                RegisterRuntimeSubjectCallCount++;
                LastSource = source ?? string.Empty;
                LastReason = reason ?? string.Empty;
                return Registry.RegisterRuntimeSubject(
                    authoredPrefix,
                    scope,
                    owner,
                    ownerObject,
                    displayName,
                    diagnosticTag,
                    source,
                    reason);
            }

            public ResetRegistryOperationResult RegisterResetParticipant(
                ResetRegistrationHandle subjectHandle,
                IResetParticipant participant,
                UnityObject owner,
                string source,
                string reason)
            {
                RegisterParticipantCallCount++;
                LastParticipantSource = source ?? string.Empty;
                LastParticipantReason = reason ?? string.Empty;
                return Registry.RegisterParticipant(
                    subjectHandle,
                    participant,
                    owner,
                    source,
                    reason);
            }

            public ResetRegistryOperationResult UnregisterResetRegistration(
                ResetRegistrationHandle handle,
                UnityObject owner,
                string source,
                string reason)
            {
                UnregisterCallCount++;
                LastUnregisterSource = source ?? string.Empty;
                LastUnregisterReason = reason ?? string.Empty;
                return Registry.Unregister(
                    handle,
                    owner,
                    source,
                    reason);
            }
        }
    }

    internal sealed class QaH2210UnityResettable :
        MonoBehaviour,
        IUnityResettable
    {
        public string ResetParticipantId =>
            "qa.h2210.inactive-resettable";

        public ResetParticipantResult Reset(ResetContext context)
        {
            return default;
        }
    }
}
