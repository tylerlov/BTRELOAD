using UnityEngine;
using UnityEngine.AI;

namespace HohStudios.Tools.ObjectParticleSpawner.Demo
{
    /// <summary>
    /// Navmesh behaviour used in the Weapon Demo Scene of the Object Particle Spawner
    /// 
    /// Simply moves the spawned objects toward the "bad guy" capsule via navmesh just for fun, after they're spawned
    /// </summary>
    [RequireComponent(typeof(ObjectParticle))]
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Rigidbody))]
    public class ObjectNavmeshBehaviour : MonoBehaviour
    {
        [SerializeField]
        private Transform _badGuy;
        private NavMeshAgent _agent;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.enabled = false;
        }


        private void Update()
        {
            if (!_badGuy)
                return;

            // On update, move the objects toward the "Bad guy capsule", stopping when it gets close enough
            if (Vector3.Distance(transform.position, _badGuy.position) > 2f)
            {
                // Make sure its on the nav mesh after spawning it before moving
                if (_agent.enabled && _agent.isOnNavMesh)
                    _agent.SetDestination(_badGuy.position);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            // On collision, stop the particle from moving undesirably when we activate the navmesh agent
            if (_agent.enabled == false && collision.gameObject.CompareTag("Ground"))
            {
                _agent.enabled = true;
                _agent.velocity = Vector3.zero;

                var rbody = GetComponent<Rigidbody>();
                if (rbody)
                    rbody.linearVelocity = Vector3.zero;
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            // On collision, stop the particle from moving undesirably when we activate the navmesh agent
            if (_agent.enabled == false && collision.gameObject.CompareTag("Ground"))
            {
                _agent.enabled = true;
                _agent.velocity = Vector3.zero;

                var rbody = GetComponent<Rigidbody>();
                if (rbody)
                    rbody.linearVelocity = Vector3.zero;
            }
        }
    }
}
