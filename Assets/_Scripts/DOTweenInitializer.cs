using DG.Tweening;
using UnityEngine;

public class DOTweenInitializer : MonoBehaviour
{
    void Awake()
    {
        // Set the maximum capacity for Tweeners and Sequences
        DOTween.SetTweensCapacity(1250, 50);
    }
}