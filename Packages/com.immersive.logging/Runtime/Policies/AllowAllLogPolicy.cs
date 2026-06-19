using System;
using Immersive.Logging.Records;

namespace Immersive.Logging.Policies
{
    public sealed class AllowAllLogPolicy : ILogPolicy
    {
        public bool ShouldWrite(LogRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            return true;
        }
    }
}
