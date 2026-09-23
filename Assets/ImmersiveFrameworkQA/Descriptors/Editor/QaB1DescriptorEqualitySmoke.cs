using System;
using System.Collections.Generic;
using System.Linq;
using Immersive.Framework.Actors;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Descriptors.Editor
{
    /// <summary>
    /// Editor-only regression for descriptor functional equality.
    /// It creates no assets, scenes or runtime objects.
    /// </summary>
    internal static class QaB1DescriptorEqualitySmoke
    {
        [MenuItem("Immersive Framework/QA/Regressions/Contracts/Run Descriptor Equality Regression")]
        private static void Run()
        {
            var completed = new List<string>();

            try
            {
                VerifyActorDescriptor(completed);
                VerifyPlayerActorDescriptor(completed);

                Debug.Log(
                    "[B1_DESCRIPTOR_EQUALITY_SMOKE] status='Passed' " +
                    $"cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[B1_DESCRIPTOR_EQUALITY_SMOKE] status='Failed' " +
                    $"exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
            }
        }

        private static void VerifyActorDescriptor(ICollection<string> completed)
        {
            ActorDescriptor baseline = CreateActor("Actor One", "Scene A", "Object A", "QA.B1.A", "baseline");
            ActorDescriptor cosmeticVariant = CreateActor("Renamed Actor", "Scene B", "Object B", "QA.B1.B", "reparented");
            AssertEqualContract(baseline, cosmeticVariant, "Actor descriptor cosmetics changed equality.");
            AssertCollectionContract(baseline, cosmeticVariant, "ActorDescriptor");
            AssertNotEqual(baseline, CreateActorWithId("qa.b1.actor.other"), "Actor id changed equality.");
            AssertNotEqual(baseline, new ActorDescriptor(ActorId.From("qa.b1.actor"), ActorKind.NonPlayer, ActorRole.Protagonist, "Actor One", "Scene A", "Object A", "QA.B1.A", "baseline"), "Actor kind changed equality.");
            AssertNotEqual(baseline, new ActorDescriptor(ActorId.From("qa.b1.actor"), ActorKind.Player, ActorRole.Ally, "Actor One", "Scene A", "Object A", "QA.B1.A", "baseline"), "Actor role changed equality.");
            completed.Add("actor-descriptor");
        }

        private static void VerifyPlayerActorDescriptor(ICollection<string> completed)
        {
            PlayerActorDescriptor baseline = CreatePlayerActor("Player One", "Scene A", "Object A", "QA.B1.A", "baseline", true);
            PlayerActorDescriptor cosmeticVariant = CreatePlayerActor("Renamed Player", "Scene B", "Object B", "QA.B1.B", "reparented", true);
            AssertEqualContract(baseline, cosmeticVariant, "PlayerActor descriptor cosmetics changed equality.");
            AssertCollectionContract(baseline, cosmeticVariant, "PlayerActorDescriptor");
            AssertNotEqual(baseline, CreatePlayerActor("Player One", "Scene A", "Object A", "QA.B1.A", "baseline", false), "Player input evidence changed equality.");
            AssertNotEqual(baseline, new PlayerActorDescriptor(ActorId.From("qa.b1.player.other"), ActorRole.Protagonist, true, "Player One", "Scene A", "Object A", "QA.B1.A", "baseline"), "Player ActorId changed equality.");
            completed.Add("player-actor-descriptor");
        }

        private static ActorDescriptor CreateActor(string displayName, string sceneName, string objectName, string source, string reason)
        {
            return new ActorDescriptor(ActorId.From("qa.b1.actor"), ActorKind.Player, ActorRole.Protagonist, displayName, sceneName, objectName, source, reason);
        }

        private static ActorDescriptor CreateActorWithId(string actorId)
        {
            return new ActorDescriptor(ActorId.From(actorId), ActorKind.Player, ActorRole.Protagonist, "Actor", "Scene", "Object", "QA.B1", "id-variant");
        }

        private static PlayerActorDescriptor CreatePlayerActor(string displayName, string sceneName, string objectName, string source, string reason, bool hasPlayerInputEvidence)
        {
            return new PlayerActorDescriptor(ActorId.From("qa.b1.player"), ActorRole.Protagonist, hasPlayerInputEvidence, displayName, sceneName, objectName, source, reason);
        }

        private static void AssertEqualContract<T>(T left, T right, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(left, right))
            {
                throw new InvalidOperationException(message);
            }

            if (left.GetHashCode() != right.GetHashCode())
            {
                throw new InvalidOperationException(message + " Equal values returned different hash codes.");
            }
        }

        private static void AssertNotEqual<T>(T left, T right, string message)
        {
            if (EqualityComparer<T>.Default.Equals(left, right))
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertCollectionContract<T>(T left, T right, string descriptorName)
        {
            var set = new HashSet<T> { left, right };
            if (set.Count != 1)
            {
                throw new InvalidOperationException($"{descriptorName} HashSet retained a cosmetic duplicate.");
            }

            var dictionary = new Dictionary<T, string> { [left] = "first" };
            dictionary[right] = "second";
            if (dictionary.Count != 1 || dictionary[left] != "second")
            {
                throw new InvalidOperationException($"{descriptorName} Dictionary did not coalesce cosmetic variants.");
            }

            if (new[] { left, right }.Distinct().Count() != 1)
            {
                throw new InvalidOperationException($"{descriptorName} Distinct retained a cosmetic duplicate.");
            }
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
        }
    }
}
