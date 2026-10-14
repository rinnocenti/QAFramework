using System;
using System.IO;
using System.Collections.Generic;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RouteLifecycle;
using Immersive.QaFramework.IfAdr046;
using Immersive.QaFramework.PlayerSession.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.QaFramework.IfAdr046.Editor
{
    public static class QaIfAdr046ScopedBindingSetup
    {
        private const string Root = "Assets/QA-IF-ADR-046/OccurrenceBinding";
        private const string Settings = Root + "/Settings";
        private const string Scenes = Root + "/Scenes";
        private const string SourceSettings = "Assets/QA-IF-ADR-046/Settings";
        private const string SourceScenes = "Assets/QA-IF-ADR-046/Scenes";

        private const string AppPath = Settings + "/GameApplication_I4_ScopedBinding.asset";
        private const string RouteAPath = Settings + "/Route_I4_Binding_A.asset";
        private const string RouteBPath = Settings + "/Route_I4_Binding_B.asset";
        private const string ActivityAPath = Settings + "/Activity_I4_Binding_A.asset";
        private const string ActivityBPath = Settings + "/Activity_I4_Binding_B.asset";
        private const string ProfileAPath = Settings + "/ActivityContentProfile_I4_Binding_A.asset";
        private const string ProfileBPath = Settings + "/ActivityContentProfile_I4_Binding_B.asset";

        private const string PersistentScenePath = Scenes + "/I4_Binding_Persistent.unity";
        private const string RouteAScenePath = Scenes + "/I4_Binding_Route_A.unity";
        private const string RouteBScenePath = Scenes + "/I4_Binding_Route_B.unity";
        private const string ActivityAScenePath = Scenes + "/I4_Binding_Activity_A.unity";
        private const string ActivityBScenePath = Scenes + "/I4_Binding_Activity_B.unity";

        [MenuItem("Immersive Framework/QA/Player/IF-ADR-046 I4/Configure Occurrence Binding Fixture")]
        public static void Configure()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EnsureFolder("Assets/QA-IF-ADR-046", "OccurrenceBinding");
                EnsureFolder(Root, "Settings");
                EnsureFolder(Root, "Scenes");

                PreflightCopies();
                CopyOwnedAsset<GameApplicationAsset>(SourceSettings + "/GameApplication_I4.asset", AppPath);
                CopyOwnedAsset<RouteAsset>(SourceSettings + "/Route_I4_A.asset", RouteAPath);
                CopyOwnedAsset<RouteAsset>(SourceSettings + "/Route_I4_A.asset", RouteBPath);
                CopyOwnedAsset<ActivityAsset>(SourceSettings + "/Activity_I4_A.asset", ActivityAPath);
                CopyOwnedAsset<ActivityAsset>(SourceSettings + "/Activity_I4_A.asset", ActivityBPath);
                CopyOwnedAsset<ActivityContentProfileAsset>(SourceSettings + "/ActivityContentProfile_I4_A.asset", ProfileAPath);
                CopyOwnedAsset<ActivityContentProfileAsset>(SourceSettings + "/ActivityContentProfile_I4_A.asset", ProfileBPath);
                CopyOwnedAsset<SceneAsset>(SourceScenes + "/I4_Persistent.unity", PersistentScenePath);
                CopyOwnedAsset<SceneAsset>(SourceScenes + "/I4_Route_A.unity", RouteAScenePath);
                CopyOwnedAsset<SceneAsset>(SourceScenes + "/I4_Route_A.unity", RouteBScenePath);
                CopyOwnedAsset<SceneAsset>(SourceScenes + "/I4_Activity_A.unity", ActivityAScenePath);
                CopyOwnedAsset<SceneAsset>(SourceScenes + "/I4_Activity_A.unity", ActivityBScenePath);

                ConfigureAssets();
                AssetDatabase.SaveAssets();
                ConfigureRouteScene(RouteAScenePath, RouteAPath, LocalPlayerProvisioningConsumerScope.Route, true);
                ConfigureRouteScene(RouteBScenePath, RouteBPath, LocalPlayerProvisioningConsumerScope.Route, false);
                ConfigureActivityScene(ActivityAScenePath, ActivityAPath, true);
                ConfigureActivityScene(ActivityBScenePath, ActivityBPath, false);
                ConfigurePersistentScene();
                AssetDatabase.SaveAssets();
                EnsureScenesInBuildSettings();
                ValidatePersistedGraph();
                Debug.Log("[QA-IF-ADR-046-I4.2-SETUP] setup='Configured' fixture='occurrence-binding' scenes='5/5-registered' minimal='independent' slice1='untouched' execution='NOT_RUN'. Select GameApplication_I4_ScopedBinding in ImmersiveFrameworkSettings and open I4_Binding_Persistent to enter Play Mode.");
            }
            catch (Exception exception)
            {
                Debug.LogError("[QA-IF-ADR-046-I4.2-SETUP] setup='Failed' fixture='occurrence-binding' stage='configure-or-preflight' execution='NOT_RUN' issue='" + exception.Message.Replace("'", "\\'") + "'");
                throw;
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(previous);
            }
        }

        private static void PreflightCopies()
        {
            RequireSource<GameApplicationAsset>(SourceSettings + "/GameApplication_I4.asset");
            RequireSource<RouteAsset>(SourceSettings + "/Route_I4_A.asset");
            RequireSource<ActivityAsset>(SourceSettings + "/Activity_I4_A.asset");
            RequireSource<ActivityContentProfileAsset>(SourceSettings + "/ActivityContentProfile_I4_A.asset");
            RequireSource<SceneAsset>(SourceScenes + "/I4_Persistent.unity");
            RequireSource<SceneAsset>(SourceScenes + "/I4_Route_A.unity");
            RequireSource<SceneAsset>(SourceScenes + "/I4_Activity_A.unity");

            PreflightDestination<GameApplicationAsset>(AppPath);
            PreflightDestination<RouteAsset>(RouteAPath, RouteBPath);
            PreflightDestination<ActivityAsset>(ActivityAPath, ActivityBPath);
            PreflightDestination<ActivityContentProfileAsset>(ProfileAPath, ProfileBPath);
            PreflightDestination<SceneAsset>(PersistentScenePath, RouteAScenePath, RouteBScenePath, ActivityAScenePath, ActivityBScenePath);
        }

        private static void ConfigureAssets()
        {
            GameApplicationAsset app = Load<GameApplicationAsset>(AppPath);
            RouteAsset routeA = Load<RouteAsset>(RouteAPath);
            RouteAsset routeB = Load<RouteAsset>(RouteBPath);
            ActivityAsset activityA = Load<ActivityAsset>(ActivityAPath);
            ActivityAsset activityB = Load<ActivityAsset>(ActivityBPath);
            ActivityContentProfileAsset profileA = Load<ActivityContentProfileAsset>(ProfileAPath);
            ActivityContentProfileAsset profileB = Load<ActivityContentProfileAsset>(ProfileBPath);

            SetReference(app, "startupRoute", routeA);
            SerializedObject appObject = new SerializedObject(app);
            Require(appObject, "applicationName").stringValue = "QA IF-ADR-046 I4 Occurrence Binding";
            SerializedProperty persistent = Require(appObject, "persistentContent");
            Require(persistent, "scenePath").stringValue = PersistentScenePath;
            Require(persistent, "sceneName").stringValue = "I4_Binding_Persistent";
            appObject.ApplyModifiedPropertiesWithoutUndo();

            ConfigureRoute(routeA, "qa.if-adr-046.i4.binding.route-a", "QA IF-ADR-046 I4 Binding Route A", RouteAScenePath, "I4_Binding_Route_A", activityA);
            ConfigureRoute(routeB, "qa.if-adr-046.i4.binding.route-b", "QA IF-ADR-046 I4 Binding Route B", RouteBScenePath, "I4_Binding_Route_B", activityB);
            ConfigureActivity(activityA, "qa.if-adr-046.i4.binding.activity-a", "QA IF-ADR-046 I4 Binding Activity A", ProfileAPath, profileA);
            ConfigureActivity(activityB, "qa.if-adr-046.i4.binding.activity-b", "QA IF-ADR-046 I4 Binding Activity B", ProfileBPath, profileB);
            ConfigureProfile(profileA, "qa.if-adr-046.i4.binding.activity-a-content", ActivityAScenePath, "I4_Binding_Activity_A");
            ConfigureProfile(profileB, "qa.if-adr-046.i4.binding.activity-b-content", ActivityBScenePath, "I4_Binding_Activity_B");
        }

        private static void ConfigureRoute(RouteAsset route, string id, string name, string scenePath, string sceneName, ActivityAsset startupActivity)
        {
            SerializedObject serialized = new SerializedObject(route);
            Require(serialized, "routeId").stringValue = id;
            Require(serialized, "routeName").stringValue = name;
            Require(serialized, "primaryScenePath").stringValue = scenePath;
            Require(serialized, "primarySceneName").stringValue = sceneName;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SetReference(route, "startupActivity", startupActivity);
        }

        private static void ConfigureActivity(ActivityAsset activity, string id, string name, string profilePath, ActivityContentProfileAsset profile)
        {
            SerializedObject serialized = new SerializedObject(activity);
            Require(serialized, "activityId").stringValue = id;
            Require(serialized, "activityName").stringValue = name;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SetReference(activity, "activityContentProfile", profile);
        }

        private static void ConfigureProfile(ActivityContentProfileAsset profile, string id, string scenePath, string sceneName)
        {
            SerializedObject serialized = new SerializedObject(profile);
            Require(serialized, "profileId").stringValue = id;
            SerializedProperty entries = Require(serialized, "scenes");
            if (entries.arraySize != 1) throw new InvalidOperationException("I4.2 profile must contain exactly one content scene: " + AssetDatabase.GetAssetPath(profile));
            SerializedProperty entry = entries.GetArrayElementAtIndex(0);
            Require(entry, "scenePath").stringValue = scenePath;
            Require(entry, "sceneName").stringValue = sceneName;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureRouteScene(string path, string routePath, LocalPlayerProvisioningConsumerScope scope, bool requireJoin)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            RouteContentContribution[] contributions = FindComponents<RouteContentContribution>(scene);
            if (contributions.Length != 1) throw new InvalidOperationException($"I4.2 Route scene '{path}' needs exactly one RouteContentContribution; found {contributions.Length}.");
            SetReference(contributions[0], "route", Load<RouteAsset>(routePath));

            RouteContentLifecycleEvents[] lifecycleObservers = FindComponents<RouteContentLifecycleEvents>(scene);
            if (lifecycleObservers.Length > 1) throw new InvalidOperationException($"I4.2 Route scene '{path}' has duplicate RouteContentLifecycleEvents observers.");
            if (lifecycleObservers.Length == 0) contributions[0].gameObject.AddComponent<RouteContentLifecycleEvents>();

            PlayerSessionJoinTrigger[] existing = FindComponents<PlayerSessionJoinTrigger>(scene);
            if (requireJoin)
            {
                PlayerSessionJoinTrigger trigger = existing.Length == 0 ? contributions[0].GetComponent<PlayerSessionJoinTrigger>() : existing[0];
                if (trigger == null) trigger = contributions[0].gameObject.AddComponent<PlayerSessionJoinTrigger>();
                if (existing.Length > 1) throw new InvalidOperationException($"I4.2 Route scene '{path}' has duplicate Join triggers.");
                ConfigureJoinTrigger(trigger, scope);
            }
            else if (existing.Length != 0)
            {
                throw new InvalidOperationException($"I4.2 Route B must not contain a Join trigger; found {existing.Length} at '{path}'.");
            }
            SaveScene(scene, path);
        }

        private static void ConfigureActivityScene(string path, string activityPath, bool requireJoin)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            ActivityContentContribution[] contributions = FindComponents<ActivityContentContribution>(scene);
            PlayerSessionObserver[] observers = FindComponents<PlayerSessionObserver>(scene);
            ActivityReadinessEvents[] readinessObservers = FindComponents<ActivityReadinessEvents>(scene);
            if (contributions.Length != 1 || observers.Length != 1)
                throw new InvalidOperationException($"I4.2 Activity scene '{path}' requires exactly one contribution and Session observer; contributions={contributions.Length}, observers={observers.Length}.");
            if (readinessObservers.Length > 1)
                throw new InvalidOperationException($"I4.2 Activity scene '{path}' has duplicate ActivityReadinessEvents observers.");
            SetReference(contributions[0], "activity", Load<ActivityAsset>(activityPath));

            foreach (QaIfAdr046JoinScenario oldScenario in FindComponents<QaIfAdr046JoinScenario>(scene)) UnityEngine.Object.DestroyImmediate(oldScenario);
            foreach (PlayerSessionJoinTrigger oldTrigger in FindComponents<PlayerSessionJoinTrigger>(scene)) UnityEngine.Object.DestroyImmediate(oldTrigger);

            if (readinessObservers.Length == 0) observers[0].gameObject.AddComponent<ActivityReadinessEvents>();
            if (requireJoin) ConfigureJoinTrigger(observers[0].gameObject.AddComponent<PlayerSessionJoinTrigger>(), LocalPlayerProvisioningConsumerScope.Activity);
            PlayerSlotProfile playerSlot = Load<GameApplicationAsset>(AppPath)
                .DefaultPlayerSessionProfile.SupportedSlots[0];
            QaPlayerSessionCommandTriggerSetup.EnsureOpenJoining(
                observers[0].gameObject,
                LocalPlayerProvisioningConsumerScope.Activity);
            QaPlayerSessionCommandTriggerSetup.EnsureCloseJoining(
                observers[0].gameObject,
                LocalPlayerProvisioningConsumerScope.Activity);
            QaPlayerSessionCommandTriggerSetup.EnsureLeave(
                observers[0].gameObject,
                "Leave Player 1",
                LocalPlayerProvisioningConsumerScope.Activity,
                playerSlot);
            SaveScene(scene, path);
        }

        private static void ConfigureJoinTrigger(PlayerSessionJoinTrigger trigger, LocalPlayerProvisioningConsumerScope scopeValue)
        {
            SerializedObject serialized = new SerializedObject(trigger);
            SerializedProperty scope = Require(serialized, "scope");
            scope.enumValueIndex = Array.IndexOf(scope.enumNames, scopeValue.ToString());
            if (scope.enumValueIndex < 0) throw new InvalidOperationException("I4.2 Join trigger scope is unavailable: " + scopeValue);
            Require(serialized, "controlScheme").stringValue = "QA.Keyboard";
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigurePersistentScene()
        {
            Scene scene = EditorSceneManager.OpenScene(PersistentScenePath, OpenSceneMode.Single);
            RouteRequestTrigger[] routes = FindComponents<RouteRequestTrigger>(scene);
            ActivityRequestTrigger[] activities = FindComponents<ActivityRequestTrigger>(scene);
            QaIfAdr046ScopedBindingScenario[] scenarios = FindComponents<QaIfAdr046ScopedBindingScenario>(scene);
            if (routes.Length > 2 || activities.Length > 2 || scenarios.Length > 1)
                throw new InvalidOperationException($"I4.2 Persistent scene has duplicate orchestration components: routes={routes.Length}, activities={activities.Length}, scenarios={scenarios.Length}.");

            RouteRequestTrigger toB = GetOrAddTrigger(routes, "QA I4.2 Route B");
            RouteRequestTrigger toA = GetOrAddTrigger(routes, "QA I4.2 Route A");
            toB.TargetRoute = Load<RouteAsset>(RouteBPath);
            toA.TargetRoute = Load<RouteAsset>(RouteAPath);

            ActivityRequestTrigger activityToB = GetOrAddActivityTrigger(activities, "QA I4.2 Activity B");
            ActivityRequestTrigger activityToA = GetOrAddActivityTrigger(activities, "QA I4.2 Activity A");
            activityToB.TargetActivity = Load<ActivityAsset>(ActivityBPath);
            activityToA.TargetActivity = Load<ActivityAsset>(ActivityAPath);

            QaIfAdr046ScopedBindingScenario scenario = scenarios.Length == 1
                ? scenarios[0]
                : new GameObject("QA IF-ADR-046 I4.2 Occurrence Binding Scenario").AddComponent<QaIfAdr046ScopedBindingScenario>();
            if (scenario.gameObject.scene != scene) SceneManager.MoveGameObjectToScene(scenario.gameObject, scene);
            SerializedObject serialized = new SerializedObject(scenario);
            Require(serialized, "routeA").objectReferenceValue = Load<RouteAsset>(RouteAPath);
            Require(serialized, "routeB").objectReferenceValue = Load<RouteAsset>(RouteBPath);
            Require(serialized, "activityA").objectReferenceValue = Load<ActivityAsset>(ActivityAPath);
            Require(serialized, "activityB").objectReferenceValue = Load<ActivityAsset>(ActivityBPath);
            Require(serialized, "routeToB").objectReferenceValue = toB;
            Require(serialized, "routeToA").objectReferenceValue = toA;
            Require(serialized, "activityToB").objectReferenceValue = activityToB;
            Require(serialized, "activityToA").objectReferenceValue = activityToA;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SaveScene(scene, PersistentScenePath);
        }

        private static RouteRequestTrigger GetOrAddTrigger(RouteRequestTrigger[] existing, string objectName)
        {
            foreach (RouteRequestTrigger trigger in existing)
                if (trigger.gameObject.name == objectName) return trigger;
            return new GameObject(objectName).AddComponent<RouteRequestTrigger>();
        }

        private static ActivityRequestTrigger GetOrAddActivityTrigger(ActivityRequestTrigger[] existing, string objectName)
        {
            foreach (ActivityRequestTrigger trigger in existing)
                if (trigger.gameObject.name == objectName) return trigger;
            return new GameObject(objectName).AddComponent<ActivityRequestTrigger>();
        }

        private static void EnsureScenesInBuildSettings()
        {
            QaIfAdr046Setup.EnsureScenesInBuildSettings(
                PersistentScenePath, RouteAScenePath, RouteBScenePath, ActivityAScenePath, ActivityBScenePath);
        }

        private static void ValidatePersistedGraph()
        {
            GameApplicationAsset app = Load<GameApplicationAsset>(AppPath);
            RouteAsset routeA = Load<RouteAsset>(RouteAPath);
            RouteAsset routeB = Load<RouteAsset>(RouteBPath);
            ActivityAsset activityA = Load<ActivityAsset>(ActivityAPath);
            ActivityAsset activityB = Load<ActivityAsset>(ActivityBPath);
            ActivityContentProfileAsset profileA = Load<ActivityContentProfileAsset>(ProfileAPath);
            ActivityContentProfileAsset profileB = Load<ActivityContentProfileAsset>(ProfileBPath);
            if (app == null || routeA == null || routeB == null || activityA == null || activityB == null || profileA == null || profileB == null ||
                !ReferenceEquals(app.StartupRoute, routeA) || !ReferenceEquals(routeA.StartupActivity, activityA) ||
                !ReferenceEquals(routeB.StartupActivity, activityB) || !ReferenceEquals(activityA.ActivityContentProfile, profileA) ||
                !ReferenceEquals(activityB.ActivityContentProfile, profileB) ||
                !string.Equals(routeA.PrimaryScenePath, RouteAScenePath, StringComparison.Ordinal) ||
                !string.Equals(routeA.PrimarySceneName, "I4_Binding_Route_A", StringComparison.Ordinal) ||
                !string.Equals(routeB.PrimaryScenePath, RouteBScenePath, StringComparison.Ordinal) ||
                !string.Equals(routeB.PrimarySceneName, "I4_Binding_Route_B", StringComparison.Ordinal) ||
                !string.Equals(profileA.Scenes[0].ScenePath, ActivityAScenePath, StringComparison.Ordinal) ||
                !string.Equals(profileA.Scenes[0].SceneName, "I4_Binding_Activity_A", StringComparison.Ordinal) ||
                !string.Equals(profileB.Scenes[0].ScenePath, ActivityBScenePath, StringComparison.Ordinal) ||
                !string.Equals(profileB.Scenes[0].SceneName, "I4_Binding_Activity_B", StringComparison.Ordinal) ||
                !string.Equals(app.PersistentContent.ContainerScenePath, PersistentScenePath, StringComparison.Ordinal) ||
                !string.Equals(app.PersistentContent.ContainerSceneName, "I4_Binding_Persistent", StringComparison.Ordinal))
                throw new InvalidOperationException("I4.2 pre-validation failed: GameApplication, startup Route, Route/Activity graph, or authored scene names/paths disagree.");
            if (ReferenceEquals(routeA, routeB) || ReferenceEquals(activityA, activityB) ||
                string.Equals(routeA.RouteId.StableText, routeB.RouteId.StableText, StringComparison.Ordinal) ||
                string.Equals(activityA.ActivityId.StableText, activityB.ActivityId.StableText, StringComparison.Ordinal))
                throw new InvalidOperationException("I4.2 pre-validation failed: Route/Activity A and B must be distinct definitions with distinct stable IDs.");

            QaIfAdr046Setup.ValidateSceneRegistration(PersistentScenePath, "I4_Binding_Persistent");
            QaIfAdr046Setup.ValidateSceneRegistration(RouteAScenePath, "I4_Binding_Route_A");
            QaIfAdr046Setup.ValidateSceneRegistration(RouteBScenePath, "I4_Binding_Route_B");
            QaIfAdr046Setup.ValidateSceneRegistration(ActivityAScenePath, "I4_Binding_Activity_A");
            QaIfAdr046Setup.ValidateSceneRegistration(ActivityBScenePath, "I4_Binding_Activity_B");
            ValidateSceneBindings();
        }

        private static void ValidateSceneBindings()
        {
            Scene routeA = EditorSceneManager.OpenScene(RouteAScenePath, OpenSceneMode.Single);
            RouteContentContribution[] routeContributions = FindComponents<RouteContentContribution>(routeA);
            RouteContentLifecycleEvents[] routeLifecycle = FindComponents<RouteContentLifecycleEvents>(routeA);
            PlayerSessionJoinTrigger[] routeJoins = FindComponents<PlayerSessionJoinTrigger>(routeA);
            if (routeContributions.Length != 1 || routeContributions[0].Route != Load<RouteAsset>(RouteAPath) ||
                routeLifecycle.Length != 1 || routeJoins.Length != 1 || routeJoins[0].Scope != LocalPlayerProvisioningConsumerScope.Route)
                throw new InvalidOperationException("I4.2 pre-validation failed: Route A scene must bind its exact Route asset, one public Route lifecycle observer, and one Route-scoped Join trigger.");

            Scene routeB = EditorSceneManager.OpenScene(RouteBScenePath, OpenSceneMode.Single);
            routeContributions = FindComponents<RouteContentContribution>(routeB);
            routeLifecycle = FindComponents<RouteContentLifecycleEvents>(routeB);
            routeJoins = FindComponents<PlayerSessionJoinTrigger>(routeB);
            if (routeContributions.Length != 1 || routeContributions[0].Route != Load<RouteAsset>(RouteBPath) || routeLifecycle.Length != 1 || routeJoins.Length != 0)
                throw new InvalidOperationException("I4.2 pre-validation failed: Route B scene must bind Route B, contain one public Route lifecycle observer, and contain no Join trigger.");

            ValidateActivityScene(ActivityAScenePath, ActivityAPath, true);
            ValidateActivityScene(ActivityBScenePath, ActivityBPath, false);

            Scene persistent = EditorSceneManager.OpenScene(PersistentScenePath, OpenSceneMode.Single);
            LocalPlayerProvisioningEndpointRegistration[] registration = FindComponents<LocalPlayerProvisioningEndpointRegistration>(persistent);
            LocalPlayerProvisioningAuthoring[] authoring = FindComponents<LocalPlayerProvisioningAuthoring>(persistent);
            UnityEngine.InputSystem.PlayerInputManager[] managers = FindComponents<UnityEngine.InputSystem.PlayerInputManager>(persistent);
            QaIfAdr046ScopedBindingScenario[] scenarios = FindComponents<QaIfAdr046ScopedBindingScenario>(persistent);
            RouteRequestTrigger[] routes = FindComponents<RouteRequestTrigger>(persistent);
            ActivityRequestTrigger[] activities = FindComponents<ActivityRequestTrigger>(persistent);
            if (registration.Length != 1 || authoring.Length != 1 || managers.Length != 1 || scenarios.Length != 1 ||
                routes.Length != 2 || activities.Length != 2 ||
                registration[0].ProvisioningAuthoring != authoring[0] || authoring[0].PlayerInputManager != managers[0] ||
                authoring[0].LocalPlayerHostPrefab == null || !authoring[0].IsManagerPrefabMaterialized)
                throw new InvalidOperationException("I4.2 pre-validation failed: Persistent Content Manager endpoint or exact orchestration cardinality is invalid.");
        }

        private static void ValidateActivityScene(string path, string activityPath, bool hasJoin)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            ActivityContentContribution[] contributions = FindComponents<ActivityContentContribution>(scene);
            PlayerSessionObserver[] observers = FindComponents<PlayerSessionObserver>(scene);
            PlayerSessionJoinTrigger[] joins = FindComponents<PlayerSessionJoinTrigger>(scene);
            ActivityReadinessEvents[] readiness = FindComponents<ActivityReadinessEvents>(scene);
            QaIfAdr046JoinScenario[] minimal = FindComponents<QaIfAdr046JoinScenario>(scene);
            if (contributions.Length != 1 || contributions[0].Activity != Load<ActivityAsset>(activityPath) ||
                observers.Length != 1 || readiness.Length != 1 || joins.Length != (hasJoin ? 1 : 0) || minimal.Length != 0 ||
                (hasJoin && joins[0].Scope != LocalPlayerProvisioningConsumerScope.Activity))
                throw new InvalidOperationException($"I4.2 pre-validation failed: Activity scene '{path}' has incorrect asset bindings, Session/readiness observers, Join, or minimal-regression components.");
        }

        private static void RequireSource<T>(string path) where T : UnityEngine.Object
        {
            if (Load<T>(path) == null) throw new InvalidOperationException($"I4.2 source asset is missing or has wrong type at '{path}'. Run Configure Minimal Join Fixture first.");
        }

        private static void PreflightDestination<T>(params string[] paths) where T : UnityEngine.Object
        {
            foreach (string path in paths)
                if (File.Exists(path) && Load<T>(path) == null)
                    throw new InvalidOperationException($"I4.2 refuses to overwrite unexpected content at '{path}'.");
        }

        private static void CopyOwnedAsset<T>(string source, string destination) where T : UnityEngine.Object
        {
            if (File.Exists(destination))
            {
                if (Load<T>(destination) == null) throw new InvalidOperationException("Unexpected content at I4.2-owned path: " + destination);
                return;
            }
            if (Load<T>(source) == null || !AssetDatabase.CopyAsset(source, destination))
                throw new InvalidOperationException($"Could not copy I4.2 source '{source}' to '{destination}'.");
        }

        private static void SaveScene(Scene scene, string path)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, path)) throw new InvalidOperationException("Could not save I4.2 scene: " + path);
        }

        private static T Load<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path);

        private static void SetReference(UnityEngine.Object target, string name, UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            Require(serialized, name).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static SerializedProperty Require(SerializedObject serialized, string name)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null) throw new InvalidOperationException("Required I4.2 serialized field is missing: " + name);
            return property;
        }

        private static SerializedProperty Require(SerializedProperty parent, string name)
        {
            SerializedProperty property = parent.FindPropertyRelative(name);
            if (property == null) throw new InvalidOperationException("Required I4.2 serialized field is missing: " + parent.propertyPath + "." + name);
            return property;
        }

        private static T[] FindComponents<T>(Scene scene) where T : Component
        {
            var found = new List<T>();
            foreach (GameObject root in scene.GetRootGameObjects()) found.AddRange(root.GetComponentsInChildren<T>(true));
            return found.ToArray();
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }
    }
}
