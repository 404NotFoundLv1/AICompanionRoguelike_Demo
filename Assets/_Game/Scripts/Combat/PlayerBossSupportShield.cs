using UnityEngine;

namespace AICompanionRoguelike.Combat
{
    public sealed class PlayerBossSupportShield : MonoBehaviour, IDamageModifier
    {
        [SerializeField, Min(0f)] private float remainingDuration;
        [SerializeField, Range(0f, 1f)] private float incomingDamageMultiplier = 0.5f;

        [Header("Visual Feedback")]
        [SerializeField] private bool showShieldVisual = true;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer visualRenderer;
        [SerializeField] private Color visualColor = new Color(0.15f, 0.85f, 1f, 0.48f);
        [SerializeField] private Vector2 visualSize = new Vector2(1.85f, 2.05f);
        [SerializeField] private int visualSortingOrder = 85;
        [SerializeField, Range(0f, 0.35f)] private float pulseScaleAmount = 0.08f;

        [Header("Debug")]
        [SerializeField] private bool logShieldMessages = true;

        private const string VisualObjectName = "BossSupportShieldVisual";
        private static Sprite sharedShieldSprite;

        public bool IsActive => remainingDuration > 0f;
        public float RemainingDuration => remainingDuration;
        public float IncomingDamageMultiplier => IsActive ? incomingDamageMultiplier : 1f;

        private void Awake()
        {
            EnsureShieldVisual();
            SetShieldVisualActive(false);
        }

        private void Update()
        {
            Tick(Time.deltaTime);
            UpdateShieldVisual();
        }

        private void OnValidate()
        {
            remainingDuration = Mathf.Max(0f, remainingDuration);
            incomingDamageMultiplier = Mathf.Clamp01(incomingDamageMultiplier);
            visualSize = new Vector2(Mathf.Max(0.1f, visualSize.x), Mathf.Max(0.1f, visualSize.y));
            visualSortingOrder = Mathf.Max(0, visualSortingOrder);

            if (!Application.isPlaying)
            {
                UpdateShieldVisual();
            }
        }

        public void Activate(float duration, float damageMultiplier)
        {
            remainingDuration = Mathf.Max(0f, duration);
            incomingDamageMultiplier = Mathf.Clamp01(damageMultiplier);
            EnsureShieldVisual();
            UpdateShieldVisual();
            SetShieldVisualActive(IsActive);

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

            if (remainingDuration <= 0f)
            {
                SetShieldVisualActive(false);
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

        private void EnsureShieldVisual()
        {
            if (!showShieldVisual)
            {
                return;
            }

            if (visualRoot == null)
            {
                Transform existingVisual = transform.Find(VisualObjectName);
                if (existingVisual != null)
                {
                    visualRoot = existingVisual;
                }
            }

            if (visualRoot == null)
            {
                GameObject visualObject = new GameObject(VisualObjectName);
                visualRoot = visualObject.transform;
                visualRoot.SetParent(transform, false);
            }

            if (visualRenderer == null)
            {
                visualRenderer = visualRoot.GetComponent<SpriteRenderer>();
                if (visualRenderer == null)
                {
                    visualRenderer = visualRoot.gameObject.AddComponent<SpriteRenderer>();
                }
            }

            visualRenderer.sprite = GetShieldSprite();
            visualRenderer.sortingOrder = visualSortingOrder;
            visualRenderer.color = visualColor;
        }

        private void UpdateShieldVisual()
        {
            if (!showShieldVisual || visualRoot == null || visualRenderer == null)
            {
                return;
            }

            float pulse = IsActive
                ? 1f + Mathf.Sin(Time.time * 12f) * pulseScaleAmount
                : 1f;
            visualRoot.localPosition = Vector3.zero;
            visualRoot.localRotation = Quaternion.identity;
            visualRoot.localScale = new Vector3(visualSize.x * pulse, visualSize.y * pulse, 1f);
            visualRenderer.color = IsActive
                ? Color.Lerp(visualColor, new Color(1f, 1f, 1f, visualColor.a), 0.25f + Mathf.PingPong(Time.time * 2f, 0.35f))
                : visualColor;
            visualRenderer.sortingOrder = visualSortingOrder;
        }

        private void SetShieldVisualActive(bool active)
        {
            if (visualRoot != null)
            {
                visualRoot.gameObject.SetActive(showShieldVisual && active);
            }
        }

        private static Sprite GetShieldSprite()
        {
            if (sharedShieldSprite != null)
            {
                return sharedShieldSprite;
            }

            const int textureSize = 64;
            Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
            {
                name = "BossSupportShieldSprite",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Vector2 center = new Vector2((textureSize - 1) * 0.5f, (textureSize - 1) * 0.5f);
            float radius = textureSize * 0.46f;
            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    float normalizedDistance = Vector2.Distance(new Vector2(x, y), center) / radius;
                    float alpha = 0f;
                    if (normalizedDistance <= 0.72f)
                    {
                        alpha = 0.2f;
                    }
                    else if (normalizedDistance <= 1f)
                    {
                        alpha = Mathf.InverseLerp(1f, 0.72f, normalizedDistance);
                    }

                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            sharedShieldSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, textureSize, textureSize),
                new Vector2(0.5f, 0.5f),
                textureSize);
            return sharedShieldSprite;
        }
    }
}
