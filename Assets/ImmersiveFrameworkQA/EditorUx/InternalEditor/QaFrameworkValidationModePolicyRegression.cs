using System;
using Immersive.Framework.Authoring;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.EditorUx.Internal.Editor
{
    internal static class QaFrameworkValidationModePolicyRegression
    {
        private const int ExpectedCases = 17;

        [MenuItem("Immersive Framework/QA/Regressions/Editor UX/Run Framework Validation Mode Policy", priority = 261)]
        private static void Run()
        {
            var cases = 0;

            VerifyMode(
                FrameworkValidationMode.Strict,
                expectedKnown: true,
                expectedWarningsAsErrors: true,
                expectedInfo: true,
                ref cases);

            VerifyMode(
                FrameworkValidationMode.Standard,
                expectedKnown: true,
                expectedWarningsAsErrors: false,
                expectedInfo: true,
                ref cases);

            VerifyMode(
                FrameworkValidationMode.Release,
                expectedKnown: true,
                expectedWarningsAsErrors: false,
                expectedInfo: false,
                ref cases);

            var unknown = (FrameworkValidationMode)int.MaxValue;
            Require(!FrameworkValidationModePolicy.IsKnown(unknown), "Unknown ValidationMode was unexpectedly accepted as known.");
            cases++;
            Require(FrameworkValidationModePolicy.RequiredConfigurationFails(unknown), "Unknown ValidationMode weakened required-configuration failure semantics.");
            cases++;
            Require(FrameworkValidationModePolicy.TreatWarningsAsErrors(unknown), "Unknown ValidationMode did not promote warnings to errors as conservative Strict semantics require.");
            cases++;
            Require(FrameworkValidationModePolicy.IncludeInfoDiagnostics(unknown), "Unknown ValidationMode suppressed info diagnostics instead of using Strict semantics.");
            cases++;
            Require(
                FrameworkValidationModePolicy.GetSummary(unknown).IndexOf("Strict", StringComparison.Ordinal) >= 0,
                "Unknown ValidationMode summary no longer identifies conservative Strict treatment.");
            cases++;

            Require(cases == ExpectedCases, "RA-04 validation governance case count drifted. Expected " + ExpectedCases + " but executed " + cases + ".");

            Debug.Log(
                "[RA04_QA_VALIDATION_GOVERNANCE] status='Passed' cases='" + cases +
                "' unknownKnown='" + FrameworkValidationModePolicy.IsKnown(unknown) +
                "' unknownWarningsAsErrors='" + FrameworkValidationModePolicy.TreatWarningsAsErrors(unknown) + "'.");
        }

        private static void VerifyMode(
            FrameworkValidationMode mode,
            bool expectedKnown,
            bool expectedWarningsAsErrors,
            bool expectedInfo,
            ref int cases)
        {
            Require(FrameworkValidationModePolicy.IsKnown(mode) == expectedKnown, mode + " known-mode contract diverged.");
            cases++;
            Require(FrameworkValidationModePolicy.RequiredConfigurationFails(mode), mode + " weakened required-configuration failure semantics.");
            cases++;
            Require(FrameworkValidationModePolicy.TreatWarningsAsErrors(mode) == expectedWarningsAsErrors, mode + " warning severity contract diverged.");
            cases++;
            Require(FrameworkValidationModePolicy.IncludeInfoDiagnostics(mode) == expectedInfo, mode + " info-diagnostic contract diverged.");
            cases++;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
