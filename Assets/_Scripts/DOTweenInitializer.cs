using DG.Tweening;
using UnityEngine;

public class DOTweenInitializer : MonoBehaviour
{

    public bool active = false;
    void Awake()
    {
        // Set the maximum capacity for Tweeners and Sequences
        if (active)
        {
            DOTween.SetTweensCapacity(1250, 50);
        }
    }
}