using Immersive.Foundation.Validation;

namespace Immersive.Foundation.Fsm
{
    public sealed class Transition : ITransition
    {
        private readonly IPredicate _predicate;

        public Transition(IState targetState, IPredicate predicate)
        {
            TargetState = Preconditions.NotNull(targetState, nameof(targetState));
            _predicate = Preconditions.NotNull(predicate, nameof(predicate));
        }

        public IState TargetState { get; }

        public bool CanTransition()
        {
            return _predicate.Evaluate();
        }
    }
}
