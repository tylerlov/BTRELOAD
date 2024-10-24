using UnityEngine;

[DefaultExecutionOrder(-200)] // Ensure this runs before other scripts
public class ProjectileInitializer : MonoBehaviour
{
    private static bool initialized = false;
    private static MaterialPropertyBlock sharedPropertyBlock;
    
    private void Awake()
    {
        if (!initialized)
        {
            sharedPropertyBlock = new MaterialPropertyBlock();
            initialized = true;
        }
    }
    
    public static MaterialPropertyBlock GetSharedPropertyBlock()
    {
        if (!initialized)
        {
            sharedPropertyBlock = new MaterialPropertyBlock();
            initialized = true;
        }
        return sharedPropertyBlock;
    }
}
