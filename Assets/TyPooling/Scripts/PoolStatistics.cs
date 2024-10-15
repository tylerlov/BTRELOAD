using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Typooling
{
    public class PoolStatistics
    {
        public int PoolSize { get { return poolSize; } }
        public int PoolActiveCount { get { return poolActiveCount; } }
        public int PoolInactiveCount { get { return poolInactiveCount; } }

        private int poolSize;
        private int poolActiveCount;
        private int poolInactiveCount;

        public PoolStatistics(Pooler pooler)
        {
            poolActiveCount = GetPoolActiveCount(pooler);
            poolInactiveCount = GetPoolInactiveCount(pooler);
            poolSize = PoolActiveCount + poolInactiveCount;
        }

        private int GetPoolActiveCount(Pooler pooler)
        {
            int c = 0;
            for (int a = 0; a < pooler.Pool.Count; a++)
            {
                if (pooler.Pool[a].IsActive() && pooler.Pool[a].GetObject() != null)
                {
                    c++;
                }
            }

            return c;
        }

        private int GetPoolInactiveCount(Pooler pooler)
        {
            int c = 0;
            for (int a = 0; a < pooler.Pool.Count; a++)
            {
                if (!pooler.Pool[a].IsActive() && pooler.Pool[a].GetObject() != null)
                {
                    c++;
                }
            }

            return c;
        }
    }
}
