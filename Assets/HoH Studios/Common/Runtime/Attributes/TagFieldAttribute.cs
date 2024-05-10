using UnityEngine;

namespace HohStudios.Common.Attributes
{
    /// <summary>
    /// Tag field selection attribute to display current tags as options, including NO tag.
    /// 
    /// Can optionally default to unity's tag field drawer if desired
    /// </summary>
    public class TagFieldAttribute : PropertyAttribute
    {
        public bool DrawUnityDefault;
        public TagFieldAttribute(bool drawUnityDefault = false)
        {
            DrawUnityDefault = drawUnityDefault;
        }
    }
}