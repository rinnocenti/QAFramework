using System;
using Immersive.Logging.Levels;
using Immersive.Logging.Records;

namespace Immersive.Logging.Policies
{
    public sealed class MinimumLogLevelPolicy : ILogPolicy
    {
        public MinimumLogLevelPolicy(LogLevel minimumLevel)
        {
            MinimumLevel = minimumLevel;
        }

        public LogLevel MinimumLevel { get; }

        public bool ShouldWrite(LogRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            return record.Level >= MinimumLevel;
        }
    }
}
