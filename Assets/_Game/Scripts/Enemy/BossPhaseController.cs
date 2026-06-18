using System;
using AICompanionRoguelike.Combat;
using UnityEngine;

namespace AICompanionRoguelike.Enemy
{
    public sealed class BossPhaseController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HealthComponent health;
        [SerializeField] private EnemyAttack2D attack;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Phase Two")]
        [SerializeField, Range(0.05f, 0.95f)] private float phaseTwoHealthRatio = 0.5f;
        [SerializeField, Min(1f)] private float phaseTwoDamageMultiplier = 1.4f;
        [SerializeField, Min(1f)] private float phaseTwoScaleMultiplier = 1.1f;
        [SerializeField] private Color phaseTwoTint = new Color(1f, 0.08f, 0.08f, 1f);

        [Header("Debug")]
        [SerializeField] private bool logPhaseMessages = true;

        private HealthComponent subscribedHealth;
        private bool isInPhaseTwo;

        public event Action<BossPhaseController> PhaseTwoStarted;

        public bool IsInPhaseTwo => isInPhaseTwo;
        public float PhaseTwoHealthRatio => phaseTwoHealthRatio;

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            SubscribeToHealth();
            EvaluatePhaseTwo();
        }

        private void OnDisable()
        {
            UnsubscribeFromHealth();
        }

        private void OnValidate()
        {
            phaseTwoHealthRatio = Mathf.Clamp(phaseTwoHealthRatio, 0.05f, 0.95f);
            phaseTwoDamageMultiplier = Mathf.Max(1f, phaseTwoDamageMultiplier);
            phaseTwoScaleMultiplier = Mathf.Max(1f, phaseTwoScaleMultiplier);
        }

        public void Configure(
            HealthComponent newHealth,
            EnemyAttack2D newAttack,
            float phaseTwoHealthRatio,
            float phaseTwoDamageMultiplier,
            float phaseTwoScaleMultiplier)
        {
            UnsubscribeFromHealth();

            health = newHealth;
            attack = newAttack;
            this.phaseTwoHealthRatio = Mathf.Clamp(phaseTwoHealthRatio, 0.05f, 0.95f);
            this.phaseTwoDamageMultiplier = Mathf.Max(1f, phaseTwoDamageMultiplier);
            this.phaseTwoScaleMultiplier = Mathf.Max(1f, phaseTwoScaleMultiplier);
            isInPhaseTwo = false;

            ResolveReferences();
            SubscribeToHealth();
            EvaluatePhaseTwo();
        }

        public void SetPhaseTwoTint(Color tint)
        {
            phaseTwoTint = tint;

            if (isInPhaseTwo && spriteRenderer != null)
            {
                spriteRenderer.color = phaseTwoTint;
            }
        }

        private void ResolveReferences()
        {
            health = health != null ? health : GetComponent<HealthComponent>();
            attack = attack != null ? attack : GetComponent<EnemyAttack2D>();
            spriteRenderer = spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>();
        }

        private void SubscribeToHealth()
        {
            if (health == null || subscribedHealth == health)
            {
                return;
            }

            subscribedHealth = health;
            subscribedHealth.Damaged += HandleDamaged;
            subscribedHealth.Healed += HandleHealed;
            subscribedHealth.Died += HandleDied;
        }

        private void UnsubscribeFromHealth()
        {
            if (subscribedHealth == null)
            {
                return;
            }

            subscribedHealth.Damaged -= HandleDamaged;
            subscribedHealth.Healed -= HandleHealed;
            subscribedHealth.Died -= HandleDied;
            subscribedHealth = null;
        }

        private void HandleDamaged(HealthComponent damagedHealth, DamageInfo damageInfo)
        {
            EvaluatePhaseTwo();
        }

        private void HandleHealed(HealthComponent healedHealth, float healedAmount)
        {
            EvaluatePhaseTwo();
        }

        private void HandleDied(HealthComponent deadHealth, DamageInfo damageInfo)
        {
            EvaluatePhaseTwo();
        }

        private void EvaluatePhaseTwo()
        {
            if (isInPhaseTwo || health == null || health.MaxHealth <= 0f || health.IsDead)
            {
                return;
            }

            float currentRatio = health.CurrentHealth / health.MaxHealth;
            if (currentRatio <= phaseTwoHealthRatio)
            {
                EnterPhaseTwo();
            }
        }

        private void EnterPhaseTwo()
        {
            if (isInPhaseTwo)
            {
                return;
            }

            isInPhaseTwo = true;

            if (attack != null)
            {
                attack.MultiplyDamage(phaseTwoDamageMultiplier);
            }

            transform.localScale *= phaseTwoScaleMultiplier;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = phaseTwoTint;
            }

            if (logPhaseMessages)
            {
                Debug.Log($"{name} entered Boss Phase Two.", this);
            }

            PhaseTwoStarted?.Invoke(this);
        }
    }
}
