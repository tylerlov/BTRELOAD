using System.Collections;
using System.Collections.Generic;
using System.Linq; // Add this for LINQ methods like OfType
using UnityEngine;
using UnityEngine.EventSystems; // Add this
using UnityEngine.UI; // Add this

public class EnemyKiller : MonoBehaviour, IPointerClickHandler // Implement the IPointerClickHandler interface
{
    private EnemyBasics enemyScript;
    public GameObject uiElement; // Reference to your UI GameObject

    private void Start()
    {
        // Add Event Trigger to your UI element
        EventTrigger eventTrigger = uiElement.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener(
            (data) =>
            {
                OnPointerClick((PointerEventData)data);
            }
        );
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
        // Find all game objects that implement the IDamageable interface
        var damageables = UnityEngine
            .Object.FindObjectsOfType<MonoBehaviour>()
            .OfType<IDamageable>();

        // Apply damage to each damageable object
        foreach (var damageable in damageables)
        {
            if (damageable.IsAlive())
            {
                damageable.Damage(100); // Assuming you want to apply a damage of 100
            }
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
