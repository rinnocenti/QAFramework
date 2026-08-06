using System;
using System.Reflection;
using Immersive.Framework.Authoring;
using Immersive.Framework.RuntimeContent;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Descriptors.Editor
{
    internal static class QaA1ActivityIdSmoke
    {
        private const string LifecycleAdmissionTypeName =
            "Immersive.Framework.PlayerParticipation.ActivityPlayerLifecycleAdmissionRuntimeContext";

        [MenuItem("Immersive Framework/QA/Regressions/Contracts/Run Activity Identity Regression")]
        private static void Run()
        {
            ActivityAsset activityA = ScriptableObject.CreateInstance<ActivityAsset>();
            ActivityAsset activityB = ScriptableObject.CreateInstance<ActivityAsset>();

            try
            {
                MethodInfo createLifecycleOwner = ResolveCreateLifecycleOwner();

                Configure(activityA, "qa.a1.activity.a", "Original Name");
                Configure(activityB, "qa.a1.activity.b", "Original Name");

                RuntimeContentOwner ownerBeforeRename =
                    RuntimeContentOwner.Activity(
                        activityA.ActivityId.StableText,
                        activityA.ActivityName);
                RuntimeContentOwner lifecycleOwnerBeforeRename =
                    InvokeCreateLifecycleOwner(createLifecycleOwner, activityA);

                Assert(
                    lifecycleOwnerBeforeRename == ownerBeforeRename,
                    "P3K Activity lifecycle owner does not use ActivityId as functional identity.");

                Configure(activityA, "qa.a1.activity.a", "Renamed Activity");

                RuntimeContentOwner ownerAfterRename =
                    RuntimeContentOwner.Activity(
                        activityA.ActivityId.StableText,
                        activityA.ActivityName);
                RuntimeContentOwner lifecycleOwnerAfterRename =
                    InvokeCreateLifecycleOwner(createLifecycleOwner, activityA);

                Assert(
                    ownerBeforeRename == ownerAfterRename,
                    "Activity rename changed RuntimeContent owner identity.");
                Assert(
                    lifecycleOwnerBeforeRename == lifecycleOwnerAfterRename,
                    "Activity rename changed P3K lifecycle owner identity.");
                Assert(
                    lifecycleOwnerAfterRename == ownerAfterRename,
                    "P3K lifecycle owner diverged from the canonical Activity owner after rename.");
                Assert(
                    activityA.ActivityId != activityB.ActivityId,
                    "Distinct Activity IDs compared equal.");
                Assert(
                    ownerAfterRename != RuntimeContentOwner.Activity(
                        activityB.ActivityId.StableText,
                        activityB.ActivityName),
                    "Distinct Activity IDs produced the same owner.");

                Configure(activityB, "   ", "Whitespace Id");
                Assert(
                    !activityB.HasValidActivityId,
                    "Whitespace Activity ID was accepted.");
                AssertInvocationThrows<ArgumentException>(
                    createLifecycleOwner,
                    activityB,
                    "P3K lifecycle owner accepted an Activity with an invalid ActivityId.");
                AssertInvocationThrows<ArgumentNullException>(
                    createLifecycleOwner,
                    null,
                    "P3K lifecycle owner accepted a null Activity.");

                Debug.Log(
                    "[A1_ACTIVITY_ID_SMOKE] status='Passed' " +
                    "cases='rename-stable,distinct-id-distinct-owner,whitespace-invalid," +
                    "p3k-owner-uses-activity-id,p3k-owner-rename-stable," +
                    "p3k-invalid-id-rejected,p3k-null-rejected'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[A1_ACTIVITY_ID_SMOKE] status='Failed' " +
                    $"exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}'.");
                throw;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(activityA);
                UnityEngine.Object.DestroyImmediate(activityB);
            }
        }

        private static MethodInfo ResolveCreateLifecycleOwner()
        {
            Type runtimeType = typeof(ActivityAsset).Assembly.GetType(
                LifecycleAdmissionTypeName,
                throwOnError: false);

            Assert(
                runtimeType != null,
                $"Runtime type '{LifecycleAdmissionTypeName}' was not found.");

            MethodInfo method = runtimeType.GetMethod(
                "CreateActivityOwner",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert(
                method != null,
                "P3K Activity lifecycle owner factory was not found.");
            Assert(
                method.ReturnType == typeof(RuntimeContentOwner),
                "P3K Activity lifecycle owner factory has an unexpected return type.");

            ParameterInfo[] parameters = method.GetParameters();
            Assert(
                parameters.Length == 1 &&
                parameters[0].ParameterType == typeof(ActivityAsset),
                "P3K Activity lifecycle owner factory has an unexpected signature.");

            return method;
        }

        private static RuntimeContentOwner InvokeCreateLifecycleOwner(
            MethodInfo method,
            ActivityAsset activity)
        {
            object result = method.Invoke(
                null,
                new object[] { activity });

            Assert(
                result is RuntimeContentOwner,
                "P3K Activity lifecycle owner factory returned no RuntimeContentOwner.");

            return (RuntimeContentOwner)result;
        }

        private static void AssertInvocationThrows<TException>(
            MethodInfo method,
            ActivityAsset activity,
            string message)
            where TException : Exception
        {
            try
            {
                method.Invoke(
                    null,
                    new object[] { activity });
            }
            catch (TargetInvocationException exception)
                when (exception.InnerException is TException)
            {
                return;
            }

            throw new InvalidOperationException(message);
        }

        private static void Configure(
            ActivityAsset activity,
            string id,
            string displayName)
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
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "\\'")
                    .Replace("\r", " ")
                    .Replace("\n", " ");
        }
    }
}
