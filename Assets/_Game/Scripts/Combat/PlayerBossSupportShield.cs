using UnityEngine;

namespace AICompanionRoguelike.Combat
{
    public sealed class PlayerBossSupportShield : MonoBehaviour, IDamageModifier
    {
        [SerializeField, Min(0f)] private float remainingDuration;
        [SerializeField, Range(0f, 1f)] private float incomingDamageMultiplier = 0.5f;
        [SerializeField] private bool logShieldMessages = true;

        public bool IsActive => remainingDuration > 0f;
        public float RemainingDuration => remainingDuration;
        public float IncomingDamageMultiplier => IsActive ? incomingDamageMultiplier : 1f;

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private void OnValidate()
        {
            remainingDuration = Mathf.Max(0f, remainingDuration);
            incomingDamageMultiplier = Mathf.Clamp01(incomingDamageMultiplier);
        }

        public void Activate(float duration, float damageMultiplier)
        {
            remainingDuration = Mathf.Max(0f, duration);
            incomingDamageMultiplier = Mathf.Clamp01(damageMultiplier);

            if (logShieldMessages)
            {
                Debug.Log(
                    $"AI boss support shield active for {remainingDuration:0.0}s. Incoming damage x{IncomingDamageMultiplier:0.##}.",
                    this);
            }
        }

        public void Tick(float deltaTime)
        {
            if (remainingDuration <= 0f)
            {
                return;
            }

            remainingDuration = Mathf.Max(0f, remainingDuration - Mathf.Max(0f, deltaTime));
            if (remainingDuration <= 0f && logShieldMessages)
            {
                Debug.Log("AI boss support shield ended.", this);
            }
        }

        public DamageInfo ModifyIncomingDamage(HealthComponent target, DamageInfo damageInfo)
        {
            if (!IsActive || damageInfo.damage <= 0f || damageInfo.sourceType != DamageSourceType.Enemy)
            {
                return damageInfo;
            }

            damageInfo.damage *= IncomingDamageMultiplier;
            return damageInfo;
        }
    }
}
