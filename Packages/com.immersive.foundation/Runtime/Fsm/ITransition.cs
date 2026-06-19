namespace Immersive.Foundation.Fsm
{
    public interface ITransition
    {
        IState TargetState { get; }

        bool CanTransition();
    }
}
