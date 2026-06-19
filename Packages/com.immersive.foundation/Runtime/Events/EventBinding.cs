using System;
using Immersive.Foundation.Validation;

namespace Immersive.Foundation.Events
{
    public sealed class EventBinding<TEvent> : IEventBinding
        where TEvent : class, IEvent
    {
        private readonly Action<TEvent> _handler;
        private bool _isDisposed;

        public EventBinding(Action<TEvent> handler)
        {
            _handler = Preconditions.NotNull(handler, nameof(handler));
        }

        public bool IsDisposed => _isDisposed;

        public bool Matches(Action<TEvent> handler)
        {
            Preconditions.NotNull(handler, nameof(handler));
            return !IsDisposed && _handler == handler;
        }

        internal bool TryInvoke(TEvent evt)
        {
            if (IsDisposed)
            {
                return false;
            }

            _handler(evt);
            return true;
        }

        public void Dispose()
        {
            _isDisposed = true;
        }
    }
}
