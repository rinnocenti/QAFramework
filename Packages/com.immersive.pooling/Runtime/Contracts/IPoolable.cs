namespace Immersive.Pooling.Contracts
{
    public interface IPoolable
    {
        void OnTakenFromPool();

        void OnReturnedToPool();
    }
}
