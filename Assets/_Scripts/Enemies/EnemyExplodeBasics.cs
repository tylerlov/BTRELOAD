using UnityEngine;
using Chronos;
using Typooling;
using PathologicalGames;
using FMODUnity;
using UnityEngine.VFX;
using SonicBloom.Koreo;

[RequireComponent(typeof(Timeline))]
public class EnemyExplodeBasics : EnemyBasics
{
    [Header("Explosion VFX")]
    [SerializeField] protected ParticleSystem trails;
    [SerializeField] protected VisualEffect pulseVisualEffect;
    
    [Header("Pulse Settings")]
    [SerializeField] protected Color pulseColor = Color.red;
    [SerializeField] protected float pulseDuration = 0.1f;
    
    [Header("Distance Settings")]
    [SerializeField] protected float farDistance = 10f;
    [SerializeField] protected float mediumDistance = 5f;
    
    [Header("Pulse Audio")]
    [SerializeField] protected EventReference pulseSound;
    [SerializeField] protected EventReference explosionSound;
    [SerializeField, EventID] protected string farPulseEventID;
    [SerializeField, EventID] protected string mediumPulseEventID;
    [SerializeField, EventID] protected string closePulseEventID;

    protected bool hasExploded = false;

    protected override void Awake()
    {
        base.Awake();
        enemyType = "Exploder"; // Set default enemy type
    }

    protected override void InitializeEnemy()
    {
        base.InitializeEnemy();
        hasExploded = false;
        if (pulseVisualEffect != null)
        {
            pulseVisualEffect.SetVector4("PulseColor", pulseColor);
        }
    }

    public bool HasExploded => hasExploded;

    protected virtual void PlayPulseEffect(string pulseEventID)
    {
        if (pulseVisualEffect != null)
        {
            pulseVisualEffect.SendEvent("OnPulse");
        }
        
        if (!pulseSound.IsNull)
        {
            RuntimeManager.PlayOneShot(pulseSound, transform.position);
        }
    }

    protected virtual void OnExploded()
    {
        hasExploded = true;
        if (!explosionSound.IsNull)
        {
            RuntimeManager.PlayOneShot(explosionSound, transform.position);
        }
        Die(); // Call protected Die() since we inherit from EnemyBasics
    }

    // Public method for combat to trigger death
    public void TriggerDeath()
    {
        Die();
    }
}
