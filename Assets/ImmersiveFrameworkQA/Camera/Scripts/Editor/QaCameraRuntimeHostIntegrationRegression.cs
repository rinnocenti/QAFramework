using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.Camera;
using Immersive.Framework.PlayerParticipation;
using ImmersiveFrameworkQA.GameFlow.Internal.Editor;
using ImmersiveFrameworkQA.Player.Editor;
using UnityEditor;
using UnityEngine;
namespace ImmersiveFrameworkQA.Camera.Editor
{
    /// <summary>
    /// Play Mode proof that PlayerGameplayCameraEligibilityRuntimeContext owns and
    /// publishes the Local Player camera capability. Gameplay Admission only aggregates
    /// Occupancy, Input and Camera; full gameplay-chain cleanup releases Admission,
    /// Camera, Input and then Occupancy.
    /// </summary>
    internal static class QaCameraRuntimeHostIntegrationRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Camera/Run Camera Runtime Host Integration Regression";
        private const string LogPrefix =
            "[CAMERA_RUNTIME_HOST_INTEGRATION_REGRESSION]";
        private const int MaxObservationFrames = 900;
        private const int ExpectedCanonicalPlayerCases = 110;

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        private static async void Run()
        {
            var completed = new List<string>();
            string publishedRequestId = string.Empty;

            try
            {
                Require(EditorApplication.isPlaying,
                    "Camera Runtime Host Integration Regression requires a fresh Play Mode session.");
                completed.Add("play-mode-required");

                Task<IReadOnlyList<string>> canonicalTask =
                    QaPlayerGameplayAdmissionRegression.RunRegressionAsync();
                completed.Add("canonical-real-player-lane-started");

                bool publicationObserved = false;
                for (int frame = 0;
                     frame < MaxObservationFrames && !canonicalTask.IsCompleted;
                     frame++)
                {
                    if (!publicationObserved &&
                        TryGetGameplaySnapshot(out PlayerGameplayRuntimeHostSnapshot snapshot) &&
                        snapshot.CameraEligibility != null &&
                        snapshot.CameraEligibility.EligibleCount > 0)
                    {
                        PlayerGameplayCameraEligibilitySummary published =
                            RequireSinglePublishedCamera(snapshot.CameraEligibility);
                        publishedRequestId = published.RequestId;
                        publicationObserved = true;

                        Require(string.Equals(
                                published.CameraPublisherSource,
                                "PlayerGameplayCameraEligibilityRuntimeContext",
                                StringComparison.Ordinal),
                            "Canonical Local Player camera request has a foreign publisher source.");
                        completed.Add("camera-capability-is-canonical-publisher");

                        Require(!string.IsNullOrWhiteSpace(published.RequestId) &&
                            !string.IsNullOrWhiteSpace(published.Token.CameraOutputId),
                            "Published Local Player camera evidence lost request or output identity.");
                        completed.Add("published-camera-identity-explicit");

                        LocalPlayerCameraRequestBinding[] sceneBindings =
                            UnityEngine.Object.FindObjectsByType<
                                LocalPlayerCameraRequestBinding>(
                                FindObjectsInactive.Include);
                        for (int index = 0; index < sceneBindings.Length; index++)
                        {
                            Require(!sceneBindings[index].IsPublished,
                                "A Scene Local Player Camera Request Binding published beside the canonical camera capability.");
                        }
                        completed.Add("scene-camera-publisher-absent");

                        CameraOutputContext output = ResolveOutputContext(
                            published.Token.CameraOutputId);
                        Require(output.Contains(
                                new CameraRequestId(published.RequestId)),
                            "Camera capability summary references a request not admitted by the output context.");
                        Require(output.CaptureSnapshot().AdmittedRequestCount > 0,
                            "Camera output has no admitted request while the Local Player request is published.");
                        completed.Add("exactly-one-local-player-request-admitted");
                    }

                    await Awaitable.NextFrameAsync();
                }

                await canonicalTask;
                Require(publicationObserved,
                    "Canonical real Player lane completed without observable Local Player camera publication.");

                IReadOnlyList<string> canonicalCases = canonicalTask.Result;
                Require(
                    canonicalCases != null &&
                    canonicalCases.Count == ExpectedCanonicalPlayerCases,
                    $"Canonical Player regression returned an unexpected case count. " +
                    $"expected='{ExpectedCanonicalPlayerCases}' " +
                    $"actual='{canonicalCases?.Count ?? 0}'.");
                completed.Add("canonical-real-player-lane-completed");

                PlayerGameplayRuntimeHostSnapshot finalSnapshot = null;
                for (int frame = 0; frame < 120; frame++)
                {
                    if (TryGetGameplaySnapshot(out finalSnapshot) &&
                        finalSnapshot.CameraEligibility != null &&
                        finalSnapshot.CameraEligibility.EligibleCount == 0)
                    {
                        break;
                    }

                    await Awaitable.NextFrameAsync();
                }

                Require(finalSnapshot != null &&
                    finalSnapshot.CameraEligibility != null &&
                    finalSnapshot.CameraEligibility.EligibleCount == 0,
                    "Local Player camera publication remained after canonical gameplay-chain cleanup.");
                completed.Add("camera-publication-released-with-gameplay-chain");

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
                            "Local Player camera request remains after canonical gameplay-chain cleanup.");
                    }
                }
                completed.Add("released-request-absent-from-output");

                Require(completed.Count == 9,
                    "Camera runtime host integration regression case count changed unexpectedly.");
                Debug.Log(
                    $"{LogPrefix} " +
                    $"status='Passed' phase='player-publication' cases='{completed.Count}' request='{publishedRequestId}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{LogPrefix} " +
                    $"status='Failed' phase='player-publication' exception='{exception.GetType().Name}' message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
            }
        }

        private static PlayerGameplayCameraEligibilitySummary RequireSinglePublishedCamera(
            PlayerGameplayCameraEligibilitySnapshot camera)
        {
            Require(camera.EligibleCount == 1,
                $"Expected one published Local Player camera capability, found '{camera.EligibleCount}'.");

            PlayerGameplayCameraEligibilitySummary result = default;
            int count = 0;
            for (int index = 0; index < camera.Slots.Count; index++)
            {
                PlayerGameplayCameraEligibilitySummary summary = camera.Slots[index];
                if (!summary.CameraRequestPublished)
                {
                    continue;
                }

                result = summary;
                count++;
            }

            Require(count == 1 && result.IsEligible,
                "Camera snapshot did not expose exactly one coherent published camera summary.");
            return result;
        }

        private static CameraOutputContext ResolveOutputContext(string outputId)
        {
            CameraOutputSessionBinding[] outputs =
                UnityEngine.Object.FindObjectsByType<CameraOutputSessionBinding>(
                    FindObjectsInactive.Include);
            CameraOutputContext resolved = null;
            CameraOutputSessionBinding resolvedBinding = null;
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
                resolvedBinding = binding;
                matches++;
            }

            Require(matches == 1 && resolved != null,
                $"Expected exactly one CameraOutputContext for output '{outputId}', found '{matches}'.");
            Require(resolvedBinding.IsInitialized && resolvedBinding.UnityCamera != null &&
                    resolvedBinding.CinemachineBrain != null,
                $"Camera output '{outputId}' is not initialized with an explicit Unity Camera and CinemachineBrain.");
            return resolved;
        }

        private static bool TryGetGameplaySnapshot(
            out PlayerGameplayRuntimeHostSnapshot snapshot)
        {
            return QaH2FrameworkReadiness.TryGetPlayerGameplaySnapshot(out snapshot);
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
