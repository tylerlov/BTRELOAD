using UnityEngine;

public class DigitalLayerEffect : MonoBehaviour
{
  [SerializeField] private Mesh targetMesh;
  [SerializeField] private Material digitalSpriteMaterial;
  [SerializeField] private ComputeShader computeShader;
  [SerializeField] private int spriteCount = 1000;
  [SerializeField] private float boundsExpansion = 0.1f;
  
  private ComputeBuffer positionBuffer;
  private ComputeBuffer meshVertBuffer;
  private ComputeBuffer meshTriBuffer;
  
  private struct SpriteData
  {
    public Vector4 position; // w component for rotation
    public Vector4 color;    // w component for scale
  }
  
  private void Start()
  {
    InitializeBuffers();
    GeneratePositions();
  }
  
  private void InitializeBuffers()
  {
    // Position buffer for sprites
    positionBuffer = new ComputeBuffer(spriteCount, sizeof(float) * 8); // SpriteData size
    
    // Mesh data buffers
    Vector3[] vertices = targetMesh.vertices;
    int[] triangles = targetMesh.triangles;
    
    meshVertBuffer = new ComputeBuffer(vertices.Length, sizeof(float) * 3);
    meshTriBuffer = new ComputeBuffer(triangles.Length, sizeof(int));
    
    meshVertBuffer.SetData(vertices);
    meshTriBuffer.SetData(triangles);
    
    digitalSpriteMaterial.SetBuffer("_SpriteBuffer", positionBuffer);
  }
  
  private void GeneratePositions()
  {
    int kernel = computeShader.FindKernel("GeneratePositions");
    
    computeShader.SetBuffer(kernel, "PositionBuffer", positionBuffer);
    computeShader.SetBuffer(kernel, "VertexBuffer", meshVertBuffer);
    computeShader.SetBuffer(kernel, "TriangleBuffer", meshTriBuffer);
    computeShader.SetInt("SpriteCount", spriteCount);
    computeShader.SetMatrix("ObjectToWorld", transform.localToWorldMatrix);
    computeShader.SetFloat("Time", Time.time);
    
    computeShader.Dispatch(kernel, Mathf.CeilToInt(spriteCount / 64f), 1, 1);
  }
  
  private void Update()
  {
    // Update time-based animations if needed
    computeShader.SetFloat("Time", Time.time);
    GeneratePositions();
  }
  
  private void OnDestroy()
  {
    positionBuffer?.Release();
    meshVertBuffer?.Release();
    meshTriBuffer?.Release();
  }
} 