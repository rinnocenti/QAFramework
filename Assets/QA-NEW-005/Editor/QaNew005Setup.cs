using System.IO;
using Immersive.Audio.Authoring;
using Immersive.Audio.Unity.Hosts;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Audio;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.RouteLifecycle;
using Immersive.QaFramework.New005;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace Immersive.QaFramework.New005.Editor
{
    public static class QaNew005Setup
    {
        private const string Root = "Assets/QA-NEW-005";
        private const string Settings = Root + "/Settings";
        private const string Scenes = Root + "/Scenes";
        private const string Audio = Root + "/Audio";
        private const string CameraOutputPrefab = "Assets/QA-NEW-001/Camera/PF_CameraOutput_QaNew001.prefab";

        [MenuItem("Immersive Framework/QA/QA-NEW-005/Configure Audio BGM Continuity")]
        public static void Configure()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.Refresh();
            EnsureFolder("Assets", "QA-NEW-005");
            EnsureFolder(Root, "Settings");
            EnsureFolder(Root, "Scenes");
            EnsureFolder(Root, "Scripts");
            EnsureFolder(Root, "Audio");

            string wavPath = Audio + "/QaNew005Tone.wav";
            if (!File.Exists(wavPath)) WriteTone(wavPath);
            AssetDatabase.Refresh();
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(wavPath);
            if (clip == null) throw new System.InvalidOperationException("QA tone AudioClip did not import.");

            AudioDefaultsAsset defaults = CreateAsset<AudioDefaultsAsset>(Settings + "/AudioDefaults_QaNew005.asset");
            Set(defaults, "masterVolume", 1f); Set(defaults, "sfxVolume", 1f); Set(defaults, "bgmVolume", 1f);
            Set(defaults, "masterBus", "Master"); Set(defaults, "sfxBus", "SFX"); Set(defaults, "bgmBus", "BGM");
            Set(defaults, "defaultFadeInSeconds", 0f); Set(defaults, "defaultFadeOutSeconds", 0f);

            CreateCue("Cue_QaNew005_Route", "qa-new-005.route", clip);
            CreateCue("Cue_QaNew005_Activity", "qa-new-005.activity", clip);
            CreateCue("Cue_QaNew005_StartupActivity", "qa-new-005.startup-activity", clip);
            CreateCue("Cue_QaNew005_ContentlessStartup", "qa-new-005.contentless-startup", clip);

            CreateAsset<QaNew005AudioEvidence>(Settings + "/QaNew005AudioEvidence.asset");
            RouteAsset neutral = CreateRoute("Route_QaNew005_Neutral", "neutral", "Route Neutral", null);
            RouteAsset play = CreateRoute("Route_QaNew005_Play", "play", "Route PlayOwn", null);
            RouteAsset preserve = CreateRoute("Route_QaNew005_Preserve", "preserve", "Route Preserve", null);
            RouteAsset silence = CreateRoute("Route_QaNew005_Silence", "silence", "Route Silence", null);
            RouteAsset afterSilence = CreateRoute("Route_QaNew005_AfterSilence", "after-silence", "Route Play After Silence", null);

            ActivityAsset own = CreateActivity("Activity_QaNew005_Own", "activity-own", "Activity Own Cue");
            ActivityAsset neutralActivity = CreateActivity("Activity_QaNew005_Neutral", "activity-neutral", "Activity Neutral");
            ActivityAsset useRoute = CreateActivity("Activity_QaNew005_UseRoute", "activity-use-route", "Activity Use Route");
            ActivityAsset startupOwn = CreateActivity("Activity_QaNew005_StartupOwn", "activity-startup-own", "Startup Activity Own Cue");
            ActivityAsset startupEmpty = CreateActivityWithoutContent("Activity_QaNew005_StartupEmpty", "activity-startup-empty", "Startup Activity Without Content");

            RouteAsset startupWithCue = CreateRoute("Route_QaNew005_StartupCue", "startup-cue", "Route Startup Activity Cue", startupOwn);
            RouteAsset startupWithoutContent = CreateRoute("Route_QaNew005_StartupEmpty", "startup-empty", "Route Contentless Startup Activity", startupEmpty);
            ValidateSceneOwnership(
                new[] { neutral, play, preserve, silence, afterSilence, startupWithCue, startupWithoutContent },
                new[] { own, neutralActivity, useRoute, startupOwn });

            CreateRouteScene("Route_QaNew005_Neutral", false, null, FrameworkBgmRoutePolicy.PreserveCurrent);
            CreateRouteScene("Route_QaNew005_Play", true, "Cue_QaNew005_Route", FrameworkBgmRoutePolicy.PlayOwn);
            CreateRouteScene("Route_QaNew005_Preserve", true, null, FrameworkBgmRoutePolicy.PreserveCurrent);
            CreateRouteScene("Route_QaNew005_Silence", true, null, FrameworkBgmRoutePolicy.Silence);
            CreateRouteScene("Route_QaNew005_AfterSilence", true, "Cue_QaNew005_Route", FrameworkBgmRoutePolicy.PlayOwn);
            CreateRouteScene("Route_QaNew005_StartupCue", true, "Cue_QaNew005_Route", FrameworkBgmRoutePolicy.PlayOwn);
            CreateRouteScene("Route_QaNew005_StartupEmpty", true, "Cue_QaNew005_ContentlessStartup", FrameworkBgmRoutePolicy.PlayOwn);
            CreateActivityScene("Activity_QaNew005_Own", "Cue_QaNew005_Activity", FrameworkBgmActivityPolicy.UseOwnOrPreserveCurrent);
            CreateActivityScene("Activity_QaNew005_Neutral", null, FrameworkBgmActivityPolicy.UseOwnOrPreserveCurrent);
            CreateActivityScene("Activity_QaNew005_UseRoute", null, FrameworkBgmActivityPolicy.UseRoute);
            CreateActivityScene("Activity_QaNew005_StartupOwn", "Cue_QaNew005_StartupActivity", FrameworkBgmActivityPolicy.UseOwnOrPreserveCurrent);

            GameApplicationAsset application = CreateAsset<GameApplicationAsset>(Settings + "/GameApplication_QaNew005.asset");
            Set(application, "applicationName", "QA-NEW-005 Audio BGM Continuity");
            Set(application, "startupRoute", LoadAsset<RouteAsset>(Settings + "/Route_QaNew005_Neutral.asset"));
            Set(application, "playerSessionEnabled", false);
            SetCameraOutput(application);

            CreatePersistentScene();
            string persistentPath = Scenes + "/QA_NEW_005_Persistent.unity";

            application = LoadAsset<GameApplicationAsset>(Settings + "/GameApplication_QaNew005.asset");
            SerializedObject appObject = new SerializedObject(application);
            appObject.FindProperty("persistentContent.containerScene").objectReferenceValue = AssetDatabase.LoadAssetAtPath<SceneAsset>(persistentPath);
            appObject.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.Refresh();
            AddScenesToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[QA-NEW-005] setup='Configured' gameApplication='GameApplication_QaNew005' persistent='QA_NEW_005_Persistent' scenes='12' audio='4 explicit cues'. Select GameApplication_QaNew005 in ImmersiveFrameworkSettings, then enter a fresh Play Mode session.");
        }

        private static AudioBgmCueAsset CreateCue(string name, string id, AudioClip clip)
        {
            var cue = CreateAsset<AudioBgmCueAsset>(Settings + "/" + name + ".asset");
            Set(cue, "cueId", id); Set(cue, "clip", clip); Set(cue, "volume", 1f);
            Set(cue, "pitch", 1f); Set(cue, "loopMode", 1); Set(cue, "routingBus", "BGM");
            Set(cue, "fadeInSeconds", 0f); Set(cue, "fadeOutSeconds", 0f);
            return cue;
        }

        private static RouteAsset CreateRoute(string file, string id, string name, ActivityAsset startup)
        {
            string scenePath = Scenes + "/QA_NEW_005_" + file.Replace("Route_QaNew005_", "") + ".unity";
            var route = CreateAsset<RouteAsset>(Settings + "/" + file + ".asset");
            Set(route, "routeId", "qa-new-005." + id); Set(route, "routeName", name);
            Set(route, "primaryScenePath", scenePath); Set(route, "primarySceneName", Path.GetFileNameWithoutExtension(scenePath));
            Set(route, "startupActivity", startup);
            return route;
        }

        private static ActivityAsset CreateActivity(string file, string id, string name)
        {
            string scenePath = GetActivityScenePath(file);
            string profilePath = Settings + "/ActivityContentProfile_QaNew005_" + file.Replace("Activity_QaNew005_", "") + ".asset";
            ActivityContentProfileAsset profile = CreateAsset<ActivityContentProfileAsset>(profilePath);
            Set(profile, "profileId", "qa-new-005." + id + ".profile");
            SerializedObject profileObject = new SerializedObject(profile);
            SerializedProperty scenes = profileObject.FindProperty("scenes");
            scenes.arraySize = 1;
            SerializedProperty entry = scenes.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("contentId").stringValue = "qa-new-005." + id + ".content";
            entry.FindPropertyRelative("scenePath").stringValue = scenePath;
            entry.FindPropertyRelative("sceneName").stringValue = Path.GetFileNameWithoutExtension(scenePath);
            entry.FindPropertyRelative("requiredness").enumValueIndex = 1;
            entry.FindPropertyRelative("loadMode").enumValueIndex = 0;
            entry.FindPropertyRelative("releasePolicy").enumValueIndex = 0;
            profileObject.ApplyModifiedPropertiesWithoutUndo();

            var activity = CreateAsset<ActivityAsset>(Settings + "/" + file + ".asset");
            Set(activity, "activityId", "qa-new-005." + id); Set(activity, "activityName", name);
            Set(activity, "activityContentProfile", profile);
            return activity;
        }

        private static ActivityAsset CreateActivityWithoutContent(string file, string id, string name)
        {
            var activity = CreateAsset<ActivityAsset>(Settings + "/" + file + ".asset");
            Set(activity, "activityId", "qa-new-005." + id); Set(activity, "activityName", name);
            Set(activity, "activityContentProfile", (Object)null);
            return activity;
        }

        private static void CreateRouteScene(string routeAssetName, bool hasAuthoring,
            string cueAssetName, FrameworkBgmRoutePolicy policy)
        {
            SceneSetup[] previousSetup;
            Scene scene = BeginGeneratedScene(out previousSetup);
            string routeAssetPath = Settings + "/" + routeAssetName + ".asset";
            try
            {
                RouteAsset route = LoadAsset<RouteAsset>(routeAssetPath);
                QaNew005AudioEvidence evidence = LoadAsset<QaNew005AudioEvidence>(Settings + "/QaNew005AudioEvidence.asset");
                AudioBgmCueAsset cue = string.IsNullOrWhiteSpace(cueAssetName)
                    ? null
                    : LoadAsset<AudioBgmCueAsset>(Settings + "/" + cueAssetName + ".asset");
                string path = route.PrimaryScenePath;
                string routeSceneName = route.PrimarySceneName;
                string routeContentId = "qa-new-005." + route.name + ".content";
                var root = new GameObject(routeSceneName + " Content");
                RouteContentContribution contribution = root.AddComponent<RouteContentContribution>();
                Set(contribution, "localContentId", routeContentId);
                Set(contribution, "route", route);
                if (hasAuthoring)
                {
                    var authoringRoot = new GameObject("Route BGM Authoring");
                    authoringRoot.transform.SetParent(root.transform, false);
                    RouteBgmAuthoring authoring = authoringRoot.AddComponent<RouteBgmAuthoring>();
                    ConfigureRouteBgm(authoring, cue, policy);
                    var probeRoot = new GameObject("Route BGM Lifecycle Probe");
                    probeRoot.transform.SetParent(authoringRoot.transform, false);
                    QaNew005RouteBgmProbe probe = probeRoot.AddComponent<QaNew005RouteBgmProbe>();
                    Set(probe, "authoring", authoring); Set(probe, "evidence", evidence);
                }
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, path))
                    throw new System.InvalidOperationException($"QA-NEW-005 scene could not be saved to '{path}'.");
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }

            VerifyRouteScene(routeAssetPath, hasAuthoring, cueAssetName, policy);
        }

        private static void CreatePersistentScene()
        {
            SceneSetup[] previousSetup;
            Scene scene = BeginGeneratedScene(out previousSetup);
            AudioDefaultsAsset defaults = LoadAsset<AudioDefaultsAsset>(Settings + "/AudioDefaults_QaNew005.asset");
            QaNew005AudioEvidence evidence = LoadAsset<QaNew005AudioEvidence>(Settings + "/QaNew005AudioEvidence.asset");
            AudioBgmCueAsset routeCue = LoadAsset<AudioBgmCueAsset>(Settings + "/Cue_QaNew005_Route.asset");
            AudioBgmCueAsset activityCue = LoadAsset<AudioBgmCueAsset>(Settings + "/Cue_QaNew005_Activity.asset");
            AudioBgmCueAsset startupCue = LoadAsset<AudioBgmCueAsset>(Settings + "/Cue_QaNew005_StartupActivity.asset");
            AudioBgmCueAsset contentlessCue = LoadAsset<AudioBgmCueAsset>(Settings + "/Cue_QaNew005_ContentlessStartup.asset");
            GameApplicationAsset application = LoadAsset<GameApplicationAsset>(Settings + "/GameApplication_QaNew005.asset");
            RouteAsset neutral = LoadAsset<RouteAsset>(Settings + "/Route_QaNew005_Neutral.asset");
            RouteAsset play = LoadAsset<RouteAsset>(Settings + "/Route_QaNew005_Play.asset");
            RouteAsset preserve = LoadAsset<RouteAsset>(Settings + "/Route_QaNew005_Preserve.asset");
            RouteAsset silence = LoadAsset<RouteAsset>(Settings + "/Route_QaNew005_Silence.asset");
            RouteAsset afterSilence = LoadAsset<RouteAsset>(Settings + "/Route_QaNew005_AfterSilence.asset");
            RouteAsset startupRoute = LoadAsset<RouteAsset>(Settings + "/Route_QaNew005_StartupCue.asset");
            RouteAsset startupEmpty = LoadAsset<RouteAsset>(Settings + "/Route_QaNew005_StartupEmpty.asset");
            ActivityAsset own = LoadAsset<ActivityAsset>(Settings + "/Activity_QaNew005_Own.asset");
            ActivityAsset activityNeutral = LoadAsset<ActivityAsset>(Settings + "/Activity_QaNew005_Neutral.asset");
            ActivityAsset useRoute = LoadAsset<ActivityAsset>(Settings + "/Activity_QaNew005_UseRoute.asset");
            ActivityAsset startupOwn = LoadAsset<ActivityAsset>(Settings + "/Activity_QaNew005_StartupOwn.asset");
            var authorityRoot = new GameObject("QA-NEW-005 Session Audio Authority");
            AudioRuntimeHost host = authorityRoot.AddComponent<AudioRuntimeHost>();
            Set(host, "defaults", defaults); Set(host, "composeOnAwake", true); Set(host, "ensurePersistentListener", false);
            FrameworkBgmDirector director = authorityRoot.AddComponent<FrameworkBgmDirector>();
            Set(director, "audioRuntimeHost", host); Set(director, "logTransitions", true);
            RouteRequestTrigger triggerNeutralRoute = AddTrigger<RouteRequestTrigger>(authorityRoot.transform, "Request Neutral", "targetRoute", neutral);
            RouteRequestTrigger triggerRoutePlay = AddTrigger<RouteRequestTrigger>(authorityRoot.transform, "Request PlayOwn Route", "targetRoute", play);
            RouteRequestTrigger triggerRoutePreserve = AddTrigger<RouteRequestTrigger>(authorityRoot.transform, "Request Preserve Route", "targetRoute", preserve);
            RouteRequestTrigger triggerRouteSilence = AddTrigger<RouteRequestTrigger>(authorityRoot.transform, "Request Silence Route", "targetRoute", silence);
            RouteRequestTrigger triggerRouteAfterSilence = AddTrigger<RouteRequestTrigger>(authorityRoot.transform, "Request Play After Silence", "targetRoute", afterSilence);
            RouteRequestTrigger triggerRouteStartupCue = AddTrigger<RouteRequestTrigger>(authorityRoot.transform, "Request Startup Cue Route", "targetRoute", startupRoute);
            RouteRequestTrigger triggerRouteStartupEmpty = AddTrigger<RouteRequestTrigger>(authorityRoot.transform, "Request Contentless Startup Route", "targetRoute", startupEmpty);
            ActivityRequestTrigger triggerOwn = AddTrigger<ActivityRequestTrigger>(authorityRoot.transform, "Request Activity Own", "targetActivity", own);
            ActivityRequestTrigger triggerNeutral = AddTrigger<ActivityRequestTrigger>(authorityRoot.transform, "Request Activity Neutral", "targetActivity", activityNeutral);
            ActivityRequestTrigger triggerUseRoute = AddTrigger<ActivityRequestTrigger>(authorityRoot.transform, "Request Activity UseRoute", "targetActivity", useRoute);
            var scenarioObject = new GameObject("QA-NEW-005 Audio BGM Continuity Scenario");
            scenarioObject.transform.SetParent(authorityRoot.transform, false);
            var scenario = scenarioObject.AddComponent<QaNew005AudioBgmContinuityScenario>();
            Set(scenario, "director", director); Set(scenario, "evidence", evidence);
            Set(scenario, "requestNeutralRoute", triggerNeutralRoute);
            Set(scenario, "requestRoutePlay", triggerRoutePlay);
            Set(scenario, "requestRoutePreserve", triggerRoutePreserve);
            Set(scenario, "requestRouteSilence", triggerRouteSilence);
            Set(scenario, "requestRouteAfterSilence", triggerRouteAfterSilence);
            Set(scenario, "requestRouteStartupCue", triggerRouteStartupCue);
            Set(scenario, "requestRouteStartupEmpty", triggerRouteStartupEmpty);
            Set(scenario, "requestActivityOwn", triggerOwn); Set(scenario, "requestActivityNeutral", triggerNeutral); Set(scenario, "requestActivityUseRoute", triggerUseRoute);
            Set(scenario, "neutralRoute", neutral); Set(scenario, "routePlay", play); Set(scenario, "routePreserve", preserve);
            Set(scenario, "routeSilence", silence); Set(scenario, "routeAfterSilence", afterSilence);
            Set(scenario, "routeStartupActivityCue", startupRoute); Set(scenario, "routeStartupActivityEmpty", startupEmpty);
            Set(scenario, "activityOwn", own); Set(scenario, "activityNeutral", activityNeutral); Set(scenario, "activityUseRoute", useRoute);
            Set(scenario, "startupActivityCue", startupCue);
            Set(scenario, "routeCue", routeCue);
            Set(scenario, "activityCue", activityCue);
            Set(scenario, "contentlessStartupRouteCue", contentlessCue);
            Set(scenario, "gameApplication", application);
            FinishGeneratedScene(scene, Scenes + "/QA_NEW_005_Persistent.unity", previousSetup);
        }

        private static void CreateActivityScene(string file, string cueAssetName,
            FrameworkBgmActivityPolicy policy)
        {
            string scenePath = GetActivityScenePath(file);
            SceneSetup[] previousSetup;
            Scene scene = BeginGeneratedScene(out previousSetup);
            try
            {
                ActivityAsset activity = LoadAsset<ActivityAsset>(Settings + "/" + file + ".asset");
                QaNew005AudioEvidence evidence = LoadAsset<QaNew005AudioEvidence>(Settings + "/QaNew005AudioEvidence.asset");
                AudioBgmCueAsset cue = string.IsNullOrWhiteSpace(cueAssetName)
                    ? null
                    : LoadAsset<AudioBgmCueAsset>(Settings + "/" + cueAssetName + ".asset");
                var root = new GameObject(Path.GetFileNameWithoutExtension(scenePath) + " Content");
                ActivityContentContribution contribution = root.AddComponent<ActivityContentContribution>();
                Set(contribution, "activity", activity); Set(contribution, "localContentId", "qa-new-005." + activity.name + ".content");
                var authoringRoot = new GameObject("Activity BGM Authoring");
                authoringRoot.transform.SetParent(root.transform, false);
                ActivityBgmAuthoring authoring = authoringRoot.AddComponent<ActivityBgmAuthoring>();
                ConfigureActivityBgm(authoring, activity, cue, policy);
                var probeRoot = new GameObject("Activity BGM Lifecycle Probe");
                probeRoot.transform.SetParent(authoringRoot.transform, false);
                QaNew005ActivityBgmProbe probe = probeRoot.AddComponent<QaNew005ActivityBgmProbe>();
                Set(probe, "authoring", authoring); Set(probe, "evidence", evidence);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, scenePath))
                    throw new System.InvalidOperationException($"QA-NEW-005 scene could not be saved to '{scenePath}'.");
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }

            VerifyActivityScene(file, scenePath, cueAssetName, policy);
        }

        private static string GetActivityScenePath(string activityAssetName)
        {
            const string activityPrefix = "Activity_QaNew005_";
            if (string.IsNullOrWhiteSpace(activityAssetName) ||
                !activityAssetName.StartsWith(activityPrefix, System.StringComparison.Ordinal))
                throw new System.ArgumentException($"Unexpected QA-NEW-005 Activity asset name '{activityAssetName}'.", nameof(activityAssetName));

            string suffix = activityAssetName.Substring(activityPrefix.Length);
            string candidate = Scenes + "/QA_NEW_005_" + suffix + ".unity";
            string[] routeAssetGuids = AssetDatabase.FindAssets("t:RouteAsset", new[] { Settings });
            foreach (string routeAssetGuid in routeAssetGuids)
            {
                string routeAssetPath = AssetDatabase.GUIDToAssetPath(routeAssetGuid);
                RouteAsset route = AssetDatabase.LoadAssetAtPath<RouteAsset>(routeAssetPath);
                if (route != null && string.Equals(route.PrimaryScenePath, candidate, System.StringComparison.Ordinal))
                    return Scenes + "/QA_NEW_005_Activity_" + suffix + ".unity";
            }
            return candidate;
        }

        private static void ValidateSceneOwnership(RouteAsset[] routes, ActivityAsset[] activities)
        {
            var routePaths = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (RouteAsset route in routes)
            {
                if (route == null || string.IsNullOrWhiteSpace(route.PrimaryScenePath))
                    throw new System.InvalidOperationException("QA-NEW-005 requires a primary scene path for every Route fixture.");
                if (!routePaths.Add(route.PrimaryScenePath))
                    throw new System.InvalidOperationException($"QA-NEW-005 Routes share primary scene path '{route.PrimaryScenePath}'.");
            }

            foreach (ActivityAsset activity in activities)
            {
                if (activity == null || !activity.HasActivityContentProfile || !activity.ActivityContentProfile.HasScenes)
                    continue;
                for (int i = 0; i < activity.ActivityContentProfile.Scenes.Count; i++)
                {
                    string activityScenePath = activity.ActivityContentProfile.Scenes[i].ScenePath;
                    if (routePaths.Contains(activityScenePath))
                        throw new System.InvalidOperationException($"QA-NEW-005 Activity '{activity.name}' content scene '{activityScenePath}' collides with a Route primary scene.");
                }
            }
        }

        private static void ConfigureRouteBgm(RouteBgmAuthoring authoring, AudioBgmCueAsset cue,
            FrameworkBgmRoutePolicy policy)
        {
            if ((policy == FrameworkBgmRoutePolicy.PlayOwn) != (cue != null))
                throw new System.InvalidOperationException($"Route BGM fixture policy '{policy}' has an invalid cue reference.");

            var serialized = new SerializedObject(authoring);
            serialized.Update();
            SerializedProperty cueProperty = RequireProperty(serialized, "routeBgm", authoring);
            SerializedProperty policyProperty = RequireProperty(serialized, "policy", authoring);
            SerializedProperty versionProperty = RequireProperty(serialized, "routePolicySerializationVersion", authoring);
            cueProperty.objectReferenceValue = cue;
            SetEnum(policyProperty, policy.ToString(), authoring);
            versionProperty.intValue = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(authoring);

            serialized.Update();
            cueProperty = RequireProperty(serialized, "routeBgm", authoring);
            policyProperty = RequireProperty(serialized, "policy", authoring);
            versionProperty = RequireProperty(serialized, "routePolicySerializationVersion", authoring);
            if (!ReferenceEquals(cueProperty.objectReferenceValue, cue) ||
                policyProperty.enumNames[policyProperty.enumValueIndex] != policy.ToString() ||
                versionProperty.intValue != 1)
                throw new System.InvalidOperationException("Route BGM authoring values did not persist in SerializedObject.");
        }

        private static void ConfigureActivityBgm(ActivityBgmAuthoring authoring, ActivityAsset activity,
            AudioBgmCueAsset cue, FrameworkBgmActivityPolicy policy)
        {
            bool permitsOwnCue = policy == FrameworkBgmActivityPolicy.UseOwnOrRoute ||
                                 policy == FrameworkBgmActivityPolicy.UseOwnOrPreserveCurrent;
            if (!permitsOwnCue && cue != null)
                throw new System.InvalidOperationException($"Activity BGM fixture policy '{policy}' has an invalid cue reference.");

            var serialized = new SerializedObject(authoring);
            serialized.Update();
            SerializedProperty activityProperty = RequireProperty(serialized, "assignedActivity", authoring);
            SerializedProperty cueProperty = RequireProperty(serialized, "activityBgm", authoring);
            SerializedProperty policyProperty = RequireProperty(serialized, "policy", authoring);
            activityProperty.objectReferenceValue = activity;
            cueProperty.objectReferenceValue = cue;
            SetEnum(policyProperty, policy.ToString(), authoring);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(authoring);

            serialized.Update();
            activityProperty = RequireProperty(serialized, "assignedActivity", authoring);
            cueProperty = RequireProperty(serialized, "activityBgm", authoring);
            policyProperty = RequireProperty(serialized, "policy", authoring);
            if (!ReferenceEquals(activityProperty.objectReferenceValue, activity) ||
                !ReferenceEquals(cueProperty.objectReferenceValue, cue) ||
                policyProperty.enumNames[policyProperty.enumValueIndex] != policy.ToString())
                throw new System.InvalidOperationException("Activity BGM authoring values did not persist in SerializedObject.");
        }

        private static void VerifyRouteScene(string routeAssetPath, bool hasAuthoring, string cueAssetName,
            FrameworkBgmRoutePolicy policy)
        {
            RouteAsset route = LoadAsset<RouteAsset>(routeAssetPath);
            AudioBgmCueAsset cue = string.IsNullOrWhiteSpace(cueAssetName)
                ? null
                : LoadAsset<AudioBgmCueAsset>(Settings + "/" + cueAssetName + ".asset");
            VerifyScene(route.PrimaryScenePath, scene =>
            {
                RouteContentContribution contribution = FindSingleContribution<RouteContentContribution>(scene, item =>
                    ReferenceEquals(item.Route, route), "RouteContentContribution for " + route.name);
                RouteBgmAuthoring[] authorings = contribution.GetComponentsInChildren<RouteBgmAuthoring>(true);
                if (hasAuthoring)
                {
                    if (authorings.Length != 1)
                        throw new System.InvalidOperationException($"Persisted Route BGM fixture is invalid for '{route.name}': expected one RouteBgmAuthoring, found {authorings.Length}.");
                    var serialized = new SerializedObject(authorings[0]);
                    SerializedProperty version = serialized.FindProperty("routePolicySerializationVersion");
                    if (!authorings[0].transform.IsChildOf(contribution.transform) ||
                        !ReferenceEquals(authorings[0].RouteBgm, cue) || authorings[0].Policy != policy ||
                        version == null || version.intValue != 1)
                        throw new System.InvalidOperationException($"Persisted Route BGM fixture is invalid for '{route.name}': expected policy='{policy}', cue='{cueAssetName ?? "<null>"}'.");
                }
                else if (authorings.Length != 0)
                {
                    throw new System.InvalidOperationException($"Neutral Route '{route.name}' unexpectedly contains RouteBgmAuthoring.");
                }
            });
        }

        private static void VerifyActivityScene(string file, string scenePath, string cueAssetName,
            FrameworkBgmActivityPolicy policy)
        {
            ActivityAsset activity = LoadAsset<ActivityAsset>(Settings + "/" + file + ".asset");
            AudioBgmCueAsset cue = string.IsNullOrWhiteSpace(cueAssetName)
                ? null
                : LoadAsset<AudioBgmCueAsset>(Settings + "/" + cueAssetName + ".asset");
            VerifyScene(scenePath, scene =>
            {
                ActivityContentContribution contribution = FindSingleContribution<ActivityContentContribution>(scene, item =>
                    ReferenceEquals(item.Activity, activity), "ActivityContentContribution for " + activity.name);
                ActivityBgmAuthoring[] authorings = contribution.GetComponentsInChildren<ActivityBgmAuthoring>(true);
                if (authorings.Length != 1 || !authorings[0].transform.IsChildOf(contribution.transform) ||
                    !ReferenceEquals(authorings[0].AssignedActivity, activity) ||
                    !ReferenceEquals(authorings[0].ActivityBgm, cue) || authorings[0].Policy != policy)
                    throw new System.InvalidOperationException($"Persisted Activity BGM fixture is invalid for '{activity.name}': expected policy='{policy}', cue='{cueAssetName ?? "<null>"}'.");
            });
        }

        private static void VerifyScene(string path, System.Action<Scene> verify)
        {
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                verify(scene);
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
        }

        private static T FindSingleContribution<T>(Scene scene, System.Predicate<T> predicate, string description)
            where T : Component
        {
            var matches = new List<T>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (T item in root.GetComponentsInChildren<T>(true))
                    if (predicate(item)) matches.Add(item);
            }
            if (matches.Count != 1)
                throw new System.InvalidOperationException($"Expected exactly one {description}; found {matches.Count} in '{scene.path}'.");
            return matches[0];
        }

        private static SerializedProperty RequireProperty(SerializedObject serialized, string name, Object target)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null)
                throw new System.InvalidOperationException($"Serialized property '{name}' missing on '{target.GetType().Name}'.");
            return property;
        }

        private static void SetEnum(SerializedProperty property, string enumName, Object target)
        {
            string[] names = property.enumNames;
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i] != enumName) continue;
                property.enumValueIndex = i;
                return;
            }
            throw new System.InvalidOperationException($"Enum value '{enumName}' missing on '{target.GetType().Name}.{property.name}'.");
        }

        private static T AddTrigger<T>(Transform parent, string name, string targetProperty, Object target) where T : MonoBehaviour
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            T trigger = go.AddComponent<T>(); Set(trigger, targetProperty, target); return trigger;
        }

        private static Scene BeginGeneratedScene(out SceneSetup[] previousSetup)
        {
            previousSetup = EditorSceneManager.GetSceneManagerSetup();
            bool hasLoadedScene = false;
            bool hasActiveScene = false;
            if (previousSetup != null)
            {
                foreach (SceneSetup setup in previousSetup)
                {
                    hasLoadedScene |= setup.isLoaded;
                    hasActiveScene |= setup.isActive && setup.isLoaded;
                }
            }

            if (!hasLoadedScene || !hasActiveScene)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                previousSetup = EditorSceneManager.GetSceneManagerSetup();
            }

            return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static void FinishGeneratedScene(Scene scene, string path, SceneSetup[] previousSetup)
        {
            try
            {
                if (!EditorSceneManager.SaveScene(scene, path))
                    throw new System.InvalidOperationException($"QA-NEW-005 scene could not be saved to '{path}'.");
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
        }

        private static T CreateAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            Object existing = AssetDatabase.LoadMainAssetAtPath(path);
            if (existing != null)
                throw new System.InvalidOperationException($"Cannot create {typeof(T).Name} at '{path}': the path is already occupied by {existing.GetType().Name}.");
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            T canonicalAsset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (canonicalAsset == null)
                throw new System.InvalidOperationException($"Created {typeof(T).Name} could not be loaded from '{path}'.");
            return canonicalAsset;
        }

        private static T LoadAsset<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new System.InvalidOperationException($"Required {typeof(T).Name} could not be loaded from '{path}'.");
            return asset;
        }

        private static void SetCameraOutput(GameApplicationAsset application)
        {
            GameObject outputPrefab = LoadAsset<GameObject>(CameraOutputPrefab);
            var serialized = new SerializedObject(application);
            SerializedProperty outputs = serialized.FindProperty("cameraSession.outputPrefabs");
            if (outputs == null)
                throw new System.InvalidOperationException("GameApplication cameraSession.outputPrefabs property is missing.");
            outputs.arraySize = 1;
            outputs.GetArrayElementAtIndex(0).objectReferenceValue = outputPrefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Set(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) throw new System.InvalidOperationException($"Serialized property '{propertyName}' missing on '{target.GetType().Name}'.");
            property.objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Set(Object target, string propertyName, string value)
        {
            var serialized = new SerializedObject(target); SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) throw new System.InvalidOperationException($"Serialized property '{propertyName}' missing on '{target.GetType().Name}'.");
            property.stringValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Set(Object target, string propertyName, bool value)
        {
            var serialized = new SerializedObject(target); SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) throw new System.InvalidOperationException($"Serialized property '{propertyName}' missing on '{target.GetType().Name}'.");
            property.boolValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Set(Object target, string propertyName, float value)
        {
            var serialized = new SerializedObject(target); SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) throw new System.InvalidOperationException($"Serialized property '{propertyName}' missing on '{target.GetType().Name}'.");
            property.floatValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Set(Object target, string propertyName, int value)
        {
            var serialized = new SerializedObject(target); SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) throw new System.InvalidOperationException($"Serialized property '{propertyName}' missing on '{target.GetType().Name}'.");
            property.enumValueIndex = value; serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string parent, string folder)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + folder)) AssetDatabase.CreateFolder(parent, folder);
        }

        private static void AddScenesToBuildSettings()
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (string path in Directory.GetFiles(Scenes, "*.unity"))
            {
                string assetPath = path.Replace('\\', '/');
                if (!scenes.Exists(item => item.path == assetPath)) scenes.Add(new EditorBuildSettingsScene(assetPath, true));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void WriteTone(string path)
        {
            const int rate = 22050; const int samples = rate / 2; const short channels = 1; const short bits = 16;
            using var stream = File.Create(path); using var writer = new BinaryWriter(stream);
            int dataBytes = samples * channels * bits / 8;
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + dataBytes);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1);
            writer.Write(channels); writer.Write(rate); writer.Write(rate * channels * bits / 8);
            writer.Write((short)(channels * bits / 8)); writer.Write(bits); writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(dataBytes);
            for (int i = 0; i < samples; i++) writer.Write((short)(Mathf.Sin(2f * Mathf.PI * 440f * i / rate) * short.MaxValue * 0.15f));
        }
    }
}
