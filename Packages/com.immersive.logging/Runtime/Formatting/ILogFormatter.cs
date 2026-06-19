using Immersive.Logging.Records;

namespace Immersive.Logging.Formatting
{
    public interface ILogFormatter
    {
        string Format(LogRecord record);
    }
}
