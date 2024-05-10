using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviorDesigner.Runtime.Tactical;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    public bool invincible;
    public Rigidbody rb;
    public int startScore;
    public bool DodgeInvincibility { get; set; } = false;

    private void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();

        if (invincible)
        {
            // Check if the score has not been set or is below 1000
            if (GameManager.instance.RetrieveScore() == 0 || GameManager.instance.RetrieveScore() < 1000)
            {
                GameManager.instance.SetScore(999999999);
            }
        }
        else
        {
            GameManager.instance.AddScore(startScore);
        }
    }

    public void Damage(float amount)
    {
        if (DodgeInvincibility)
        {
            return;
        }

        GameManager.instance.AddScore(-(int)amount);
        ConditionalDebug.Log("Player is taking damage");
        
        if (GameManager.instance.Score == 0 && !invincible)
        {
            gameObject.SetActive(false);
        }
    }

    // Is the object alive?
    public bool IsAlive()
    {
        return GameManager.instance.Score > 0;
    }
    public float getCurrentHealth()
    {
        return GameManager.instance.Score;
    }

    public void ResetScore()
    {
        GameManager.instance.AddScore(startScore);
        gameObject.SetActive(true);
    }
}
