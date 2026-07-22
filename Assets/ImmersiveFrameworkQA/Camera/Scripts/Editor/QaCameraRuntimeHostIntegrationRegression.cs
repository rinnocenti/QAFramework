using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.Camera;
using Immersive.Framework.PlayerParticipation;
using ImmersiveFrameworkQA.GameFlow.Internal.Editor.ImmersiveFrameworkQA.GameFlow.InternalEditor;
using ImmersiveFrameworkQA.Player.Editor;
using UnityEditor;
using UnityEngine;
namespace ImmersiveFrameworkQA.Camera.Scripts.Editor
{
    /// <summary>
    /// Play Mode proof that the official gameplay admission lane is the sole Local Player
    /// camera publisher and that its request is released with the admission.
    /// </summary>
    internal static class QaCameraRuntimeHostIntegrationRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Camera/Run Camera Runtime Host Integration Regression";
        private const string LogPrefix =
            "[CAMERA_RUNTIME_HOST_INTEGRATION_REGRESSION]";
        private const int MaxObservationFrames = 900;

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
                Require(canonicalCases != null && canonicalCases.Count == 51,
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
