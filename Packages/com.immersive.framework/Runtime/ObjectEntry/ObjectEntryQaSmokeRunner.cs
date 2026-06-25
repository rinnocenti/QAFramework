#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.Identity;
using Immersive.Logging.Records;
using UnityEngine;

namespace Immersive.Framework.ObjectEntry
{
    /// <summary>
    /// Development-only runner for F13 Object Entry synthetic contract smokes.
    /// It validates passive descriptors and sets only. It does not discover GameObjects, materialize prefabs, create Player/Actor state or perform reset.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "Shared QA runner for F13 Object Entry synthetic set smoke; no Unity object entry or gameplay entry.")]
    internal static class ObjectEntryQaSmokeRunner
    {
        internal const string SyntheticSetSmokeName = "Object Entry Synthetic Set Smoke";

        internal const string DeclarationSourceSmokeName = "Object Entry Declaration Source Smoke";

        internal static Task<bool> RunSyntheticSetSmokeAsync(
            FrameworkLogger logger,
            string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            source = string.IsNullOrWhiteSpace(source) ? nameof(ObjectEntryQaSmokeRunner) : source;

            try
            {
                var routeOwner = new FrameworkIdentityKey(FrameworkIdentityDomain.Route, new FrameworkIdentityValue("qa.route.canonical"));
                var activityOwner = new FrameworkIdentityKey(FrameworkIdentityDomain.Activity, new FrameworkIdentityValue("qa.activity.primary"));

                var routeRequired = new ObjectEntryDescriptor(
                    ObjectEntryId.From("qa.object-entry.route.required"),
                    ObjectEntryScope.Route,
                    ObjectEntrySourceKind.SceneAuthored,
                    ObjectEntryRequiredness.Required,
                    "QA Route Required Object Entry",
                    routeOwner);

                var routeOptional = new ObjectEntryDescriptor(
                    ObjectEntryId.From("qa.object-entry.route.optional"),
                    ObjectEntryScope.Route,
                    ObjectEntrySourceKind.RuntimeRegistered,
                    ObjectEntryRequiredness.Optional,
                    "QA Route Optional Object Entry",
                    routeOwner);

                var activityRequired = new ObjectEntryDescriptor(
                    ObjectEntryId.From("qa.object-entry.activity.required"),
                    ObjectEntryScope.Activity,
                    ObjectEntrySourceKind.SceneAuthored,
                    ObjectEntryRequiredness.Required,
                    "QA Activity Required Object Entry",
                    activityOwner);

                var set = new ObjectEntrySet(new[] { routeRequired, routeOptional, activityRequired });
                if (!ValidateSet(logger, set, routeRequired, activityRequired))
                {
                    return Task.FromResult(false);
                }

                var accepted = ObjectEntryResult.Accepted(routeRequired);
                if (!ValidateAcceptedResult(logger, accepted))
                {
                    return Task.FromResult(false);
                }

                if (!ValidateDuplicateRejection(logger, routeRequired, routeOptional))
                {
                    return Task.FromResult(false);
                }

                logger.Info(
                    "QA Object Entry Synthetic Set Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "synthetic-set"),
                        LogFields.Field("source", source),
                        LogFields.Field("objectEntries", set.Count),
                        LogFields.Field("required", set.RequiredCount),
                        LogFields.Field("optional", set.OptionalCount),
                        LogFields.Field("route", set.GetByScope(ObjectEntryScope.Route).Count),
                        LogFields.Field("activity", set.GetByScope(ObjectEntryScope.Activity).Count),
                        LogFields.Field("session", set.GetByScope(ObjectEntryScope.Session).Count),
                        LogFields.Field("resultStatus", accepted.Status.ToString()),
                        LogFields.Field("blockingIssues", accepted.BlockingIssueCount),
                        LogFields.Field("nonBlockingIssues", accepted.NonBlockingIssueCount),
                        LogFields.Field("summary", set.Summary)));

                return Task.FromResult(true);
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Object Entry Synthetic Set Smoke step failed.",
                    LogFields.Of(
                        LogFields.Field("step", "synthetic-set"),
                        LogFields.Field("reason", exception.Message)));
                return Task.FromResult(false);
            }
        }


        internal static Task<bool> RunDeclarationSourceSmokeAsync(
            FrameworkLogger logger,
            string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            source = string.IsNullOrWhiteSpace(source) ? nameof(ObjectEntryQaSmokeRunner) : source;

            GameObject routeObject = null;
            GameObject activityObject = null;
            GameObject duplicateObject = null;

            try
            {
                routeObject = new GameObject("QA_ObjectEntryDeclarationSource_Route");
                activityObject = new GameObject("QA_ObjectEntryDeclarationSource_Activity");
                duplicateObject = new GameObject("QA_ObjectEntryDeclarationSource_Duplicate");

                var routeDeclaration = routeObject.AddComponent<ObjectEntryDeclaration>();
                routeDeclaration.ConfigureForQa(
                    "qa.object-entry.declaration-source.route",
                    ObjectEntryScope.Route,
                    ObjectEntryRequiredness.Required,
                    "QA Declaration Source Route Entry");

                var activityDeclaration = activityObject.AddComponent<ObjectEntryDeclaration>();
                activityDeclaration.ConfigureForQa(
                    "qa.object-entry.declaration-source.activity",
                    ObjectEntryScope.Activity,
                    ObjectEntryRequiredness.Optional,
                    "QA Declaration Source Activity Entry");

                var duplicateDeclaration = duplicateObject.AddComponent<ObjectEntryDeclaration>();
                duplicateDeclaration.ConfigureForQa(
                    "qa.object-entry.declaration-source.route",
                    ObjectEntryScope.Route,
                    ObjectEntryRequiredness.Required,
                    "QA Declaration Source Duplicate Entry");

                var declarationSource = new ObjectEntryDeclarationSource(includeInactiveDeclarations: true);
                var result = declarationSource.Collect(
                    new[] { routeDeclaration, activityDeclaration },
                    source);

                if (!ValidateDeclarationSourceResult(logger, result, routeDeclaration, activityDeclaration))
                {
                    return Task.FromResult(false);
                }

                logger.Info(
                    "QA Object Entry Declaration Source Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "declaration-source-set"),
                        LogFields.Field("source", source),
                        LogFields.Field("declarations", result.DeclarationCount),
                        LogFields.Field("acceptedDeclarations", result.AcceptedDeclarationCount),
                        LogFields.Field("rejectedDeclarations", result.RejectedDeclarationCount),
                        LogFields.Field("objectEntries", result.ObjectEntries.Count),
                        LogFields.Field("required", result.ObjectEntries.RequiredCount),
                        LogFields.Field("optional", result.ObjectEntries.OptionalCount),
                        LogFields.Field("route", result.ObjectEntries.GetByScope(ObjectEntryScope.Route).Count),
                        LogFields.Field("activity", result.ObjectEntries.GetByScope(ObjectEntryScope.Activity).Count),
                        LogFields.Field("session", result.ObjectEntries.GetByScope(ObjectEntryScope.Session).Count),
                        LogFields.Field("blockingIssues", result.BlockingIssueCount),
                        LogFields.Field("nonBlockingIssues", result.NonBlockingIssueCount),
                        LogFields.Field("summary", result.Summary)));

                var duplicateResult = declarationSource.Collect(
                    new[] { routeDeclaration, duplicateDeclaration },
                    source);

                if (!ValidateDeclarationSourceDuplicateRejection(logger, duplicateResult))
                {
                    return Task.FromResult(false);
                }

                logger.Info(
                    "QA Object Entry Declaration Source Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "declaration-source-duplicate-identity"),
                        LogFields.Field("source", source),
                        LogFields.Field("duplicateRejected", true),
                        LogFields.Field("blockingIssues", duplicateResult.BlockingIssueCount),
                        LogFields.Field("nonBlockingIssues", duplicateResult.NonBlockingIssueCount),
                        LogFields.Field("summary", duplicateResult.Summary)));

                return Task.FromResult(true);
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Object Entry Declaration Source Smoke step failed.",
                    LogFields.Of(
                        LogFields.Field("step", "declaration-source"),
                        LogFields.Field("reason", exception.Message)));
                return Task.FromResult(false);
            }
            finally
            {
                DestroyQaObject(routeObject);
                DestroyQaObject(activityObject);
                DestroyQaObject(duplicateObject);
            }
        }

        private static bool ValidateSet(
            FrameworkLogger logger,
            ObjectEntrySet set,
            ObjectEntryDescriptor routeRequired,
            ObjectEntryDescriptor activityRequired)
        {
            if (set.Count != 3 || set.RequiredCount != 2 || set.OptionalCount != 1)
            {
                logger.Warning($"QA Object Entry Synthetic Set Smoke step failed. step='synthetic-set' reason='Unexpected aggregate counts'. {set.Summary}");
                return false;
            }

            if (set.GetByScope(ObjectEntryScope.Route).Count != 2 || set.GetByScope(ObjectEntryScope.Activity).Count != 1 || set.GetByScope(ObjectEntryScope.Session).Count != 0)
            {
                logger.Warning($"QA Object Entry Synthetic Set Smoke step failed. step='synthetic-set' reason='Unexpected scope counts'. {set.Summary}");
                return false;
            }

            if (!set.TryGet(routeRequired.Id, out var foundRoute) || !foundRoute.Equals(routeRequired))
            {
                logger.Warning("QA Object Entry Synthetic Set Smoke step failed. step='synthetic-set' reason='Required Route object entry was not retrievable by id'.");
                return false;
            }

            if (!set.TryGet(activityRequired.Id, out var foundActivity) || !foundActivity.Equals(activityRequired))
            {
                logger.Warning("QA Object Entry Synthetic Set Smoke step failed. step='synthetic-set' reason='Required Activity object entry was not retrievable by id'.");
                return false;
            }

            return true;
        }

        private static bool ValidateAcceptedResult(FrameworkLogger logger, ObjectEntryResult result)
        {
            if (!result.Succeeded || result.Failed || result.BlockingIssueCount != 0)
            {
                logger.Warning($"QA Object Entry Synthetic Set Smoke step failed. step='result' reason='Accepted result did not report success'. {result.Summary}");
                return false;
            }

            return true;
        }


        private static bool ValidateDeclarationSourceResult(
            FrameworkLogger logger,
            ObjectEntryDeclarationSourceResult result,
            ObjectEntryDeclaration routeDeclaration,
            ObjectEntryDeclaration activityDeclaration)
        {
            if (result == null || result.Failed || result.BlockingIssueCount != 0)
            {
                logger.Warning($"QA Object Entry Declaration Source Smoke step failed. step='declaration-source-set' reason='Result failed'. summary='{result?.Summary ?? "<missing>"}'");
                return false;
            }

            if (result.DeclarationCount != 2 || result.AcceptedDeclarationCount != 2 || result.RejectedDeclarationCount != 0)
            {
                logger.Warning($"QA Object Entry Declaration Source Smoke step failed. step='declaration-source-set' reason='Unexpected declaration counts'. {result.Summary}");
                return false;
            }

            var set = result.ObjectEntries;
            if (set.Count != 2 || set.RequiredCount != 1 || set.OptionalCount != 1)
            {
                logger.Warning($"QA Object Entry Declaration Source Smoke step failed. step='declaration-source-set' reason='Unexpected ObjectEntrySet counts'. {result.Summary}");
                return false;
            }

            if (set.GetByScope(ObjectEntryScope.Route).Count != 1 || set.GetByScope(ObjectEntryScope.Activity).Count != 1 || set.GetByScope(ObjectEntryScope.Session).Count != 0)
            {
                logger.Warning($"QA Object Entry Declaration Source Smoke step failed. step='declaration-source-set' reason='Unexpected ObjectEntrySet scope counts'. {result.Summary}");
                return false;
            }

            var routeDescriptor = routeDeclaration.CreateDescriptor();
            var activityDescriptor = activityDeclaration.CreateDescriptor();
            if (!set.TryGet(routeDescriptor.Id, out var foundRoute) || !foundRoute.Equals(routeDescriptor))
            {
                logger.Warning("QA Object Entry Declaration Source Smoke step failed. step='declaration-source-set' reason='Route declaration descriptor was not retrievable by id'.");
                return false;
            }

            if (!set.TryGet(activityDescriptor.Id, out var foundActivity) || !foundActivity.Equals(activityDescriptor))
            {
                logger.Warning("QA Object Entry Declaration Source Smoke step failed. step='declaration-source-set' reason='Activity declaration descriptor was not retrievable by id'.");
                return false;
            }

            return true;
        }

        private static bool ValidateDeclarationSourceDuplicateRejection(
            FrameworkLogger logger,
            ObjectEntryDeclarationSourceResult result)
        {
            if (result == null)
            {
                logger.Warning("QA Object Entry Declaration Source Smoke step failed. step='declaration-source-duplicate-identity' reason='Duplicate result was missing'.");
                return false;
            }

            bool hasDuplicateIssue = result.Issues.Any(issue => issue.Kind == ObjectEntryIssueKind.DuplicateIdentity && issue.IsBlocking);
            if (!result.Failed || result.BlockingIssueCount == 0 || !hasDuplicateIssue)
            {
                logger.Warning($"QA Object Entry Declaration Source Smoke step failed. step='declaration-source-duplicate-identity' reason='Duplicate object entry identity was accepted'. {result.ToDiagnosticString()}");
                return false;
            }

            return true;
        }

        private static void DestroyQaObject(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(gameObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private static bool ValidateDuplicateRejection(
            FrameworkLogger logger,
            ObjectEntryDescriptor first,
            ObjectEntryDescriptor second)
        {
            try
            {
                _ = new ObjectEntrySet(new[] { first, first, second });
                logger.Warning("QA Object Entry Synthetic Set Smoke step failed. step='duplicate-identity' reason='Duplicate object entry identity was accepted'.");
                return false;
            }
            catch (ArgumentException)
            {
                logger.Info(
                    "QA Object Entry Synthetic Set Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "duplicate-identity"),
                        LogFields.Field("duplicateRejected", true),
                        LogFields.Field("objectEntry", first.Id.StableText)));
                return true;
            }
        }
    }
}
#endif
