using System;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Editor.CameraAuthoring;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Camera.Editor
{
    /// <summary>
    /// Owns only the reusable Camera definition/behavior assets shared by current
    /// IF-ADR-032 QA fixtures. It does not author physical Outputs, Presentations
    /// or Player policy components into Persistent Content.
    /// </summary>
    internal static class QaCameraDefinitionAssetGuard
    {
        private const string DefinitionFolder =
            "Assets/ImmersiveFrameworkQA/Camera/Definitions";

        internal const string OutputAPath =
            DefinitionFolder + "/MainOutput.asset";
        internal const string OutputBPath =
            DefinitionFolder + "/SecondaryOutput.asset";
        internal const string MissingOutputPath =
            DefinitionFolder + "/MissingOutput.asset";
        internal const string FixedBehaviorPath =
            DefinitionFolder + "/FixedBehavior.asset";
        internal const string GroupBehaviorPath =
            DefinitionFolder + "/GroupBehavior.asset";
        internal const string FollowBehaviorPath =
            DefinitionFolder + "/FollowBehavior.asset";
        internal const string MountedBehaviorPath =
            DefinitionFolder + "/MountedBehavior.asset";

        internal static void Ensure()
        {
            EnsureFolder();

            CreateDefinitionIfMissing<CameraOutputDefinition>(
                OutputAPath);
            CreateDefinitionIfMissing<CameraOutputDefinition>(
                OutputBPath);
            CreateDefinitionIfMissing<CameraOutputDefinition>(
                MissingOutputPath);

            CreateBehaviorIfMissing<FixedCameraRigBehaviorDefinition>(
                FixedBehaviorPath);
            CreateBehaviorIfMissing<GroupCameraRigBehaviorDefinition>(
                GroupBehaviorPath);
            CreateBehaviorIfMissing<FollowCameraRigBehaviorDefinition>(
                FollowBehaviorPath);
            CreateBehaviorIfMissing<MountedCameraRigBehaviorDefinition>(
                MountedBehaviorPath);

            AssetDatabase.SaveAssets();
        }

        internal static T RequireDefinition<T>(
            string path)
            where T : ScriptableObject
        {
            T definition =
                AssetDatabase.LoadAssetAtPath<T>(
                    path);

            if (definition == null)
            {
                throw new InvalidOperationException(
                    $"Required Camera definition is missing at '{path}'. Run Camera QA setup.");
            }

            string issue =
                CameraDefinitionIdentityEditorUtility.Validate(
                    definition);
            if (!string.IsNullOrEmpty(issue))
            {
                throw new InvalidOperationException(
                    $"Camera definition '{path}' is invalid. {issue}");
            }

            return definition;
        }

        internal static T RequireBehavior<T>(
            string path)
            where T : CameraRigBehaviorDefinition
        {
            T definition =
                AssetDatabase.LoadAssetAtPath<T>(
                    path);

            if (definition == null)
            {
                throw new InvalidOperationException(
                    $"Required Camera Rig Behavior definition is missing at '{path}'. Run Camera QA setup.");
            }

            if (!definition.TryValidate(
                    out string issue))
            {
                throw new InvalidOperationException(
                    $"Camera Rig Behavior definition '{path}' is invalid. {issue}");
            }

            return definition;
        }

        private static T CreateDefinitionIfMissing<T>(
            string path)
            where T : ScriptableObject
        {
            if (AssetDatabase.LoadMainAssetAtPath(
                    path) == null)
            {
                T definition =
                    ScriptableObject.CreateInstance<T>();
                CameraDefinitionIdentityEditorUtility
                    .GenerateMissingId(definition);
                AssetDatabase.CreateAsset(
                    definition,
                    path);
            }

            return RequireDefinition<T>(
                path);
        }

        private static T CreateBehaviorIfMissing<T>(
            string path)
            where T : CameraRigBehaviorDefinition
        {
            if (AssetDatabase.LoadMainAssetAtPath(
                    path) == null)
            {
                T definition =
                    ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(
                    definition,
                    path);
            }

            return RequireBehavior<T>(
                path);
        }

        private static void EnsureFolder()
        {
            if (AssetDatabase.IsValidFolder(
                    DefinitionFolder))
            {
                return;
            }

            const string parent =
                "Assets/ImmersiveFrameworkQA/Camera";
            if (!AssetDatabase.IsValidFolder(parent))
            {
                throw new InvalidOperationException(
                    $"Camera QA root folder '{parent}' is missing.");
            }

            AssetDatabase.CreateFolder(
                parent,
                "Definitions");
        }
    }
}
