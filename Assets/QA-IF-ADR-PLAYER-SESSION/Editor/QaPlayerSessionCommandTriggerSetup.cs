using System;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Immersive.QaFramework.PlayerSession.Editor
{
    internal static class QaPlayerSessionCommandTriggerSetup
    {
        internal static PlayerSessionOpenJoiningCommandTrigger EnsureOpenJoining(
            GameObject owner,
            LocalPlayerProvisioningConsumerScope scope) =>
            Ensure<PlayerSessionOpenJoiningCommandTrigger>(owner, "Open Joining", scope);

        internal static PlayerSessionCloseJoiningCommandTrigger EnsureCloseJoining(
            GameObject owner,
            LocalPlayerProvisioningConsumerScope scope) =>
            Ensure<PlayerSessionCloseJoiningCommandTrigger>(owner, "Close Joining", scope);

        internal static PlayerSessionLeaveCommandTrigger EnsureLeave(
            GameObject owner,
            string childName,
            LocalPlayerProvisioningConsumerScope scope,
            PlayerSlotProfile slot,
            int expectedOccurrenceRevision = -1)
        {
            PlayerSessionLeaveCommandTrigger trigger = EnsureChild<PlayerSessionLeaveCommandTrigger>(
                owner,
                childName);
            SerializedObject serialized = new SerializedObject(trigger);
            serialized.Update();
            Require(serialized, "scope").intValue = (int)scope;
            Require(serialized, "playerSlot").objectReferenceValue = slot;
            Require(serialized, "expectedLeaveOccurrenceRevision").intValue =
                expectedOccurrenceRevision;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            MarkDirty(trigger);
            return trigger;
        }

        internal static PlayerGameplayAvailabilityBlockTrigger EnsureAvailabilityBlock(
            GameObject owner,
            string childName,
            LocalPlayerProvisioningConsumerScope scope,
            PlayerSlotProfile slot)
        {
            PlayerGameplayAvailabilityBlockTrigger trigger = EnsureChild<PlayerGameplayAvailabilityBlockTrigger>(
                owner,
                childName);
            SerializedObject serialized = new SerializedObject(trigger);
            serialized.Update();
            Require(serialized, "scope").intValue = (int)scope;
            Require(serialized, "playerSlot").objectReferenceValue = slot;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            MarkDirty(trigger);
            return trigger;
        }

        private static T Ensure<T>(
            GameObject owner,
            string childName,
            LocalPlayerProvisioningConsumerScope scope)
            where T : PlayerSessionCommandTriggerBase
        {
            T trigger = EnsureChild<T>(owner, childName);
            SerializedObject serialized = new SerializedObject(trigger);
            serialized.Update();
            Require(serialized, "scope").intValue = (int)scope;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            MarkDirty(trigger);
            return trigger;
        }

        private static T EnsureChild<T>(GameObject owner, string childName)
            where T : Component
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            Transform child = owner.transform.Find(childName);
            GameObject childObject = child != null
                ? child.gameObject
                : new GameObject(childName);
            if (child == null)
                childObject.transform.SetParent(owner.transform, false);

            T component = childObject.GetComponent<T>();
            if (component == null)
                component = childObject.AddComponent<T>();
            return component;
        }

        private static SerializedProperty Require(
            SerializedObject serialized,
            string propertyName)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException(
                    $"Serialized Player Session command field '{propertyName}' was not found on '{serialized.targetObject.name}'.");
            return property;
        }

        private static void MarkDirty(Component component)
        {
            EditorUtility.SetDirty(component);
            if (component.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
        }
    }
}
