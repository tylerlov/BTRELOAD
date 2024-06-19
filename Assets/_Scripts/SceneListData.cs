using UnityEngine;

[CreateAssetMenu(fileName = "SceneListData", menuName = "ScriptableObjects/SceneListData", order = 1)]
public class SceneListData : ScriptableObject
{
    public SceneGroup[] sceneGroups;
}