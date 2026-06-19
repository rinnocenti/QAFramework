using Immersive.Logging.Records;

namespace Immersive.Logging.Sinks
{
    public interface ILogSink
    {
        void Write(LogRecord record);
    }
}
