using Immersive.Pooling.Contracts;
using UnityEngine;

namespace Immersive.Pooling.Unity.Instances
{
    public abstract class PoolableBehaviour : MonoBehaviour, IPoolable
    {
        public bool IsTakenFromPool { get; private set; }

        public void OnTakenFromPool()
        {
            IsTakenFromPool = true;
            HandleTakenFromPool();
        }

        public void OnReturnedToPool()
        {
            IsTakenFromPool = false;
            HandleReturnedToPool();
        }

        protected virtual void HandleTakenFromPool()
        {
        }

        protected virtual void HandleReturnedToPool()
        {
        }
    }
}
