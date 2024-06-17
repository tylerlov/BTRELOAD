namespace Raymarcher
{
    public static class RMDebug
    {
        public static void Debug(System.Type sender, string message, bool asError = false)
        {
            Debug(sender == null ? "UNKNOWN SENDER" : sender.Name, message, asError);
        }

        public static void Debug<T>(T sender, string message, bool asError = false) where T : class
        {
            Debug(sender == null ? "UNKNOWN SENDER" : sender.GetType().Name, message, asError);
        }

        public static void Debug(string senderName, string message, bool asError = false)
        {
            if (asError)
                UnityEngine.Debug.LogError($"Raymarcher [{senderName}] Output: {message}");
            else
                UnityEngine.Debug.Log($"Raymarcher [{senderName}] Output: {message}");
        }
    }
}