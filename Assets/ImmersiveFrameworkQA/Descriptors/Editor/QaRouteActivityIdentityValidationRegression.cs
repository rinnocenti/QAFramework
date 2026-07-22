using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Authoring;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Descriptors.Editor
{
    internal static class QaRouteActivityIdentityValidationRegression
    {
        private const string MenuPath = "Immersive Framework/QA/Regressions/Authoring/Run Route and Activity Identity Validation Regression";
        private const string LogPrefix = "[ROUTE_ACTIVITY_IDENTITY_VALIDATION_REGRESSION]";
        private const string TempFolder = "Assets/ImmersiveFrameworkQA/__RouteActivityIdentityValidationTemp";
        private const string CanonicalRoot = "Assets/ImmersiveFrameworkQA";
        private const int ExpectedCanonicalRouteCount = 15;
        private static bool ownsTempFolder;

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => !EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        private static void Run()
        {
            var completed = new List<string>();
            try
            {
                Require(!EditorApplication.isPlaying, "Identity validation regression requires Edit Mode.");
                completed.Add("edit-mode-required");
                ValidateCanonicalAssets(completed);
                ValidateIsolatedNegativeAssets(completed);
                Require(completed.Count == 12, $"Identity validation case count changed. actual='{completed.Count}'.");
                Debug.Log($"{LogPrefix} status='Passed' cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"{LogPrefix} status='Failed' exception='{exception.GetType().Name}' message='{Escape(exception.Message)}' completed='{string.Join(",", completed)}'.");
                throw;
            }
            finally
            {
                if (ownsTempFolder)
                {
                    AssetDatabase.DeleteAsset(TempFolder);
                    ownsTempFolder = false;
                }
            }
        }

        private static void ValidateCanonicalAssets(ICollection<string> completed)
        {
            RouteAsset[] routes = LoadAssets<RouteAsset>(CanonicalRoot);
            Require(routes.Length == ExpectedCanonicalRouteCount,
                $"Expected exactly '{ExpectedCanonicalRouteCount}' canonical QA Route assets, found '{routes.Length}'.");
            completed.Add("canonical-route-count-is-15");

            var routeIds = new HashSet<RouteId>();
            foreach (RouteAsset route in routes)
            {
                Require(route.HasValidRouteId, $"Canonical Route '{AssetDatabase.GetAssetPath(route)}' has no valid RouteId.");
                Require(routeIds.Add(route.RouteId), $"Canonical RouteId '{route.RouteId}' is duplicated.");
            }
            completed.Add("canonical-route-ids-valid-and-unique");

            ActivityAsset[] activities = LoadAssets<ActivityAsset>(CanonicalRoot);
            var activityIds = new HashSet<ActivityId>();
            foreach (ActivityAsset activity in activities)
            {
                Require(activity.HasValidActivityId, $"Canonical Activity '{AssetDatabase.GetAssetPath(activity)}' has no valid ActivityId.");
                Require(activityIds.Add(activity.ActivityId), $"Canonical ActivityId '{activity.ActivityId}' is duplicated.");
            }
            completed.Add("canonical-activity-ids-valid-and-unique");
        }

        private static void ValidateIsolatedNegativeAssets(ICollection<string> completed)
        {
            Require(!AssetDatabase.IsValidFolder(TempFolder), $"Temporary QA folder already exists: '{TempFolder}'.");
            AssetDatabase.CreateFolder("Assets/ImmersiveFrameworkQA", "__RouteActivityIdentityValidationTemp");
            ownsTempFolder = true;
            CreateValidationRoute("RouteMissing.asset", string.Empty);
            CreateValidationRoute("RouteInvalid.asset", "Invalid Route");
            CreateValidationRoute("RouteDuplicateA.asset", "qa.identity.duplicate.route");
            CreateValidationRoute("RouteDuplicateB.asset", "qa.identity.duplicate.route");
            CreateValidationActivity("ActivityMissing.asset", string.Empty);
            CreateValidationActivity("ActivityInvalid.asset", "Invalid Activity");
            CreateValidationActivity("ActivityDuplicateA.asset", "qa.identity.duplicate.activity");
            CreateValidationActivity("ActivityDuplicateB.asset", "qa.identity.duplicate.activity");
            AssetDatabase.SaveAssets();

            string messages = RunCanonicalValidator();
            Require(messages.Contains("Route ID is missing."), "Missing RouteId was not reported.");
            completed.Add("missing-route-id-rejected");
            Require(messages.Contains("Route ID is invalid."), "Invalid RouteId was not reported.");
            completed.Add("invalid-route-id-rejected");
            Require(messages.Contains("Duplicate Route ID 'qa.identity.duplicate.route'"), "Duplicate RouteId was not reported.");
            completed.Add("duplicate-route-id-rejected");
            Require(messages.Contains("Activity ID is missing."), "Missing ActivityId was not reported.");
            completed.Add("missing-activity-id-rejected");
            Require(messages.Contains("Activity ID is invalid."), "Invalid ActivityId was not reported.");
            completed.Add("invalid-activity-id-rejected");
            Require(messages.Contains("Duplicate Activity ID 'qa.identity.duplicate.activity'"), "Duplicate ActivityId was not reported.");
            completed.Add("duplicate-activity-id-rejected");

            RouteAsset[] temporaryRoutes = LoadAssets<RouteAsset>(TempFolder);
            Require(temporaryRoutes.Length == 4, "Negative Route fixture count changed.");
            Require(temporaryRoutes[2].RouteName == temporaryRoutes[3].RouteName &&
                    temporaryRoutes[2].PrimaryScenePath == temporaryRoutes[3].PrimaryScenePath,
                "Negative Route fixtures no longer share display name and scene path.");
            completed.Add("route-name-and-scene-path-are-not-identity");
            completed.Add("negative-fixtures-isolated-from-canonical-set");
        }

        private static T[] LoadAssets<T>(string root) where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { root });
            var assets = new List<T>(guids.Length);
            foreach (string guid in guids)
            {
                T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) assets.Add(asset);
            }
            return assets.ToArray();
        }

        private static string RunCanonicalValidator()
        {
            Assembly editorAssembly = FindAssembly("Immersive.Framework.Editor");
            Type validatorType = editorAssembly.GetType("Immersive.Framework.Editor.Editor.Authoring.FrameworkIdentityAuthoringValidator", true);
            MethodInfo validate = validatorType.GetMethod("ValidateProjectAssets", BindingFlags.Static | BindingFlags.NonPublic);
            Require(validate != null, "Canonical identity validator entry point was not found.");
            object report = validate.Invoke(null, new object[] { FrameworkValidationMode.Standard });
            PropertyInfo issuesProperty = report.GetType().GetProperty("Issues", BindingFlags.Instance | BindingFlags.NonPublic);
            Require(issuesProperty != null, "Canonical validation report has no Issues evidence.");
            var messages = new System.Text.StringBuilder();
            foreach (object issue in (IEnumerable)issuesProperty.GetValue(report))
            {
                PropertyInfo messageProperty = issue.GetType().GetProperty("Message", BindingFlags.Instance | BindingFlags.NonPublic);
                Require(messageProperty != null, "Canonical validation issue has no Message evidence.");
                messages.AppendLine((string)messageProperty.GetValue(issue));
            }
            return messages.ToString();
        }

        private static void CreateValidationRoute(string fileName, string id)
        {
            RouteAsset route = ScriptableObject.CreateInstance<RouteAsset>();
            var serialized = new SerializedObject(route);
            serialized.FindProperty("routeId").stringValue = id;
            serialized.FindProperty("routeName").stringValue = "Shared Route";
            serialized.FindProperty("primaryScenePath").stringValue = "Assets/Shared.unity";
            serialized.FindProperty("primarySceneName").stringValue = "Shared";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(route, $"{TempFolder}/{fileName}");
        }

        private static void CreateValidationActivity(string fileName, string id)
        {
            ActivityAsset activity = ScriptableObject.CreateInstance<ActivityAsset>();
            var serialized = new SerializedObject(activity);
            serialized.FindProperty("activityId").stringValue = id;
            serialized.FindProperty("activityName").stringValue = "Shared Activity";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(activity, $"{TempFolder}/{fileName}");
        }

        private static Assembly FindAssembly(string name)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                if (assembly.GetName().Name == name) return assembly;
            throw new InvalidOperationException($"Assembly '{name}' was not loaded.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static string Escape(string value) => string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
    }
}
