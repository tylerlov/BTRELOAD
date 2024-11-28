using UnityEngine;
using Chronos;

public class SimpleMovement : MonoBehaviour
{
  [Header("Movement Speeds")]
  [Tooltip("Speed on Red (X) axis")]
  [SerializeField]
  private float redAxisSpeed = 0f;
  
  [Tooltip("Speed on Blue (Z) axis")]
  [SerializeField]
  private float blueAxisSpeed = 0f;
  
  [Tooltip("Vertical Speed (Y axis)")]
  [SerializeField]
  private float verticalSpeed = 0f;

  private Timeline time;
  private Vector3 movementSpeed;

  void Awake()
  {
    time = GetComponent<Timeline>();
    movementSpeed = new Vector3(redAxisSpeed, verticalSpeed, blueAxisSpeed);
  }

  void Update()
  {
    transform.position += movementSpeed * time.deltaTime;
  }
}
