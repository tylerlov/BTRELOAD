using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemyInstantKill : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        var enemy = other.GetComponent<EnemyBasics>();
        if (enemy != null)
        {
            enemy.TakeDamage(float.MaxValue);
        }
    }
}
