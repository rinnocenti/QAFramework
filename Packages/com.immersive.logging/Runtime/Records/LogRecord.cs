using System;
using Immersive.Logging.Levels;

namespace Immersive.Logging.Records
{
    public sealed class LogRecord
    {
        private readonly LogLevel _level;
        private readonly string _message;
        private readonly string _category;
        private readonly string _context;
        private readonly Exception _exception;
        private readonly DateTime _timestampUtc;

        public LogRecord(
            LogLevel level,
            string message,
            string category = null,
            string context = null,
            Exception exception = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message cannot be null, empty, or whitespace.", nameof(message));
            }

            _level = level;
            _message = message;
            _category = category;
            _context = context;
            _exception = exception;
            _timestampUtc = DateTime.UtcNow;
        }

        public LogLevel Level => _level;

        public string Message => _message;

        public string Category => _category;

        public string Context => _context;

        public Exception Exception => _exception;

        public DateTime TimestampUtc => _timestampUtc;
    }
}
