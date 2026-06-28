using System;
using AICompanionRoguelike.Combat;
using UnityEngine;

namespace AICompanionRoguelike.Enemy
{
    public sealed class EnemyProjectile2D : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float speed = 7f;
        [SerializeField, Min(0.1f)] private float lifetime = 2.5f;
        [SerializeField, Min(0.01f)] private float hitRadius = 0.28f;
        [SerializeField] private Color projectileColor = new Color(0.35f, 0.78f, 1f, 1f);

        private static Sprite sharedProjectileSprite;

        private HealthComponent targetHealth;
        private Vector2 direction = Vector2.right;
        private float damage;
        private GameObject sourceObject;
        private float remainingLifetime;
        private SpriteRenderer visualRenderer;
        private Action<bool> resolvedCallback;
        private bool isResolved;
        private bool countedAsActive;

        public static int ActiveProjectileCount { get; private set; }

        public HealthComponent TargetHealth => targetHealth;
        public Vector2 Direction => direction;
        public float Speed => speed;
        public float RemainingLifetime => remainingLifetime;
        public bool HasVisual => visualRenderer != null && visualRenderer.sprite != null;
        public bool IsResolved => isResolved;

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (countedAsActive)
            {
                ActiveProjectileCount = Mathf.Max(0, ActiveProjectileCount - 1);
                countedAsActive = false;
            }
        }

        public static EnemyProjectile2D Create(
            Vector2 origin,
            Vector2 launchDirection,
            HealthComponent target,
            float projectileDamage,
            GameObject source,
            float projectileSpeed,
            Color color,
            Action<bool> onResolved)
        {
            GameObject projectileObject = new GameObject($"{source?.name ?? "Enemy"}_Projectile");
            projectileObject.transform.position = new Vector3(origin.x, origin.y, 0f);
            EnemyProjectile2D projectile = projectileObject.AddComponent<EnemyProjectile2D>();
            projectile.Configure(
                launchDirection,
                target,
                projectileDamage,
                source,
                projectileSpeed,
                color,
                onResolved);
            return projectile;
        }

        public void Configure(
            Vector2 launchDirection,
            HealthComponent target,
            float projectileDamage,
            GameObject source,
            float projectileSpeed,
            Color color,
            Action<bool> onResolved)
        {
            direction = launchDirection.sqrMagnitude > 0.0001f
                ? launchDirection.normalized
                : Vector2.right;
            targetHealth = target;
            damage = Mathf.Max(0f, projectileDamage);
            sourceObject = source;
            speed = Mathf.Max(0.1f, projectileSpeed);
            projectileColor = color;
            remainingLifetime = Mathf.Max(0.1f, lifetime);
            resolvedCallback = onResolved;
            isResolved = false;

            if (!countedAsActive)
            {
                ActiveProjectileCount++;
                countedAsActive = true;
            }

            EnsureVisual();
            UpdateVisualTransform();
        }

        public void Tick(float deltaTime)
        {
            if (isResolved)
            {
                return;
            }

            float safeDeltaTime = Mathf.Max(0f, deltaTime);
            Vector2 previousPosition = transform.position;
            Vector2 nextPosition = previousPosition + direction * speed * safeDeltaTime;
            transform.position = new Vector3(nextPosition.x, nextPosition.y, transform.position.z);

            if (targetHealth != null
                && !targetHealth.IsDead
                && SegmentIntersectsTarget(
                    previousPosition,
                    nextPosition,
                    targetHealth.transform.position,
                    hitRadius))
            {
                float healthBeforeHit = targetHealth.CurrentHealth;
                targetHealth.TakeDamage(new DamageInfo(damage, DamageSourceType.Enemy, sourceObject));
                bool damageLanded = targetHealth.CurrentHealth < healthBeforeHit;
                if (!damageLanded && targetHealth.TryGetComponent(out PlayerCounterplayFeedback counterplayFeedback))
                {
                    counterplayFeedback.ReportProjectileDodge();
                }

                Resolve(damageLanded);
                return;
            }

            remainingLifetime = Mathf.Max(0f, remainingLifetime - safeDeltaTime);
            if (remainingLifetime <= 0f || targetHealth == null || targetHealth.IsDead)
            {
                Resolve(false);
            }
        }

        public static bool SegmentIntersectsTarget(
            Vector2 segmentStart,
            Vector2 segmentEnd,
            Vector2 targetPosition,
            float radius)
        {
            Vector2 segment = segmentEnd - segmentStart;
            float segmentLengthSquared = segment.sqrMagnitude;
            if (segmentLengthSquared <= 0.000001f)
            {
                return Vector2.Distance(segmentStart, targetPosition) <= Mathf.Max(0f, radius);
            }

            float projection = Vector2.Dot(targetPosition - segmentStart, segment) / segmentLengthSquared;
            Vector2 closestPoint = segmentStart + segment * Mathf.Clamp01(projection);
            return Vector2.Distance(closestPoint, targetPosition) <= Mathf.Max(0f, radius);
        }

        private void Resolve(bool hit)
        {
            if (isResolved)
            {
                return;
            }

            isResolved = true;
            Action<bool> callback = resolvedCallback;
            resolvedCallback = null;
            callback?.Invoke(hit);

            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }

        private void EnsureVisual()
        {
            visualRenderer = visualRenderer != null
                ? visualRenderer
                : GetComponent<SpriteRenderer>();
            if (visualRenderer == null)
            {
                visualRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            visualRenderer.sprite = GetProjectileSprite();
            visualRenderer.color = projectileColor;
            visualRenderer.sortingOrder = 52;
            transform.localScale = new Vector3(0.36f, 0.14f, 1f);
        }

        private void UpdateVisualTransform()
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private static Sprite GetProjectileSprite()
        {
            if (sharedProjectileSprite != null)
            {
                return sharedProjectileSprite;
            }

            Texture2D texture = Texture2D.whiteTexture;
            sharedProjectileSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                texture.width);
            sharedProjectileSprite.hideFlags = HideFlags.HideAndDontSave;
            return sharedProjectileSprite;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            ActiveProjectileCount = 0;
            sharedProjectileSprite = null;
        }
    }
}
