using AICompanionRoguelike.Character;
using AICompanionRoguelike.Enemy;
using AICompanionRoguelike.Roguelike;
using UnityEngine;

namespace AICompanionRoguelike.Combat
{
    public enum PlayerCounterplayFeedbackKind
    {
        None,
        DashStarted,
        DashDodge,
        ProjectileDodge,
        RecoveryBlock,
        HitTaken,
        GuardOpening
    }

    [RequireComponent(typeof(HealthComponent))]
    [RequireComponent(typeof(PlayerMovement2D))]
    public sealed class PlayerCounterplayFeedback : MonoBehaviour, IDamageModifier
    {
        [Header("References")]
        [SerializeField] private HealthComponent health;
        [SerializeField] private PlayerMovement2D movement;

        [Header("Counterplay")]
        [SerializeField, Min(0f)] private float postHitInvulnerabilityDuration = 0.45f;
        [SerializeField, Min(0f)] private float dodgeDamageBoostDuration;
        [SerializeField, Min(1f)] private float dodgeDamageMultiplier = 1f;
        [SerializeField, Min(1f)] private float guardOpeningDamageMultiplier = 1f;
        [SerializeField, Min(0.05f)] private float feedbackDuration = 1.15f;

        [Header("Visual Feedback")]
        [SerializeField] private bool showCounterplayVisual = true;
        [SerializeField] private SpriteRenderer visualRenderer;
        [SerializeField] private Color dodgeColor = new Color(0.25f, 0.9f, 1f, 0.65f);
        [SerializeField] private Color recoveryColor = new Color(1f, 0.84f, 0.25f, 0.45f);
        [SerializeField] private Color counterColor = new Color(1f, 0.25f, 0.7f, 0.7f);

        private const string VisualObjectName = "PlayerCounterplayVisual";

        private static Sprite sharedVisualSprite;

        private HealthComponent subscribedHealth;
        private PlayerMovement2D subscribedMovement;
        private float recoveryTimer;
        private float dodgeDamageBoostTimer;
        private float feedbackTimer;

        public event System.Action<PlayerCounterplayFeedback, PlayerCounterplayFeedbackKind, string> FeedbackIssued;

        public bool IsRecovering => recoveryTimer > 0f;
        public bool HasFeedback => LastFeedbackKind != PlayerCounterplayFeedbackKind.None
            && !string.IsNullOrWhiteSpace(LastFeedbackMessage);
        public PlayerCounterplayFeedbackKind LastFeedbackKind { get; private set; }
        public string LastFeedbackMessage { get; private set; } = string.Empty;
        public float PostHitInvulnerabilityDuration => postHitInvulnerabilityDuration;
        public float DodgeDamageBoostDuration => dodgeDamageBoostDuration;
        public float DodgeDamageMultiplier => dodgeDamageMultiplier;
        public float GuardOpeningDamageMultiplier => guardOpeningDamageMultiplier;
        public float EffectiveDodgeDamageBoostDuration => dodgeDamageBoostDuration + GetCounterplayRouteDodgeBoostDurationBonus();
        public float EffectiveDodgeDamageMultiplier => dodgeDamageMultiplier * GetCounterplayRouteDamageMultiplier();
        public float EffectiveGuardOpeningDamageMultiplier => guardOpeningDamageMultiplier * GetCounterplayRouteDamageMultiplier();
        public float DodgeDamageBoostRemaining => dodgeDamageBoostTimer;
        public bool IsDodgeDamageBoostActive => dodgeDamageBoostTimer > 0f && EffectiveDodgeDamageMultiplier > 1f;

        private void Reset()
        {
            health = GetComponent<HealthComponent>();
            movement = GetComponent<PlayerMovement2D>();
        }

        private void Awake()
        {
            ResolveReferences();
            EnsureVisual();
            UpdateVisual();
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
            EnsureVisual();
            UpdateVisual();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnValidate()
        {
            postHitInvulnerabilityDuration = Mathf.Max(0f, postHitInvulnerabilityDuration);
            dodgeDamageBoostDuration = Mathf.Max(0f, dodgeDamageBoostDuration);
            dodgeDamageMultiplier = Mathf.Max(1f, dodgeDamageMultiplier);
            guardOpeningDamageMultiplier = Mathf.Max(1f, guardOpeningDamageMultiplier);
            feedbackDuration = Mathf.Max(0.05f, feedbackDuration);
            UpdateVisual();
        }

        public void Tick(float deltaTime)
        {
            float safeDeltaTime = Mathf.Max(0f, deltaTime);
            if (recoveryTimer > 0f)
            {
                recoveryTimer = Mathf.Max(0f, recoveryTimer - safeDeltaTime);
            }

            if (feedbackTimer > 0f)
            {
                feedbackTimer = Mathf.Max(0f, feedbackTimer - safeDeltaTime);
                if (feedbackTimer <= 0f)
                {
                    ClearFeedback();
                }
            }

            if (dodgeDamageBoostTimer > 0f)
            {
                dodgeDamageBoostTimer = Mathf.Max(0f, dodgeDamageBoostTimer - safeDeltaTime);
            }

            UpdateVisual();
        }

        public void ClearFeedback()
        {
            LastFeedbackKind = PlayerCounterplayFeedbackKind.None;
            LastFeedbackMessage = string.Empty;
            feedbackTimer = 0f;
            UpdateVisual();
        }

        public void ReportProjectileDodge()
        {
            ActivateDodgeDamageBoost();
            IssueFeedback(PlayerCounterplayFeedbackKind.ProjectileDodge, "Perfect dodge: projectile avoided.");
        }

        public void ReportPlayerHitTarget(HealthComponent targetHealth, DamageInfo damageInfo)
        {
            HandleTargetHit(null, targetHealth, damageInfo);
        }

        public DamageInfo ModifyIncomingDamage(HealthComponent target, DamageInfo damageInfo)
        {
            ResolveReferences();
            if (target != health
                || damageInfo.damage <= 0f
                || damageInfo.sourceType != DamageSourceType.Enemy)
            {
                return damageInfo;
            }

            if (movement != null && movement.IsInvincible)
            {
                damageInfo.damage = 0f;
                ActivateDodgeDamageBoost();
                IssueFeedback(PlayerCounterplayFeedbackKind.DashDodge, "Perfect dodge: no damage.");
                return damageInfo;
            }

            if (IsRecovering)
            {
                damageInfo.damage = 0f;
                IssueFeedback(PlayerCounterplayFeedbackKind.RecoveryBlock, "Recovering: avoided follow-up damage.");
                return damageInfo;
            }

            BeginRecovery();
            return damageInfo;
        }

        public DamageInfo ModifyOutgoingDamage(HealthComponent targetHealth, DamageInfo damageInfo)
        {
            if (targetHealth == null
                || damageInfo.damage <= 0f
                || damageInfo.sourceType != DamageSourceType.Player)
            {
                return damageInfo;
            }

            float outgoingMultiplier = 1f;
            if (IsDodgeDamageBoostActive)
            {
                outgoingMultiplier *= EffectiveDodgeDamageMultiplier;
            }

            if (IsGuardOpeningTarget(targetHealth))
            {
                outgoingMultiplier *= EffectiveGuardOpeningDamageMultiplier;
            }

            if (outgoingMultiplier > 1f)
            {
                damageInfo.damage *= outgoingMultiplier;
            }

            return damageInfo;
        }

        public void AddRecoveryDuration(float durationBonus)
        {
            postHitInvulnerabilityDuration = Mathf.Max(
                0f,
                postHitInvulnerabilityDuration + Mathf.Max(0f, durationBonus));
        }

        public void ImproveDodgeDamageBoost(float duration, float multiplier)
        {
            dodgeDamageBoostDuration = Mathf.Max(dodgeDamageBoostDuration, Mathf.Max(0f, duration));
            dodgeDamageMultiplier = Mathf.Max(1f, dodgeDamageMultiplier * Mathf.Max(1f, multiplier));
        }

        public void MultiplyGuardOpeningDamage(float multiplier)
        {
            guardOpeningDamageMultiplier = Mathf.Max(
                1f,
                guardOpeningDamageMultiplier * Mathf.Max(1f, multiplier));
        }

        public string GetStatusLabel()
        {
            ResolveReferences();
            string dashLabel = movement != null ? $"{movement.DashCooldown:0.00}s" : "--";
            string boostLabel = IsDodgeDamageBoostActive
                ? $"x{EffectiveDodgeDamageMultiplier:0.##} ({dodgeDamageBoostTimer:0.0}s)"
                : $"x{EffectiveDodgeDamageMultiplier:0.##}";
            return $"Counterplay: Dash CD {dashLabel} | Recovery {postHitInvulnerabilityDuration:0.00}s | Dodge {boostLabel} | Opening x{EffectiveGuardOpeningDamageMultiplier:0.##}";
        }

        private void ResolveReferences()
        {
            health = health != null ? health : GetComponent<HealthComponent>();
            movement = movement != null ? movement : GetComponent<PlayerMovement2D>();
        }

        private void Subscribe()
        {
            SubscribeToHealth();
            SubscribeToMovement();
        }

        private void Unsubscribe()
        {
            if (subscribedHealth != null)
            {
                subscribedHealth.Damaged -= HandleDamaged;
                subscribedHealth = null;
            }

            if (subscribedMovement != null)
            {
                subscribedMovement.DashStarted -= HandleDashStarted;
                subscribedMovement = null;
            }

        }

        private void SubscribeToHealth()
        {
            if (health == subscribedHealth)
            {
                return;
            }

            if (subscribedHealth != null)
            {
                subscribedHealth.Damaged -= HandleDamaged;
            }

            subscribedHealth = health;
            if (subscribedHealth != null)
            {
                subscribedHealth.Damaged += HandleDamaged;
            }
        }

        private void SubscribeToMovement()
        {
            if (movement == subscribedMovement)
            {
                return;
            }

            if (subscribedMovement != null)
            {
                subscribedMovement.DashStarted -= HandleDashStarted;
            }

            subscribedMovement = movement;
            if (subscribedMovement != null)
            {
                subscribedMovement.DashStarted += HandleDashStarted;
            }
        }

        private void HandleDashStarted()
        {
            IssueFeedback(PlayerCounterplayFeedbackKind.DashStarted, "Dash: invulnerable.");
        }

        private void HandleDamaged(HealthComponent damagedHealth, DamageInfo damageInfo)
        {
            if (damagedHealth != health || damageInfo.sourceType != DamageSourceType.Enemy)
            {
                return;
            }

            BeginRecovery();
        }

        private void BeginRecovery()
        {
            recoveryTimer = Mathf.Max(recoveryTimer, postHitInvulnerabilityDuration);
            IssueFeedback(PlayerCounterplayFeedbackKind.HitTaken, "Hit taken: recovery window active.");
        }

        private void HandleTargetHit(PlayerCombat2D playerCombat, HealthComponent targetHealth, DamageInfo damageInfo)
        {
            if (targetHealth == null || damageInfo.sourceType != DamageSourceType.Player)
            {
                return;
            }

            if (IsGuardOpeningTarget(targetHealth))
            {
                IssueFeedback(PlayerCounterplayFeedbackKind.GuardOpening, "Counter hit: Guard opening punished.");
            }
        }

        private void ActivateDodgeDamageBoost()
        {
            float effectiveDuration = EffectiveDodgeDamageBoostDuration;
            if (effectiveDuration <= 0f || EffectiveDodgeDamageMultiplier <= 1f)
            {
                return;
            }

            dodgeDamageBoostTimer = Mathf.Max(dodgeDamageBoostTimer, effectiveDuration);
        }

        private static float GetCounterplayRouteDodgeBoostDurationBonus()
        {
            RunManager runManager = RunManager.FindActiveRunManager();
            return runManager != null ? runManager.CounterplayRouteDodgeBoostDurationBonus : 0f;
        }

        private static float GetCounterplayRouteDamageMultiplier()
        {
            RunManager runManager = RunManager.FindActiveRunManager();
            return runManager != null ? runManager.CounterplayRouteDamageMultiplier : 1f;
        }

        private static bool IsGuardOpeningTarget(HealthComponent targetHealth)
        {
            EnemyAttackPattern2D pattern = targetHealth.GetComponent<EnemyAttackPattern2D>();
            if (pattern == null)
            {
                pattern = targetHealth.GetComponentInParent<EnemyAttackPattern2D>();
            }

            return pattern != null && pattern.IsGuardVulnerable;
        }

        private void IssueFeedback(PlayerCounterplayFeedbackKind kind, string message)
        {
            LastFeedbackKind = kind;
            LastFeedbackMessage = message ?? string.Empty;
            feedbackTimer = feedbackDuration;
            UpdateVisual();
            FeedbackIssued?.Invoke(this, LastFeedbackKind, LastFeedbackMessage);
        }

        private void EnsureVisual()
        {
            if (!showCounterplayVisual)
            {
                return;
            }

            if (visualRenderer == null)
            {
                Transform existing = transform.Find(VisualObjectName);
                if (existing != null)
                {
                    visualRenderer = existing.GetComponent<SpriteRenderer>();
                }
            }

            if (visualRenderer == null)
            {
                GameObject visualObject = new GameObject(VisualObjectName);
                visualObject.transform.SetParent(transform, false);
                visualRenderer = visualObject.AddComponent<SpriteRenderer>();
            }

            visualRenderer.sprite = GetVisualSprite();
            visualRenderer.sortingOrder = 84;
        }

        private void UpdateVisual()
        {
            if (!showCounterplayVisual || visualRenderer == null)
            {
                return;
            }

            bool active = (movement != null && movement.IsInvincible)
                || IsRecovering
                || LastFeedbackKind == PlayerCounterplayFeedbackKind.GuardOpening
                || IsDodgeDamageBoostActive;
            visualRenderer.gameObject.SetActive(active);
            visualRenderer.transform.localPosition = Vector3.zero;
            visualRenderer.transform.localRotation = Quaternion.identity;
            visualRenderer.transform.localScale = new Vector3(1.15f, 1.85f, 1f);

            if (LastFeedbackKind == PlayerCounterplayFeedbackKind.GuardOpening)
            {
                visualRenderer.color = counterColor;
            }
            else if (movement != null && movement.IsInvincible)
            {
                visualRenderer.color = dodgeColor;
            }
            else
            {
                visualRenderer.color = recoveryColor;
            }
        }

        private static Sprite GetVisualSprite()
        {
            if (sharedVisualSprite != null)
            {
                return sharedVisualSprite;
            }

            Texture2D texture = Texture2D.whiteTexture;
            sharedVisualSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                texture.width);
            sharedVisualSprite.hideFlags = HideFlags.HideAndDontSave;
            return sharedVisualSprite;
        }
    }
}
