using System;
using UnityEngine;

namespace AICompanionRoguelike.Combat
{
    public sealed class HealthComponent : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField] private bool destroyOnDeath;

        private float currentHealth;
        private bool isDead;

        public event Action<HealthComponent, DamageInfo> Damaged;
        public event Action<HealthComponent, float> Healed;
        public event Action<HealthComponent, DamageInfo> Died;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead => isDead;

        private void Awake()
        {
            currentHealth = Mathf.Clamp(currentHealth <= 0f ? maxHealth : currentHealth, 0f, maxHealth);
            isDead = currentHealth <= 0f;
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);

            if (!Application.isPlaying)
            {
                currentHealth = maxHealth;
            }
        }

        public void SetMaxHealth(float value, bool refillHealth)
        {
            maxHealth = Mathf.Max(1f, value);

            if (refillHealth)
            {
                currentHealth = maxHealth;
                isDead = false;
            }
            else
            {
                currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
                isDead = currentHealth <= 0f;
            }
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (isDead || damageInfo.damage <= 0f)
            {
                return;
            }

            currentHealth = Mathf.Max(0f, currentHealth - damageInfo.damage);
            Damaged?.Invoke(this, damageInfo);

            if (currentHealth <= 0f)
            {
                Die(damageInfo);
            }
        }

        public void Heal(float amount)
        {
            if (isDead || amount <= 0f)
            {
                return;
            }

            float previousHealth = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            float healedAmount = currentHealth - previousHealth;

            if (healedAmount > 0f)
            {
                Healed?.Invoke(this, healedAmount);
            }
        }

        public void Revive(float health)
        {
            currentHealth = Mathf.Clamp(health, 1f, maxHealth);
            isDead = false;
        }

        public void Die(DamageInfo damageInfo)
        {
            if (isDead)
            {
                return;
            }

            isDead = true;
            currentHealth = 0f;
            Died?.Invoke(this, damageInfo);

            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }
        }
    }
}
