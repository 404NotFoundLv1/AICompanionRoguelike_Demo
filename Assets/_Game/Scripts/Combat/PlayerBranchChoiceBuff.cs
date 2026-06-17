using UnityEngine;

namespace AICompanionRoguelike.Combat
{
    public sealed class PlayerBranchChoiceBuff : MonoBehaviour, IDamageModifier
    {
        [SerializeField, Min(0f)] private float remainingDuration;
        [SerializeField, Min(0f)] private float outgoingDamageMultiplier = 1f;
        [SerializeField, Min(0f)] private float incomingDamageMultiplier = 1f;
        [SerializeField] private bool logBuffChanges = true;

        public bool IsActive => remainingDuration > 0f;
        public float RemainingDuration => remainingDuration;
        public float OutgoingDamageMultiplier => IsActive ? outgoingDamageMultiplier : 1f;
        public float IncomingDamageMultiplier => IsActive ? incomingDamageMultiplier : 1f;

        private void Update()
        {
            if (remainingDuration <= 0f)
            {
                return;
            }

            remainingDuration = Mathf.Max(0f, remainingDuration - Time.deltaTime);
            if (remainingDuration <= 0f && logBuffChanges)
            {
                Debug.Log("Branch challenge combat buff ended.", this);
            }
        }

        public void Activate(float duration, float outgoingMultiplier, float incomingMultiplier)
        {
            remainingDuration = Mathf.Max(0f, duration);
            outgoingDamageMultiplier = Mathf.Max(0f, outgoingMultiplier);
            incomingDamageMultiplier = Mathf.Max(0f, incomingMultiplier);

            if (logBuffChanges)
            {
                Debug.Log(
                    $"Branch challenge combat buff active for {remainingDuration:0.0}s. Outgoing x{OutgoingDamageMultiplier:0.##}, incoming x{IncomingDamageMultiplier:0.##}.",
                    this);
            }
        }

        public DamageInfo ModifyIncomingDamage(HealthComponent target, DamageInfo damageInfo)
        {
            if (!IsActive || damageInfo.damage <= 0f)
            {
                return damageInfo;
            }

            damageInfo.damage *= IncomingDamageMultiplier;
            return damageInfo;
        }
    }
}
