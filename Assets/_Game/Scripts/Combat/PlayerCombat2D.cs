using AICompanionRoguelike.Character;
using UnityEngine;

namespace AICompanionRoguelike.Combat
{
    [RequireComponent(typeof(PlayerInputReader))]
    public sealed class PlayerCombat2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private PlayerMovement2D movement;
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

        private void Reset()
        {
            inputReader = GetComponent<PlayerInputReader>();
            movement = GetComponent<PlayerMovement2D>();
            attackOrigin = transform;
        }

        private void Awake()
        {
            inputReader = inputReader != null ? inputReader : GetComponent<PlayerInputReader>();
            movement = movement != null ? movement : GetComponent<PlayerMovement2D>();
            attackOrigin = attackOrigin != null ? attackOrigin : transform;
            body = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
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
            DamageInfo damageInfo = new DamageInfo(damage, DamageSourceType.Player, gameObject);

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

                health.TakeDamage(damageInfo);
                Debug.Log($"Player hit {health.name} for {damageInfo.damage} damage. HP: {health.CurrentHealth}/{health.MaxHealth}", health);
            }
        }

        private Vector2 GetAttackCenter(int facingDirection)
        {
            Vector3 origin = attackOrigin != null ? attackOrigin.position : transform.position;
            return origin + new Vector3(attackOffset.x * facingDirection, attackOffset.y, 0f);
        }

        private void OnDrawGizmosSelected()
        {
            int facingDirection = movement != null ? movement.FacingDirection : 1;
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(GetAttackCenter(facingDirection), attackBoxSize);
        }
    }
}
