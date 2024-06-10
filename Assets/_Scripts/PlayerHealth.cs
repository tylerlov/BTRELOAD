using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviorDesigner.Runtime.Tactical;
using MoreMountains.Feedbacks;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    public bool invincible;
    public Rigidbody rb;
    public int startScore;
    public GameObject hitEffectPrefab; // Prefab for the hit effect
    public int poolSize = 10; // Size of the pool for hit effects
    private List<GameObject> hitEffectsPool; // Pool of hit effects
    public GameObject hitFeedbackObject; // Reference to the feedback object

    public bool DodgeInvincibility { get; set; } = false;

    private void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
        InitializeHitEffectsPool();

        if (invincible)
        {
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

    private void InitializeHitEffectsPool()
    {
        hitEffectsPool = new List<GameObject>();
        for (int i = 0; i < poolSize; i++)
        {
            GameObject effect = Instantiate(hitEffectPrefab);
            effect.SetActive(false);
            effect.transform.SetParent(transform); // Optional: Parent to the player for organizational purposes
            hitEffectsPool.Add(effect);
        }
    }

    private GameObject GetPooledEffect()
    {
        foreach (GameObject effect in hitEffectsPool)
        {
            if (!effect.activeInHierarchy)
            {
                return effect;
            }
        }
        return null; // Return null if all effects are in use (consider resizing the pool or waiting)
    }

    public void Damage(float amount)
    {
        if (DodgeInvincibility)
        {
            return;
        }

        GameManager.instance.AddScore(-(int)amount);
        ConditionalDebug.Log("Player is taking damage");

        GameObject effect = GetPooledEffect();
        if (effect != null)
        {
            effect.transform.position = transform.position; // Position the effect at the player's location
            effect.SetActive(true);
            PlayEffectWithChildren(effect);
            StartCoroutine(DeactivateEffect(effect));
        }

        if (hitFeedbackObject != null)
        {
            var feedbacks = hitFeedbackObject.GetComponent<MMFeedbacks>();
            if (feedbacks != null)
            {
                feedbacks.PlayFeedbacks();
            }
        }

        if (GameManager.instance.Score == 0 && !invincible)
        {
            gameObject.SetActive(false);
        }
    }

    private void PlayEffectWithChildren(GameObject effect)
    {
        ParticleSystem[] particleSystems = effect.GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem ps in particleSystems)
        {
            ps.Play();
        }
    }

    private IEnumerator DeactivateEffect(GameObject effect)
    {
        yield return new WaitForSeconds(1.0f); // Wait for the duration of the effect
        effect.SetActive(false);
    }

    private IEnumerator DeactivateFeedbackObject(GameObject feedbackObject)
    {
        yield return new WaitForSeconds(1.0f); // Adjust the duration as needed
        feedbackObject.SetActive(false);
    }

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
