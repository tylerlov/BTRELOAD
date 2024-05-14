using UnityEngine;

[CreateAssetMenu(fileName = "RequiredActiveObjects", menuName = "SceneManagement/RequiredActiveObjects", order = 1)]
public class RequiredActiveObjects : ScriptableObject
{
    public GameObject[] requiredactiveGameObjects;
}