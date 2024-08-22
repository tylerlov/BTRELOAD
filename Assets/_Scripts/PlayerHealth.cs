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
        GameManager.instance.AddScore(-damageAmount);
        GameManager.instance.ReportDamage(damageAmount); // New method to report damage
        SpawnHitEffect();
        PlayHitFeedback();

        if (GameManager.instance.Score <= 0)
        {
            GameOver();
        }
    }

    private void GameOver()
    {
        Debug.Log("Game Over!");
        gameObject.SetActive(false);
        // Add any additional game over logic here
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

    public bool IsAlive() => GameManager.instance.Score > 0;

    public float getCurrentHealth() => GameManager.instance.Score;

    public void ResetScore()
    {
        GameManager.instance.SetScore(startScore);
        gameObject.SetActive(true);
    }

    public void SetInvincibleInternal(bool value)
    {
        isInvincible = value;
        if (isInvincible)
        {
            GameManager.instance.SetScore(999999);
        }
    }
}
