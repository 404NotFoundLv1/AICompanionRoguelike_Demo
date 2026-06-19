using System;
using AICompanionRoguelike.Combat;
using UnityEngine;

namespace AICompanionRoguelike.Enemy
{
    public sealed class BossTelegraphedAttack2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform target;
        [SerializeField] private HealthComponent targetHealth;
        [SerializeField] private BossPhaseController phaseController;
        [SerializeField] private Transform warningRoot;
        [SerializeField] private SpriteRenderer warningRenderer;

        [Header("Attack")]
        [SerializeField, Min(0f)] private float damage = 18f;
        [SerializeField, Min(0f)] private float triggerRange = 4.5f;
        [SerializeField, Min(0.05f)] private float warningDuration = 0.9f;
        [SerializeField, Min(0.05f)] private float cooldown = 4f;
        [SerializeField] private Vector2 attackSize = new Vector2(2.8f, 1.35f);

        [Header("Phase Two Tuning")]
        [SerializeField, Min(1f)] private float phaseTwoDamageMultiplier = 1.25f;
        [SerializeField, Range(0.1f, 1f)] private float phaseTwoCooldownMultiplier = 0.75f;

        [Header("Warning Visual")]
        [SerializeField] private Color warningColor = new Color(1f, 0.05f, 0.02f, 0.38f);
        [SerializeField] private int warningSortingOrder = 40;

        [Header("Debug")]
        [SerializeField] private bool logAttackMessages = true;

        private static Sprite sharedWarningSprite;

        private bool isWarningActive;
        private bool phaseTwoTuningApplied;
        private float warningTimer;
        private float cooldownTimer;
        private Vector2 warnedCenter;

        public event Action<BossTelegraphedAttack2D> WarningStarted;
        public event Action<BossTelegraphedAttack2D, bool> AttackResolved;

        public bool IsWarningActive => isWarningActive;
        public float WarningProgress01 => warningDuration <= 0f ? 1f : Mathf.Clamp01(1f - (warningTimer / warningDuration));
        public Vector2 WarnedCenter => warnedCenter;
        public Vector2 AttackSize => attackSize;
        public float Damage => damage;
        public float Cooldown => cooldown;

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
            EnsureWarningVisual();
            SetWarningVisualActive(false);
        }

        private void OnEnable()
        {
            ResolveReferences();
            SubscribeToPhaseController();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private void OnDisable()
        {
            UnsubscribeFromPhaseController();
            SetWarningVisualActive(false);
        }

        private void OnDestroy()
        {
            if (warningRoot == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(warningRoot.gameObject);
            }
            else
            {
                DestroyImmediate(warningRoot.gameObject);
            }
        }

        private void OnValidate()
        {
            damage = Mathf.Max(0f, damage);
            triggerRange = Mathf.Max(0f, triggerRange);
            warningDuration = Mathf.Max(0.05f, warningDuration);
            cooldown = Mathf.Max(0.05f, cooldown);
            attackSize = new Vector2(Mathf.Max(0.1f, attackSize.x), Mathf.Max(0.1f, attackSize.y));
            phaseTwoDamageMultiplier = Mathf.Max(1f, phaseTwoDamageMultiplier);
            phaseTwoCooldownMultiplier = Mathf.Clamp(phaseTwoCooldownMultiplier, 0.1f, 1f);
        }

        public void Configure(
            Transform newTarget,
            float damage,
            float triggerRange,
            float warningDuration,
            float cooldown,
            Vector2 attackSize)
        {
            target = newTarget;
            targetHealth = ResolveTargetHealth(target);
            this.damage = Mathf.Max(0f, damage);
            this.triggerRange = Mathf.Max(0f, triggerRange);
            this.warningDuration = Mathf.Max(0.05f, warningDuration);
            this.cooldown = Mathf.Max(0.05f, cooldown);
            this.attackSize = new Vector2(Mathf.Max(0.1f, attackSize.x), Mathf.Max(0.1f, attackSize.y));

            ResolveReferences();
            EnsureWarningVisual();
            UpdateWarningVisual();
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            targetHealth = ResolveTargetHealth(target);
        }

        public void SetPhaseTwoTuning(float damageMultiplier, float cooldownMultiplier)
        {
            phaseTwoDamageMultiplier = Mathf.Max(1f, damageMultiplier);
            phaseTwoCooldownMultiplier = Mathf.Clamp(cooldownMultiplier, 0.1f, 1f);
        }

        public void Tick(float deltaTime)
        {
            deltaTime = Mathf.Max(0f, deltaTime);

            if (cooldownTimer > 0f)
            {
                cooldownTimer = Mathf.Max(0f, cooldownTimer - deltaTime);
            }

            if (isWarningActive)
            {
                warningTimer -= deltaTime;
                UpdateWarningVisual();

                if (warningTimer <= 0f)
                {
                    ResolveImpact();
                }

                return;
            }

            if (cooldownTimer <= 0f)
            {
                TryStartWarning();
            }
        }

        public bool TryStartWarning()
        {
            if (isWarningActive || cooldownTimer > 0f || target == null)
            {
                return false;
            }

            targetHealth = targetHealth != null ? targetHealth : ResolveTargetHealth(target);
            if (targetHealth == null || targetHealth.IsDead)
            {
                return false;
            }

            float distance = Vector2.Distance(transform.position, target.position);
            if (distance > triggerRange)
            {
                return false;
            }

            warnedCenter = target.position;
            warningTimer = warningDuration;
            isWarningActive = true;

            EnsureWarningVisual();
            UpdateWarningVisual();
            SetWarningVisualActive(true);

            if (logAttackMessages)
            {
                Debug.Log($"{name} is warning a Boss area attack.", this);
            }

            WarningStarted?.Invoke(this);
            return true;
        }

        private void ResolveReferences()
        {
            phaseController = phaseController != null ? phaseController : GetComponent<BossPhaseController>();
            targetHealth = targetHealth != null ? targetHealth : ResolveTargetHealth(target);
        }

        private void SubscribeToPhaseController()
        {
            if (phaseController == null)
            {
                return;
            }

            phaseController.PhaseTwoStarted -= HandlePhaseTwoStarted;
            phaseController.PhaseTwoStarted += HandlePhaseTwoStarted;
        }

        private void UnsubscribeFromPhaseController()
        {
            if (phaseController == null)
            {
                return;
            }

            phaseController.PhaseTwoStarted -= HandlePhaseTwoStarted;
        }

        private void HandlePhaseTwoStarted(BossPhaseController controller)
        {
            if (phaseTwoTuningApplied)
            {
                return;
            }

            phaseTwoTuningApplied = true;
            damage *= phaseTwoDamageMultiplier;
            cooldown *= phaseTwoCooldownMultiplier;
            cooldownTimer = Mathf.Min(cooldownTimer, cooldown);
        }

        private void ResolveImpact()
        {
            bool hit = false;
            if (targetHealth != null && !targetHealth.IsDead && IsTargetInsideWarnedArea())
            {
                targetHealth.TakeDamage(new DamageInfo(damage, DamageSourceType.Enemy, gameObject));
                hit = true;
            }

            isWarningActive = false;
            cooldownTimer = cooldown;
            SetWarningVisualActive(false);

            if (logAttackMessages)
            {
                string outcome = hit ? "hit" : "missed";
                Debug.Log($"{name} Boss area attack {outcome}.", this);
            }

            AttackResolved?.Invoke(this, hit);
        }

        private bool IsTargetInsideWarnedArea()
        {
            if (target == null)
            {
                return false;
            }

            Vector2 targetPosition = target.position;
            Vector2 halfSize = attackSize * 0.5f;
            return Mathf.Abs(targetPosition.x - warnedCenter.x) <= halfSize.x
                && Mathf.Abs(targetPosition.y - warnedCenter.y) <= halfSize.y;
        }

        private void EnsureWarningVisual()
        {
            if (warningRoot != null && warningRenderer != null)
            {
                return;
            }

            GameObject visualObject = warningRoot != null
                ? warningRoot.gameObject
                : new GameObject($"{name}_AttackWarning");
            warningRoot = visualObject.transform;

            if (warningRenderer == null)
            {
                warningRenderer = visualObject.GetComponent<SpriteRenderer>();
                if (warningRenderer == null)
                {
                    warningRenderer = visualObject.AddComponent<SpriteRenderer>();
                }
            }

            warningRenderer.sprite = GetWarningSprite();
            warningRenderer.color = warningColor;
            warningRenderer.sortingOrder = warningSortingOrder;
        }

        private void UpdateWarningVisual()
        {
            if (warningRoot == null || warningRenderer == null)
            {
                return;
            }

            warningRoot.position = new Vector3(warnedCenter.x, warnedCenter.y, 0f);
            warningRoot.localScale = new Vector3(attackSize.x, attackSize.y, 1f);
            warningRenderer.color = Color.Lerp(warningColor, new Color(1f, 0f, 0f, 0.62f), WarningProgress01);
            warningRenderer.sortingOrder = warningSortingOrder;
        }

        private void SetWarningVisualActive(bool active)
        {
            if (warningRoot != null)
            {
                warningRoot.gameObject.SetActive(active);
            }
        }

        private static HealthComponent ResolveTargetHealth(Transform targetTransform)
        {
            if (targetTransform == null)
            {
                return null;
            }

            if (!targetTransform.TryGetComponent(out HealthComponent health))
            {
                health = targetTransform.GetComponentInParent<HealthComponent>();
            }

            return health;
        }

        private static Sprite GetWarningSprite()
        {
            if (sharedWarningSprite != null)
            {
                return sharedWarningSprite;
            }

            Texture2D texture = Texture2D.whiteTexture;
            sharedWarningSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                texture.width);
            return sharedWarningSprite;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
            Vector2 center = isWarningActive ? warnedCenter : (Vector2)transform.position;
            Gizmos.DrawWireCube(center, attackSize);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, triggerRange);
        }
    }
}
