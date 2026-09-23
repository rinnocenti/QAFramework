using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ActivityFlow;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    internal sealed class QaParticipantAwareReadinessParticipants : IAsyncDisposable
    {
        private const string RootName = "QA_READY_PROGRESS_01_Participants";
        private const string IdPrefix = "qa.ready-progress-01";

        private readonly GameObject _root;
        private readonly Scene _ownerScene;
        private readonly List<ActivityReadinessParticipant> _all =
            new List<ActivityReadinessParticipant>();
        private readonly List<ActivityReadinessParticipant> _required =
            new List<ActivityReadinessParticipant>();
        private readonly List<ListenerRegistration> _listeners =
            new List<ListenerRegistration>();
        private readonly TaskCompletionSource<bool> _allPreparing =
            new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
        private int _preparingCount;
        private int _releasedCount;
        private bool _disposed;

        private QaParticipantAwareReadinessParticipants(
            QaActivityEntryReadinessFixture fixture,
            GameObject root,
            Scene ownerScene)
        {
            Fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
            this._root = root ?? throw new ArgumentNullException(nameof(root));
            this._ownerScene = ownerScene;

            _required.Add(fixture.Participant);
            _all.Add(fixture.Participant);

            for (int index = 1; index < 4; index++)
            {
                ActivityReadinessParticipant participant = CreateParticipant(
                    root.transform,
                    $"Required {index + 1}",
                    $"{IdPrefix}.required.{index + 1}",
                    ActivityContentExecutionRequiredness.Required,
                    1000 + (index * 10));
                _required.Add(participant);
                _all.Add(participant);
            }

            Optional = CreateParticipant(
                root.transform,
                "Optional 1",
                $"{IdPrefix}.optional.1",
                ActivityContentExecutionRequiredness.Optional,
                1100);
            _all.Add(Optional);

            for (int index = 0; index < _all.Count; index++)
            {
                Register(_all[index]);
            }
        }

        internal QaActivityEntryReadinessFixture Fixture { get; }
        internal IReadOnlyList<ActivityReadinessParticipant> Required => _required;
        internal ActivityReadinessParticipant Optional { get; }
        internal IReadOnlyList<ActivityReadinessParticipant> All => _all;
        internal Task AllPreparing => _allPreparing.Task;
        internal int PreparingCount => _preparingCount;
        internal int ReleasedCount => _releasedCount;

        internal static QaParticipantAwareReadinessParticipants Create(
            QaActivityEntryReadinessFixture fixture)
        {
            Require(fixture != null,
                "Participant-aware readiness fixture is required.");
            Require(fixture.InitialRoute != null && fixture.InitialRoute.HasPrimaryScene,
                "Participant-aware readiness requires the fixture Route primary scene.");

            Scene scene = SceneManager.GetSceneByPath(
                fixture.InitialRoute.PrimaryScenePath);
            Require(scene.IsValid() && scene.isLoaded,
                "Participant-aware readiness owner scene is not loaded.");
            RequireNoRoot(scene);

            var root = new GameObject(RootName);
            try
            {
                SceneManager.MoveGameObjectToScene(root, scene);
                return new QaParticipantAwareReadinessParticipants(
                    fixture,
                    root,
                    scene);
            }
            catch
            {
                UnityEngine.Object.Destroy(root);
                throw;
            }
        }

        internal void RequireAllPreparing()
        {
            Require(_preparingCount == _all.Count,
                $"Expected all participant preparations. expected='{_all.Count}' actual='{_preparingCount}'.");
            int occurrence = 0;
            for (int index = 0; index < _all.Count; index++)
            {
                ActivityReadinessParticipant participant = _all[index];
                Require(participant != null &&
                    participant.State == ActivityReadinessParticipantState.Preparing &&
                    participant.Occurrence > 0,
                    $"Participant '{index}' is not Preparing.");
                occurrence = occurrence == 0
                    ? participant.Occurrence
                    : occurrence;
                Require(participant.Occurrence == occurrence,
                    "Participant occurrences diverged.");
            }
        }

        internal void FailOptional(string reason)
        {
            Require(Optional != null &&
                Optional.State == ActivityReadinessParticipantState.Preparing,
                "Optional participant must be Preparing before failure.");
            Optional.FailPreparation(reason);
            Require(Optional.State == ActivityReadinessParticipantState.Failed,
                "Optional participant did not enter Failed.");
        }

        internal void CompleteRequired(int index)
        {
            Require(index >= 0 && index < _required.Count,
                $"Required participant index '{index}' is outside the fixture range.");
            ActivityReadinessParticipant participant = _required[index];
            Require(participant.State == ActivityReadinessParticipantState.Preparing,
                $"Required participant '{index}' must be Preparing before completion.");
            participant.CompletePreparation();
            Require(participant.State == ActivityReadinessParticipantState.Completed,
                $"Required participant '{index}' did not enter Completed.");
        }

        internal Task CompleteAllPendingForUnwindAsync()
        {
            for (int index = 0; index < _all.Count; index++)
            {
                ActivityReadinessParticipant participant = _all[index];
                if (participant != null &&
                    participant.State == ActivityReadinessParticipantState.Preparing)
                {
                    participant.CompletePreparation();
                }
            }

            return Task.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            bool neverEntered = _preparingCount == 0 && _releasedCount == 0;
            bool enteredAndReleased = _preparingCount == _all.Count &&
                _releasedCount == _all.Count;
            Require(neverEntered || enteredAndReleased,
                "Participant-aware cleanup requires either no entry or a full " +
                $"release. preparing='{_preparingCount}' " +
                $"released='{_releasedCount}' expected='{_all.Count}'.");
            if (enteredAndReleased)
            {
                for (int index = 0; index < _all.Count; index++)
                {
                    Require(_all[index] == null ||
                        _all[index].State ==
                        ActivityReadinessParticipantState.Released,
                        $"Participant '{index}' was not Released before cleanup.");
                }
            }

            for (int index = 0; index < _listeners.Count; index++)
            {
                _listeners[index].Remove();
            }
            _listeners.Clear();

            UnityEngine.Object.Destroy(_root);
            await Awaitable.NextFrameAsync();
            RequireNoRoot(_ownerScene);
            _disposed = true;
        }

        private void Register(ActivityReadinessParticipant participant)
        {
            UnityAction started = () =>
            {
                _preparingCount++;
                if (_preparingCount == _all.Count)
                {
                    _allPreparing.TrySetResult(true);
                }
            };
            UnityAction released = () => _releasedCount++;
            participant.PreparationStarted.AddListener(started);
            participant.PreparationReleased.AddListener(released);
            _listeners.Add(new ListenerRegistration(
                participant,
                started,
                released));
        }

        private static ActivityReadinessParticipant CreateParticipant(
            Transform parent,
            string label,
            string participantId,
            ActivityContentExecutionRequiredness requiredness,
            int order)
        {
            var child = new GameObject(label);
            child.transform.SetParent(parent, false);
            ActivityReadinessParticipant participant =
                child.AddComponent<ActivityReadinessParticipant>();
            var serialized = new SerializedObject(participant);
            RequireProperty(serialized, "participantId").stringValue = participantId;
            SetEnumName(
                RequireProperty(serialized, "requiredness"),
                requiredness.ToString());
            RequireProperty(serialized, "order").intValue = order;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Require(string.Equals(
                    participant.ParticipantId,
                    participantId,
                    StringComparison.Ordinal) &&
                participant.Requiredness == requiredness,
                $"Participant '{participantId}' configuration was not applied.");
            return participant;
        }

        private static SerializedProperty RequireProperty(
            SerializedObject serialized,
            string name)
        {
            SerializedProperty property = serialized.FindProperty(name);
            Require(property != null,
                $"Required serialized property '{name}' was not found.");
            return property;
        }

        private static void SetEnumName(
            SerializedProperty property,
            string value)
        {
            string[] names = property.enumNames;
            for (int index = 0; index < names.Length; index++)
            {
                if (string.Equals(names[index], value, StringComparison.Ordinal))
                {
                    property.enumValueIndex = index;
                    return;
                }
            }

            throw new InvalidOperationException(
                $"Serialized enum value '{value}' is not available.");
        }

        private static void RequireNoRoot(Scene scene)
        {
            Require(scene.IsValid() && scene.isLoaded,
                "Participant-aware owner scene is unavailable.");
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                Require(roots[index] == null ||
                    !string.Equals(
                        roots[index].name,
                        RootName,
                        StringComparison.Ordinal),
                    $"Temporary participant-aware root '{RootName}' already exists.");
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private readonly struct ListenerRegistration
        {
            internal ListenerRegistration(
                ActivityReadinessParticipant participant,
                UnityAction started,
                UnityAction released)
            {
                Participant = participant;
                Started = started;
                Released = released;
            }

            private ActivityReadinessParticipant Participant { get; }
            private UnityAction Started { get; }
            private UnityAction Released { get; }

            internal void Remove()
            {
                if (Participant == null)
                {
                    return;
                }

                Participant.PreparationStarted.RemoveListener(Started);
                Participant.PreparationReleased.RemoveListener(Released);
            }
        }
    }
}
