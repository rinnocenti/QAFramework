using System;
using Immersive.Logging.Formatting;
using Immersive.Logging.Levels;
using Immersive.Logging.Records;
using Immersive.Logging.Sinks;
using UnityEngine;

namespace Immersive.Logging.Unity
{
    public sealed class UnityConsoleLogSink : ILogSink
    {
        private readonly ILogFormatter _formatter;

        public UnityConsoleLogSink(ILogFormatter formatter)
        {
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        }

        public void Write(LogRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            string message = _formatter.Format(record);

            switch (record.Level)
            {
                case LogLevel.Warning:
                    Debug.LogWarning(message);
                    return;
                case LogLevel.Error:
                case LogLevel.Fatal:
                    Debug.LogError(message);
                    return;
                default:
                    Debug.Log(message);
                    return;
            }
        }
    }
}
