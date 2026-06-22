using Immersive.Logging.Formatting;
using Immersive.Logging.Loggers;
using Immersive.Logging.Policies;
using Immersive.Logging.Unity;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Diagnostics
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Framework logging adapter; not game-facing API.")]
    internal sealed class FrameworkLogger
    {
        private const string Prefix = "[Immersive Framework]";

        private readonly Logger _logger;

        private FrameworkLogger(Logger logger)
        {
            _logger = logger;
        }

        internal static FrameworkLogger Create()
        {
            return new FrameworkLogger(
                new Logger(
                    new UnityConsoleLogSink(new PlainTextLogFormatter()),
                    new AllowAllLogPolicy()));
        }

        internal void Info(string message)
        {
            _logger.Info(message, Prefix);
        }

        internal void Warning(string message)
        {
            _logger.Warning(message, Prefix);
        }

        internal void Error(string message)
        {
            _logger.Error(message, Prefix);
        }
    }
}
