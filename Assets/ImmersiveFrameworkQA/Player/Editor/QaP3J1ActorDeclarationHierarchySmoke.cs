using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.Player.Editor
{
    public static class QaP3J1ActorDeclarationHierarchySmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3J.1 Run Actor Declaration Hierarchy Smoke";

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var completed = new List<string>();
            GameObject genericHost = null;
            GameObject playerHost = null;

            try
            {
                AssertTrue(typeof(ActorDeclaration).IsAssignableFrom(typeof(PlayerActorDeclaration)),
                    "PlayerActorDeclaration does not inherit ActorDeclaration.");
                completed.Add("player-declaration-inherits-actor-declaration");

                AssertTrue(!typeof(ActorDeclaration).IsSealed,
                    "ActorDeclaration must be extensible.");
                AssertTrue(typeof(PlayerActorDeclaration).IsSealed,
                    "PlayerActorDeclaration should remain final.");
                completed.Add("hierarchy-sealing-policy");

                genericHost = new GameObject("P3J1 Generic Actor");
                ActorDeclaration generic = genericHost.AddComponent<ActorDeclaration>();
                ConfigureGeneric(generic);
                AssertEqual(ActorKind.NonPlayer, generic.ActorKind,
                    "Generic Actor Kind changed.");
                AssertEqual(ActorRole.Neutral, generic.ActorRole,
                    "Generic Actor Role changed.");
                completed.Add("generic-classification-authored");

                AssertTrue(generic.TryCreateDescriptor(
                        nameof(QaP3J1ActorDeclarationHierarchySmoke),
                        out ActorDescriptor genericDescriptor,
                        out ActorSetIssue genericIssue),
                    "Generic descriptor failed. " + genericIssue.Message);
                AssertEqual(new ActorId("qa.p3j1.actor.generic"), genericDescriptor.ActorId,
                    "Generic descriptor lost ActorId.");
                completed.Add("generic-descriptor-preserved");

                playerHost = new GameObject("P3J1 Contextual Player Actor");
                PlayerActorDeclaration player =
                    playerHost.AddComponent<PlayerActorDeclaration>();
                AssertTrue(playerHost.GetComponent<PlayerInput>() == null,
                    "PlayerActorDeclaration still adds a same-object PlayerInput.");
                ConfigurePlayer(player);
                AssertTrue(!player.HasPlayerInputEvidence,
                    "Authored Logical Actor unexpectedly has PlayerInput evidence.");
                completed.Add("playerinput-no-longer-required-on-actor");

                AssertTrue(player is ActorDeclaration,
                    "Player declaration cannot be consumed through base type.");
                AssertEqual(1, playerHost.GetComponents<ActorDeclaration>().Length,
                    "Player host exposes more than one ActorDeclaration authority.");
                completed.Add("single-actor-identity-component");

                AssertEqual(ActorKind.Player, player.ActorKind,
                    "Player kind is not fixed.");
                AssertEqual(ActorRole.Protagonist, player.ActorRole,
                    "Player role is not fixed.");
                completed.Add("player-classification-fixed");

                ActorDeclaration baseView = player;
                AssertEqual(new ActorId("qa.p3j1.actor.player"), baseView.ActorId,
                    "Upcast Player Actor lost inherited ActorId.");
                AssertEqual("P3J1 Player", baseView.ActorDisplayName,
                    "Upcast Player Actor lost display name.");
                completed.Add("inherited-identity-preserved");

                AssertTrue(baseView.TryCreateDescriptor(
                        nameof(QaP3J1ActorDeclarationHierarchySmoke),
                        out ActorDescriptor baseDescriptor,
                        out ActorSetIssue baseIssue),
                    "Base descriptor for Player failed. " + baseIssue.Message);
                AssertEqual(ActorKind.Player, baseDescriptor.ActorKind,
                    "Base descriptor ignored specialization.");
                AssertEqual(ActorRole.Protagonist, baseDescriptor.ActorRole,
                    "Base descriptor ignored specialized role.");
                completed.Add("base-descriptor-uses-specialized-classification");

                AssertTrue(player.TryCreateDescriptor(
                        nameof(QaP3J1ActorDeclarationHierarchySmoke),
                        out PlayerActorDescriptor playerDescriptor,
                        out PlayerActorSetIssue playerIssue),
                    "Specialized descriptor failed. " + playerIssue.Message);
                AssertEqual(new ActorId("qa.p3j1.actor.player"), playerDescriptor.ActorId,
                    "Player descriptor lost inherited ActorId.");
                AssertTrue(!playerDescriptor.HasPlayerInputEvidence,
                    "Contextual Player descriptor inferred PlayerInput.");
                completed.Add("specialized-player-descriptor-preserved");

                SerializedObject serializedPlayer = new SerializedObject(player);
                AssertNotNull(serializedPlayer.FindProperty("actorId"),
                    "Inherited actorId is not serialized.");
                AssertNotNull(serializedPlayer.FindProperty("displayName"),
                    "Inherited displayName is not serialized.");
                AssertNotNull(serializedPlayer.FindProperty("reason"),
                    "Inherited reason is not serialized.");
                AssertNotNull(serializedPlayer.FindProperty("playerInput"),
                    "Optional runtime PlayerInput evidence is not serialized.");
                completed.Add("serialized-inheritance-shape");

                int actorInterfaceCount = 0;
                foreach (MonoBehaviour behaviour in playerHost.GetComponents<MonoBehaviour>())
                {
                    if (behaviour is IActor)
                    {
                        actorInterfaceCount++;
                    }
                }
                AssertEqual(1, actorInterfaceCount,
                    "Player host exposes duplicated IActor authorities.");
                completed.Add("single-iactor-authority");

                AssertEqual(1,
                    playerHost.GetComponentsInChildren<ActorDeclaration>(true).Length,
                    "Base declaration discovery duplicated Player declaration.");
                AssertEqual(1,
                    playerHost.GetComponentsInChildren<PlayerActorDeclaration>(true).Length,
                    "Specialized declaration discovery is incorrect.");
                completed.Add("profile-discovery-compatible");

                string before = EditorJsonUtility.ToJson(player);
                baseView.TryCreateDescriptor("QA.P3J1.ReadOnly", out _, out _);
                player.TryCreateDescriptor(
                    "QA.P3J1.ReadOnly",
                    out PlayerActorDescriptor _,
                    out PlayerActorSetIssue _);
                AssertEqual(before, EditorJsonUtility.ToJson(player),
                    "Descriptor creation mutated authoring state.");
                completed.Add("descriptor-creation-non-mutating");

                Debug.Log(
                    "[P3J1_ACTOR_DECLARATION_HIERARCHY_SMOKE] status='Passed' " +
                    $"cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3J1_ACTOR_DECLARATION_HIERARCHY_SMOKE] status='Failed' " +
                    $"exception='{exception.GetType().Name}' message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
            }
            finally
            {
                if (playerHost != null)
                {
                    UnityEngine.Object.DestroyImmediate(playerHost);
                }
                if (genericHost != null)
                {
                    UnityEngine.Object.DestroyImmediate(genericHost);
                }
            }
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => !EditorApplication.isPlayingOrWillChangePlaymode;

        private static void ConfigureGeneric(ActorDeclaration declaration)
        {
            var serialized = new SerializedObject(declaration);
            serialized.FindProperty("actorId").stringValue = "qa.p3j1.actor.generic";
            serialized.FindProperty("actorKind").intValue = (int)ActorKind.NonPlayer;
            serialized.FindProperty("actorRole").intValue = (int)ActorRole.Neutral;
            serialized.FindProperty("displayName").stringValue = "P3J1 Generic";
            serialized.FindProperty("reason").stringValue = "qa.p3j1.generic";
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigurePlayer(PlayerActorDeclaration declaration)
        {
            var serialized = new SerializedObject(declaration);
            serialized.FindProperty("actorId").stringValue = "qa.p3j1.actor.player";
            serialized.FindProperty("displayName").stringValue = "P3J1 Player";
            serialized.FindProperty("reason").stringValue = "qa.p3j1.player";
            serialized.FindProperty("playerInput").objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertNotNull(object value, string message)
        {
            if (value == null)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    $"{message} expected='{expected}' actual='{actual}'.");
            }
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
        }
    }
}
