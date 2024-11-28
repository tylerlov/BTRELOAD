using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralTreeGenerator : MonoBehaviour
{
  [SerializeField] private TreeConfiguration configuration;
  [SerializeField] private Material trunkMaterial;
  [SerializeField] private Material branchMaterial;
  private TreeGrowthManager growthManager;
  private TreeMeshGenerator meshGenerator;
  private TreeBoundaryManager boundaryManager;

  public TreeConfiguration Configuration => configuration;

  private void Start()
  {
    if (!TryGetComponent<MeshFilter>(out var meshFilter))
    {
      meshFilter = gameObject.AddComponent<MeshFilter>();
    }
    
    if (!TryGetComponent<MeshRenderer>(out var meshRenderer))
    {
      meshRenderer = gameObject.AddComponent<MeshRenderer>();
    }

    growthManager = new TreeGrowthManager(this, configuration);
    meshGenerator = new TreeMeshGenerator(this, trunkMaterial, branchMaterial);
    boundaryManager = new TreeBoundaryManager(this);
  }

  private void Update()
  {
    growthManager.UpdateGrowth();
  }

  private void FixedUpdate()
  {
    meshGenerator.UpdateMesh(growthManager);
  }

  public void ResetTree()
  {
    growthManager.ResetTreeState();
  }

  private void OnDestroy()
  {
    growthManager.Dispose();
  }

  private void OnDrawGizmos()
  {
    if (configuration == null) return;

    if (!Application.isPlaying || boundaryManager == null)
    {
      if (configuration.showBoundaryGizmo)
      {
        DrawWireCylinder(transform.position, configuration.maxRadius, 20f, configuration.boundaryGizmoColor);
        
        var toleranceColor = new Color(
          configuration.boundaryGizmoColor.r, 
          configuration.boundaryGizmoColor.g, 
          configuration.boundaryGizmoColor.b, 
          configuration.boundaryGizmoColor.a * 0.5f
        );
        DrawWireCylinder(
          transform.position, 
          configuration.maxRadius * (1f + configuration.radiusOvershootTolerance), 
          20f, 
          toleranceColor
        );
      }
    }
    else
    {
      boundaryManager.DrawGizmos();
      
      if (growthManager != null && growthManager.Branches.IsCreated)
      {
        Gizmos.color = Color.green;
        var branches = growthManager.Branches;
        for (int i = 0; i < branches.Length; i++)
        {
          var branch = branches[i];
          Gizmos.DrawLine(branch.startPosition, branch.endPosition);
        }
      }
    }
  }

  private void DrawWireCylinder(Vector3 position, float radius, float height, Color color)
  {
    Gizmos.color = color;
    
    int segments = 32;
    float angleStep = 2f * Mathf.PI / segments;
    
    Vector3 prevBottom = new Vector3(radius, -height/2f, 0);
    Vector3 prevTop = new Vector3(radius, height/2f, 0);
    
    for (int i = 1; i <= segments; i++)
    {
      float angle = i * angleStep;
      Vector3 nextBottom = new Vector3(
        radius * Mathf.Cos(angle), 
        -height/2f, 
        radius * Mathf.Sin(angle)
      );
      Vector3 nextTop = new Vector3(
        radius * Mathf.Cos(angle), 
        height/2f, 
        radius * Mathf.Sin(angle)
      );
      
      Gizmos.DrawLine(position + prevBottom, position + nextBottom);
      Gizmos.DrawLine(position + prevTop, position + nextTop);
      Gizmos.DrawLine(position + prevBottom, position + prevTop);
      
      prevBottom = nextBottom;
      prevTop = nextTop;
    }
  }
} 