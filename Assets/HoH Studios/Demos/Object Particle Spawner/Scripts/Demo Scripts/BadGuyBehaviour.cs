using UnityEngine;
using UnityEngine.AI;

namespace HohStudios.Tools.ObjectParticleSpawner.Demo
{
    /// <summary>
    /// Short script used for the Weapon Demo scene of the object particle spawner.
    /// 
    /// Simply moves the "bad guy" capsule toward the player via navmesh
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class BadGuyBehaviour : MonoBehaviour
    {
        [SerializeField]
        private Transform _player;
        private NavMeshAgent _agent;

        private void Start()
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        private void Update()
        {
            if (!_player)
                return;

            // Move the capsule toward the player by setting the navmesh position, stopping when it gets close enough
            if (Vector3.Distance(transform.position, _player.transform.position) > 3)
                _agent.SetDestination(_player.position);
        }
    }
}
