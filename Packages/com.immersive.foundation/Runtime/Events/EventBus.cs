using System;
using System.Collections.Generic;
using Immersive.Foundation.Validation;

namespace Immersive.Foundation.Events
{
    public sealed class EventBus<TEvent>
        where TEvent : class, IEvent
    {
        private readonly List<EventBinding<TEvent>> _bindings = new List<EventBinding<TEvent>>();

        public int SubscriberCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _bindings.Count; i++)
                {
                    if (!_bindings[i].IsDisposed)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public IEventBinding Subscribe(Action<TEvent> handler)
        {
            var binding = new EventBinding<TEvent>(Preconditions.NotNull(handler, nameof(handler)));
            _bindings.Add(binding);
            return binding;
        }

        public bool Unsubscribe(IEventBinding binding)
        {
            var typedBinding = Preconditions.NotNull(binding, nameof(binding)) as EventBinding<TEvent>;
            if (typedBinding == null)
            {
                return false;
            }

            int index = _bindings.IndexOf(typedBinding);
            if (index < 0)
            {
                return false;
            }

            _bindings.RemoveAt(index);
            typedBinding.Dispose();
            return true;
        }

        public int Publish(TEvent evt)
        {
            evt = Preconditions.NotNull(evt, nameof(evt));

            var snapshot = new EventBinding<TEvent>[_bindings.Count];
            _bindings.CopyTo(snapshot, 0);

            int invoked = 0;
            for (int i = 0; i < snapshot.Length; i++)
            {
                if (snapshot[i].TryInvoke(evt))
                {
                    invoked++;
                }
            }

            return invoked;
        }

        public void Clear()
        {
            for (int i = 0; i < _bindings.Count; i++)
            {
                _bindings[i].Dispose();
            }

            _bindings.Clear();
        }
    }
}
