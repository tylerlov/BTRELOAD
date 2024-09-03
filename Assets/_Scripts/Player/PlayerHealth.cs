using System.Collections.Generic;
using BehaviorDesigner.Runtime.Tactical;
using Chronos;
using MoreMountains.Feedbacks;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    public int startScore = 100;

    [SerializeField]
    private GameObject hitEffectPrefab;

    [SerializeField]
    private int poolSize = 10;

    [SerializeField]
    private GameObject hitFeedbackObject;

    private List<GameObject> hitEffectsPool;
    public bool DodgeInvincibility { get; set; } = false;

    private bool isInvincible = false;

    private float health;
    private float maxHealth; // New variable to store the original health value

    public PlayerHealth(float initialHealth)
    {
        health = initialHealth;
        maxHealth = initialHealth; // Store the initial health as the max health
    }

    private void Start()
    {
        InitializeHitEffectsPool();
        ResetScore();
    }

    public void Damage(float amount)
    {
        if (DodgeInvincibility || isInvincible)
            return;

        int damageAmount = (int)amount;
        ScoreManager.Instance.AddScore(-damageAmount);
        ScoreManager.Instance.ReportDamage(damageAmount); // New method to report damage
        SpawnHitEffect();
        PlayHitFeedback();

        if (ScoreManager.Instance.Score <= 0)
        {
            GameOver();
        }
    }

    private void GameOver()
    {
        Debug.Log("Game Over!");
        GameManager.Instance.HandlePlayerDeath();
    }

    private void InitializeHitEffectsPool()
    {
        hitEffectsPool = new List<GameObject>();
        for (int i = 0; i < poolSize; i++)
        {
            GameObject effect = Instantiate(hitEffectPrefab);
            effect.SetActive(false);
            effect.transform.SetParent(transform);
            hitEffectsPool.Add(effect);
        }
    }

    private void SpawnHitEffect()
    {
        GameObject effect = hitEffectsPool.Find(e => !e.activeInHierarchy);
        if (effect != null)
        {
            effect.transform.position = transform.position;
            effect.SetActive(true);
            StartCoroutine(DeactivateEffect(effect));
        }
    }

    private void PlayHitFeedback()
    {
        if (hitFeedbackObject != null)
        {
            var feedbacks = hitFeedbackObject.GetComponent<MMFeedbacks>();
            if (feedbacks != null)
            {
                feedbacks.PlayFeedbacks();
            }
        }
    }

    private System.Collections.IEnumerator DeactivateEffect(GameObject effect)
    {
        yield return new WaitForSeconds(1.0f);
        effect.SetActive(false);
    }

    public bool IsAlive() => ScoreManager.Instance.Score > 0;

    public float getCurrentHealth() => ScoreManager.Instance.Score;

    public void ResetScore()
    {
        ScoreManager.Instance.SetScore(startScore);
        gameObject.SetActive(true);
    }

    public void SetInvincibleInternal(bool value)
    {
        isInvincible = value;
        if (isInvincible)
        {
            ScoreManager.Instance.SetScore(999999);
        }
    }

    public void ResetHealth()
    {
        health = maxHealth; // Reset health to its original value
    }
}
