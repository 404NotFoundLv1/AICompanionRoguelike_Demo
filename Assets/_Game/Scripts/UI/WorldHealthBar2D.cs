using AICompanionRoguelike.Combat;
using UnityEngine;

namespace AICompanionRoguelike.UI
{
    public sealed class WorldHealthBar2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HealthComponent health;
        [SerializeField] private Transform barRoot;
        [SerializeField] private Transform backgroundTransform;
        [SerializeField] private Transform fillTransform;
        [SerializeField] private SpriteRenderer backgroundRenderer;
        [SerializeField] private SpriteRenderer fillRenderer;

        [Header("Layout")]
        [SerializeField] private Vector2 localOffset = new Vector2(0f, 1.1f);
        [SerializeField] private Vector2 size = new Vector2(1.2f, 0.12f);
        [SerializeField] private int sortingOrder = 50;
        [SerializeField] private bool hideWhenFull;

        [Header("Colors")]
        [SerializeField] private Color backgroundColor = new Color(0.05f, 0.05f, 0.05f, 0.85f);
        [SerializeField] private Color fillColor = new Color(0.2f, 0.9f, 0.35f, 1f);

        private void Reset()
        {
            health = GetComponent<HealthComponent>();
        }

        private void Awake()
        {
            health = health != null ? health : GetComponent<HealthComponent>();
            ApplyStaticVisuals();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Damaged += HandleDamaged;
                health.Healed += HandleHealed;
                health.Died += HandleDied;
            }
        }

        private void Start()
        {
            UpdateFill();
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Damaged -= HandleDamaged;
                health.Healed -= HandleHealed;
                health.Died -= HandleDied;
            }
        }

        private void OnValidate()
        {
            ApplyStaticVisuals();
            UpdateFill();
        }

        public void SetFillColor(Color color)
        {
            fillColor = color;
            ApplyStaticVisuals();
        }

        private void HandleDamaged(HealthComponent damagedHealth, DamageInfo damageInfo)
        {
            UpdateFill();
        }

        private void HandleHealed(HealthComponent healedHealth, float healedAmount)
        {
            UpdateFill();
        }

        private void HandleDied(HealthComponent deadHealth, DamageInfo damageInfo)
        {
            UpdateFill();
        }

        private void ApplyStaticVisuals()
        {
            if (barRoot != null)
            {
                barRoot.localPosition = localOffset;
            }

            if (backgroundTransform != null)
            {
                backgroundTransform.localPosition = Vector3.zero;
                backgroundTransform.localScale = new Vector3(size.x, size.y, 1f);
            }

            if (backgroundRenderer != null)
            {
                backgroundRenderer.color = backgroundColor;
                backgroundRenderer.sortingOrder = sortingOrder;
            }

            if (fillRenderer != null)
            {
                fillRenderer.color = fillColor;
                fillRenderer.sortingOrder = sortingOrder + 1;
            }
        }

        private void UpdateFill()
        {
            if (fillTransform == null)
            {
                return;
            }

            float ratio = 1f;
            if (health != null && health.MaxHealth > 0f)
            {
                ratio = Mathf.Clamp01(health.CurrentHealth / health.MaxHealth);
            }

            fillTransform.localScale = new Vector3(size.x * ratio, size.y, 1f);
            fillTransform.localPosition = new Vector3((-size.x * 0.5f) + (size.x * ratio * 0.5f), 0f, 0f);

            if (barRoot != null)
            {
                barRoot.gameObject.SetActive(!hideWhenFull || ratio < 1f);
            }
        }
    }
}
