using UnityEngine;
using UnityEngine.UI;

namespace OccaSoftware.BOP
{
    public class GetPoolDataExample : MonoBehaviour
    {
        [SerializeField] Text text;
        [SerializeField] Pooler pooler;

        void LateUpdate()
        {
            PoolStatistics stats = pooler.GetPoolStats();
            text.text = "Pool Size: " + stats.PoolSize + "\nPool Active Count: " + stats.PoolActiveCount + "\nPool Inactive Count: " + stats.PoolInactiveCount;
        }
    }
}