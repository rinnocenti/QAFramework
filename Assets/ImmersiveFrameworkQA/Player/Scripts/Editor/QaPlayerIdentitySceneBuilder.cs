using Immersive.Framework.GameFlow;
using ImmersiveFrameworkQA.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Editor-only deterministic builder for the synthetic Actor Identity QA scene.
    /// </summary>
    public static class QaPlayerIdentitySceneBuilder
    {
        private const string Root = "Assets/ImmersiveFrameworkQA/Player";
        private const string Scenes = Root + "/Scenes";
        private const string Scripts = Root + "/Scripts";
        private const string ScenePath = Scenes + "/QA_PlayerIdentity.unity";
        private const string HubRoutePath = "Assets/ImmersiveFrameworkQA/Hub/Routes/QA_HubRoute.asset";

        [MenuItem("Immersive Framework QA/Player/Create or Refresh Player Identity QA Scene")]
        public static void CreateOrRefreshPlayerIdentityScene()
        {
            EnsureFolders();
            CreateScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[QA_ACTOR_IDENTITY_SETUP] Player Identity QA scene created or refreshed at '" + ScenePath + "'.");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "ImmersiveFrameworkQA");
            EnsureFolder("Assets/ImmersiveFrameworkQA", "Player");
            EnsureFolder(Root, "Scenes");
            EnsureFolder(Root, "Scripts");
            EnsureFolder(Scripts, "Runtime");
            EnsureFolder(Scripts, "Editor");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static void CreateScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "QA_PlayerIdentity";

            CreateCamera();
            Canvas canvas = CreateCanvas();
            Text summary = CreateText(
                canvas.transform,
                "QA_PlayerIdentitySummary",
                "Actor Identity QA | passes=0 failures=0",
                new Vector2(0f, 260f),
                new Vector2(900f, 64f),
                30);

            Text result = CreateText(
                canvas.transform,
                "QA_PlayerIdentityResult",
                "Idle. Run an Actor Identity QA probe.",
                new Vector2(0f, 125f),
                new Vector2(900f, 180f),
                22);

            RouteRequestTrigger backToHubTrigger = CreateBackToHubTrigger(canvas.transform);

            QaPlayerIdentityPanel panel = canvas.gameObject.AddComponent<QaPlayerIdentityPanel>();
            panel.Configure(summary, result, backToHubTrigger, new Rect(16f, 16f, 620f, 680f));

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void CreateCamera()
        {
            GameObject cameraObject = new GameObject("QA_PlayerIdentityCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.055f, 0.045f, 1f);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject("QA_PlayerIdentityCanvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string value,
            Vector2 anchoredPosition,
            Vector2 size,
            int fontSize)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            textObject.AddComponent<CanvasRenderer>();

            Text text = textObject.AddComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static RouteRequestTrigger CreateBackToHubTrigger(Transform parent)
        {
            GameObject triggerObject = new GameObject("RouteTrigger_BackToQaHub");
            triggerObject.transform.SetParent(parent, false);

            RouteRequestTrigger trigger = triggerObject.AddComponent<RouteRequestTrigger>();
            Object hubRoute = AssetDatabase.LoadAssetAtPath<Object>(HubRoutePath);
            if (hubRoute == null)
            {
                Debug.LogError("[QA_ACTOR_IDENTITY_SETUP] Hub route asset not found. path='" + HubRoutePath + "'.", triggerObject);
            }

            SerializedObject serialized = new SerializedObject(trigger);
            serialized.FindProperty("targetRoute").objectReferenceValue = hubRoute;
            serialized.FindProperty("reason").stringValue = "qa.actor-identity.back-to-hub";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(trigger);
            return trigger;
        }
    }
}
