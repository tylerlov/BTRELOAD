using UnityEngine;

public class ShaderPrewarmer : MonoBehaviour
{
    [SerializeField] private ShaderVariantCollection shaderVariants;
    
    private void Awake()
    {
        if (shaderVariants != null)
        {
            shaderVariants.WarmUp();
        }
    }
} 