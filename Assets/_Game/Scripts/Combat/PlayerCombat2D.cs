using AICompanionRoguelike.Character;
using AICompanionRoguelike.Roguelike;
using UnityEngine;

namespace AICompanionRoguelike.Combat
{
    [RequireComponent(typeof(PlayerInputReader))]
    public sealed class PlayerCombat2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private PlayerMovement2D movement;
        [SerializeField] private HealthComponent health;
        [SerializeField] private PlayerBranchChoiceBuff branchChoiceBuff;
        [SerializeField] private Transform attackOrigin;

        [Header("Attack")]
        [SerializeField, Min(0f)] private float damage = 35f;
        [SerializeField, Min(0f)] private float cooldown = 0.35f;
        [SerializeField] private Vector2 attackBoxSize = new Vector2(1.4f, 1f);
        [SerializeField] private Vector2 attackOffset = new Vector2(0.95f, 0f);
        [SerializeField] private LayerMask targetLayerMask = ~0;

        private readonly Collider2D[] hitBuffer = new Collider2D[12];
        private Rigidbody2D body;
        private float cooldownTimer;

        public event System.Action<PlayerCombat2D, HealthComponent, DamageInfo> TargetHit;

        public float Damage => damage;
        public float Cooldown => cooldown;
        public float EffectiveDamage => damage * GetGrowthRouteDamageMultiplier();

        private void Reset()
        {
            inputReader = GetComponent<PlayerInputReader>();
            movement = GetComponent<PlayerMovement2D>();
            health = GetComponent<HealthComponent>();
            branchChoiceBuff = GetComponent<PlayerBranchChoiceBuff>();
            attackOrigin = transform;
        }

        private void Awake()
        {
            inputReader = inputReader != null ? inputReader : GetComponent<PlayerInputReader>();
            movement = movement != null ? movement : GetComponent<PlayerMovement2D>();
            health = health != null ? health : GetComponent<HealthComponent>();
            branchChoiceBuff = branchChoiceBuff != null ? branchChoiceBuff : GetComponent<PlayerBranchChoiceBuff>();
            attackOrigin = attackOrigin != null ? attackOrigin : transform;
            body = GetComponent<Rigidbody2D>();
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

            if (inputReader == null || !inputReader.ConsumeAttackPressed() || cooldownTimer > 0f)
            {
                return;
            }

            Attack();
            cooldownTimer = cooldown;
        }

        private void Attack()
        {
            int facingDirection = movement != null ? movement.FacingDirection : 1;
            Vector2 center = GetAttackCenter(facingDirection);
            ContactFilter2D targetFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = targetLayerMask,
                useTriggers = false
            };
            int hitCount = Physics2D.OverlapBox(center, attackBoxSize, 0f, targetFilter, hitBuffer);
            branchChoiceBuff = branchChoiceBuff != null ? branchChoiceBuff : GetComponent<PlayerBranchChoiceBuff>();
            float outgoingMultiplier = branchChoiceBuff != null ? branchChoiceBuff.OutgoingDamageMultiplier : 1f;
            DamageInfo damageInfo = new DamageInfo(
                damage * outgoingMultiplier * GetGrowthRouteDamageMultiplier(),
                DamageSourceType.Player,
                gameObject);
            PlayerCounterplayFeedback[] counterplayFeedbacks = GetComponents<PlayerCounterplayFeedback>();

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = hitBuffer[i];
                if (hit == null || hit.attachedRigidbody == body)
                {
                    continue;
                }

                if (!hit.TryGetComponent(out HealthComponent health))
                {
                    health = hit.GetComponentInParent<HealthComponent>();
                }

                if (health == null || health.IsDead)
                {
                    continue;
                }

                DamageInfo hitDamageInfo = damageInfo;
                for (int feedbackIndex = 0; feedbackIndex < counterplayFeedbacks.Length; feedbackIndex++)
                {
                    hitDamageInfo = counterplayFeedbacks[feedbackIndex].ModifyOutgoingDamage(health, hitDamageInfo);
                }

                health.TakeDamage(hitDamageInfo);
                TargetHit?.Invoke(this, health, hitDamageInfo);
                for (int feedbackIndex = 0; feedbackIndex < counterplayFeedbacks.Length; feedbackIndex++)
                {
                    counterplayFeedbacks[feedbackIndex].ReportPlayerHitTarget(health, hitDamageInfo);
                }

                Debug.Log($"Player hit {health.name} for {hitDamageInfo.damage} damage. HP: {health.CurrentHealth}/{health.MaxHealth}", health);
            }
        }

        private Vector2 GetAttackCenter(int facingDirection)
        {
            Vector3 origin = attackOrigin != null ? attackOrigin.position : transform.position;
            return origin + new Vector3(attackOffset.x * facingDirection, attackOffset.y, 0f);
        }

        public void MultiplyDamage(float multiplier)
        {
            damage = Mathf.Max(0f, damage * Mathf.Max(0f, multiplier));
        }

        private static float GetGrowthRouteDamageMultiplier()
        {
            RunManager runManager = RunManager.FindActiveRunManager();
            return runManager != null ? runManager.PlayerRouteDamageMultiplier : 1f;
        }

        private void OnDrawGizmosSelected()
        {
            int facingDirection = movement != null ? movement.FacingDirection : 1;
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(GetAttackCenter(facingDirection), attackBoxSize);
        }
    }
}
