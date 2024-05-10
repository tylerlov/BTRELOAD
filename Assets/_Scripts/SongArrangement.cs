using UnityEngine;

[CreateAssetMenu(fileName = "SongArrangement", menuName = "ScriptableObjects/Song Arrangement", order = 2)]
public class SongArrangement : ScriptableObject
{
    [System.Serializable]
    public class Section
    {
        public string name;
        public int section;
        public int waves;
    }

    public Section[] sections;
}