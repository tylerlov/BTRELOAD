using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyInstantKill : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            // Assuming the enemy game object has a script that has the Death method
            EnemyBasicSetup enemyScript = collision.gameObject.GetComponent<EnemyBasicSetup>();
            if (enemyScript != null)
            {
                enemyScript.Damage(1000f);
            }
            else
            {
                Debug.LogError("EnemyScript not found on enemy game object.");
            }
        }
    }
}
