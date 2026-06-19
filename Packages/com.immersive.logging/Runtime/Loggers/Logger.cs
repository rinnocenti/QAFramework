using System;
using Immersive.Logging.Levels;
using Immersive.Logging.Policies;
using Immersive.Logging.Records;
using Immersive.Logging.Sinks;

namespace Immersive.Logging.Loggers
{
    public sealed class Logger
    {
        private readonly ILogSink _sink;
        private readonly ILogPolicy _policy;

        public Logger(ILogSink sink, ILogPolicy policy = null)
        {
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            _policy = policy;
        }

        public void Trace(string message, string category = null, string context = null)
        {
            Write(new LogRecord(LogLevel.Trace, message, category, context));
        }

        public void Debug(string message, string category = null, string context = null)
        {
            Write(new LogRecord(LogLevel.Debug, message, category, context));
        }

        public void Info(string message, string category = null, string context = null)
        {
            Write(new LogRecord(LogLevel.Info, message, category, context));
        }

        public void Warning(string message, string category = null, string context = null)
        {
            Write(new LogRecord(LogLevel.Warning, message, category, context));
        }

        public void Error(string message, string category = null, string context = null, Exception exception = null)
        {
            Write(new LogRecord(LogLevel.Error, message, category, context, exception));
        }

        public void Fatal(string message, string category = null, string context = null, Exception exception = null)
        {
            Write(new LogRecord(LogLevel.Fatal, message, category, context, exception));
        }

        public void Write(LogRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            if (_policy != null && !_policy.ShouldWrite(record))
            {
                return;
            }

            _sink.Write(record);
        }
    }
}
