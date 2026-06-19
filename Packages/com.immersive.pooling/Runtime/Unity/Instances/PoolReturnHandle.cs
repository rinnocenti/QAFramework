using System;
using Immersive.Pooling.Unity.Pools;
using UnityEngine;

namespace Immersive.Pooling.Unity.Instances
{
    public sealed class PoolReturnHandle : MonoBehaviour
    {
        private GameObjectPool _pool;
        private bool _isReturned = true;

        public bool HasPool => _pool != null;

        public bool IsReturned => _isReturned;

        public void Bind(GameObjectPool pool)
        {
            if (pool == null)
            {
                throw new ArgumentNullException(nameof(pool));
            }

            _pool = pool;
            _isReturned = false;
        }

        public bool ReturnToPool()
        {
            if (_pool == null || _isReturned)
            {
                return false;
            }

            if (!_pool.Return(gameObject))
            {
                return false;
            }

            _isReturned = true;
            return true;
        }

        public void ClearBinding()
        {
            _pool = null;
            _isReturned = true;
        }

        internal void MarkReturned()
        {
            _isReturned = true;
        }
    }
}
