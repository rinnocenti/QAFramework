using System.Collections;
using System.Collections.Generic;
using Immersive.Framework.Pause;
using Immersive.Framework.UnityInput;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.Player
{
    /// <summary>
    /// P2E runtime proof for the canonical framework Gate -> Unity PlayerInput adapter path.
    /// The fixture requests Pause and Resume through the public PauseRequestTrigger,
    /// then observes adapter-owned action-map blocking and restoration.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Player/P2E PlayerInput Gate Runtime Fixture")]
    public sealed class QaP2EPlayerInputGateRuntimeFixture : MonoBehaviour
    {
        private const string Prefix = "[P2E_PLAYER_INPUT_GATE_RUNTIME]";
        private const int DefaultTimeoutFrames = 120;
        private const int EntryTransitionTimeoutFrames = 900;

        [Header("Canonical Runtime Components")]
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private UnityPlayerInputGateAdapter gateAdapter;
        [SerializeField] private PauseRequestTrigger pauseRequestTrigger;

        [Header("Expected Authoring")]
        [SerializeField] private string gameplayActionMapName = "Player";

        [Header("Execution")]
        [SerializeField] private bool runOnStart = true;
        [SerializeField, Min(1)] private int timeoutFrames = DefaultTimeoutFrames;
        [SerializeField] private bool throwOnFailure;

        private IEnumerator Start()
        {
            if (!runOnStart)
            {
                yield break;
            }

            yield return null;
            yield return null;

            yield return RunSmoke();
        }

        [ContextMenu("Run P2E PlayerInput Gate Runtime Smoke")]
        public void RunFromContextMenu()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError(
                    $"{Prefix} status='Failed' case='RequiresPlayMode' " +
                    "message='Run this fixture through the QA Hub in Play Mode.'",
                    this);
                return;
            }

            StartCoroutine(RunSmoke());
        }

        private IEnumerator RunSmoke()
        {
            var cases = new List<CaseResult>();
            InputActionMap gameplayMap = ResolveGameplayMap();

            cases.Add(Case(
                "RuntimeComponentsPresent",
                playerInput != null
                && gateAdapter != null
                && pauseRequestTrigger != null,
                $"playerInput='{playerInput != null}' " +
                $"gateAdapter='{gateAdapter != null}' " +
                $"pauseTrigger='{pauseRequestTrigger != null}'"));

            cases.Add(Case(
                "GameplayActionMapResolved",
                gameplayMap != null,
                $"map='{gameplayActionMapName}' resolved='{gameplayMap != null}'"));

            if (playerInput == null
                || gateAdapter == null
                || pauseRequestTrigger == null
                || gameplayMap == null)
            {
                Complete(cases);
                yield break;
            }

            bool entryTransitionReleased = false;
            yield return WaitUntil(
                () =>
                {
                    gateAdapter.ApplyCurrentGate();
                    entryTransitionReleased =
                        !gateAdapter.IsBlockedByAdapter
                        && gameplayMap.enabled;
                    return entryTransitionReleased;
                },
                EntryTransitionTimeoutFrames);

            cases.Add(Case(
                "EntryTransitionGateReleased",
                entryTransitionReleased,
                $"released='{entryTransitionReleased}' " +
                $"blockedByAdapter='{gateAdapter.IsBlockedByAdapter}' " +
                $"status='{gateAdapter.LastStatus}' " +
                $"mapEnabled='{gameplayMap.enabled}'"));

            if (!entryTransitionReleased)
            {
                Complete(cases);
                yield break;
            }

            if (pauseRequestTrigger.IsPaused)
            {
                pauseRequestTrigger.RequestResume();
                yield return WaitUntil(
                    () => !pauseRequestTrigger.IsPaused
                        && !gateAdapter.IsBlockedByAdapter
                        && gameplayMap.enabled,
                    timeoutFrames);
            }

            gateAdapter.ApplyCurrentGate();
            yield return null;

            cases.Add(Case(
                "InitialPauseStateAllowed",
                !pauseRequestTrigger.IsPaused,
                $"isPaused='{pauseRequestTrigger.IsPaused}'"));

            cases.Add(Case(
                "InitialAdapterStateAllowed",
                !gateAdapter.IsBlockedByAdapter,
                $"blockedByAdapter='{gateAdapter.IsBlockedByAdapter}' " +
                $"status='{gateAdapter.LastStatus}'"));

            cases.Add(Case(
                "InitialGameplayMapEnabled",
                gameplayMap.enabled,
                $"map='{gameplayMap.name}' enabled='{gameplayMap.enabled}'"));

            pauseRequestTrigger.RequestPause();

            bool pauseObserved = false;
            yield return WaitUntil(
                () =>
                {
                    pauseObserved =
                        pauseRequestTrigger.IsPaused
                        && gateAdapter.IsBlockedByAdapter
                        && !gameplayMap.enabled;
                    return pauseObserved;
                },
                timeoutFrames);

            cases.Add(Case(
                "PauseRequestSucceeded",
                pauseRequestTrigger.LastRequestSucceeded
                && pauseRequestTrigger.IsPaused,
                $"succeeded='{pauseRequestTrigger.LastRequestSucceeded}' " +
                $"status='{pauseRequestTrigger.LastStatus}' " +
                $"previous='{pauseRequestTrigger.LastPreviousState}' " +
                $"current='{pauseRequestTrigger.LastCurrentState}'"));

            cases.Add(Case(
                "GateAdapterBlockedDuringPause",
                pauseObserved
                && gateAdapter.IsBlockedByAdapter,
                $"observed='{pauseObserved}' " +
                $"blockedByAdapter='{gateAdapter.IsBlockedByAdapter}' " +
                $"status='{gateAdapter.LastStatus}' " +
                $"reason='{gateAdapter.LastReason}'"));

            cases.Add(Case(
                "GameplayMapDisabledDuringPause",
                pauseObserved
                && !gameplayMap.enabled,
                $"map='{gameplayMap.name}' enabled='{gameplayMap.enabled}'"));

            cases.Add(Case(
                "PlayerInputComponentRemainsEnabled",
                playerInput.enabled,
                $"enabled='{playerInput.enabled}' " +
                $"blockMode='{gateAdapter.BlockMode}'"));

            pauseRequestTrigger.RequestResume();

            bool resumeObserved = false;
            yield return WaitUntil(
                () =>
                {
                    resumeObserved =
                        !pauseRequestTrigger.IsPaused
                        && !gateAdapter.IsBlockedByAdapter
                        && gameplayMap.enabled;
                    return resumeObserved;
                },
                timeoutFrames);

            cases.Add(Case(
                "ResumeRequestSucceeded",
                pauseRequestTrigger.LastRequestSucceeded
                && !pauseRequestTrigger.IsPaused,
                $"succeeded='{pauseRequestTrigger.LastRequestSucceeded}' " +
                $"status='{pauseRequestTrigger.LastStatus}' " +
                $"previous='{pauseRequestTrigger.LastPreviousState}' " +
                $"current='{pauseRequestTrigger.LastCurrentState}'"));

            cases.Add(Case(
                "GateAdapterReleasedAfterResume",
                resumeObserved
                && !gateAdapter.IsBlockedByAdapter,
                $"observed='{resumeObserved}' " +
                $"blockedByAdapter='{gateAdapter.IsBlockedByAdapter}' " +
                $"status='{gateAdapter.LastStatus}' " +
                $"reason='{gateAdapter.LastReason}'"));

            cases.Add(Case(
                "GameplayMapRestoredAfterResume",
                resumeObserved
                && gameplayMap.enabled,
                $"map='{gameplayMap.name}' enabled='{gameplayMap.enabled}'"));

            int playerInputCount =
                CountPlayerInputsInScene();
            cases.Add(Case(
                "ExactlyOnePlayerInputInQaScene",
                playerInputCount == 1,
                $"count='{playerInputCount}'"));

            bool succeeded = Complete(cases);
            if (!succeeded && throwOnFailure)
            {
                throw new System.InvalidOperationException(
                    "P2E PlayerInput Gate runtime smoke failed. Inspect the preceding QA evidence.");
            }
        }

        private InputActionMap ResolveGameplayMap()
        {
            if (playerInput == null || playerInput.actions == null)
            {
                return null;
            }

            return playerInput.actions.FindActionMap(
                gameplayActionMapName,
                false);
        }

        private IEnumerator WaitUntil(
            System.Func<bool> predicate,
            int maximumFrames)
        {
            int safeMaximum =
                Mathf.Max(1, maximumFrames);

            for (int frame = 0; frame < safeMaximum; frame++)
            {
                if (predicate())
                {
                    yield break;
                }

                yield return null;
            }
        }

        private int CountPlayerInputsInScene()
        {
            PlayerInput[] inputs =
                Object.FindObjectsByType<PlayerInput>(
                    FindObjectsInactive.Include);

            int count = 0;
            for (int i = 0; i < inputs.Length; i++)
            {
                PlayerInput input = inputs[i];
                if (input != null
                    && input.gameObject.scene == gameObject.scene)
                {
                    count++;
                }
            }

            return count;
        }

        private static CaseResult Case(
            string name,
            bool passed,
            string detail)
        {
            return new CaseResult(
                name,
                passed,
                detail);
        }

        private bool Complete(
            IReadOnlyList<CaseResult> cases)
        {
            int passed = 0;
            int failed = 0;

            for (int i = 0; i < cases.Count; i++)
            {
                CaseResult result = cases[i];
                if (result.Passed)
                {
                    passed++;
                    Debug.Log(
                        $"{Prefix} case='{result.Name}' status='Passed' detail='{result.Detail}'",
                        this);
                }
                else
                {
                    failed++;
                    Debug.LogError(
                        $"{Prefix} case='{result.Name}' status='Failed' detail='{result.Detail}'",
                        this);
                }
            }

            string status =
                failed == 0
                    ? "Succeeded"
                    : "Failed";
            string summary =
                $"{Prefix} status='{status}' passed='{passed}' failed='{failed}' cases='{cases.Count}'.";

            if (failed == 0)
            {
                Debug.Log(summary, this);
                return true;
            }

            Debug.LogError(summary, this);
            return false;
        }

        private readonly struct CaseResult
        {
            public CaseResult(
                string name,
                bool passed,
                string detail)
            {
                Name = name ?? string.Empty;
                Passed = passed;
                Detail = detail ?? string.Empty;
            }

            public string Name { get; }

            public bool Passed { get; }

            public string Detail { get; }
        }
    }
}
