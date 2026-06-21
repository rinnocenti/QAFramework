using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Foundation.Events;
using Immersive.Framework.Diagnostics;
using UnityEngine;

namespace Immersive.Framework.CameraFlow
{
    /// <summary>
    /// Session-local authority for semantic camera requests.
    /// It decides which virtual camera request should be live; concrete adapters translate that decision.
    /// </summary>
    public static class FrameworkCameraAuthority
    {
        private const string None = "<none>";

        private static readonly Dictionary<IFrameworkCameraRequestDriver, FrameworkCameraRequest> Requests = new Dictionary<IFrameworkCameraRequestDriver, FrameworkCameraRequest>();
        private static readonly EventBus<FrameworkCameraActivatedEvent> ActivatedEvents = new EventBus<FrameworkCameraActivatedEvent>();
        private static readonly EventBus<FrameworkCameraDeactivatedEvent> DeactivatedEvents = new EventBus<FrameworkCameraDeactivatedEvent>();
        private static readonly FrameworkLogger Logger = FrameworkLogger.Create();

        private static FrameworkCameraRequest _activeRequest;
        private static int _sequence;

        public static FrameworkCameraRequest ActiveRequest => IsValid(_activeRequest) ? _activeRequest : null;

        public static bool HasActiveRequest => ActiveRequest != null;

        public static IEventBinding SubscribeActivated(Action<FrameworkCameraActivatedEvent> handler)
        {
            return ActivatedEvents.Subscribe(handler);
        }

        public static IEventBinding SubscribeDeactivated(Action<FrameworkCameraDeactivatedEvent> handler)
        {
            return DeactivatedEvents.Subscribe(handler);
        }

        internal static void RegisterOrUpdate(
            MonoBehaviour owner,
            IFrameworkCameraRequestDriver driver,
            Unity.Cinemachine.CinemachineCamera cinemachineCamera,
            FrameworkCameraScope scope,
            int priorityOffset,
            string source,
            string reason)
        {
            if (owner == null || driver == null || cinemachineCamera == null)
            {
                Logger.Error(
                    $"Camera request rejected. owner='{FormatName(owner)}' driver='{FormatDriver(driver)}' cinemachineCamera='{FormatName(cinemachineCamera)}' reason='request_invalid'.");
                return;
            }

            if (scope == FrameworkCameraScope.Auto)
            {
                Logger.Error(
                    $"Camera request rejected. owner='{FormatName(owner)}' cinemachineCamera='{FormatName(cinemachineCamera)}' reason='scope_auto_unresolved'.");
                return;
            }

            var request = new FrameworkCameraRequest(
                owner,
                driver,
                cinemachineCamera,
                scope,
                priorityOffset,
                ++_sequence,
                Normalize(source),
                Normalize(reason));

            Requests[driver] = request;
            LogRequestRegistered(request);
            RefreshActiveRequest("request-registered");
        }

        internal static void Release(IFrameworkCameraRequestDriver driver, string source, string reason)
        {
            if (driver == null)
            {
                return;
            }

            if (!Requests.TryGetValue(driver, out var request))
            {
                return;
            }

            bool wasActive = ReferenceEquals(_activeRequest, request);
            Requests.Remove(driver);
            driver.ApplyCameraAuthorityState(request, false);

            Logger.Info(
                $"Camera request released. camera='{FormatCamera(request)}' scope='{request.Scope}' priority='{request.EffectivePriority}' sequence='{request.Sequence}' wasActive='{wasActive}' source='{FormatValue(source)}' reason='{FormatValue(reason)}'.");

            RefreshActiveRequest("request-released");
        }

        private static void RefreshActiveRequest(string resolutionReason)
        {
            RemoveInvalidRequests();

            FrameworkCameraRequest previous = _activeRequest;
            FrameworkCameraRequest next = ResolveBestRequest();
            _activeRequest = next;

            foreach (FrameworkCameraRequest request in Requests.Values)
            {
                request.Driver?.ApplyCameraAuthorityState(request, ReferenceEquals(request, next));
            }

            if (ReferenceEquals(previous, next))
            {
                LogResolutionUnchanged(next, resolutionReason);
                return;
            }

            if (next != null)
            {
                LogResolution(next, previous, resolutionReason);
                ActivatedEvents.Publish(new FrameworkCameraActivatedEvent(next, previous, resolutionReason));
                return;
            }

            if (previous != null)
            {
                Logger.Warning(
                    $"Camera Authority has no active camera request. previous='{FormatCamera(previous)}' reason='{FormatValue(resolutionReason)}'.");
                DeactivatedEvents.Publish(new FrameworkCameraDeactivatedEvent(previous, resolutionReason));
            }
        }

        private static FrameworkCameraRequest ResolveBestRequest()
        {
            FrameworkCameraRequest best = null;
            foreach (FrameworkCameraRequest request in Requests.Values)
            {
                if (!IsValid(request))
                {
                    continue;
                }

                if (best == null
                    || request.EffectivePriority > best.EffectivePriority
                    || request.EffectivePriority == best.EffectivePriority && request.Sequence > best.Sequence)
                {
                    best = request;
                }
            }

            return best;
        }

        private static void RemoveInvalidRequests()
        {
            List<IFrameworkCameraRequestDriver> invalidDrivers = null;

            foreach (KeyValuePair<IFrameworkCameraRequestDriver, FrameworkCameraRequest> pair in Requests)
            {
                if (IsValid(pair.Value))
                {
                    continue;
                }

                invalidDrivers ??= new List<IFrameworkCameraRequestDriver>();
                invalidDrivers.Add(pair.Key);
            }

            if (invalidDrivers == null)
            {
                return;
            }

            for (int i = 0; i < invalidDrivers.Count; i++)
            {
                Requests.Remove(invalidDrivers[i]);
            }
        }

        private static bool IsValid(FrameworkCameraRequest request)
        {
            return request != null
                && request.Owner != null
                && request.Driver != null
                && request.CinemachineCamera != null;
        }

        private static void LogRequestRegistered(FrameworkCameraRequest request)
        {
            Logger.Info(
                $"Camera request registered. camera='{FormatCamera(request)}' scope='{request.Scope}' priority='{request.EffectivePriority}' sequence='{request.Sequence}' source='{FormatValue(request.Source)}' reason='{FormatValue(request.Reason)}' candidates='{BuildCandidatesMessage()}'.");
        }

        private static void LogResolution(
            FrameworkCameraRequest active,
            FrameworkCameraRequest previous,
            string reason)
        {
            Logger.Info(
                $"Camera Authority resolved active camera. active='{FormatCamera(active)}' previous='{FormatCamera(previous)}' scope='{active.Scope}' priority='{active.EffectivePriority}' sequence='{active.Sequence}' source='{FormatValue(active.Source)}' reason='{FormatValue(active.Reason)}' resolutionReason='{FormatValue(reason)}' candidates='{BuildCandidatesMessage()}'.");
        }

        private static void LogResolutionUnchanged(
            FrameworkCameraRequest active,
            string reason)
        {
            if (active == null)
            {
                Logger.Warning(
                    $"Camera Authority has no camera candidates. reason='{FormatValue(reason)}'.");
                return;
            }

            Logger.Info(
                $"Camera Authority kept active camera. active='{FormatCamera(active)}' scope='{active.Scope}' priority='{active.EffectivePriority}' sequence='{active.Sequence}' resolutionReason='{FormatValue(reason)}' candidates='{BuildCandidatesMessage()}'.");
        }

        private static string BuildCandidatesMessage()
        {
            if (Requests.Count == 0)
            {
                return None;
            }

            var candidates = new List<FrameworkCameraRequest>();
            foreach (FrameworkCameraRequest request in Requests.Values)
            {
                if (IsValid(request))
                {
                    candidates.Add(request);
                }
            }

            candidates.Sort((left, right) =>
            {
                int priority = right.EffectivePriority.CompareTo(left.EffectivePriority);
                return priority != 0 ? priority : right.Sequence.CompareTo(left.Sequence);
            });

            if (candidates.Count == 0)
            {
                return None;
            }

            var builder = new StringBuilder();
            for (int i = 0; i < candidates.Count; i++)
            {
                FrameworkCameraRequest request = candidates[i];
                if (i > 0)
                {
                    builder.Append(" | ");
                }

                builder.Append(FormatCamera(request));
                builder.Append(" scope=");
                builder.Append(request.Scope);
                builder.Append(" priority=");
                builder.Append(request.EffectivePriority);
                builder.Append(" sequence=");
                builder.Append(request.Sequence);
            }

            return builder.ToString();
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string FormatCamera(FrameworkCameraRequest request)
        {
            return request != null ? request.CameraName : None;
        }

        private static string FormatName(UnityEngine.Object target)
        {
            return target != null ? target.name : None;
        }

        private static string FormatDriver(IFrameworkCameraRequestDriver driver)
        {
            return driver != null ? driver.DriverName : None;
        }

        private static string FormatValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? None : value.Trim();
        }
    }
}
