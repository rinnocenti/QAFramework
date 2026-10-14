using System;
using System.Collections.Generic;
using System.Linq;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.GameFlow;
using Immersive.Framework.Pause;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.Transition;
using Immersive.Framework.TransitionEffects;
using Immersive.QaFramework.IfAdr036;
using Immersive.QaFramework.PlayerSession.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Immersive.QaFramework.IfAdr036.Editor
{
    public static class QaIfAdr036Setup
    {
        private const string Root = "Assets/QA-IF-ADR-036/Generated";
        private const string Settings = Root + "/Settings";
        private const string Scenes = Root + "/Scenes";
        private const string Base = "Assets/QA-IF-ADR-044";
        private const string AppPath = Settings + "/GameApplication_QaIfAdr036.asset";
        private const string RoutePath = Settings + "/Route_QaIfAdr036.asset";
        private const string BActivityPath = Settings + "/Activity_QaIfAdr036_B.asset";
        private const string BProfilePath = Settings + "/ActivityContentProfile_QaIfAdr036_B.asset";
        private const string PersistentPath = Scenes + "/QA_IF_ADR_036_Persistent.unity";
        private const string RouteScenePath = Scenes + "/QA_IF_ADR_036_Route.unity";
        private const string BScenePath = Scenes + "/QA_IF_ADR_036_ActivityB.unity";
        private const string AActivitySourcePath = Base + "/Settings/Activity_QaIfAdr044.asset";
        private const string AProfileSourcePath = Base + "/Settings/ActivityContentProfile_QaIfAdr044.asset";
        private const string ASceneSourcePath = Base + "/Scenes/QA_IF_ADR_044_ActivityContent.unity";
        private const string AActivityPath = Settings + "/Activity_QaIfAdr036_A.asset";
        private const string AProfilePath = Settings + "/ActivityContentProfile_QaIfAdr036_A.asset";
        private const string AScenePath = Scenes + "/QA_IF_ADR_036_ActivityA.unity";
        private const string P1Path = "Assets/QA-IF-ADR-042/Settings/PlayerSlot_QaIfAdr042_P1.asset";

        [MenuItem("Immersive Framework/QA/IF-ADR-036/Configure GameplayReady Transition-Pause Proof")]
        public static void Configure()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureFolder("Assets", "QA-IF-ADR-036");
            EnsureFolder("Assets/QA-IF-ADR-036", "Generated");
            EnsureFolder(Root, "Settings");
            EnsureFolder(Root, "Scenes");
            ValidateSourceFixture();

            PlayerSlotProfile p1 = Load<PlayerSlotProfile>(P1Path);
            ActivityContentProfileAsset profileA = CopyOrLoad<ActivityContentProfileAsset>(AProfileSourcePath, AProfilePath);
            Set(profileA, "profileId", "qa-if-adr-036.activity-a-content");
            SetSceneEntry(profileA, AScenePath, "qa-if-adr-036.activity-a.main");
            ActivityAsset activityA = CopyOrLoad<ActivityAsset>(AActivitySourcePath, AActivityPath);
            Set(activityA, "activityId", "qa-if-adr-036.activity-a");
            Set(activityA, "activityName", "IF-ADR-036 Baseline Activity A");
            Set(activityA, "activityContentProfile", profileA);
            SetEnum(activityA, "visualTransitionMode", "Fade");
            CopyOrLoad<SceneAsset>(ASceneSourcePath, AScenePath);
            UpdateActivityAScene(activityA);

            ActivityContentProfileAsset profileB = CopyOrLoad<ActivityContentProfileAsset>(
                Base + "/Settings/ActivityContentProfile_QaIfAdr044.asset", BProfilePath);
            Set(profileB, "profileId", "qa-if-adr-036.activity-b-content");
            SetSceneEntry(profileB, BScenePath, "qa-if-adr-036.activity-b.main");
            p1 = Load<PlayerSlotProfile>(P1Path);
            RequireAsset<PlayerSlotProfile>(P1Path, p1);
            ActivityAsset activityB = CopyOrLoad<ActivityAsset>(AActivitySourcePath, BActivityPath);
            Set(activityB, "activityId", "qa-if-adr-036.activity-b");
            Set(activityB, "activityName", "IF-ADR-036 Controlled Transition B");
            Set(activityB, "activityContentProfile", profileB);
            SetSingleExplicitSlot(activityB, p1);
            SetEnum(activityB, "activityEntryReadinessPolicy", "WaitVisible");
            SetEnum(activityB, "visualTransitionMode", "Fade");
            SetEnum(activityB, "transitionGateMode", "InputInteractionAndGameplay");

            CopyOrLoad<SceneAsset>(Base + "/Scenes/QA_IF_ADR_044_Persistent.unity", PersistentPath);
            EnsurePersistentTransitionSurface(PersistentPath);

            // Scene open/restore operations can invalidate Unity object wrappers. Reload all
            // authored assets after that boundary before assigning cross-asset references.
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            profileA = Load<ActivityContentProfileAsset>(AProfilePath);
            activityA = Load<ActivityAsset>(AActivityPath);
            profileB = Load<ActivityContentProfileAsset>(BProfilePath);
            activityB = Load<ActivityAsset>(BActivityPath);

            RouteAsset route = CopyOrLoad<RouteAsset>(Base + "/Settings/Route_QaIfAdr044.asset", RoutePath);
            Set(route, "routeId", "qa-if-adr-036.start");
            Set(route, "routeName", "IF-ADR-036 GameplayReady Transition/Pause Proof");
            Set(route, "primaryScenePath", RouteScenePath);
            Set(route, "primarySceneName", "QA_IF_ADR_036_Route");
            Set(route, "startupActivity", activityA);
            GameApplicationAsset app = CopyOrLoad<GameApplicationAsset>(Base + "/Settings/GameApplication_QaIfAdr044.asset", AppPath);
            Set(app, "applicationName", "IF-ADR-036 GameplayReady Transition/Pause Proof");
            Set(app, "startupRoute", route);
            SetNested(app, "persistentContent.scenePath", PersistentPath);
            SetNested(app, "persistentContent.sceneName", "QA_IF_ADR_036_Persistent");
            SetNested(app, "persistentContent.containerScene", AssetDatabase.LoadAssetAtPath<SceneAsset>(PersistentPath));

            // NewScene(Single) invalidates wrappers for unsaved ScriptableObjects. Persist all
            // cross-asset references before creating scenes, then reload them in each builder.
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            CreateBScene(activityB);
            CreateRouteScene(app, route, activityA, activityB, p1);
            AddToBuildSettings(PersistentPath);
            AddToBuildSettings(RouteScenePath);
            AddToBuildSettings(BScenePath);
            AddToBuildSettings(AScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            ValidatePersisted(app, route, activityA, activityB, p1);
            Debug.Log("[IF-ADR-036] setup='Configured' baseline='Authored' application='" + AppPath +
                "' route='" + RouteScenePath + "' activityA='" + AActivityPath + "' activityB='" + BActivityPath +
                "' persistent='" + PersistentPath + "'. Select the generated Game Application and run a fresh Play Mode session.");
        }

        private static void ValidateSourceFixture()
        {
            ActivityAsset activity = Load<ActivityAsset>(AActivitySourcePath);
            PlayerSlotProfile p1 = Load<PlayerSlotProfile>(P1Path);
            GameApplicationAsset app = Load<GameApplicationAsset>(Base + "/Settings/GameApplication_QaIfAdr044.asset");
            if (activity == null || p1 == null || app == null || activity.ActivityContentProfile == null ||
                activity.PlayerParticipationRequirementLevel != PlayerParticipationRequirementLevel.GameplayReady ||
                activity.ActivityContentProfile.SceneCount != 1 ||
                AssetDatabase.LoadAssetAtPath<GameObject>(Base + "/Players/PF_LocalPlayerHost_QaIfAdr044.prefab") == null)
                throw new InvalidOperationException("IF-ADR-036 requires the completed IF-ADR-044 source composition; configure QA-IF-ADR-044 first.");
        }

        private static void CreateBScene(ActivityAsset activity)
        {
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                activity = Load<ActivityAsset>(BActivityPath);
                GameObject root = new GameObject("IF-ADR-036 Activity B Content");
                ActivityContentContribution contribution = root.AddComponent<ActivityContentContribution>();
                Set(contribution, "activity", activity);
                Set(contribution, "localContentId", "qa-if-adr-036.activity-b.main");
                PlayerSessionObserver observer = root.AddComponent<PlayerSessionObserver>();
                SetEnum(observer, "scope", "Activity");
                PlayerSlotProfile player1 = Load<PlayerSlotProfile>(P1Path);
                QaPlayerSessionCommandTriggerSetup.EnsureLeave(
                    root,
                    "Leave Player 1",
                    LocalPlayerProvisioningConsumerScope.Activity,
                    player1);
                ActivityReadinessParticipant participant = root.AddComponent<ActivityReadinessParticipant>();
                Set(participant, "participantId", "qa-if-adr-036.activity-b.required");
                SetEnum(participant, "requiredness", "Required");
                if (contribution.Activity != activity || participant.Requiredness != ActivityContentExecutionRequiredness.Required)
                    throw new InvalidOperationException("Generated B content lost its Activity/Required readiness binding.");
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, BScenePath)) throw new InvalidOperationException("Could not save Activity B scene.");
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(previous); }
        }

        private static void UpdateActivityAScene(ActivityAsset activity)
        {
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene = OpenOrGet(AScenePath);
                ActivityContentContribution[] contributions = Components<ActivityContentContribution>(scene);
                if (contributions.Length != 1)
                    throw new InvalidOperationException("Cloned Activity A scene must contain exactly one Activity content contribution.");
                Set(contributions[0], "activity", activity);
                Set(contributions[0], "localContentId", "qa-if-adr-036.activity-a.main");
                PlayerSessionObserver observer = Components<PlayerSessionObserver>(scene).SingleOrDefault();
                if (observer == null)
                    throw new InvalidOperationException("Cloned Activity A scene must retain its unique Player Session observer.");
                PlayerSlotProfile player1 = Load<PlayerSlotProfile>(P1Path);
                QaPlayerSessionCommandTriggerSetup.EnsureLeave(
                    observer.gameObject,
                    "Leave Player 1",
                    LocalPlayerProvisioningConsumerScope.Activity,
                    player1);
                if (contributions[0].Activity != activity)
                    throw new InvalidOperationException("Cloned Activity A contribution does not target the QA-IF-ADR-036 Activity asset.");
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, AScenePath))
                    throw new InvalidOperationException("Could not save cloned Activity A scene.");
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(previous); }
        }

        private static void EnsurePersistentTransitionSurface(string path)
        {
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene = OpenOrGet(path);
                UnityFadeCurtainEffectAdapter[] adapters = Components<UnityFadeCurtainEffectAdapter>(scene);
                if (adapters.Length > 1)
                    throw new InvalidOperationException("Persistent scene has duplicate transition adapters.");

                GameObject[] roots = scene.GetRootGameObjects();
                GameObject curtain = roots.SingleOrDefault(value => value.name == "IF-ADR-036 Transition Curtain");
                if (curtain == null)
                {
                    curtain = new GameObject("IF-ADR-036 Transition Curtain");
                    SceneManager.MoveGameObjectToScene(curtain, scene);
                }
                if (adapters.Length == 1 && adapters[0].transform != curtain.transform &&
                    !adapters[0].transform.IsChildOf(curtain.transform))
                    throw new InvalidOperationException("Persistent scene transition adapter is outside its authored curtain.");

                Canvas[] existingCanvases = curtain.GetComponentsInChildren<Canvas>(true);
                if (existingCanvases.Length > 1)
                    throw new InvalidOperationException("Transition curtain has duplicate Canvas components.");

                GameObject surface;
                if (existingCanvases.Length == 1)
                {
                    surface = existingCanvases[0].gameObject;
                }
                else
                {
                    if (adapters.Length == 1)
                        Object.DestroyImmediate(adapters[0]);

                    Transform partialSurface = curtain.transform.Find("Transition Canvas");
                    if (partialSurface != null)
                        Object.DestroyImmediate(partialSurface.gameObject);

                    // Canvas is created together with RectTransform so Unity establishes the
                    // required UI transform before any other component is attached.
                    surface = new GameObject("Transition Canvas", typeof(RectTransform), typeof(Canvas),
                        typeof(CanvasGroup), typeof(CanvasRenderer), typeof(Image));
                    SceneManager.MoveGameObjectToScene(surface, scene);
                    surface.transform.SetParent(curtain.transform, false);
                }
                surface.SetActive(false);
                if (adapters.Length == 1 && adapters[0].gameObject != surface)
                    Object.DestroyImmediate(adapters[0]);

                Canvas canvas = surface.GetComponent<Canvas>();
                CanvasGroup group = surface.GetComponent<CanvasGroup>() ?? surface.AddComponent<CanvasGroup>();
                CanvasRenderer renderer = surface.GetComponent<CanvasRenderer>() ?? surface.AddComponent<CanvasRenderer>();
                Image image = surface.GetComponent<Image>() ?? surface.AddComponent<Image>();
                UnityFadeCurtainEffectAdapter adapter = surface.GetComponent<UnityFadeCurtainEffectAdapter>() ??
                    surface.AddComponent<UnityFadeCurtainEffectAdapter>();
                if (surface.GetComponent<RectTransform>() == null)
                    throw new InvalidOperationException("Transition curtain surface has Transform but no RectTransform required by Canvas.");
                if (canvas == null)
                    throw new InvalidOperationException("Transition curtain Canvas was not created with its RectTransform.");
                if (group == null || renderer == null || image == null || adapter == null)
                    throw new InvalidOperationException("Transition curtain is missing CanvasGroup, CanvasRenderer, Image, or effect adapter.");
                SerializedObject adapterConfiguration = new SerializedObject(adapter);
                SerializedProperty keepSurfaceActive = adapterConfiguration.FindProperty("setSurfaceRootActive");
                if (keepSurfaceActive == null)
                    throw new InvalidOperationException("Transition curtain adapter surface lifecycle setting is unavailable.");
                keepSurfaceActive.boolValue = false;
                adapterConfiguration.ApplyModifiedPropertiesWithoutUndo();
                UnityFadeCurtainEffectAdapter[] configuredAdapters = Components<UnityFadeCurtainEffectAdapter>(scene);
                if (configuredAdapters.Length != 1 || configuredAdapters[0] != adapter)
                    throw new InvalidOperationException("Persistent scene transition adapter is outside its authored curtain.");

                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.overrideSorting = true;
                canvas.sortingOrder = short.MaxValue;
                RectTransform rect = surface.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
                image.color = Color.black;
                image.raycastTarget = false;
                surface.SetActive(true);

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, path))
                    throw new InvalidOperationException("Could not save persistent transition surface scene.");
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(previous); }
        }

        private static void CreateRouteScene(GameApplicationAsset app, RouteAsset route, ActivityAsset activityA,
            ActivityAsset activityB, PlayerSlotProfile p1)
        {
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                app = Load<GameApplicationAsset>(AppPath);
                route = Load<RouteAsset>(RoutePath);
                activityA = Load<ActivityAsset>(AActivityPath);
                activityB = Load<ActivityAsset>(BActivityPath);
                p1 = Load<PlayerSlotProfile>(P1Path);
                GameObject root = new GameObject("IF-ADR-036 Route Content");
                RouteContentContribution contribution = root.AddComponent<RouteContentContribution>();
                Set(contribution, "route", route);
                Set(contribution, "localContentId", "qa-if-adr-036.route.main");
                PlayerSessionObserver observer = root.AddComponent<PlayerSessionObserver>();
                SetEnum(observer, "scope", "Route");
                QaPlayerSessionCommandTriggerSetup.EnsureOpenJoining(
                    root,
                    LocalPlayerProvisioningConsumerScope.Route);
                QaPlayerSessionCommandTriggerSetup.EnsureCloseJoining(
                    root,
                    LocalPlayerProvisioningConsumerScope.Route);
                PlayerSessionJoinTrigger join = root.AddComponent<PlayerSessionJoinTrigger>();
                SetEnum(join, "scope", "Route");
                GameObject requestBObject = new GameObject("Request Activity B");
                SceneManager.MoveGameObjectToScene(requestBObject, scene);
                ActivityRequestTrigger requestB = requestBObject.AddComponent<ActivityRequestTrigger>();
                requestB.TargetActivity = activityB;
                GameObject requestAObject = new GameObject("Request Activity A");
                SceneManager.MoveGameObjectToScene(requestAObject, scene);
                ActivityRequestTrigger requestA = requestAObject.AddComponent<ActivityRequestTrigger>();
                requestA.TargetActivity = activityA;
                PauseRequestTrigger pause = root.AddComponent<PauseRequestTrigger>();
                QaIfAdr036GameplayReadinessTransitionScenario scenario =
                    new GameObject("IF-ADR-036 GameplayReady Transition/Pause Proof").AddComponent<QaIfAdr036GameplayReadinessTransitionScenario>();
                SceneManager.MoveGameObjectToScene(scenario.gameObject, scene);
                Set(scenario, "gameApplication", app);
                Set(scenario, "activityA", activityA);
                Set(scenario, "activityB", activityB);
                Set(scenario, "playerSlot", p1);
                Set(scenario, "routeObserver", observer);
                Set(scenario, "joinTrigger", join);
                Set(scenario, "requestA", requestA);
                Set(scenario, "requestB", requestB);
                Set(scenario, "pauseTrigger", pause);
                if (contribution.Route != route || requestA.TargetActivity != activityA || requestB.TargetActivity != activityB)
                    throw new InvalidOperationException("Generated Route requests/contribution do not retain their typed targets.");
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, RouteScenePath)) throw new InvalidOperationException("Could not save Route scene.");
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(previous); }
        }

        private static void ValidatePersisted(GameApplicationAsset app, RouteAsset route, ActivityAsset activityA,
            ActivityAsset activityB, PlayerSlotProfile p1)
        {
            app = Load<GameApplicationAsset>(AppPath);
            route = Load<RouteAsset>(RoutePath);
            activityA = Load<ActivityAsset>(AActivityPath);
            activityB = Load<ActivityAsset>(BActivityPath);
            p1 = Load<PlayerSlotProfile>(P1Path);
            ActivityContentProfileAsset profileA = Load<ActivityContentProfileAsset>(AProfilePath);
            ActivityContentProfileAsset profileB = Load<ActivityContentProfileAsset>(BProfilePath);
            RequireAsset<GameApplicationAsset>(AppPath, app);
            RequireAsset<RouteAsset>(RoutePath, route);
            RequireAsset<ActivityAsset>(AActivityPath, activityA);
            RequireAsset<ActivityAsset>(BActivityPath, activityB);
            RequireAsset<PlayerSlotProfile>(P1Path, p1);
            RequireAsset<ActivityContentProfileAsset>(AProfilePath, profileA);
            RequireAsset<ActivityContentProfileAsset>(BProfilePath, profileB);
            RequireAsset<SceneAsset>(PersistentPath, AssetDatabase.LoadAssetAtPath<SceneAsset>(PersistentPath));
            RequireAsset<SceneAsset>(RouteScenePath, AssetDatabase.LoadAssetAtPath<SceneAsset>(RouteScenePath));
            RequireAsset<SceneAsset>(AScenePath, AssetDatabase.LoadAssetAtPath<SceneAsset>(AScenePath));
            RequireAsset<SceneAsset>(BScenePath, AssetDatabase.LoadAssetAtPath<SceneAsset>(BScenePath));

            RequireReference(AppPath, "startupRoute", route, app.StartupRoute);
            RequireReference(RoutePath, "startupActivity", activityA, route.StartupActivity);
            RequireEqual(RoutePath, "primaryScenePath", RouteScenePath, route.PrimaryScenePath);
            RequireEqual(RoutePath, "primarySceneName", "QA_IF_ADR_036_Route", route.PrimarySceneName);
            RequireEqual(AppPath, "persistentContent.scenePath", PersistentPath, app.PersistentContent.ContainerScenePath);
            RequireEqual(AppPath, "persistentContent.sceneName", "QA_IF_ADR_036_Persistent",
                app.PersistentContent.ContainerSceneName);
            RequireReference(AppPath, "persistentContent.containerScene",
                AssetDatabase.LoadAssetAtPath<SceneAsset>(PersistentPath), app.PersistentContent.ContainerScene);
            RequireEqual(AActivityPath, "playerParticipationRequirementLevel",
                PlayerParticipationRequirementLevel.GameplayReady, activityA.PlayerParticipationRequirementLevel);
            RequireEqual(AActivityPath, "visualTransitionMode", ActivityVisualTransitionMode.Fade, activityA.VisualTransitionMode);
            RequireReference(AActivityPath, "activityContentProfile", profileA, activityA.ActivityContentProfile);
            RequireEqual(AProfilePath, "scenes.Array.size", 1, profileA.SceneCount);
            RequireEqual(AProfilePath, "scenes[0].scenePath", AScenePath, profileA.Scenes[0].ScenePath);
            RequireEqual(BActivityPath, "activityEntryReadinessPolicy", ActivityEntryReadinessPolicy.WaitVisible,
                activityB.EntryReadinessPolicy);
            RequireEqual(BActivityPath, "visualTransitionMode", ActivityVisualTransitionMode.Fade, activityB.VisualTransitionMode);
            RequireEqual(BActivityPath, "transitionGateMode", TransitionGateMode.InputInteractionAndGameplay,
                activityB.TransitionGateMode);
            RequireEqual(BActivityPath, "playerParticipationRequirementLevel",
                PlayerParticipationRequirementLevel.GameplayReady, activityB.PlayerParticipationRequirementLevel);
            RequireEqual(BActivityPath, "playerParticipationProjectionMode",
                ActivityParticipationProjectionMode.ExplicitSlots, activityB.PlayerParticipationProjectionMode);
            RequireEqual(BActivityPath, "playerParticipationExplicitSlotProfiles.Array.size", 1,
                activityB.PlayerParticipationExplicitSlotProfiles.Count);
            RequireReference(BActivityPath, "playerParticipationExplicitSlotProfiles[0]", p1,
                activityB.PlayerParticipationExplicitSlotProfiles[0]);
            RequireEqual(BProfilePath, "scenes.Array.size", 1, profileB.SceneCount);
            RequireEqual(BProfilePath, "scenes[0].scenePath", BScenePath, profileB.Scenes[0].ScenePath);
            RequireDifferentGuid(AActivitySourcePath, AActivityPath);
            RequireDifferentGuid(AProfileSourcePath, AProfilePath);
            RequireDifferentGuid(ASceneSourcePath, AScenePath);
            RequireBuildScene(PersistentPath);
            RequireBuildScene(RouteScenePath);
            RequireBuildScene(AScenePath);
            RequireBuildScene(BScenePath);

            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene persistentScene = OpenOrGet(PersistentPath);
                Scene routeScene = OpenOrGet(RouteScenePath);
                Scene aScene = OpenOrGet(AScenePath);
                Scene bScene = OpenOrGet(BScenePath);
                UnityFadeCurtainEffectAdapter[] transitionAdapters =
                    Components<UnityFadeCurtainEffectAdapter>(persistentScene);
                Canvas[] transitionCanvases = Components<Canvas>(persistentScene);
                Image[] transitionImages = Components<Image>(persistentScene);
                RequireEqual(PersistentPath, "UnityFadeCurtainEffectAdapter count", 1, transitionAdapters.Length);
                RequireEqual(PersistentPath, "UnityFadeCurtainEffectAdapter[0].HasCanvasGroup", true,
                    transitionAdapters[0].HasCanvasGroup);
                RequireEqual(PersistentPath, "Canvas count", 1, transitionCanvases.Length);
                RequireEqual(PersistentPath, "Canvas[0].renderMode", RenderMode.ScreenSpaceOverlay,
                    transitionCanvases[0].renderMode);
                RequireEqual(PersistentPath, "Image count", 1, transitionImages.Length);

                ActivityContentContribution[] aContributions = Components<ActivityContentContribution>(aScene);
                PlayerSessionObserver[] aObservers = Components<PlayerSessionObserver>(aScene);
                RequireEqual(AScenePath, "ActivityContentContribution count", 1, aContributions.Length);
                RequireReference(AScenePath, "ActivityContentContribution[0].activity", activityA,
                    aContributions[0].Activity);
                RequireEqual(AScenePath, "PlayerSessionObserver count", 1, aObservers.Length);
                RequireEqual(AScenePath, "PlayerSessionObserver[0].scope", LocalPlayerProvisioningConsumerScope.Activity,
                    aObservers[0].Scope);

                RouteContentContribution[] routeContributions = Components<RouteContentContribution>(routeScene);
                PlayerSessionObserver[] routeObservers = Components<PlayerSessionObserver>(routeScene);
                PlayerSessionJoinTrigger[] joins = Components<PlayerSessionJoinTrigger>(routeScene);
                ActivityRequestTrigger[] requests = Components<ActivityRequestTrigger>(routeScene);
                PauseRequestTrigger[] pauses = Components<PauseRequestTrigger>(routeScene);
                QaIfAdr036GameplayReadinessTransitionScenario[] scenarios = Components<QaIfAdr036GameplayReadinessTransitionScenario>(routeScene);
                RequireEqual(RouteScenePath, "RouteContentContribution count", 1, routeContributions.Length);
                RequireReference(RouteScenePath, "RouteContentContribution[0].route", route,
                    routeContributions[0].Route);
                RequireEqual(RouteScenePath, "PlayerSessionObserver count", 1, routeObservers.Length);
                RequireEqual(RouteScenePath, "PlayerSessionObserver[0].scope", LocalPlayerProvisioningConsumerScope.Route,
                    routeObservers[0].Scope);
                RequireEqual(RouteScenePath, "PlayerSessionJoinTrigger count", 1, joins.Length);
                RequireEqual(RouteScenePath, "PlayerSessionJoinTrigger[0].scope", LocalPlayerProvisioningConsumerScope.Route,
                    joins[0].Scope);
                RequireEqual(RouteScenePath, "ActivityRequestTrigger count", 2, requests.Length);
                RequireEqual(RouteScenePath, "ActivityRequestTrigger targeting Activity A count", 1,
                    requests.Count(value => value.TargetActivity == activityA));
                RequireEqual(RouteScenePath, "ActivityRequestTrigger targeting Activity B count", 1,
                    requests.Count(value => value.TargetActivity == activityB));
                RequireEqual(RouteScenePath, "PauseRequestTrigger count", 1, pauses.Length);
                RequireEqual(RouteScenePath, "QA scenario count", 1, scenarios.Length);
                SerializedObject scenario = new SerializedObject(scenarios[0]);
                RequireReference(RouteScenePath, "Scenario.gameApplication", app,
                    Required(scenario, "gameApplication").objectReferenceValue);
                RequireReference(RouteScenePath, "Scenario.activityA", activityA,
                    Required(scenario, "activityA").objectReferenceValue);
                RequireReference(RouteScenePath, "Scenario.activityB", activityB,
                    Required(scenario, "activityB").objectReferenceValue);
                RequireReference(RouteScenePath, "Scenario.playerSlot", p1,
                    Required(scenario, "playerSlot").objectReferenceValue);
                RequireReference(RouteScenePath, "Scenario.routeObserver", routeObservers[0],
                    Required(scenario, "routeObserver").objectReferenceValue);
                RequireReference(RouteScenePath, "Scenario.joinTrigger", joins[0],
                    Required(scenario, "joinTrigger").objectReferenceValue);
                RequireReference(RouteScenePath, "Scenario.requestA", requests.Single(value => value.TargetActivity == activityA),
                    Required(scenario, "requestA").objectReferenceValue);
                RequireReference(RouteScenePath, "Scenario.requestB", requests.Single(value => value.TargetActivity == activityB),
                    Required(scenario, "requestB").objectReferenceValue);
                RequireReference(RouteScenePath, "Scenario.pauseTrigger", pauses[0],
                    Required(scenario, "pauseTrigger").objectReferenceValue);

                ActivityContentContribution[] contributions = Components<ActivityContentContribution>(bScene);
                PlayerSessionObserver[] bObservers = Components<PlayerSessionObserver>(bScene);
                ActivityReadinessParticipant[] participants = Components<ActivityReadinessParticipant>(bScene);
                RequireEqual(BScenePath, "ActivityContentContribution count", 1, contributions.Length);
                RequireReference(BScenePath, "ActivityContentContribution[0].activity", activityB,
                    contributions[0].Activity);
                RequireEqual(BScenePath, "PlayerSessionObserver count", 1, bObservers.Length);
                RequireEqual(BScenePath, "PlayerSessionObserver[0].scope", LocalPlayerProvisioningConsumerScope.Activity,
                    bObservers[0].Scope);
                RequireEqual(BScenePath, "ActivityReadinessParticipant count", 1, participants.Length);
                RequireEqual(BScenePath, "ActivityReadinessParticipant[0].participantId",
                    "qa-if-adr-036.activity-b.required", participants[0].ParticipantId);
                RequireEqual(BScenePath, "ActivityReadinessParticipant[0].requiredness",
                    ActivityContentExecutionRequiredness.Required, participants[0].Requiredness);
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(previous); }
        }

        private static Scene OpenOrGet(string path)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            return scene.IsValid() && scene.isLoaded ? scene : EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }

        private static T[] Components<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        private static void RequireAsset<T>(string path, T asset) where T : Object
        {
            if (asset == null)
                throw new InvalidOperationException("IF-ADR-036 postflight asset mismatch: asset='" + path +
                    "' field='<asset>' expected='imported " + typeof(T).Name + "' observed='<missing>'.");
        }

        private static void RequireEqual<T>(string assetPath, string field, T expected, T observed)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, observed))
                throw new InvalidOperationException("IF-ADR-036 postflight mismatch: asset='" + assetPath + "' field='" + field +
                    "' expected='" + Describe(expected) + "' observed='" + Describe(observed) + "'.");
        }

        private static void RequireReference(string assetPath, string field, Object expected, Object observed)
        {
            if (expected != observed)
                throw new InvalidOperationException("IF-ADR-036 postflight reference mismatch: asset='" + assetPath + "' field='" + field +
                    "' expected='" + DescribeReference(expected) + "' observed='" + DescribeReference(observed) + "'.");
        }

        private static void RequireDifferentGuid(string sourcePath, string generatedPath)
        {
            string sourceGuid = AssetDatabase.AssetPathToGUID(sourcePath);
            string generatedGuid = AssetDatabase.AssetPathToGUID(generatedPath);
            if (string.IsNullOrEmpty(sourceGuid) || string.IsNullOrEmpty(generatedGuid) || sourceGuid == generatedGuid)
                throw new InvalidOperationException("IF-ADR-036 postflight GUID mismatch: asset='" + generatedPath +
                    "' field='independent GUID' expected='non-empty GUID distinct from source " + sourcePath +
                    " ('" + sourceGuid + "')' observed='" + generatedGuid + "'.");
        }

        private static void RequireBuildScene(string path)
        {
            EditorBuildSettingsScene[] matches = EditorBuildSettings.scenes.Where(value => value.path == path).ToArray();
            RequireEqual("ProjectSettings/EditorBuildSettings.asset", path + " scene entry count", 1, matches.Length);
            RequireEqual("ProjectSettings/EditorBuildSettings.asset", path + " enabled", true, matches[0].enabled);
        }

        private static string Describe<T>(T value) => ReferenceEquals(value, null) ? "<null>" : value.ToString();

        private static string DescribeReference(Object value)
        {
            if (value == null) return "<null>";
            string path = AssetDatabase.GetAssetPath(value);
            return value.GetType().Name + " path='" + path + "' guid='" +
                (string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path)) + "'";
        }

        private static T CopyOrLoad<T>(string source, string target) where T : Object
        {
            if (AssetDatabase.LoadMainAssetAtPath(source) == null)
                throw new InvalidOperationException("IF-ADR-036 setup source asset missing: path='" + source +
                    "' expected='imported source asset' observed='<missing>'.");
            T asset = AssetDatabase.LoadAssetAtPath<T>(target);
            if (asset != null) return asset;
            Object existingTarget = AssetDatabase.LoadMainAssetAtPath(target);
            if (existingTarget != null)
                throw new InvalidOperationException("IF-ADR-036 setup target type mismatch: asset='" + target +
                    "' field='<asset>' expected='" + typeof(T).Name + "' observed='" + existingTarget.GetType().Name + "'.");
            EnsureParent(target);
            if (!AssetDatabase.CopyAsset(source, target)) throw new InvalidOperationException("Asset copy failed: " + source + " -> " + target);
            AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceUpdate);
            asset = AssetDatabase.LoadAssetAtPath<T>(target);
            if (asset == null) throw new InvalidOperationException("Copied asset did not import as " + typeof(T).Name + ": '" + target + "'; source='" + source + "'.");
            return asset;
        }

        private static void SetSceneEntry(ActivityContentProfileAsset profile, string path, string contentId)
        {
            SerializedObject so = new SerializedObject(profile); so.Update();
            SerializedProperty scenes = Required(so, "scenes"); scenes.arraySize = 1;
            SerializedProperty entry = scenes.GetArrayElementAtIndex(0);
            Required(entry, "contentId").stringValue = contentId;
            Required(entry, "scenePath").stringValue = path;
            Required(entry, "sceneName").stringValue = System.IO.Path.GetFileNameWithoutExtension(path);
            SetEnum(Required(entry, "requiredness"), "Required");
            SetEnum(Required(entry, "loadMode"), "Additive");
            SetEnum(Required(entry, "releasePolicy"), "ReleaseOnActivityChange");
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(profile);
        }

        private static void SetSingleExplicitSlot(ActivityAsset activity, PlayerSlotProfile playerSlot)
        {
            SerializedObject so = new SerializedObject(activity); so.Update();
            SetEnum(Required(so, "playerParticipationProjectionMode"), "ExplicitSlots");
            SerializedProperty slots = Required(so, "playerParticipationExplicitSlotProfiles");
            slots.arraySize = 1;
            slots.GetArrayElementAtIndex(0).objectReferenceValue = playerSlot;
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(activity);
        }

        private static void Set(Object target, string property, string value)
        {
            SerializedObject so = new SerializedObject(target); so.Update(); Required(so, property).stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
        }
        private static void Set(Object target, string property, Object value)
        {
            SerializedObject so = new SerializedObject(target); so.Update(); Required(so, property).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
        }
        private static void SetNested(Object target, string property, string value)
        {
            SerializedObject so = new SerializedObject(target); so.Update(); Required(so, property).stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
        }
        private static void SetNested(Object target, string property, Object value)
        {
            SerializedObject so = new SerializedObject(target); so.Update(); Required(so, property).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
        }
        private static void SetEnum(Object target, string property, string value)
        {
            SerializedObject so = new SerializedObject(target); so.Update(); SetEnum(Required(so, property), value);
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
        }
        private static void SetEnum(SerializedProperty property, string value)
        {
            int index = Array.IndexOf(property.enumNames, value);
            if (index < 0) throw new InvalidOperationException("Serialized enum value is missing: " + value + " on " + property.propertyPath);
            property.enumValueIndex = index;
        }
        private static SerializedProperty Required(SerializedObject so, string path) =>
            so.FindProperty(path) ?? throw new InvalidOperationException("Serialized field is missing: " + path);
        private static SerializedProperty Required(SerializedProperty parent, string path) =>
            parent.FindPropertyRelative(path) ?? throw new InvalidOperationException("Serialized field is missing: " + parent.propertyPath + "." + path);

        private static void EnsureFolder(string parent, string child)
        { if (!AssetDatabase.IsValidFolder(parent + "/" + child)) AssetDatabase.CreateFolder(parent, child); }
        private static void EnsureParent(string assetPath)
        {
            string parent = System.IO.Path.GetDirectoryName(assetPath).Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(parent)) return;
            string grand = System.IO.Path.GetDirectoryName(parent).Replace('\\', '/');
            EnsureFolder(grand, System.IO.Path.GetFileName(parent));
        }
        private static void AddToBuildSettings(string path)
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            int index = scenes.FindIndex(value => value.path == path);
            if (index >= 0) scenes[index] = new EditorBuildSettingsScene(path, true);
            else scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
        private static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);
    }
}
