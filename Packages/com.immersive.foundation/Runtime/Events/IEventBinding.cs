using System;

namespace Immersive.Foundation.Events
{
    public interface IEventBinding : IDisposable
    {
        bool IsDisposed { get; }
    }
}
