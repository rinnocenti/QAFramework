using System;
using Immersive.Framework.Authoring;
using Immersive.Framework.RuntimeContent;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Descriptors.Editor
{
    internal static class QaA1ActivityIdSmoke
    {
        [MenuItem("Immersive Framework QA/Contracts/A1 Run Activity ID Smoke")]
        private static void Run()
        {
            ActivityAsset activityA = ScriptableObject.CreateInstance<ActivityAsset>();
            ActivityAsset activityB = ScriptableObject.CreateInstance<ActivityAsset>();

            try
            {
                Configure(activityA, "qa.a1.activity.a", "Original Name");
                Configure(activityB, "qa.a1.activity.b", "Original Name");

                RuntimeContentOwner ownerBeforeRename = RuntimeContentOwner.Activity(activityA.ActivityId.StableText, activityA.ActivityName);
                Configure(activityA, "qa.a1.activity.a", "Renamed Activity");
                RuntimeContentOwner ownerAfterRename = RuntimeContentOwner.Activity(activityA.ActivityId.StableText, activityA.ActivityName);

                Assert(ownerBeforeRename == ownerAfterRename, "Activity rename changed RuntimeContent owner identity.");
                Assert(activityA.ActivityId != activityB.ActivityId, "Distinct Activity IDs compared equal.");
                Assert(ownerAfterRename != RuntimeContentOwner.Activity(activityB.ActivityId.StableText, activityB.ActivityName), "Distinct Activity IDs produced the same owner.");

                Configure(activityB, "   ", "Whitespace Id");
                Assert(!activityB.HasValidActivityId, "Whitespace Activity ID was accepted.");

                Debug.Log("[A1_ACTIVITY_ID_SMOKE] status='Passed' cases='rename-stable,distinct-id-distinct-owner,whitespace-invalid'.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[A1_ACTIVITY_ID_SMOKE] status='Failed' exception='{exception.GetType().Name}' message='{Escape(exception.Message)}'.");
                throw;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(activityA);
                UnityEngine.Object.DestroyImmediate(activityB);
            }
        }

        private static void Configure(ActivityAsset activity, string id, string displayName)
        {
            var serialized = new SerializedObject(activity);
            serialized.FindProperty("activityId").stringValue = id;
            serialized.FindProperty("activityName").stringValue = displayName;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
        }
    }
}
