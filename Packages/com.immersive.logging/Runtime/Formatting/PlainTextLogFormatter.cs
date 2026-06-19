using System;
using System.Globalization;
using System.Text;
using Immersive.Logging.Records;

namespace Immersive.Logging.Formatting
{
    public sealed class PlainTextLogFormatter : ILogFormatter
    {
        public string Format(LogRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            var builder = new StringBuilder();

            builder.Append(record.TimestampUtc.ToString("O", CultureInfo.InvariantCulture));
            builder.Append(" [");
            builder.Append(record.Level);
            builder.Append("]");

            if (!string.IsNullOrWhiteSpace(record.Category))
            {
                builder.Append(" category=");
                builder.Append(record.Category);
            }

            if (!string.IsNullOrWhiteSpace(record.Context))
            {
                builder.Append(" context=");
                builder.Append(record.Context);
            }

            builder.Append(" message=");
            builder.Append(record.Message);

            if (record.Exception != null)
            {
                builder.Append(" exception=");
                builder.Append(record.Exception.GetType().FullName ?? record.Exception.GetType().Name);
                builder.Append(": ");
                builder.Append(record.Exception.Message);
            }

            return builder.ToString();
        }
    }
}
