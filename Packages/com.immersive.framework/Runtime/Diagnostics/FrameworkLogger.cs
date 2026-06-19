using Immersive.Logging.Formatting;
using Immersive.Logging.Loggers;
using Immersive.Logging.Policies;
using Immersive.Logging.Records;
using Immersive.Logging.Unity;

namespace Immersive.Framework.Diagnostics
{
    internal sealed class FrameworkLogger
    {
        private const string Prefix = "[Immersive Framework]";

        private readonly Logger logger;

        private FrameworkLogger(Logger logger)
        {
            this.logger = logger;
        }

        internal static FrameworkLogger Create()
        {
            return new FrameworkLogger(
                new Logger(
                    new UnityConsoleLogSink(new FrameworkMessageFormatter()),
                    new AllowAllLogPolicy()));
        }

        internal void Info(string message)
        {
            logger.Info(Format(message));
        }

        internal void Warning(string message)
        {
            logger.Warning(Format(message));
        }

        internal void Error(string message)
        {
            logger.Error(Format(message));
        }

        private static string Format(string message)
        {
            return $"{Prefix} {message}";
        }

        private sealed class FrameworkMessageFormatter : ILogFormatter
        {
            public string Format(LogRecord record)
            {
                return record.Message;
            }
        }
    }
}
