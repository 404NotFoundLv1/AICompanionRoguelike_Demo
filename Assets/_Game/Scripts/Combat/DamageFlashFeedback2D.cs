using UnityEngine;

namespace AICompanionRoguelike.Combat
{
    [ExecuteAlways]
    [RequireComponent(typeof(HealthComponent))]
    public sealed class DamageFlashFeedback2D : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField, Min(0.01f)] private float flashDuration = 0.12f;
        [SerializeField] private Color damageFlashColor = new Color(1f, 0.18f, 0.12f, 1f);
        [SerializeField] private Color healFlashColor = new Color(0.36f, 1f, 0.5f, 1f);

        private HealthComponent health;
        private Color originalColor = Color.white;
        private Color currentFlashColor = Color.white;
        private float flashTimer;
        private bool hasOriginalColor;

        public bool IsFlashing => flashTimer > 0f;
        public Color OriginalColor => originalColor;
        public Color CurrentFlashColor => currentFlashColor;
        public float RemainingFlashTime => flashTimer;

        private void Reset()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Awake()
        {
            ResolveReferences();
            CaptureOriginalColor();
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private void OnDisable()
        {
            Unsubscribe();
            RestoreOriginalColor();
        }

        public void Tick(float deltaTime)
        {
            if (flashTimer <= 0f)
            {
                return;
            }

            flashTimer = Mathf.Max(0f, flashTimer - Mathf.Max(0f, deltaTime));
            if (flashTimer <= 0f)
            {
                RestoreOriginalColor();
            }
        }

        private void ResolveReferences()
        {
            health = health != null ? health : GetComponent<HealthComponent>();
            spriteRenderer = spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        private void Subscribe()
        {
            if (health == null)
            {
                return;
            }

            health.Damaged -= HandleDamaged;
            health.Healed -= HandleHealed;
            health.Damaged += HandleDamaged;
            health.Healed += HandleHealed;
        }

        private void Unsubscribe()
        {
            if (health == null)
            {
                return;
            }

            health.Damaged -= HandleDamaged;
            health.Healed -= HandleHealed;
        }

        private void HandleDamaged(HealthComponent damagedHealth, DamageInfo damageInfo)
        {
            StartFlash(damageFlashColor);
        }

        private void HandleHealed(HealthComponent healedHealth, float healedAmount)
        {
            StartFlash(healFlashColor);
        }

        private void StartFlash(Color flashColor)
        {
            ResolveReferences();
            CaptureOriginalColor();

            if (spriteRenderer == null)
            {
                return;
            }

            currentFlashColor = flashColor;
            flashTimer = flashDuration;
            spriteRenderer.color = currentFlashColor;
        }

        private void CaptureOriginalColor()
        {
            if (hasOriginalColor || spriteRenderer == null)
            {
                return;
            }

            originalColor = spriteRenderer.color;
            hasOriginalColor = true;
        }

        private void RestoreOriginalColor()
        {
            if (spriteRenderer != null && hasOriginalColor)
            {
                spriteRenderer.color = originalColor;
            }

            flashTimer = 0f;
        }
    }
}
