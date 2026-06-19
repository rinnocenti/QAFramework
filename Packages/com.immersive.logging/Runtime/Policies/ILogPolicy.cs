using Immersive.Logging.Records;

namespace Immersive.Logging.Policies
{
    public interface ILogPolicy
    {
        bool ShouldWrite(LogRecord record);
    }
}
