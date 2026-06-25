using AICompanionRoguelike.Combat;
using UnityEngine;

namespace AICompanionRoguelike.Enemy
{
    public sealed class EnemyAttack2D : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float damage = 10f;
        [SerializeField, Min(0f)] private float cooldown = 1f;
        [SerializeField, Min(0f)] private float attackRange = 1.2f;

        private EnemyController2D owner;
        private float cooldownTimer;
        private float tacticalSuppressionTimer;
        private float tacticalDamageMultiplier = 1f;

        public float Damage => damage;
        public float CurrentDamage => damage * TacticalDamageMultiplier;
        public bool IsTacticallySuppressed => tacticalSuppressionTimer > 0f;
        public float TacticalDamageMultiplier => IsTacticallySuppressed ? tacticalDamageMultiplier : 1f;

        private void Awake()
        {
            owner = owner != null ? owner : GetComponent<EnemyController2D>();
        }

        private void Update()
        {
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
            }

            TickTacticalSuppression(Time.deltaTime);
        }

        public void SetOwner(EnemyController2D enemyOwner)
        {
            owner = enemyOwner;
        }

        public void TryAttack(Transform target)
        {
            if (target == null || cooldownTimer > 0f)
            {
                return;
            }

            float distance = Vector2.Distance(transform.position, target.position);
            if (distance > attackRange)
            {
                return;
            }

            if (!target.TryGetComponent(out HealthComponent targetHealth))
            {
                targetHealth = target.GetComponentInParent<HealthComponent>();
            }

            if (targetHealth == null || targetHealth.IsDead)
            {
                return;
            }

            float currentDamage = CurrentDamage;
            DamageInfo damageInfo = new DamageInfo(currentDamage, DamageSourceType.Enemy, gameObject);
            targetHealth.TakeDamage(damageInfo);
            cooldownTimer = cooldown;

            Debug.Log($"{name} attacked {target.name} for {currentDamage} damage. Target HP: {targetHealth.CurrentHealth}/{targetHealth.MaxHealth}", this);
        }

        public void MultiplyDamage(float multiplier)
        {
            damage = Mathf.Max(0f, damage * Mathf.Max(0f, multiplier));
        }

        public void ApplyTacticalSuppression(float duration, float damageMultiplier)
        {
            if (duration <= 0f)
            {
                return;
            }

            tacticalSuppressionTimer = Mathf.Max(tacticalSuppressionTimer, duration);
            tacticalDamageMultiplier = Mathf.Min(
                IsTacticallySuppressed ? tacticalDamageMultiplier : 1f,
                Mathf.Clamp(damageMultiplier, 0.05f, 1f));
        }

        public void TickTacticalSuppression(float deltaTime)
        {
            if (tacticalSuppressionTimer <= 0f)
            {
                tacticalDamageMultiplier = 1f;
                return;
            }

            tacticalSuppressionTimer = Mathf.Max(0f, tacticalSuppressionTimer - Mathf.Max(0f, deltaTime));
            if (tacticalSuppressionTimer <= 0f)
            {
                tacticalDamageMultiplier = 1f;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
