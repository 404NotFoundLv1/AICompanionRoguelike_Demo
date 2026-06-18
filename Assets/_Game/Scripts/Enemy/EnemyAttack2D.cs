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

        public float Damage => damage;

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

            DamageInfo damageInfo = new DamageInfo(damage, DamageSourceType.Enemy, gameObject);
            targetHealth.TakeDamage(damageInfo);
            cooldownTimer = cooldown;

            Debug.Log($"{name} attacked {target.name} for {damage} damage. Target HP: {targetHealth.CurrentHealth}/{targetHealth.MaxHealth}", this);
        }

        public void MultiplyDamage(float multiplier)
        {
            damage = Mathf.Max(0f, damage * Mathf.Max(0f, multiplier));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
