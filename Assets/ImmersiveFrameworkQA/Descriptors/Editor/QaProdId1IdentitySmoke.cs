using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Authoring;
using Immersive.Framework.Identity;
using Immersive.Framework.ObjectEntry;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.RuntimeContent;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Descriptors.Editor
{
    internal static class QaProdId1IdentitySmoke
    {
        private const string TempFolder = "Assets/ImmersiveFrameworkQA/__ProdId1IdentityTemp";
        private static bool ownsTempFolder;

        [MenuItem("Immersive Framework/QA/Regressions/Contracts/Run PROD-ID-1 Identity Regression")]
        private static void Run()
        {
            var objects = new List<UnityEngine.Object>();
            try
            {
                RouteAsset routeA = CreateRoute("qa.prod-id-1.route.a", "Shared Route", "Assets/Shared.unity", objects);
                RouteAsset routeB = CreateRoute("qa.prod-id-1.route.b", "Shared Route", "Assets/Shared.unity", objects);
                ActivityAsset activity = CreateActivity("qa.prod-id-1.activity", "Original Activity", objects);

                VerifyRouteIdentity(routeA, routeB);
                VerifyAdmissionToken(routeA, routeB, activity);
                VerifyActivityIdentityClosure(activity, objects);
                VerifyMalformedIds();
                VerifyAuthoringValidation();

                Debug.Log("[PROD_ID_1_IDENTITY_SMOKE] status='Passed' cases='same-scene-distinct-route-id,same-name-distinct-route-id,route-rename-stable,route-scene-change-stable,missing-route-id-rejected,invalid-route-id-rejected,duplicate-route-id-rejected,admission-token-route-id,activity-object-entry-id,activity-local-contribution-owner,activity-ledger-id,missing-activity-id-rejected,invalid-activity-id-rejected,duplicate-activity-id-rejected'.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[PROD_ID_1_IDENTITY_SMOKE] status='Failed' exception='{exception.GetType().Name}' message='{Escape(exception.Message)}'.");
                throw;
            }
            finally
            {
                if (ownsTempFolder)
                {
                    AssetDatabase.DeleteAsset(TempFolder);
                    ownsTempFolder = false;
                }
                for (int index = objects.Count - 1; index >= 0; index--)
                {
                    if (objects[index] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(objects[index]);
                    }
                }
            }
        }

        private static void VerifyRouteIdentity(RouteAsset routeA, RouteAsset routeB)
        {
            Assert(routeA.PrimaryScenePath == routeB.PrimaryScenePath, "Regression fixture no longer shares one Primary Scene.");
            Assert(routeA.RouteName == routeB.RouteName, "Regression fixture no longer shares one Route Name.");
            Assert(routeA.RouteId != routeB.RouteId, "Distinct RouteIds compared equal.");

            MethodInfo createIdentity = ResolveRuntimeMethod(
                "Immersive.Framework.RouteLifecycle.RouteRuntimeState",
                "CreateRouteIdentity");
            MethodInfo createOwner = ResolveRuntimeMethod(
                "Immersive.Framework.RouteLifecycle.RouteLifecycleRuntime",
                "CreateRouteOwner");

            FrameworkIdentityKey identityBefore = (FrameworkIdentityKey)createIdentity.Invoke(null, new object[] { routeA });
            RuntimeContentOwner ownerBefore = (RuntimeContentOwner)createOwner.Invoke(null, new object[] { routeA });
            RuntimeContentOwner ownerB = (RuntimeContentOwner)createOwner.Invoke(null, new object[] { routeB });
            Assert(ownerBefore != ownerB, "Routes sharing name and scene produced the same functional owner.");

            ConfigureRoute(routeA, routeA.RouteId.StableText, "Renamed Route", "Assets/Changed.unity");
            FrameworkIdentityKey identityAfter = (FrameworkIdentityKey)createIdentity.Invoke(null, new object[] { routeA });
            RuntimeContentOwner ownerAfter = (RuntimeContentOwner)createOwner.Invoke(null, new object[] { routeA });
            Assert(identityBefore == identityAfter, "Route rename or Primary Scene change changed Route identity.");
            Assert(ownerBefore == ownerAfter, "Route rename or Primary Scene change changed Route owner equality.");
        }

        private static void VerifyAdmissionToken(RouteAsset previousRoute, RouteAsset targetRoute, ActivityAsset activity)
        {
            RuntimeContentOwner previousActivityOwner = RuntimeContentOwner.Activity(activity.ActivityId.StableText, activity.ActivityName);
            RuntimeContentOwner targetActivityOwner = RuntimeContentOwner.Activity("qa.prod-id-1.activity.target", "Target Activity");
            ConstructorInfo constructor = typeof(ActivityPlayerLifecycleAdmissionToken).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[]
                {
                    typeof(string), typeof(RuntimeContentOwner), typeof(RuntimeContentOwner),
                    typeof(ActivityPlayerLifecycleAdmissionFlowKind), typeof(RouteId), typeof(RouteId), typeof(int)
                },
                null);
            Assert(constructor != null, "Typed admission token constructor was not found.");

            var token = (ActivityPlayerLifecycleAdmissionToken)constructor.Invoke(new object[]
            {
                "qa.prod-id-1.session", previousActivityOwner, targetActivityOwner,
                ActivityPlayerLifecycleAdmissionFlowKind.RouteStartupActivitySwitch,
                previousRoute.RouteId, targetRoute.RouteId, 1
            });
            RouteId alternateTargetId = RouteId.From("qa.prod-id-1.route.alternate");
            var alternate = (ActivityPlayerLifecycleAdmissionToken)constructor.Invoke(new object[]
            {
                "qa.prod-id-1.session", previousActivityOwner, targetActivityOwner,
                ActivityPlayerLifecycleAdmissionFlowKind.RouteStartupActivitySwitch,
                previousRoute.RouteId, alternateTargetId, 1
            });

            Assert(token.IsValid, "Typed Route Startup admission token is invalid.");
            Assert(token.TargetRouteId == targetRoute.RouteId, "Admission token did not retain TargetRouteId.");
            Assert(token != alternate && token.GetHashCode() != alternate.GetHashCode(), "Admission token equality/hash ignored RouteId.");
        }

        private static void VerifyActivityIdentityClosure(ActivityAsset activity, ICollection<UnityEngine.Object> objects)
        {
            GameObject root = new GameObject("PROD-ID-1 Object Entry");
            objects.Add(root);
            ObjectEntryDeclaration declaration = root.AddComponent<ObjectEntryDeclaration>();
            var serialized = new SerializedObject(declaration);
            serialized.FindProperty("objectEntryId").stringValue = "qa.prod-id-1.entry";
            serialized.FindProperty("scope").intValue = (int)ObjectEntryScope.Activity;
            serialized.FindProperty("activityOwner").objectReferenceValue = activity;
            serialized.FindProperty("requiredness").intValue = (int)ObjectEntryRequiredness.Required;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            ObjectEntryDescriptor entryBefore = declaration.CreateDescriptor();
            FrameworkIdentityKey localOwnerBefore = InvokeActivityOwnerKey(activity);
            object ledger = CreateLedgerEntry(activity);
            ActivityId ledgerIdBefore = ReadLedgerActivityId(ledger);

            ConfigureActivity(activity, activity.ActivityId.StableText, "Renamed Activity");
            ObjectEntryDescriptor entryAfter = declaration.CreateDescriptor();
            FrameworkIdentityKey localOwnerAfter = InvokeActivityOwnerKey(activity);
            ActivityId ledgerIdAfter = ReadLedgerActivityId(ledger);

            Assert(entryBefore.OwnerIdentity == entryAfter.OwnerIdentity, "Activity rename changed Object Entry owner identity.");
            Assert(entryAfter.OwnerIdentity.HasValue && entryAfter.OwnerIdentity.Value.Value.Value == activity.ActivityId.StableText, "Object Entry owner does not use ActivityId.");
            Assert(localOwnerBefore == localOwnerAfter && localOwnerAfter.Value.Value == activity.ActivityId.StableText, "Activity rename changed Local Contribution ownership.");
            Assert(ledgerIdBefore == activity.ActivityId && ledgerIdAfter == activity.ActivityId, "Activity Scene Ledger did not expose the real ActivityId.");
        }

        private static void VerifyMalformedIds()
        {
            Assert(!RouteId.IsValidText(string.Empty), "Missing RouteId was accepted.");
            Assert(!RouteId.IsValidText("Invalid Route"), "Malformed RouteId was accepted.");
            Assert(!ActivityId.IsValidText(string.Empty), "Missing ActivityId was accepted.");
            Assert(!ActivityId.IsValidText("Invalid Activity"), "Malformed ActivityId was accepted.");
        }

        private static void VerifyAuthoringValidation()
        {
            Assert(!AssetDatabase.IsValidFolder(TempFolder), $"Temporary QA folder already exists: '{TempFolder}'.");
            AssetDatabase.CreateFolder("Assets/ImmersiveFrameworkQA", "__ProdId1IdentityTemp");
            ownsTempFolder = true;
            CreateValidationRoute("RouteMissing.asset", string.Empty);
            CreateValidationRoute("RouteInvalid.asset", "Invalid Route");
            CreateValidationRoute("RouteDuplicateA.asset", "qa.prod-id-1.duplicate.route");
            CreateValidationRoute("RouteDuplicateB.asset", "qa.prod-id-1.duplicate.route");
            CreateValidationActivity("ActivityMissing.asset", string.Empty);
            CreateValidationActivity("ActivityInvalid.asset", "Invalid Activity");
            CreateValidationActivity("ActivityDuplicateA.asset", "qa.prod-id-1.duplicate.activity");
            CreateValidationActivity("ActivityDuplicateB.asset", "qa.prod-id-1.duplicate.activity");
            AssetDatabase.SaveAssets();

            Assembly editorAssembly = FindAssembly("Immersive.Framework.Editor");
            Type validatorType = editorAssembly.GetType(
                "Immersive.Framework.Editor.Editor.Authoring.FrameworkIdentityAuthoringValidator",
                true);
            MethodInfo validate = validatorType.GetMethod("ValidateProjectAssets", BindingFlags.Static | BindingFlags.NonPublic);
            object report = validate.Invoke(null, new object[] { FrameworkValidationMode.Standard });
            string messages = ReadValidationMessages(report);

            Assert(messages.Contains("Route ID is missing."), "Canonical validator did not report missing RouteId.");
            Assert(messages.Contains("Route ID is invalid."), "Canonical validator did not report invalid RouteId.");
            Assert(messages.Contains("Duplicate Route ID 'qa.prod-id-1.duplicate.route'"), "Canonical validator did not report duplicate RouteId.");
            Assert(messages.Contains("Activity ID is missing."), "Canonical validator did not report missing ActivityId.");
            Assert(messages.Contains("Activity ID is invalid."), "Canonical validator did not report invalid ActivityId.");
            Assert(messages.Contains("Duplicate Activity ID 'qa.prod-id-1.duplicate.activity'"), "Canonical validator did not report duplicate ActivityId.");
        }

        private static RouteAsset CreateRoute(string id, string name, string scenePath, ICollection<UnityEngine.Object> objects)
        {
            RouteAsset route = ScriptableObject.CreateInstance<RouteAsset>();
            objects.Add(route);
            ConfigureRoute(route, id, name, scenePath);
            return route;
        }

        private static ActivityAsset CreateActivity(string id, string name, ICollection<UnityEngine.Object> objects)
        {
            ActivityAsset activity = ScriptableObject.CreateInstance<ActivityAsset>();
            objects.Add(activity);
            ConfigureActivity(activity, id, name);
            return activity;
        }

        private static void ConfigureRoute(RouteAsset route, string id, string name, string scenePath)
        {
            var serialized = new SerializedObject(route);
            serialized.FindProperty("routeId").stringValue = id;
            serialized.FindProperty("routeName").stringValue = name;
            serialized.FindProperty("primaryScenePath").stringValue = scenePath;
            serialized.FindProperty("primarySceneName").stringValue = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureActivity(ActivityAsset activity, string id, string name)
        {
            var serialized = new SerializedObject(activity);
            serialized.FindProperty("activityId").stringValue = id;
            serialized.FindProperty("activityName").stringValue = name;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static MethodInfo ResolveRuntimeMethod(string typeName, string methodName)
        {
            Type type = typeof(RouteAsset).Assembly.GetType(typeName, true);
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            Assert(method != null, $"Runtime method '{typeName}.{methodName}' was not found.");
            return method;
        }

        private static FrameworkIdentityKey InvokeActivityOwnerKey(ActivityAsset activity)
        {
            MethodInfo method = ResolveRuntimeMethod(
                "Immersive.Framework.LocalContribution.LocalContributionDiscovery",
                "CreateActivityOwnerKey");
            return (FrameworkIdentityKey)method.Invoke(null, new object[] { activity });
        }

        private static object CreateLedgerEntry(ActivityAsset activity)
        {
            Assembly runtimeAssembly = typeof(RouteAsset).Assembly;
            Type entryType = runtimeAssembly.GetType("Immersive.Framework.ActivityFlow.ActivitySceneLedgerEntry", true);
            ConstructorInfo constructor = entryType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)[0];
            ParameterInfo[] parameters = constructor.GetParameters();
            object[] arguments = new object[parameters.Length];
            arguments[0] = "qa.prod-id-1.route-instance";
            arguments[1] = null;
            arguments[2] = activity;
            for (int index = 3; index < arguments.Length; index++)
            {
                arguments[index] = Activator.CreateInstance(parameters[index].ParameterType);
            }
            return constructor.Invoke(arguments);
        }

        private static ActivityId ReadLedgerActivityId(object ledger)
        {
            PropertyInfo property = ledger.GetType().GetProperty("ActivityId", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert(property != null && property.PropertyType == typeof(ActivityId), "Activity Scene Ledger ActivityId is not strongly typed.");
            return (ActivityId)property.GetValue(ledger);
        }

        private static void CreateValidationRoute(string fileName, string id)
        {
            RouteAsset route = ScriptableObject.CreateInstance<RouteAsset>();
            ConfigureRoute(route, id, fileName, "Assets/Validation.unity");
            AssetDatabase.CreateAsset(route, TempFolder + "/" + fileName);
        }

        private static void CreateValidationActivity(string fileName, string id)
        {
            ActivityAsset activity = ScriptableObject.CreateInstance<ActivityAsset>();
            ConfigureActivity(activity, id, fileName);
            AssetDatabase.CreateAsset(activity, TempFolder + "/" + fileName);
        }

        private static Assembly FindAssembly(string name)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == name)
                {
                    return assembly;
                }
            }
            throw new InvalidOperationException($"Assembly '{name}' was not loaded.");
        }

        private static string ReadValidationMessages(object report)
        {
            PropertyInfo issuesProperty = report.GetType().GetProperty("Issues", BindingFlags.Instance | BindingFlags.NonPublic);
            var messages = new System.Text.StringBuilder();
            foreach (object issue in (IEnumerable)issuesProperty.GetValue(report))
            {
                PropertyInfo messageProperty = issue.GetType().GetProperty("Message", BindingFlags.Instance | BindingFlags.NonPublic);
                messages.AppendLine((string)messageProperty.GetValue(issue));
            }
            return messages.ToString();
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static string Escape(string value) => string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
    }
}
