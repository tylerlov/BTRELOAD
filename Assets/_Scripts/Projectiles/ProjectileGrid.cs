using UnityEngine;
using System.Collections.Generic;
using System.Text;

public class ProjectileGrid : MonoBehaviour
{
    private const float GRID_CELL_SIZE = 10f;
    private Dictionary<Vector2Int, HashSet<ProjectileStateBased>> playerProjectileGrid = new Dictionary<Vector2Int, HashSet<ProjectileStateBased>>();
    private Dictionary<Vector2Int, HashSet<ProjectileStateBased>> enemyProjectileGrid = new Dictionary<Vector2Int, HashSet<ProjectileStateBased>>();
    private HashSet<ProjectileStateBased> tempProjectileSet = new HashSet<ProjectileStateBased>();

    [Header("Debug Settings")]
    [SerializeField] private bool showDebugGrid = false;
    [SerializeField] private bool showProjectileConnections = false;
    [SerializeField] private Color gridColor = new Color(0.2f, 1f, 0.2f, 0.1f);
    [SerializeField] private Color connectionColor = new Color(1f, 0f, 0f, 0.5f);

    public Vector2Int GetGridCell(Vector3 worldPosition)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPosition.x / GRID_CELL_SIZE),
            Mathf.FloorToInt(worldPosition.z / GRID_CELL_SIZE)
        );
    }

    public void UpdateProjectileGridPosition(ProjectileStateBased projectile, Vector3 oldPosition, Vector3 newPosition)
    {
        Vector2Int oldCell = GetGridCell(oldPosition);
        Vector2Int newCell = GetGridCell(newPosition);

        if (oldCell == newCell) return;

        var grid = projectile.isPlayerShot ? playerProjectileGrid : enemyProjectileGrid;

        // Remove from old cell
        if (grid.ContainsKey(oldCell))
        {
            grid[oldCell].Remove(projectile);
            if (grid[oldCell].Count == 0)
            {
                grid.Remove(oldCell);
            }
        }

        // Add to new cell
        if (!grid.ContainsKey(newCell))
        {
            grid[newCell] = new HashSet<ProjectileStateBased>();
        }
        grid[newCell].Add(projectile);
    }

    public IEnumerable<ProjectileStateBased> GetNearbyProjectiles(Vector3 position, float radius, bool isPlayerShot)
    {
        tempProjectileSet.Clear();
        int cellRadius = Mathf.CeilToInt(radius / GRID_CELL_SIZE);
        Vector2Int centerCell = GetGridCell(position);

        var targetGrid = isPlayerShot ? enemyProjectileGrid : playerProjectileGrid;

        for (int x = -cellRadius; x <= cellRadius; x++)
        {
            for (int y = -cellRadius; y <= cellRadius; y++)
            {
                Vector2Int cell = new Vector2Int(centerCell.x + x, centerCell.y + y);
                if (targetGrid.TryGetValue(cell, out var projectiles))
                {
                    foreach (var projectile in projectiles)
                    {
                        if (projectile != null)
                        {
                            tempProjectileSet.Add(projectile);
                        }
                    }
                }
            }
        }

        return tempProjectileSet;
    }

    public void ClearGrids()
    {
        playerProjectileGrid.Clear();
        enemyProjectileGrid.Clear();
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGrid && !showProjectileConnections) return;

        if (showDebugGrid)
        {
            DrawGrid();
        }

        if (showProjectileConnections)
        {
            DrawProjectileConnections();
        }
    }

    private void DrawGrid()
    {
        Gizmos.color = gridColor;
        float drawDistance = 100f;
        int cellCount = Mathf.CeilToInt(drawDistance / GRID_CELL_SIZE);

        for (int x = -cellCount; x <= cellCount; x++)
        {
            for (int z = -cellCount; z <= cellCount; z++)
            {
                Vector3 center = new Vector3(x * GRID_CELL_SIZE, 0, z * GRID_CELL_SIZE);
                Vector3 size = new Vector3(GRID_CELL_SIZE, 0.1f, GRID_CELL_SIZE);
                Gizmos.DrawWireCube(center, size);
            }
        }
    }

    private void DrawProjectileConnections()
    {
        Gizmos.color = connectionColor;
        foreach (var grid in new[] { playerProjectileGrid, enemyProjectileGrid })
        {
            foreach (var cellProjectiles in grid.Values)
            {
                foreach (var projectile in cellProjectiles)
                {
                    if (projectile != null)
                    {
                        Gizmos.DrawLine(
                            projectile.transform.position,
                            new Vector3(
                                projectile.transform.position.x,
                                0,
                                projectile.transform.position.z
                            )
                        );
                    }
                }
            }
        }
    }
}
