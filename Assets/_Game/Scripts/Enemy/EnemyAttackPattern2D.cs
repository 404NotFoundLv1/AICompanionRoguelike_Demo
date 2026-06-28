using AICompanionRoguelike.Combat;
using UnityEngine;

namespace AICompanionRoguelike.Enemy
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(EnemyAttack2D))]
    [RequireComponent(typeof(EnemyController2D))]
    public sealed class EnemyAttackPattern2D : MonoBehaviour, IDamageModifier
    {
        [SerializeField] private EnemyArchetypeType archetypeType = EnemyArchetypeType.Melee;
        [SerializeField] private EnemyAttack2D attack;
        [SerializeField] private EnemyController2D controller;

        [Header("Melee Lunge")]
        [SerializeField, Min(0.1f)] private float lungeSpeed = 5.5f;
        [SerializeField, Min(0.05f)] private float lungeDuration = 0.18f;
        [SerializeField, Min(0.05f)] private float maximumLungeDistance = 0.75f;

        [Header("Guard Defense")]
        [SerializeField, Range(0.05f, 1f)] private float guardBlockMultiplier = 0.35f;
        [SerializeField, Min(1f)] private float guardVulnerabilityMultiplier = 1.35f;
        [SerializeField, Min(0.1f)] private float guardVulnerabilityDuration = 1.1f;

        private const string BehaviorVisualName = "EnemyAttackBehaviorVisual";

        private static Sprite sharedBehaviorSprite;

        private Rigidbody2D body;
        private SpriteRenderer behaviorRenderer;
        private float lungeTimer;
        private float remainingLungeDistance;
        private Vector2 lungeDirection;
        private float guardWindupTimer;
        private float guardVulnerabilityTimer;
        private bool subscribed;

        public EnemyArchetypeType ArchetypeType => archetypeType;
        public bool IsLunging => archetypeType == EnemyArchetypeType.Melee && lungeTimer > 0f;
        public bool IsGuardVulnerable => archetypeType == EnemyArchetypeType.Guard
            && guardVulnerabilityTimer > 0f;
        public bool HasBehaviorVisual => behaviorRenderer != null && behaviorRenderer.sprite != null;
        public bool LastDamageWasBlocked { get; private set; }

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            SubscribeToAttack();
        }

        private void OnDisable()
        {
            UnsubscribeFromAttack();
            lungeTimer = 0f;
            guardWindupTimer = 0f;
            guardVulnerabilityTimer = 0f;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private void OnValidate()
        {
            lungeSpeed = Mathf.Max(0.1f, lungeSpeed);
            lungeDuration = Mathf.Max(0.05f, lungeDuration);
            maximumLungeDistance = Mathf.Max(0.05f, maximumLungeDistance);
            guardBlockMultiplier = Mathf.Clamp(guardBlockMultiplier, 0.05f, 1f);
            guardVulnerabilityMultiplier = Mathf.Max(1f, guardVulnerabilityMultiplier);
            guardVulnerabilityDuration = Mathf.Max(0.1f, guardVulnerabilityDuration);
        }

        public void Configure(EnemyArchetypeType newArchetypeType)
        {
            ResolveReferences();
            archetypeType = newArchetypeType;
            lungeSpeed = EnemyArchetypeRules.GetMeleeLungeSpeed();
            maximumLungeDistance = EnemyArchetypeRules.GetMeleeLungeDistance();
            guardBlockMultiplier = EnemyArchetypeRules.GetGuardBlockMultiplier();
            guardVulnerabilityMultiplier = EnemyArchetypeRules.GetGuardVulnerabilityMultiplier();
            guardVulnerabilityDuration = EnemyArchetypeRules.GetGuardVulnerabilityDuration();
            EnsureBehaviorVisual();
            SubscribeToAttack();
            UpdateBehaviorVisual();
        }

        public void Tick(float deltaTime)
        {
            float safeDeltaTime = Mathf.Max(0f, deltaTime);
            TickMeleeLunge(safeDeltaTime);
            TickGuardState(safeDeltaTime);
            UpdateBehaviorVisual();
        }

        public DamageInfo ModifyIncomingDamage(HealthComponent target, DamageInfo damageInfo)
        {
            LastDamageWasBlocked = false;
            if (archetypeType != EnemyArchetypeType.Guard || damageInfo.damage <= 0f)
            {
                return damageInfo;
            }

            if (IsGuardVulnerable)
            {
                damageInfo.damage *= guardVulnerabilityMultiplier;
                return damageInfo;
            }

            if (IsSourceInFront(damageInfo.sourceObject))
            {
                damageInfo.damage *= guardBlockMultiplier;
                LastDamageWasBlocked = true;
            }

            return damageInfo;
        }

        private void ResolveReferences()
        {
            body = body != null ? body : GetComponent<Rigidbody2D>();
            attack = attack != null ? attack : GetComponent<EnemyAttack2D>();
            controller = controller != null ? controller : GetComponent<EnemyController2D>();
        }

        private void SubscribeToAttack()
        {
            if (subscribed || attack == null)
            {
                return;
            }

            attack.WarningStarted += HandleWarningStarted;
            subscribed = true;
        }

        private void UnsubscribeFromAttack()
        {
            if (!subscribed || attack == null)
            {
                return;
            }

            attack.WarningStarted -= HandleWarningStarted;
            subscribed = false;
        }

        private void HandleWarningStarted(EnemyAttack2D sourceAttack)
        {
            if (archetypeType == EnemyArchetypeType.Melee)
            {
                BeginMeleeLunge();
                return;
            }

            if (archetypeType == EnemyArchetypeType.Guard)
            {
                guardVulnerabilityTimer = 0f;
                guardWindupTimer = Mathf.Max(0.01f, sourceAttack.WarningDuration);
            }
        }

        private void BeginMeleeLunge()
        {
            Transform target = controller != null ? controller.Target : null;
            if (target == null || body == null)
            {
                return;
            }

            Vector2 delta = (Vector2)target.position - body.position;
            if (delta.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            lungeDirection = delta.normalized;
            remainingLungeDistance = Mathf.Min(
                maximumLungeDistance,
                Mathf.Max(0.05f, delta.magnitude - 0.25f));
            lungeTimer = lungeDuration;
        }

        private void TickMeleeLunge(float deltaTime)
        {
            if (!IsLunging || body == null)
            {
                return;
            }

            float movementTime = Mathf.Min(deltaTime, lungeTimer);
            float movement = Mathf.Min(remainingLungeDistance, lungeSpeed * movementTime);
            Vector2 nextPosition = body.position + (lungeDirection * movement);
            body.position = nextPosition;
            if (!Application.isPlaying)
            {
                transform.position = new Vector3(nextPosition.x, nextPosition.y, transform.position.z);
            }
            remainingLungeDistance = Mathf.Max(0f, remainingLungeDistance - movement);
            lungeTimer = Mathf.Max(0f, lungeTimer - deltaTime);

            if (remainingLungeDistance <= 0f)
            {
                lungeTimer = 0f;
            }
        }

        private void TickGuardState(float deltaTime)
        {
            if (archetypeType != EnemyArchetypeType.Guard)
            {
                return;
            }

            if (guardWindupTimer > 0f)
            {
                guardWindupTimer = Mathf.Max(0f, guardWindupTimer - deltaTime);
                if (guardWindupTimer <= 0f)
                {
                    guardVulnerabilityTimer = guardVulnerabilityDuration;
                }

                return;
            }

            if (guardVulnerabilityTimer > 0f)
            {
                guardVulnerabilityTimer = Mathf.Max(0f, guardVulnerabilityTimer - deltaTime);
            }
        }

        private bool IsSourceInFront(GameObject sourceObject)
        {
            if (sourceObject == null)
            {
                return false;
            }

            float deltaX = sourceObject.transform.position.x - transform.position.x;
            if (Mathf.Abs(deltaX) <= 0.01f)
            {
                return true;
            }

            int sourceDirection = deltaX > 0f ? 1 : -1;
            int facingDirection = controller != null ? controller.FacingDirection : -1;
            return sourceDirection == facingDirection;
        }

        private void EnsureBehaviorVisual()
        {
            if (archetypeType == EnemyArchetypeType.Ranged)
            {
                if (behaviorRenderer != null)
                {
                    behaviorRenderer.gameObject.SetActive(false);
                }

                return;
            }

            Transform visualTransform = transform.Find(BehaviorVisualName);
            if (visualTransform == null)
            {
                GameObject visualObject = new GameObject(BehaviorVisualName);
                visualTransform = visualObject.transform;
                visualTransform.SetParent(transform, false);
            }

            behaviorRenderer = visualTransform.GetComponent<SpriteRenderer>();
            if (behaviorRenderer == null)
            {
                behaviorRenderer = visualTransform.gameObject.AddComponent<SpriteRenderer>();
            }

            behaviorRenderer.sprite = GetBehaviorSprite();
            behaviorRenderer.sortingOrder = 51;
        }

        private void UpdateBehaviorVisual()
        {
            if (behaviorRenderer == null)
            {
                return;
            }

            int facingDirection = controller != null ? controller.FacingDirection : -1;
            if (archetypeType == EnemyArchetypeType.Melee)
            {
                behaviorRenderer.gameObject.SetActive(IsLunging);
                behaviorRenderer.transform.localPosition = new Vector3(-facingDirection * 0.5f, 0f, 0f);
                behaviorRenderer.transform.localScale = new Vector3(0.55f, 0.16f, 1f);
                behaviorRenderer.color = new Color(1f, 0.2f, 0.08f, 0.55f);
                return;
            }

            if (archetypeType == EnemyArchetypeType.Guard)
            {
                behaviorRenderer.gameObject.SetActive(true);
                behaviorRenderer.transform.localPosition = new Vector3(facingDirection * 0.62f, 0f, 0f);
                behaviorRenderer.transform.localScale = new Vector3(0.12f, 1.05f, 1f);
                behaviorRenderer.color = IsGuardVulnerable
                    ? new Color(1f, 0.2f, 0.7f, 0.82f)
                    : new Color(1f, 0.9f, 0.2f, 0.9f);
            }
        }

        private static Sprite GetBehaviorSprite()
        {
            if (sharedBehaviorSprite != null)
            {
                return sharedBehaviorSprite;
            }

            Texture2D texture = Texture2D.whiteTexture;
            sharedBehaviorSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                texture.width);
            sharedBehaviorSprite.hideFlags = HideFlags.HideAndDontSave;
            return sharedBehaviorSprite;
        }
    }
}
