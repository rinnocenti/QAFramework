using System;
using System.Collections;
using System.Text;
using Immersive.Audio.Authoring;
using Immersive.Audio.Contracts;
using Immersive.Audio.Unity.Hosts;
using Immersive.Framework.Audio;
using UnityEngine;

namespace ImmersiveFrameworkQA.Audio
{
    public sealed class AudioQaPanel : MonoBehaviour
    {
        [Header("Runtime")]
        [SerializeField] private AudioRuntimeHost audioRuntimeHost;
        [SerializeField] private AudioRuntimeHost missingDefaultsHost;
        [SerializeField] private AudioRuntimeHost missingPoolHost;
        [SerializeField] private AudioListenerRuntimeHost listenerRuntimeHost;

        [Header("Cues")]
        [SerializeField] private AudioSfxCueAsset sfxCue;
        [SerializeField] private AudioSfxCueAsset pooledSfxCue;
        [SerializeField] private AudioSfxCueAsset missingClipSfxCue;
        [SerializeField] private AudioBgmCueAsset bgmCue;
        [SerializeField] private FrameworkBgmQaPanel frameworkBgmPanel;

        [Header("Panel")]
        [SerializeField] private Rect panelRect = new Rect(16f, 16f, 560f, 650f);
        [SerializeField] private Vector2 minimumPanelSize = new Vector2(360f, 320f);
        [SerializeField] private float scrollContentWidth = 520f;

        private IAudioPlaybackHandle lastSfxHandle;
        private IAudioPlaybackHandle lastBgmHandle;
        private int sfxPlayRequests;
        private int bgmPlayRequests;
        private int bgmStopRequests;
        private int passedSmokes;
        private int failedSmokes;
        private string lastResult = "Audio QA ready.";
        private bool lastPassed = true;
        private Vector2 scroll;

        private void Start()
        {
            if (audioRuntimeHost != null)
            {
                AudioSettingsResolution settings = audioRuntimeHost.Compose();
                SetResult(settings.IsResolved, $"Initial compose status='{settings.Status}' issues='{settings.Issues.Count}'.");
            }
        }

        public void PlaySfx()
        {
            RunOperation(
                "Play SFX",
                () =>
                {
                    AudioPlaybackResult result = audioRuntimeHost != null
                        ? audioRuntimeHost.PlaySfx(sfxCue)
                        : AudioPlaybackResult.Failure(
                            AudioPlaybackStatus.FailedServiceNotReady,
                            new AudioConfigurationIssue("audio_qa_host_missing", "AudioRuntimeHost is missing.", nameof(audioRuntimeHost)));

                    sfxPlayRequests++;
                    lastSfxHandle = result.Handle;
                    return Expect(result, AudioPlaybackStatus.Succeeded, "SFX direct playback should succeed.");
                });
        }

        public void PlayBgm()
        {
            RunOperation(
                "Play BGM",
                () =>
                {
                    AudioPlaybackResult result = audioRuntimeHost != null
                        ? audioRuntimeHost.PlayBgm(bgmCue)
                        : AudioPlaybackResult.Failure(
                            AudioPlaybackStatus.FailedServiceNotReady,
                            new AudioConfigurationIssue("audio_qa_host_missing", "AudioRuntimeHost is missing.", nameof(audioRuntimeHost)));

                    bgmPlayRequests++;
                    lastBgmHandle = result.Handle;
                    return Expect(result, AudioPlaybackStatus.Succeeded, "BGM playback should succeed.");
                });
        }

        public void StopBgm()
        {
            RunOperation(
                "Stop BGM",
                () =>
                {
                    AudioPlaybackResult result = audioRuntimeHost != null
                        ? audioRuntimeHost.StopBgm()
                        : AudioPlaybackResult.Failure(
                            AudioPlaybackStatus.FailedServiceNotReady,
                            new AudioConfigurationIssue("audio_qa_host_missing", "AudioRuntimeHost is missing.", nameof(audioRuntimeHost)));

                    bgmStopRequests++;
                    return Expect(result, AudioPlaybackStatus.Stopped, "BGM stop should report Stopped.");
                });
        }

        public void RunDirectSfxSmoke()
        {
            RunOperation(
                "Direct SFX Smoke",
                () =>
                {
                    AudioPlaybackResult result = audioRuntimeHost != null
                        ? audioRuntimeHost.PlaySfx(sfxCue)
                        : AudioPlaybackResult.Failure(
                            AudioPlaybackStatus.FailedServiceNotReady,
                            new AudioConfigurationIssue("audio_qa_host_missing", "AudioRuntimeHost is missing.", nameof(audioRuntimeHost)));

                    sfxPlayRequests++;
                    lastSfxHandle = result.Handle;
                    bool ok = result.Succeeded && result.Handle != null && result.Handle.IsValid;
                    SetResult(ok, $"Direct SFX smoke status='{result.Status}' handleValid='{result.Handle != null && result.Handle.IsValid}'.");
                    return ok;
                });
        }

        public void RunMissingClipSmoke()
        {
            RunOperation(
                "Missing Clip Smoke",
                () =>
                {
                    AudioPlaybackResult result = audioRuntimeHost != null
                        ? audioRuntimeHost.PlaySfx(missingClipSfxCue)
                        : AudioPlaybackResult.Failure(
                            AudioPlaybackStatus.FailedServiceNotReady,
                            new AudioConfigurationIssue("audio_qa_host_missing", "AudioRuntimeHost is missing.", nameof(audioRuntimeHost)));

                    bool ok = result.Status == AudioPlaybackStatus.FailedMissingClip;
                    SetResult(ok, $"Missing clip smoke status='{result.Status}' issues='{FormatIssues(result)}'.");
                    return ok;
                });
        }

        public void RunMissingDefaultsSmoke()
        {
            RunOperation(
                "Missing Defaults Smoke",
                () =>
                {
                    AudioPlaybackResult result = missingDefaultsHost != null
                        ? missingDefaultsHost.PlaySfx(sfxCue)
                        : AudioPlaybackResult.Failure(
                            AudioPlaybackStatus.FailedServiceNotReady,
                            new AudioConfigurationIssue("audio_qa_missing_defaults_host_missing", "Missing-defaults host is missing.", nameof(missingDefaultsHost)));

                    bool ok = result.Status == AudioPlaybackStatus.FailedMissingDefaults;
                    SetResult(ok, $"Missing defaults smoke status='{result.Status}' issues='{FormatIssues(result)}'.");
                    return ok;
                });
        }

        public void RunPooledSfxSmoke()
        {
            RunOperation(
                "Pooled SFX Smoke",
                () =>
                {
                    AudioPlaybackResult result = audioRuntimeHost != null
                        ? audioRuntimeHost.PlaySfx(pooledSfxCue)
                        : AudioPlaybackResult.Failure(
                            AudioPlaybackStatus.FailedServiceNotReady,
                            new AudioConfigurationIssue("audio_qa_host_missing", "AudioRuntimeHost is missing.", nameof(audioRuntimeHost)));

                    sfxPlayRequests++;
                    lastSfxHandle = result.Handle;
                    bool ok = result.Succeeded && result.Handle != null && result.Handle.IsValid;
                    SetResult(ok, $"Pooled SFX smoke status='{result.Status}' handleValid='{result.Handle != null && result.Handle.IsValid}' issues='{FormatIssues(result)}'.");
                    return ok;
                });
        }

        public void RunMissingPoolSmoke()
        {
            RunOperation(
                "Missing Pool Smoke",
                () =>
                {
                    AudioPlaybackResult result = missingPoolHost != null
                        ? missingPoolHost.PlaySfx(pooledSfxCue)
                        : AudioPlaybackResult.Failure(
                            AudioPlaybackStatus.FailedServiceNotReady,
                            new AudioConfigurationIssue("audio_qa_missing_pool_host_missing", "Missing-pool host is missing.", nameof(missingPoolHost)));

                    bool ok = result.Status == AudioPlaybackStatus.FailedMissingPoolService;
                    SetResult(ok, $"Missing pool smoke status='{result.Status}' issues='{FormatIssues(result)}'.");
                    return ok;
                });
        }

        public void RunBgmSmoke()
        {
            StartCoroutine(RunBgmSmokeRoutine());
        }

        public void RunListenerSmoke()
        {
            RunOperation(
                "Listener Smoke",
                () =>
                {
                    if (listenerRuntimeHost == null)
                    {
                        SetResult(false, "Listener smoke failed: AudioListenerRuntimeHost is missing.");
                        return false;
                    }

                    Transform hostTarget = listenerRuntimeHost.ListenerTarget != null
                        ? listenerRuntimeHost.ListenerTarget
                        : listenerRuntimeHost.transform;
                    AudioListener hostListenerBefore = hostTarget != null
                        ? hostTarget.GetComponent<AudioListener>()
                        : null;
                    bool hostListenerExistedBefore = hostListenerBefore != null;
                    bool hostListenerEnabledBefore = hostListenerBefore != null && hostListenerBefore.enabled;

                    GameObject duplicate = null;
                    AudioListener duplicateListener = null;
                    AudioListenerHostReport report = default;
                    bool duplicateStillEnabled = false;
                    bool evidencePassed = false;
                    bool cleanupRestored = false;

                    try
                    {
                        duplicate = new GameObject("QA_TemporaryDuplicateAudioListener");
                        duplicateListener = duplicate.AddComponent<AudioListener>();
                        duplicateListener.enabled = true;

                        report = listenerRuntimeHost.EnsureListenerAndReport();
                        duplicateStillEnabled = duplicateListener != null && duplicateListener.enabled;
                        evidencePassed = report.Status == AudioConfigurationStatus.IssuesDetected
                            && report.DuplicatePolicy == AudioListenerDuplicatePolicy.ReportOnly
                            && report.DuplicateEnabledListeners > 0
                            && duplicateStillEnabled;
                    }
                    finally
                    {
                        if (duplicate != null)
                        {
                            duplicate.SetActive(false);
                            Destroy(duplicate);
                        }

                        AudioListener hostListenerAfter = hostTarget != null
                            ? hostTarget.GetComponent<AudioListener>()
                            : null;
                        if (hostListenerAfter != null)
                        {
                            hostListenerAfter.enabled = hostListenerEnabledBefore;
                            if (!hostListenerExistedBefore)
                            {
                                Destroy(hostListenerAfter);
                            }
                        }

                        cleanupRestored = hostListenerExistedBefore
                            ? hostListenerAfter != null && hostListenerAfter.enabled == hostListenerEnabledBefore
                            : hostListenerAfter == null || !hostListenerAfter.enabled;
                    }

                    bool ok = evidencePassed && cleanupRestored;
                    SetResult(
                        ok,
                        $"Listener smoke status='{report.Status}' policy='{report.DuplicatePolicy}' duplicates='{report.DuplicateEnabledListeners}' duplicateStillEnabled='{duplicateStillEnabled}' hostListenerEnabledBefore='{hostListenerEnabledBefore}' cleanupRestored='{cleanupRestored}'.");
                    return ok;
                });
        }

        public void RunAllSmokes()
        {
            StartCoroutine(RunAllSmokesRoutine());
        }

        public void ResetQaCounters()
        {
            sfxPlayRequests = 0;
            bgmPlayRequests = 0;
            bgmStopRequests = 0;
            passedSmokes = 0;
            failedSmokes = 0;
            lastSfxHandle = null;
            lastBgmHandle = null;
            SetResult(true, "Audio QA counters reset.");
        }

        public void ConfigureFrameworkBgmPanel(FrameworkBgmQaPanel panel)
        {
            frameworkBgmPanel = panel;
        }

        private IEnumerator RunBgmSmokeRoutine()
        {
            bool playOk = false;
            bool stopOk = false;

            AudioPlaybackResult play = audioRuntimeHost != null
                ? audioRuntimeHost.PlayBgm(bgmCue)
                : AudioPlaybackResult.Failure(
                    AudioPlaybackStatus.FailedServiceNotReady,
                    new AudioConfigurationIssue("audio_qa_host_missing", "AudioRuntimeHost is missing.", nameof(audioRuntimeHost)));

            bgmPlayRequests++;
            lastBgmHandle = play.Handle;
            playOk = play.Status == AudioPlaybackStatus.Succeeded;

            yield return new WaitForSecondsRealtime(0.15f);

            AudioPlaybackResult stop = audioRuntimeHost != null
                ? audioRuntimeHost.StopBgm()
                : AudioPlaybackResult.Failure(
                    AudioPlaybackStatus.FailedServiceNotReady,
                    new AudioConfigurationIssue("audio_qa_host_missing", "AudioRuntimeHost is missing.", nameof(audioRuntimeHost)));

            bgmStopRequests++;
            stopOk = stop.Status == AudioPlaybackStatus.Stopped;

            bool ok = playOk && stopOk;
            if (ok)
            {
                passedSmokes++;
            }
            else
            {
                failedSmokes++;
            }

            SetResult(ok, $"BGM smoke play='{play.Status}' stop='{stop.Status}'.");
        }

        private IEnumerator RunAllSmokesRoutine()
        {
            ResetQaCounters();
            yield return null;

            RunDirectSfxSmoke();
            yield return new WaitForSecondsRealtime(0.1f);

            RunMissingClipSmoke();
            yield return null;

            RunMissingDefaultsSmoke();
            yield return null;

            RunPooledSfxSmoke();
            yield return new WaitForSecondsRealtime(0.1f);

            RunMissingPoolSmoke();
            yield return null;

            RunListenerSmoke();
            yield return null;

            yield return RunBgmSmokeRoutine();

            FrameworkBgmSyntheticSuite.SyntheticSuiteResult frameworkResult = null;
            yield return FrameworkBgmSyntheticSuite.Run(frameworkBgmPanel, result => frameworkResult = result);
            passedSmokes += frameworkResult.Passed;
            failedSmokes += frameworkResult.Failed;

            bool ok = failedSmokes == 0 && passedSmokes == 26;
            SetResult(ok, $"status='{(ok ? "Passed" : "Failed")}' core='7/7' frameworkBgm='{frameworkResult.FrameworkBgmPassed}/{frameworkResult.FrameworkBgmTotal}' adr013a='{frameworkResult.Adr013aPassed}/{frameworkResult.Adr013aTotal}' total='{passedSmokes}/{passedSmokes + failedSmokes}' failed='{failedSmokes}'.");
        }

        private void RunOperation(string operation, Func<bool> action)
        {
            try
            {
                bool ok = action != null && action();
                if (ok)
                {
                    passedSmokes++;
                }
                else
                {
                    failedSmokes++;
                }
            }
            catch (Exception exception)
            {
                failedSmokes++;
                SetResult(false, $"{operation} exception: {exception.GetType().Name}: {exception.Message}");
            }
        }

        private bool Expect(AudioPlaybackResult result, AudioPlaybackStatus expected, string label)
        {
            bool ok = result.Status == expected;
            SetResult(ok, $"{label} expected='{expected}' actual='{result.Status}' issues='{FormatIssues(result)}'.");
            return ok;
        }

        private void SetResult(bool passed, string message)
        {
            lastPassed = passed;
            lastResult = message ?? string.Empty;
            string prefix = passed ? "PASS" : "FAIL";
            Debug.Log($"[AUDIO_QA] {prefix}. {lastResult}", this);
        }

        private static string FormatIssues(AudioPlaybackResult result)
        {
            if (result.Issues == null || result.Issues.Count == 0)
            {
                return "<none>";
            }

            var builder = new StringBuilder();
            for (int i = 0; i < result.Issues.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append("; ");
                }

                AudioConfigurationIssue issue = result.Issues[i];
                builder.Append(issue.Code);
            }

            return builder.ToString();
        }

        private void OnGUI()
        {
            panelRect = ClampPanelRect(panelRect);
            panelRect = ClampPanelRect(GUI.Window(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this), panelRect, DrawWindow, "AUDIO QA"));
        }

        private void DrawWindow(int windowId)
        {
            scroll = GUILayout.BeginScrollView(
                scroll,
                true,
                true,
                GUILayout.Width(Mathf.Max(minimumPanelSize.x, panelRect.width - 10f)),
                GUILayout.Height(Mathf.Max(minimumPanelSize.y, panelRect.height - 48f)));

            GUILayout.BeginVertical(GUILayout.Width(Mathf.Max(scrollContentWidth, panelRect.width - 42f)));

            GUILayout.Space(8f);
            DrawStatusLine("Last Result", lastResult);
            DrawStatusLine("Last Passed", lastPassed.ToString());
            DrawStatusLine("Passed Smokes", passedSmokes.ToString());
            DrawStatusLine("Failed Smokes", failedSmokes.ToString());
            DrawStatusLine("SFX Requests", sfxPlayRequests.ToString());
            DrawStatusLine("BGM Play Requests", bgmPlayRequests.ToString());
            DrawStatusLine("BGM Stop Requests", bgmStopRequests.ToString());
            DrawStatusLine("Last SFX Handle", FormatHandle(lastSfxHandle));
            DrawStatusLine("Last BGM Handle", FormatHandle(lastBgmHandle));

            GUILayout.Space(8f);
            if (audioRuntimeHost != null)
            {
                AudioSettingsResolution settings = audioRuntimeHost.Settings;
                DrawStatusLine("Settings", $"{settings.Status} issues={settings.Issues.Count}");
            }
            else
            {
                DrawStatusLine("Settings", "AudioRuntimeHost missing");
            }

            if (listenerRuntimeHost != null)
            {
                AudioListenerHostReport report = listenerRuntimeHost.LastReport;
                DrawStatusLine("Listener", $"{report.Status} enabled={report.EnabledListeners} duplicates={report.DuplicateEnabledListeners} policy={report.DuplicatePolicy}");
            }

            GUILayout.Space(10f);
            if (GUILayout.Button("Compose Runtime Host"))
            {
                RunOperation(
                    "Compose Runtime Host",
                    () =>
                    {
                        AudioSettingsResolution settings = audioRuntimeHost != null
                            ? audioRuntimeHost.Compose()
                            : AudioSettingsResolution.Failed(new AudioConfigurationIssue("audio_qa_host_missing", "AudioRuntimeHost is missing.", nameof(audioRuntimeHost)));

                        SetResult(settings.IsResolved, $"Compose status='{settings.Status}' issues='{settings.Issues.Count}'.");
                        return settings.IsResolved;
                    });
            }

            if (GUILayout.Button("Play SFX"))
            {
                PlaySfx();
            }

            if (GUILayout.Button("Play BGM"))
            {
                PlayBgm();
            }

            if (GUILayout.Button("Stop BGM"))
            {
                StopBgm();
            }

            GUILayout.Space(10f);
            if (GUILayout.Button("Run Direct SFX Smoke"))
            {
                RunDirectSfxSmoke();
            }

            if (GUILayout.Button("Run Missing Clip Smoke"))
            {
                RunMissingClipSmoke();
            }

            if (GUILayout.Button("Run Missing Defaults Smoke"))
            {
                RunMissingDefaultsSmoke();
            }

            if (GUILayout.Button("Run Pooled SFX Smoke"))
            {
                RunPooledSfxSmoke();
            }

            if (GUILayout.Button("Run Missing Pool Smoke"))
            {
                RunMissingPoolSmoke();
            }

            if (GUILayout.Button("Run Listener Smoke"))
            {
                RunListenerSmoke();
            }

            if (GUILayout.Button("Run BGM Smoke"))
            {
                RunBgmSmoke();
            }

            if (GUILayout.Button("Run All Audio QA"))
            {
                RunAllSmokes();
            }

            GUILayout.Space(10f);
            GUILayout.Label("Framework BGM", GUI.skin.label);
            if (frameworkBgmPanel == null)
            {
                DrawStatusLine("Framework BGM", "Fixture missing. Run Configure Audio QA.");
            }
            else
            {
                if (GUILayout.Button("Request Startup Activity BGM")) frameworkBgmPanel.RequestStartupActivity();
                if (GUILayout.Button("Request Activity Own BGM")) frameworkBgmPanel.RequestOwnActivity();
                if (GUILayout.Button("Request Retain Previous Activity")) frameworkBgmPanel.RequestRetainPreviousActivity();
                if (GUILayout.Button("Request Route Fallback Activity")) frameworkBgmPanel.RequestRouteFallbackActivity();
                if (GUILayout.Button("Request Silence Activity")) frameworkBgmPanel.RequestSilenceActivity();
                if (GUILayout.Button("Clear Activity BGM")) frameworkBgmPanel.ClearActivity();
                if (GUILayout.Button("Request Route B Fixture")) frameworkBgmPanel.RequestOtherRoute();
            }

            GUILayout.Space(10f);
            if (GUILayout.Button("Reset QA Counters"))
            {
                ResetQaCounters();
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }

        private Rect ClampPanelRect(Rect rect)
        {
            float width = Mathf.Max(minimumPanelSize.x, rect.width);
            float height = Mathf.Max(minimumPanelSize.y, rect.height);
            float maxX = Mathf.Max(0f, Screen.width - width);
            float maxY = Mathf.Max(0f, Screen.height - height);
            float x = Mathf.Clamp(rect.x, 0f, maxX);
            float y = Mathf.Clamp(rect.y, 0f, maxY);

            return new Rect(x, y, width, height);
        }

        private static void DrawStatusLine(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(150f));
            GUILayout.Label(value ?? string.Empty);
            GUILayout.EndHorizontal();
        }

        private static string FormatHandle(IAudioPlaybackHandle handle)
        {
            return handle == null
                ? "<none>"
                : $"valid={handle.IsValid} playing={handle.IsPlaying}";
        }
    }
}
