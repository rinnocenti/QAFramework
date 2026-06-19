using System.Collections.Generic;
using Immersive.Foundation.Validation;

namespace Immersive.Foundation.Fsm
{
    public sealed class StateMachine
    {
        private readonly List<ITransition> _anyTransitions = new List<ITransition>();
        private readonly List<StateTransitionGroup> _transitions = new List<StateTransitionGroup>();
        private IState _currentState;

        public IState CurrentState => _currentState;

        public void SetState(IState state)
        {
            state = Preconditions.NotNull(state, nameof(state));

            if (ReferenceEquals(_currentState, state))
            {
                return;
            }

            var previousState = _currentState;
            previousState?.OnExit();

            _currentState = state;
            _currentState.OnEnter();
        }

        public void AddTransition(IState from, IState to, IPredicate predicate)
        {
            from = Preconditions.NotNull(from, nameof(from));
            to = Preconditions.NotNull(to, nameof(to));
            predicate = Preconditions.NotNull(predicate, nameof(predicate));

            GetOrCreateGroup(from).Transitions.Add(new Transition(to, predicate));
        }

        public void AddAnyTransition(IState to, IPredicate predicate)
        {
            to = Preconditions.NotNull(to, nameof(to));
            predicate = Preconditions.NotNull(predicate, nameof(predicate));

            _anyTransitions.Add(new Transition(to, predicate));
        }

        public void Tick()
        {
            if (TryTransition(_anyTransitions))
            {
                return;
            }

            var currentTransitions = GetTransitionsForCurrentState();
            if (TryTransition(currentTransitions))
            {
                return;
            }

            _currentState?.Tick();
        }

        private bool TryTransition(IReadOnlyList<ITransition> transitions)
        {
            if (transitions == null)
            {
                return false;
            }

            for (int i = 0; i < transitions.Count; i++)
            {
                var transition = transitions[i];
                if (!transition.CanTransition())
                {
                    continue;
                }

                SetState(transition.TargetState);
                return true;
            }

            return false;
        }

        private IReadOnlyList<ITransition> GetTransitionsForCurrentState()
        {
            if (_currentState == null)
            {
                return null;
            }

            for (int i = 0; i < _transitions.Count; i++)
            {
                if (ReferenceEquals(_transitions[i].From, _currentState))
                {
                    return _transitions[i].Transitions;
                }
            }

            return null;
        }

        private StateTransitionGroup GetOrCreateGroup(IState from)
        {
            for (int i = 0; i < _transitions.Count; i++)
            {
                if (ReferenceEquals(_transitions[i].From, from))
                {
                    return _transitions[i];
                }
            }

            var group = new StateTransitionGroup(from);
            _transitions.Add(group);
            return group;
        }

        private sealed class StateTransitionGroup
        {
            public StateTransitionGroup(IState from)
            {
                From = from;
                Transitions = new List<ITransition>();
            }

            public IState From { get; }

            public List<ITransition> Transitions { get; }
        }
    }
}
