using UnityEngine;

public class SparkManager : MonoBehaviour
{
  private static SparkManager instance;
  public static SparkManager Instance => instance;

  [SerializeField] private Material sparkMaterial;
  private MaterialPropertyBlock propertyBlock;
  private ComputeBuffer positionBuffer;
  private Vector4[] sparkPositions;
  private int activeCount;
  
  private static readonly int PositionsID = Shader.PropertyToID("_SparkPositions");
  private static readonly int CountID = Shader.PropertyToID("_ActiveCount");
  
  private void Awake()
  {
    instance = this;
    propertyBlock = new MaterialPropertyBlock();
    sparkPositions = new Vector4[1024]; // Adjust size as needed
    positionBuffer = new ComputeBuffer(sparkPositions.Length, 16);
  }
  
  public void TriggerSpark(Vector3 position)
  {
    if (activeCount >= sparkPositions.Length) return;
    
    sparkPositions[activeCount] = new Vector4(position.x, position.y, position.z, Time.time);
    activeCount++;
    
    positionBuffer.SetData(sparkPositions);
    sparkMaterial.SetBuffer(PositionsID, positionBuffer);
    sparkMaterial.SetInt(CountID, activeCount);
  }
  
  private void Update()
  {
    // Clean up old sparks
    float currentTime = Time.time;
    int newCount = 0;
    
    for (int i = 0; i < activeCount; i++)
    {
      if (currentTime - sparkPositions[i].w < 0.1f) // Spark lifetime
      {
        sparkPositions[newCount] = sparkPositions[i];
        newCount++;
      }
    }
    
    activeCount = newCount;
    positionBuffer.SetData(sparkPositions);
    sparkMaterial.SetInt(CountID, activeCount);
  }
  
  private void OnDestroy()
  {
    positionBuffer?.Release();
  }
} 