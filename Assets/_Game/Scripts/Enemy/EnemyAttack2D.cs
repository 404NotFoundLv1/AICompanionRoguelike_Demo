using System;
using AICompanionRoguelike.Combat;
using UnityEngine;

namespace AICompanionRoguelike.Enemy
{
    public sealed class EnemyAttack2D : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float damage = 10f;
        [SerializeField, Min(0f)] private float cooldown = 1f;
        [SerializeField, Min(0f)] private float attackRange = 1.2f;
        [SerializeField, Min(0f)] private float warningDuration = 0.35f;
        [SerializeField] private Vector2 warningSize = new Vector2(1.35f, 0.8f);
        [SerializeField] private Color warningColor = new Color(1f, 0.18f, 0.08f, 0.42f);
        [SerializeField] private int warningSortingOrder = 42;

        private EnemyController2D owner;
        private float cooldownTimer;
        private float tacticalSuppressionTimer;
        private float tacticalDamageMultiplier = 1f;
        private Transform warnedTarget;
        private HealthComponent warnedTargetHealth;
        private Transform warningRoot;
        private SpriteRenderer warningRenderer;
        private bool isWarningActive;
        private float warningTimer;

        private static Sprite sharedWarningSprite;

        public event Action<EnemyAttack2D> WarningStarted;
        public event Action<EnemyAttack2D, bool> AttackResolved;

        public float Damage => damage;
        public float CurrentDamage => damage * TacticalDamageMultiplier;
        public bool IsTacticallySuppressed => tacticalSuppressionTimer > 0f;
        public float TacticalDamageMultiplier => IsTacticallySuppressed ? tacticalDamageMultiplier : 1f;
        public bool IsWarningActive => isWarningActive;
        public bool HasWarningVisual => warningRenderer != null;
        public float WarningProgress01 => warningDuration <= 0f ? 1f : Mathf.Clamp01(1f - (warningTimer / warningDuration));

        private void Awake()
        {
            owner = owner != null ? owner : GetComponent<EnemyController2D>();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private void OnDisable()
        {
            CancelWarning();
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

        public void Tick(float deltaTime)
        {
            if (cooldownTimer > 0f)
            {
                cooldownTimer = Mathf.Max(0f, cooldownTimer - Mathf.Max(0f, deltaTime));
            }

            TickTacticalSuppression(deltaTime);

            if (!isWarningActive)
            {
                return;
            }

            warningTimer = Mathf.Max(0f, warningTimer - Mathf.Max(0f, deltaTime));
            UpdateWarningVisual();

            if (warningTimer <= 0f)
            {
                ResolveWarnedAttack();
            }
        }

        public void SetOwner(EnemyController2D enemyOwner)
        {
            owner = enemyOwner;
        }

        public void TryAttack(Transform target)
        {
            if (target == null || cooldownTimer > 0f || isWarningActive)
            {
                return;
            }

            float distance = Vector2.Distance(transform.position, target.position);
            if (distance > attackRange)
            {
                return;
            }

            HealthComponent targetHealth = ResolveTargetHealth(target);
            if (targetHealth == null || targetHealth.IsDead)
            {
                return;
            }

            if (warningDuration <= 0f)
            {
                ResolveImmediateAttack(target, targetHealth);
                return;
            }

            StartWarning(target, targetHealth);
        }

        public void MultiplyDamage(float multiplier)
        {
            damage = Mathf.Max(0f, damage * Mathf.Max(0f, multiplier));
        }

        public void ApplyTacticalSuppression(float duration, float damageMultiplier)
        {
            if (duration <= 0f)
            {
                return;
            }

            tacticalSuppressionTimer = Mathf.Max(tacticalSuppressionTimer, duration);
            tacticalDamageMultiplier = Mathf.Min(
                IsTacticallySuppressed ? tacticalDamageMultiplier : 1f,
                Mathf.Clamp(damageMultiplier, 0.05f, 1f));
        }

        public void TickTacticalSuppression(float deltaTime)
        {
            if (tacticalSuppressionTimer <= 0f)
            {
                tacticalDamageMultiplier = 1f;
                return;
            }

            tacticalSuppressionTimer = Mathf.Max(0f, tacticalSuppressionTimer - Mathf.Max(0f, deltaTime));
            if (tacticalSuppressionTimer <= 0f)
            {
                tacticalDamageMultiplier = 1f;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            Gizmos.color = new Color(1f, 0.15f, 0.05f, 0.35f);
            Vector3 center = warnedTarget != null ? warnedTarget.position : transform.position;
            Gizmos.DrawWireCube(center, warningSize);
        }

        private void StartWarning(Transform target, HealthComponent targetHealth)
        {
            warnedTarget = target;
            warnedTargetHealth = targetHealth;
            warningTimer = warningDuration;
            isWarningActive = true;

            EnsureWarningVisual();
            UpdateWarningVisual();
            SetWarningVisualActive(true);
            WarningStarted?.Invoke(this);
        }

        private void ResolveImmediateAttack(Transform target, HealthComponent targetHealth)
        {
            ApplyDamageToTarget(target, targetHealth);
            cooldownTimer = cooldown;
        }

        private void ResolveWarnedAttack()
        {
            bool hit = false;
            if (warnedTarget != null)
            {
                warnedTargetHealth = warnedTargetHealth != null
                    ? warnedTargetHealth
                    : ResolveTargetHealth(warnedTarget);

                if (warnedTargetHealth != null
                    && !warnedTargetHealth.IsDead
                    && Vector2.Distance(transform.position, warnedTarget.position) <= attackRange)
                {
                    ApplyDamageToTarget(warnedTarget, warnedTargetHealth);
                    hit = true;
                }
            }

            isWarningActive = false;
            warnedTarget = null;
            warnedTargetHealth = null;
            cooldownTimer = cooldown;
            SetWarningVisualActive(false);
            AttackResolved?.Invoke(this, hit);
        }

        private void CancelWarning()
        {
            isWarningActive = false;
            warningTimer = 0f;
            warnedTarget = null;
            warnedTargetHealth = null;
            SetWarningVisualActive(false);
        }

        private void ApplyDamageToTarget(Transform target, HealthComponent targetHealth)
        {
            float currentDamage = CurrentDamage;
            DamageInfo damageInfo = new DamageInfo(currentDamage, DamageSourceType.Enemy, gameObject);
            targetHealth.TakeDamage(damageInfo);
            Debug.Log($"{name} attacked {target.name} for {currentDamage} damage. Target HP: {targetHealth.CurrentHealth}/{targetHealth.MaxHealth}", this);
        }

        private void EnsureWarningVisual()
        {
            if (warningRoot != null && warningRenderer != null)
            {
                return;
            }

            GameObject visualObject = warningRoot != null
                ? warningRoot.gameObject
                : new GameObject($"{name}_EnemyWarning");
            warningRoot = visualObject.transform;

            warningRenderer = warningRenderer != null
                ? warningRenderer
                : visualObject.GetComponent<SpriteRenderer>();
            if (warningRenderer == null)
            {
                warningRenderer = visualObject.AddComponent<SpriteRenderer>();
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

            Vector3 center = warnedTarget != null ? warnedTarget.position : transform.position;
            warningRoot.position = new Vector3(center.x, center.y, 0f);
            warningRoot.localScale = new Vector3(Mathf.Max(0.1f, warningSize.x), Mathf.Max(0.1f, warningSize.y), 1f);
            warningRenderer.color = Color.Lerp(warningColor, new Color(1f, 0f, 0f, 0.65f), WarningProgress01);
            warningRenderer.sortingOrder = warningSortingOrder;
        }

        private void SetWarningVisualActive(bool active)
        {
            if (warningRoot != null)
            {
                warningRoot.gameObject.SetActive(active);
            }
        }

        private static HealthComponent ResolveTargetHealth(Transform target)
        {
            if (target == null)
            {
                return null;
            }

            if (!target.TryGetComponent(out HealthComponent targetHealth))
            {
                targetHealth = target.GetComponentInParent<HealthComponent>();
            }

            return targetHealth;
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
    }
}
