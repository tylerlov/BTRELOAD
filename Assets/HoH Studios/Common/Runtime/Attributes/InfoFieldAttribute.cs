using UnityEngine;

namespace HohStudios.Common.Attributes
{
    /// <summary>
    /// Attribute to show an info box in the editor
    /// </summary>
    public class InfoFieldAttribute : PropertyAttribute
    {
        public string Message;
        public MessageType InfoType;
        public bool Expand;
        public float PaddingAbove;
        public float PaddingBelow;
        public enum MessageType
        {
            None,
            Info,
            Warning,
            Error
        }

        public InfoFieldAttribute(string message, MessageType infoType = MessageType.Info, float paddingAbove = -20, float paddingBelow = 0, bool expand = true)
        {
            Message = message;
            InfoType = infoType;
            Expand = expand;
            PaddingAbove = paddingAbove;
            PaddingBelow = paddingBelow;
        }
    }
}