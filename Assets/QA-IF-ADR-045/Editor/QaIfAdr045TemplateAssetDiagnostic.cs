using System;
using System.IO;
using System.Linq;
using System.Text;
using Immersive.Framework.Authoring;
using UnityEditor;
using UnityEngine;

namespace Immersive.QaFramework.IfAdr045.Editor
{
    /// <summary>
    /// Read-only inspection of the QA-NEW-004 Game Application asset import.
    /// </summary>
    internal static class QaIfAdr045TemplateAssetDiagnostic
    {
        private const string AssetPath = "Assets/QA-NEW-004/Settings/GameApplication_QaNew004.asset";

        [MenuItem("Immersive Framework/QA/IF-ADR-045/Diagnose QA-NEW-004 Game Application Template")]
        private static void Diagnose()
        {
            var report = new StringBuilder();
            string absolutePath = Path.GetFullPath(Path.Combine(Application.dataPath, "QA-NEW-004/Settings/GameApplication_QaNew004.asset"));
            string guid = AssetDatabase.AssetPathToGUID(AssetPath);
            string reversePath = string.IsNullOrEmpty(guid) ? string.Empty : AssetDatabase.GUIDToAssetPath(guid);
            Type recognizedMainType = AssetDatabase.GetMainAssetTypeAtPath(AssetPath);
            UnityEngine.Object mainAsset = AssetDatabase.LoadMainAssetAtPath(AssetPath);
            GameApplicationAsset typedAsset = AssetDatabase.LoadAssetAtPath<GameApplicationAsset>(AssetPath);

            report.AppendLine("[IF-ADR-045-TEMPLATE-DIAGNOSTIC]");
            report.AppendLine($"assetPath='{AssetPath}'");
            report.AppendLine($"physicalFileExists='{File.Exists(absolutePath)}' absolutePath='{absolutePath}'");
            report.AppendLine($"guid='{(string.IsNullOrEmpty(guid) ? "<empty>" : guid)}' reversePath='{(string.IsNullOrEmpty(reversePath) ? "<empty>" : reversePath)}'");
            report.AppendLine($"recognizedMainType='{FormatType(recognizedMainType)}'");
            report.AppendLine($"mainAsset='{FormatObject(mainAsset)}' isMainAsset='{(mainAsset != null && AssetDatabase.IsMainAsset(mainAsset))}'");
            report.AppendLine($"typedGameApplication='{FormatObject(typedAsset)}'");
            report.AppendLine($"editorCompiling='{EditorApplication.isCompiling}' editorUpdating='{EditorApplication.isUpdating}'");

            MonoScript matchingScript = FindGameApplicationScript();
            report.AppendLine($"matchingScript='{FormatObject(matchingScript)}' scriptPath='{(matchingScript == null ? "<unresolved>" : AssetDatabase.GetAssetPath(matchingScript))}' " +
                              $"scriptGuid='{(matchingScript == null ? "<unresolved>" : AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(matchingScript)))}'");
            AppendRelevantEditorLog(report);

            Debug.Log(report.ToString());
        }

        private static MonoScript FindGameApplicationScript()
        {
            foreach (string scriptGuid in AssetDatabase.FindAssets("t:MonoScript"))
            {
                string scriptPath = AssetDatabase.GUIDToAssetPath(scriptGuid);
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
                if (script != null && script.GetClass() == typeof(GameApplicationAsset))
                    return script;
            }

            return null;
        }

        private static string FormatObject(UnityEngine.Object value)
        {
            if (value == null)
                return "<null>";

            Type type = value.GetType();
            return $"name='{value.name}' type='{type.FullName}' assembly='{type.Assembly.GetName().Name}' assetPath='{AssetDatabase.GetAssetPath(value)}'";
        }

        private static string FormatType(Type type)
        {
            return type == null
                ? "<null>"
                : $"'{type.FullName}' assembly='{type.Assembly.GetName().Name}'";
        }

        private static void AppendRelevantEditorLog(StringBuilder report)
        {
            string logPath = Application.consoleLogPath;
            report.AppendLine($"editorLogPath='{logPath}'");
            if (string.IsNullOrWhiteSpace(logPath) || !File.Exists(logPath))
            {
                report.AppendLine("relevantEditorLog='unavailable; inspect Unity Console and Editor.log manually'");
                return;
            }

            try
            {
                string[] matches = File.ReadAllLines(logPath)
                    .Where(line => line.IndexOf("GameApplicationAsset", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   line.IndexOf("QA-NEW-004", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   line.IndexOf("Immersive.Framework.Runtime", StringComparison.OrdinalIgnoreCase) >= 0)
                    .TakeLast(30)
                    .ToArray();
                report.AppendLine("relevantEditorLogBegin");
                report.AppendLine(matches.Length == 0 ? "<no matching lines>" : string.Join(Environment.NewLine, matches));
                report.AppendLine("relevantEditorLogEnd");
            }
            catch (Exception exception)
            {
                report.AppendLine($"relevantEditorLog='unreadable: {exception.GetType().Name}: {exception.Message}'");
            }
        }
    }
}
