using AICompanionRoguelike.Combat;
using UnityEngine;

namespace AICompanionRoguelike.Companion
{
    [RequireComponent(typeof(CompanionSensor))]
    [RequireComponent(typeof(HealthComponent))]
    public sealed class CompanionCombat : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CompanionSensor sensor;
        [SerializeField] private CompanionMovement movement;
        [SerializeField] private HealthComponent health;
        [SerializeField] private Transform attackOrigin;

        [Header("Attack")]
        [SerializeField, Min(0f)] private float damage = 12f;
        [SerializeField, Min(0.05f)] private float cooldown = 0.85f;
        [SerializeField, Min(0f)] private float attackRange = 2.8f;

        private float cooldownTimer;

        public float Damage => damage;
        public float Cooldown => cooldown;

        private void Reset()
        {
            sensor = GetComponent<CompanionSensor>();
            movement = GetComponent<CompanionMovement>();
            health = GetComponent<HealthComponent>();
            attackOrigin = transform;
        }

        private void Awake()
        {
            sensor = sensor != null ? sensor : GetComponent<CompanionSensor>();
            movement = movement != null ? movement : GetComponent<CompanionMovement>();
            health = health != null ? health : GetComponent<HealthComponent>();
            attackOrigin = attackOrigin != null ? attackOrigin : transform;
        }

        private void Update()
        {
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
            }

            if (health != null && health.IsDead)
            {
                return;
            }

            HealthComponent targetHealth = sensor != null ? sensor.CurrentTargetHealth : null;
            if (!CanAttack(targetHealth))
            {
                return;
            }

            Attack(targetHealth);
        }

        private bool CanAttack(HealthComponent targetHealth)
        {
            if (cooldownTimer > 0f || targetHealth == null || targetHealth.IsDead)
            {
                return false;
            }

            Vector2 origin = attackOrigin != null ? attackOrigin.position : transform.position;
            float distance = Vector2.Distance(origin, targetHealth.transform.position);
            return distance <= attackRange;
        }

        private void Attack(HealthComponent targetHealth)
        {
            DamageInfo damageInfo = new DamageInfo(damage, DamageSourceType.Companion, gameObject);
            float previousHealth = targetHealth.CurrentHealth;
            targetHealth.TakeDamage(damageInfo);
            float appliedDamage = Mathf.Max(0f, previousHealth - targetHealth.CurrentHealth);
            cooldownTimer = cooldown;

            if (movement != null && targetHealth != null && appliedDamage > 0f)
            {
                Debug.Log($"Companion attacked {targetHealth.name} for {appliedDamage} damage. Target HP: {targetHealth.CurrentHealth}/{targetHealth.MaxHealth}", targetHealth);
            }
        }

        public void MultiplyCooldown(float multiplier)
        {
            cooldown = Mathf.Max(0.05f, cooldown * Mathf.Max(0f, multiplier));
            cooldownTimer = Mathf.Min(cooldownTimer, cooldown);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = attackOrigin != null ? attackOrigin.position : transform.position;
            Gizmos.color = new Color(0.8f, 0.4f, 1f, 1f);
            Gizmos.DrawWireSphere(origin, attackRange);
        }
    }
}
