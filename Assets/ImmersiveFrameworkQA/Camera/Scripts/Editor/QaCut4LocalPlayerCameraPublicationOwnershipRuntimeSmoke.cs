using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Immersive.Framework.Camera;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Camera.Editor
{
    /// <summary>
    /// Play Mode proof that the real P3 gameplay admission lane is the sole Local Player
    /// camera publisher and that its request is released with the admission.
    /// </summary>
    internal static class QaCut4LocalPlayerCameraPublicationOwnershipRuntimeSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Camera/Run Local Player Camera Publication Regression";
        private const string CanonicalSmokeTypeName =
            "ImmersiveFrameworkQA.Player.Editor.QaP3K7HRouteStartupActivityPlayerAdmissionSmoke";
        private const string RuntimeHostTypeName =
            "Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost";
        private const string GameplayModuleTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerGameplayRuntimeHostModule";
        private const int MaxObservationFrames = 900;

        private static readonly BindingFlags InstanceAny =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly BindingFlags StaticAny =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        [MenuItem(MenuPath)]
        private static async void Run()
        {
            var completed = new List<string>();
            string publishedRequestId = string.Empty;

            try
            {
                Require(EditorApplication.isPlaying,
                    "Cut 4 runtime smoke must run in a fresh Play Mode session.");
                completed.Add("play-mode-required");

                MethodInfo canonicalMethod = ResolveType(CanonicalSmokeTypeName)
                    .GetMethod("RunCanonicalAsync", StaticAny);
                Require(canonicalMethod != null,
                    "Canonical P3K.7H runtime smoke entry point was not found.");

                object taskObject = canonicalMethod.Invoke(null, Array.Empty<object>());
                Require(taskObject is Task,
                    "Canonical P3K.7H runtime smoke did not return a Task.");
                var canonicalTask = (Task)taskObject;
                completed.Add("canonical-real-player-lane-started");

                bool publicationObserved = false;
                for (int frame = 0;
                     frame < MaxObservationFrames && !canonicalTask.IsCompleted;
                     frame++)
                {
                    if (!publicationObserved &&
                        TryGetGameplaySnapshot(out PlayerGameplayRuntimeHostSnapshot snapshot) &&
                        snapshot.Admission != null &&
                        snapshot.Admission.PublishedCameraCount > 0)
                    {
                        PlayerGameplayAdmissionSummary published =
                            RequireSinglePublishedAdmission(snapshot.Admission);
                        publishedRequestId = published.CameraRequestId;
                        publicationObserved = true;

                        Require(string.Equals(
                                published.CameraPublisherSource,
                                "PlayerGameplayAdmissionRuntimeContext",
                                StringComparison.Ordinal),
                            "Canonical Local Player camera request has a foreign publisher source.");
                        completed.Add("admission-is-canonical-camera-publisher");

                        Require(!string.IsNullOrWhiteSpace(published.CameraRequestId) &&
                            !string.IsNullOrWhiteSpace(published.CameraOutputId),
                            "Published Local Player camera evidence lost request or output identity.");
                        completed.Add("published-camera-identity-explicit");

                        LocalPlayerCameraRequestBinding[] sceneBindings =
                            UnityEngine.Object.FindObjectsByType<
                                LocalPlayerCameraRequestBinding>(
                                FindObjectsInactive.Include);
                        for (int index = 0; index < sceneBindings.Length; index++)
                        {
                            Require(!sceneBindings[index].IsPublished,
                                "A Scene Local Player Camera Request Binding published beside gameplay admission.");
                        }
                        completed.Add("scene-camera-publisher-absent");

                        CameraOutputContext output = ResolveOutputContext(
                            published.CameraOutputId);
                        Require(output.Contains(
                                new CameraRequestId(published.CameraRequestId)),
                            "Gameplay admission summary references a camera request not admitted by the output context.");
                        Require(CountLocalPlayerRequests(output) == 1,
                            "Camera output contains more than one Local Player request while one Slot is admitted.");
                        completed.Add("exactly-one-local-player-request-admitted");
                    }

                    await Awaitable.NextFrameAsync();
                }

                await canonicalTask;
                Require(publicationObserved,
                    "Canonical real Player lane completed without observable Local Player camera publication.");

                PropertyInfo resultProperty = taskObject.GetType().GetProperty(
                    "Result",
                    InstanceAny);
                Require(resultProperty != null,
                    "Canonical runtime Task has no result evidence.");
                IEnumerable canonicalCases = resultProperty.GetValue(taskObject) as IEnumerable;
                Require(canonicalCases != null,
                    "Canonical runtime Task returned no case evidence.");
                completed.Add("canonical-real-player-lane-completed");

                PlayerGameplayRuntimeHostSnapshot finalSnapshot = null;
                for (int frame = 0; frame < 120; frame++)
                {
                    if (TryGetGameplaySnapshot(out finalSnapshot) &&
                        finalSnapshot.Admission != null &&
                        finalSnapshot.Admission.PublishedCameraCount == 0)
                    {
                        break;
                    }

                    await Awaitable.NextFrameAsync();
                }

                Require(finalSnapshot != null &&
                    finalSnapshot.Admission != null &&
                    finalSnapshot.Admission.PublishedCameraCount == 0,
                    "Local Player camera publication remained after gameplay admission release.");
                completed.Add("camera-publication-released-with-admission");

                if (!string.IsNullOrWhiteSpace(publishedRequestId))
                {
                    CameraOutputSessionBinding[] outputs =
                        UnityEngine.Object.FindObjectsByType<CameraOutputSessionBinding>(
                            FindObjectsInactive.Include);
                    for (int index = 0; index < outputs.Length; index++)
                    {
                        CameraOutputContext context = outputs[index] != null
                            ? outputs[index].Context
                            : null;
                        Require(context == null ||
                            !context.Contains(new CameraRequestId(publishedRequestId)),
                            "Released Local Player camera request remains in a CameraOutputContext.");
                    }
                }
                completed.Add("released-request-absent-from-output");

                Require(completed.Count == 9,
                    "Cut 4 runtime smoke case count changed unexpectedly.");
                Debug.Log(
                    "[CUT4_LOCAL_PLAYER_CAMERA_PUBLICATION_OWNERSHIP_RUNTIME_SMOKE] " +
                    $"status='Passed' cases='{completed.Count}' request='{publishedRequestId}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Exception resolved = Unwrap(exception);
                Debug.LogError(
                    "[CUT4_LOCAL_PLAYER_CAMERA_PUBLICATION_OWNERSHIP_RUNTIME_SMOKE] " +
                    $"status='Failed' exception='{resolved.GetType().Name}' message='{Escape(resolved.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw resolved;
            }
        }

        private static PlayerGameplayAdmissionSummary RequireSinglePublishedAdmission(
            PlayerGameplayAdmissionSnapshot admission)
        {
            Require(admission.PublishedCameraCount == 1,
                $"Expected one published Local Player camera admission, found '{admission.PublishedCameraCount}'.");

            PlayerGameplayAdmissionSummary result = default;
            int count = 0;
            for (int index = 0; index < admission.Slots.Count; index++)
            {
                PlayerGameplayAdmissionSummary summary = admission.Slots[index];
                if (!summary.CameraRequestPublished)
                {
                    continue;
                }

                result = summary;
                count++;
            }

            Require(count == 1 && result.IsAdmitted,
                "Admission snapshot did not expose exactly one coherent published camera summary.");
            return result;
        }

        private static CameraOutputContext ResolveOutputContext(string outputId)
        {
            CameraOutputSessionBinding[] outputs =
                UnityEngine.Object.FindObjectsByType<CameraOutputSessionBinding>(
                    FindObjectsInactive.Include);
            CameraOutputContext resolved = null;
            int matches = 0;
            for (int index = 0; index < outputs.Length; index++)
            {
                CameraOutputSessionBinding binding = outputs[index];
                if (binding == null || binding.Context == null ||
                    !string.Equals(binding.OutputIdText, outputId, StringComparison.Ordinal))
                {
                    continue;
                }

                resolved = binding.Context;
                matches++;
            }

            Require(matches == 1 && resolved != null,
                $"Expected exactly one CameraOutputContext for output '{outputId}', found '{matches}'.");
            return resolved;
        }

        private static int CountLocalPlayerRequests(CameraOutputContext context)
        {
            FieldInfo admittedField = typeof(CameraOutputContext).GetField(
                "admittedRequests",
                InstanceAny);
            Require(admittedField != null,
                "CameraOutputContext admitted request storage was not found.");
            IDictionary admitted = admittedField.GetValue(context) as IDictionary;
            Require(admitted != null,
                "CameraOutputContext admitted request storage is unavailable.");

            int count = 0;
            foreach (DictionaryEntry entry in admitted)
            {
                if (entry.Value is CameraRequest request &&
                    request.Owner.Kind == CameraRequestOwnerKind.LocalPlayer)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool TryGetGameplaySnapshot(
            out PlayerGameplayRuntimeHostSnapshot snapshot)
        {
            snapshot = null;
            Type hostType = ResolveType(RuntimeHostTypeName);
            MethodInfo tryGetCurrent = hostType.GetMethod("TryGetCurrent", StaticAny);
            if (tryGetCurrent == null)
            {
                return false;
            }

            object[] hostArguments = { null };
            if (!(bool)tryGetCurrent.Invoke(null, hostArguments) ||
                !(hostArguments[0] is Component host))
            {
                return false;
            }

            Type moduleType = ResolveType(GameplayModuleTypeName);
            Component module = host.GetComponent(moduleType);
            if (module == null)
            {
                return false;
            }

            MethodInfo tryGetSnapshot = moduleType.GetMethod(
                "TryGetSnapshot",
                InstanceAny);
            if (tryGetSnapshot == null)
            {
                return false;
            }

            object[] snapshotArguments = { null };
            bool available = (bool)tryGetSnapshot.Invoke(module, snapshotArguments);
            snapshot = snapshotArguments[0] as PlayerGameplayRuntimeHostSnapshot;
            return snapshot != null && (available || !snapshot.IsInitialized);
        }

        private static Type ResolveType(string fullName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int index = 0; index < assemblies.Length; index++)
            {
                Type type = assemblies[index].GetType(fullName, throwOnError: false);
                if (type != null)
                {
                    return type;
                }
            }

            throw new InvalidOperationException($"Type '{fullName}' was not found.");
        }

        private static Exception Unwrap(Exception exception)
        {
            if (exception is TargetInvocationException invocation &&
                invocation.InnerException != null)
            {
                return Unwrap(invocation.InnerException);
            }

            if (exception is AggregateException aggregate &&
                aggregate.InnerExceptions.Count == 1)
            {
                return Unwrap(aggregate.InnerExceptions[0]);
            }

            return exception;
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", " ")
                .Replace("\n", " ");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
