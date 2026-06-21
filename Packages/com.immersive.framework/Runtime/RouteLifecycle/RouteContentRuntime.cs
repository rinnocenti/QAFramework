using System;
using Immersive.Framework.Authoring;
using Immersive.Framework.Diagnostics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// API status: Deferred until F3. This type is retained as frozen transitional source, not as active baseline API.
    /// Minimal owner for notifying Route-scoped scene content.
    /// It does not load scenes, start activities, spawn actors, or own Route identity.
    /// </summary>
    internal sealed class RouteContentRuntime
    {
        private readonly FrameworkLogger _logger = FrameworkLogger.Create();

        internal void ExitRouteContent(RouteAsset route, RouteAsset nextRoute, string source, string reason)
        {
            string resolvedSource = NormalizeSource(source);
            string resolvedReason = NormalizeReason(reason);

            if (route == null || ReferenceEquals(route, nextRoute))
            {
                return;
            }

            RouteContentBinding[] bindings = Object.FindObjectsByType<RouteContentBinding>(FindObjectsInactive.Include);
            if (bindings == null || bindings.Length == 0)
            {
                return;
            }

            for (int i = 0; i < bindings.Length; i++)
            {
                var binding = bindings[i];
                if (!IsValidBindingForRoute(binding, route))
                {
                    continue;
                }

                DispatchRouteContentExited(binding, route, nextRoute, resolvedSource, resolvedReason);
            }
        }

        internal void EnterRouteContent(RouteAsset route, RouteAsset previousRoute, string source, string reason)
        {
            string resolvedSource = NormalizeSource(source);
            string resolvedReason = NormalizeReason(reason);

            if (route == null || ReferenceEquals(route, previousRoute))
            {
                return;
            }

            RouteContentBinding[] bindings = Object.FindObjectsByType<RouteContentBinding>(FindObjectsInactive.Include);
            if (bindings == null || bindings.Length == 0)
            {
                return;
            }

            for (int i = 0; i < bindings.Length; i++)
            {
                var binding = bindings[i];
                if (!IsValidBindingForRoute(binding, route))
                {
                    continue;
                }

                DispatchRouteContentEntered(binding, route, previousRoute, resolvedSource, resolvedReason);
            }
        }


        private static string NormalizeSource(string source)
        {
            return string.IsNullOrWhiteSpace(source) ? "Unknown" : source.Trim();
        }

        private static string NormalizeReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "None" : reason.Trim();
        }
        private static bool IsValidBindingForRoute(RouteContentBinding binding, RouteAsset route)
        {
            return binding != null && binding.MatchesRoute(route);
        }

        private void DispatchRouteContentEntered(
            RouteContentBinding binding,
            RouteAsset route,
            RouteAsset previousRoute,
            string source,
            string reason)
        {
            var context = RouteContentLifecycleContext.Entered(route, previousRoute, binding, source, reason);
            DispatchRouteContentLifecycle(
                binding,
                "Entered",
                route,
                true,
                receiver => receiver.OnRouteContentEntered(context));
        }

        private void DispatchRouteContentExited(
            RouteContentBinding binding,
            RouteAsset route,
            RouteAsset nextRoute,
            string source,
            string reason)
        {
            var context = RouteContentLifecycleContext.Exited(route, nextRoute, binding, source, reason);
            DispatchRouteContentLifecycle(
                binding,
                "Exited",
                route,
                false,
                receiver => receiver.OnRouteContentExited(context));
        }

        private void DispatchRouteContentLifecycle(
            RouteContentBinding binding,
            string phase,
            RouteAsset route,
            bool parentFirst,
            Action<IRouteContentLifecycleReceiver> dispatch)
        {
            if (binding == null || dispatch == null)
            {
                return;
            }

            MonoBehaviour[] behaviours = binding.GetComponentsInChildren<MonoBehaviour>(true);
            if (behaviours == null || behaviours.Length == 0)
            {
                return;
            }

            int start = parentFirst ? 0 : behaviours.Length - 1;
            int end = parentFirst ? behaviours.Length : -1;
            int step = parentFirst ? 1 : -1;

            for (int i = start; i != end; i += step)
            {
                if (behaviours[i] is not IRouteContentLifecycleReceiver receiver)
                {
                    continue;
                }

                try
                {
                    dispatch(receiver);
                }
                catch (Exception exception)
                {
                    LogRouteContentReceiverException(binding, phase, route, receiver, exception);
                }
            }
        }

        private void LogRouteContentReceiverException(
            RouteContentBinding binding,
            string phase,
            RouteAsset route,
            IRouteContentLifecycleReceiver receiver,
            Exception exception)
        {
            string receiverType = receiver != null ? receiver.GetType().FullName : "<missing>";
            string routeName = route != null ? route.RouteName : "<none>";
            string exceptionType = exception != null ? exception.GetType().Name : "<unknown>";
            string exceptionMessage = exception != null ? exception.Message : string.Empty;

            _logger.Error(
                $"Route Content lifecycle receiver failed. phase='{FormatValue(phase)}' route='{FormatValue(routeName)}' object='{FormatValue(binding.ObjectName)}' scene='{FormatValue(binding.SceneName)}' receiver='{FormatValue(receiverType)}' exception='{FormatValue(exceptionType)}' message='{FormatValue(exceptionMessage)}'.");
        }

        private static string FormatValue(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "<empty>"
                : value.Replace("'", "\\'");
        }
    }
}
