using System;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player.Internal.Editor
{
    /// <summary>
    /// Historical ADR-021 Activity-owned Initial Placement suite.
    /// Historical result: 9/9 PASS against the superseded ActivityOwnedScenes boundary.
    /// Model B replacement owners:
    ///   QaAdr21RoutePlayerSpatialEntryRegression
    ///   QaAdr21ActivityPlayerRelocationRegression
    /// This type is retained as a documentary marker. It is not a current
    /// certification owner and must not be rewritten as if it always tested relocation.
    /// </summary>
    public static class QaAdr21ActivityPlayerInitialPlacementRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/Run ADR-021 Initial Placement QA (Historical / Superseded)";
        private const string Prefix = "[QA_ADR021_INITIAL_PLACEMENT]";
        private const int HistoricalCaseCount = 9;

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() =>
            !EditorApplication.isPlayingOrWillChangePlaymode;

        [MenuItem(MenuPath)]
        private static void RunFromMenu()
        {
            Execute(out _);
        }

        /// <summary>
        /// Intentionally not executable against current APIs. The removed
        /// ActivityPlayerInitialPlacement* surface no longer exists.
        /// </summary>
        public static bool Execute(out string error)
        {
            error =
                "QaAdr21ActivityPlayerInitialPlacementRegression is superseded by IF-ADR-021 Model B. " +
                "Historical result remains 9/9 against the former Activity-owned Initial Placement boundary. " +
                "Current owners are QaAdr21RoutePlayerSpatialEntryRegression and QaAdr21ActivityPlayerRelocationRegression.";
            Debug.LogWarning(
                $"{Prefix} status='Superseded' historical='{HistoricalCaseCount}/{HistoricalCaseCount}' " +
                "verdict='HISTORICAL ACTIVITY-OWNED INITIAL PLACEMENT 9/9' " +
                "current='QaAdr21RoutePlayerSpatialEntryRegression + QaAdr21ActivityPlayerRelocationRegression' " +
                $"error='{Escape(error)}'.");
            return false;
        }

        private static string Escape(string value) =>
            string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "''").Replace("\r", " ").Replace("\n", " ");
    }
}
