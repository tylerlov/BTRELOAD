using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Add this
using UnityEngine.EventSystems; // Add this


public class EnemyKiller : MonoBehaviour, IPointerClickHandler // Implement the IPointerClickHandler interface
{
    private EnemyBasicSetup enemyScript;
    public GameObject uiElement; // Reference to your UI GameObject

    private void Start()
    {
        // Add Event Trigger to your UI element
        EventTrigger eventTrigger = uiElement.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener((data) => { OnPointerClick((PointerEventData)data); });
        eventTrigger.triggers.Add(entry);
    }
    public void OnPointerClick(PointerEventData eventData) // This function will be called when the UI element is clicked
    {
        KillAllEnemies();
        //Projectile is layer 3
        CallDeathOnLayerObjects(3);
    }

    public void KillAllEnemies()
    {
        // Find all active enemy game objects
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        //Debug.Log(enemies.Length + " are the number of enemies found");

        // For each enemy, call the Death method
        foreach (GameObject enemy in enemies)
        {
            enemy.GetComponent<EnemyBasicSetup>().DebugTriggerDeath();
        }
    }
    public void CallDeathOnLayerObjects(int layer)
    {
        var gameObjects = FindObjectsOfType(typeof(GameObject)) as GameObject[];

        foreach (var obj in gameObjects)
        {
            if (obj.layer == layer)
            {
                var projectileState = obj.GetComponent<ProjectileStateBased>();
                if (projectileState != null)
                {
                    projectileState.Death();
                }
            }
        }
    }
}
