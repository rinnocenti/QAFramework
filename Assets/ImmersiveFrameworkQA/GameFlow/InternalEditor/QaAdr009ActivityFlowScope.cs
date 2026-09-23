using System;
using System.Threading.Tasks;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.RouteLifecycle;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    internal sealed class QaAdr009ActivityFlowScope
    {
        private const string RouteAPath = "Assets/ImmersiveFrameworkQA/Lifecycle/Routes/QA_LifecycleRouteA.asset";
        private const string RouteBPath = "Assets/ImmersiveFrameworkQA/Lifecycle/Routes/QA_LifecycleRouteB.asset";
        private const string ActivityAPath = "Assets/ImmersiveFrameworkQA/Lifecycle/Activities/QA_LifecycleActivityA.asset";
        private const string ActivityCPath = "Assets/ImmersiveFrameworkQA/Lifecycle/Activities/QA_LifecycleNoContentActivity.asset";
        private const string AdditionalScenePath = "Assets/ImmersiveFrameworkQA/Lifecycle/Scenes/QA_LifecycleAdditional.unity";

        private readonly string _source;

        internal QaAdr009ActivityFlowScope(string source)
        {
            _source = string.IsNullOrWhiteSpace(source) ? "QaAdr009ActivityFlowScope" : source;
        }

        internal FrameworkRuntimeHost Host { get; private set; }
        internal IRouteRuntimePort Routes { get; private set; }
        internal IActivityRuntimePort Activities { get; private set; }
        internal RouteAsset InitialRoute { get; private set; }
        internal ActivityAsset InitialActivity { get; private set; }
        internal RouteAsset RouteA { get; private set; }
        internal RouteAsset RouteB { get; private set; }
        internal ActivityAsset ActivityA { get; private set; }
        internal ActivityAsset ActivityC { get; private set; }
        internal Scene AdditionalScene { get; private set; }

        internal async Task InitializeAsync()
        {
            Require(EditorApplication.isPlaying, "ADR-009 focused regressions require Play Mode.");
            Require(QaH2FrameworkReadiness.TryResolveUniqueHost(out FrameworkRuntimeHost host, out string diagnostic), diagnostic);
            Require(host.State.GameFlowStarted && host.State.CurrentRoute != null, "Game Flow is not ready.");

            Host = host;
            Routes = (IRouteRuntimePort)host;
            Activities = (IActivityRuntimePort)host;
            InitialRoute = host.State.CurrentRoute;
            InitialActivity = host.State.CurrentActivity;
            RouteA = Load<RouteAsset>(RouteAPath);
            RouteB = Load<RouteAsset>(RouteBPath);
            ActivityA = Load<ActivityAsset>(ActivityAPath);
            ActivityC = Load<ActivityAsset>(ActivityCPath);

            if (ReferenceEquals(host.State.CurrentRoute, RouteA))
            {
                await RequestRouteAsync(RouteB, "adr009-route-a-reload-precondition");
            }

            await RequestRouteAsync(RouteA, "adr009-load-lifecycle-additional");
            AdditionalScene = SceneManager.GetSceneByPath(AdditionalScenePath);
            Require(AdditionalScene.IsValid() && AdditionalScene.isLoaded,
                "QA_LifecycleAdditional must be loaded by Route A.");
            await EnsureActivityAsync(ActivityA, "adr009-activity-a-baseline");
        }

        internal async Task EnsureActivityAsync(ActivityAsset activity, string reason)
        {
            Require(activity != null, "Target Activity is required.");
            if (ReferenceEquals(Host.State.CurrentActivity, activity))
            {
                return;
            }

            FrameworkActivityRequestResult result = await Activities.RequestActivityAsync(activity, _source, reason);
            Require(result.Succeeded && result.DestinationAuthoritative, result.Message);
            Require(ReferenceEquals(Host.State.CurrentActivity, activity),
                $"Activity '{activity.ActivityName}' did not become canonical.");
        }

        internal async Task RestoreAsync()
        {
            if (Host == null || InitialRoute == null)
            {
                return;
            }

            if (!ReferenceEquals(Host.State.CurrentRoute, InitialRoute))
            {
                await RequestRouteAsync(InitialRoute, "adr009-restore-route");
            }

            if (InitialActivity == null)
            {
                if (Host.State.CurrentActivity != null)
                {
                    FrameworkActivityRequestResult clear = await Activities.ClearActivityAsync(_source, "adr009-restore-no-active");
                    Require(clear.Succeeded, clear.Message);
                }

                return;
            }

            await EnsureActivityAsync(InitialActivity, "adr009-restore-activity");
        }

        internal GameObject CreateTemporaryRoot(string name)
        {
            Require(AdditionalScene.IsValid() && AdditionalScene.isLoaded,
                "ADR-009 temporary root requires the loaded additional Scene.");
            var root = new GameObject(name);
            SceneManager.MoveGameObjectToScene(root, AdditionalScene);
            return root;
        }

        internal static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        internal static string Escape(string value) => string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");

        private async Task RequestRouteAsync(RouteAsset route, string reason)
        {
            FrameworkRouteRequestResult result = await Routes.RequestRouteAsync(route, _source, reason);
            Require(result.Succeeded, result.Message);
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Require(asset != null, "Missing QA asset: " + path);
            return asset;
        }
    }
}
