using System;
using AICompanionRoguelike.Combat;
using UnityEngine;

namespace AICompanionRoguelike.Enemy
{
    [RequireComponent(typeof(HealthComponent))]
    public sealed class CompanionKillProtection : MonoBehaviour, IDamageModifier
    {
        [SerializeField, Range(0.01f, 1f)] private float minimumHealthPercent = 0.2f;
        [SerializeField] private bool logFinisherRequest = true;

        private bool finisherRequested;

        public static event Action<CompanionKillProtection, HealthComponent, DamageInfo> FinisherRequested;
        public event Action<CompanionKillProtection, HealthComponent, DamageInfo> LocalFinisherRequested;

        public float MinimumHealthPercent => minimumHealthPercent;

        private void OnEnable()
        {
            finisherRequested = false;
        }

        private void OnValidate()
        {
            minimumHealthPercent = Mathf.Clamp(minimumHealthPercent, 0.01f, 1f);
        }

        public DamageInfo ModifyIncomingDamage(HealthComponent target, DamageInfo damageInfo)
        {
            if (target == null || damageInfo.sourceType != DamageSourceType.Companion || damageInfo.damage <= 0f)
            {
                return damageInfo;
            }

            float minimumHealth = target.MaxHealth * minimumHealthPercent;
            if (target.CurrentHealth <= minimumHealth)
            {
                RequestFinisher(target, damageInfo);
                damageInfo.damage = 0f;
                return damageInfo;
            }

            float maximumCompanionDamage = target.CurrentHealth - minimumHealth;
            if (damageInfo.damage < maximumCompanionDamage)
            {
                return damageInfo;
            }

            RequestFinisher(target, damageInfo);
            damageInfo.damage = maximumCompanionDamage;
            return damageInfo;
        }

        private void RequestFinisher(HealthComponent target, DamageInfo damageInfo)
        {
            if (finisherRequested)
            {
                return;
            }

            finisherRequested = true;
            LocalFinisherRequested?.Invoke(this, target, damageInfo);
            FinisherRequested?.Invoke(this, target, damageInfo);

            if (!logFinisherRequest)
            {
                return;
            }

            int percent = Mathf.RoundToInt(minimumHealthPercent * 100f);
            Debug.Log($"Companion cannot finish {target.name}. Player finisher requested at {percent}% HP threshold.", target);
        }
    }
}
