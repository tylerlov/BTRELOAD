using UnityEngine;

namespace HohStudios.Tools.ObjectParticleSpawner.Demo
{
    /// <summary>
    /// The player demo component is used in the Object Particle system demo scenes.
    /// 
    /// A player controller was added to navigate the demo scenes in a fun way.
    /// It is a very simple character controller implementation that is not intended for actual game use.
    /// 
    /// Hit ESC to free the mouse from the look behaviour for editor use.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerDemo : MonoBehaviour
    {
        [SerializeField] private Camera _playerCamera;
        [SerializeField] private ParticleSystem _weaponParticleSystem;

        private CharacterController _controller;
        private Vector3 _velocity;

        private float _horizontalInput;
        private float _verticalInput;
        private bool _escaped = false;

        private void Start()
        {
            _controller = GetComponent<CharacterController>();

            if (_weaponParticleSystem)
                _weaponParticleSystem.Stop();

            if (!_playerCamera)
                _playerCamera = Camera.main;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                _escaped = !_escaped;

            if (_escaped)
                return;

            // Don't do anything for a half second (undesirable initial behaviour)
            if (Time.time < 0.5f)
                return;

            var deltaTime = Time.deltaTime;

            // Get the keyboard inputs
            var forward = Input.GetAxis("Vertical");
            var horizontal = Input.GetAxis("Horizontal");

            // Get the move direction based on the camera's forward and camera's right
            var moveDirection = (_playerCamera.transform.forward.normalized * forward) +
                                (_playerCamera.transform.right * horizontal);

            // No Y movement allowed in this controller
            moveDirection.y = 0;

            // Move the player toward the move direction as desired
            _controller.Move(moveDirection * deltaTime * 15);

            float velocity = 0;
            float velocity2 = 0;

            // We use smooth damping for the rotational mouse inputs to make the rotations less jittery
            _horizontalInput = Mathf.SmoothDamp(_horizontalInput, Input.GetAxis("Mouse X") / Screen.width, ref velocity, 0.035f,
                Mathf.Infinity, deltaTime);
            _verticalInput = Mathf.SmoothDamp(_verticalInput, Input.GetAxis("Mouse Y") / Screen.height, ref velocity2, 0.035f,
                Mathf.Infinity, deltaTime);

            // Perform horizontal rotations
            _playerCamera.transform.RotateAround(transform.position + (Vector3.up * 0.6f), Vector3.up, _horizontalInput * 2500);

            var vertRotation = _verticalInput * 2500;
            var angle = Vector3.SignedAngle(_playerCamera.transform.forward, transform.up, -_playerCamera.transform.right);

            // limit rotation in vertical axis
            if ((vertRotation + angle < 45 && vertRotation > 0) || (vertRotation + angle > 145 && vertRotation < 0))
                vertRotation = 0;

            // Perform vertical rotations
            _playerCamera.transform.RotateAround(transform.position + (Vector3.up * 0.6f), -_playerCamera.transform.right, vertRotation);

            // Shoot particles from the particle system only while mouse button (0) is held by starting/stopping the particle system
            if (_weaponParticleSystem)
            {
                if (UnityEngine.Input.GetMouseButton(0))
                {
                    if (_weaponParticleSystem && _weaponParticleSystem.isStopped)
                        _weaponParticleSystem.Play();
                }
                else
                {
                    if (_weaponParticleSystem && _weaponParticleSystem.isPlaying)
                        _weaponParticleSystem.Stop();
                }
            }
        }
    }
}
