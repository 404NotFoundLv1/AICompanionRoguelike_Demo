using System.Collections.Generic;
using UnityEngine;

namespace AICompanionRoguelike.QTE
{
    public sealed class QTEImpactEffect : MonoBehaviour
    {
        private const int SortingOrder = 120;

        private static Sprite pixelSprite;

        private readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>(4);
        private readonly List<Color> baseColors = new List<Color>(4);
        private float duration = 0.45f;
        private float elapsed;

        public static void Spawn(Vector3 requesterPosition, Vector3 targetPosition, Color color, float effectDuration)
        {
            GameObject root = new GameObject("QTEImpactEffect");
            root.transform.position = targetPosition + new Vector3(0f, 0.15f, -0.25f);

            QTEImpactEffect effect = root.AddComponent<QTEImpactEffect>();
            effect.Initialize(requesterPosition, targetPosition, color, effectDuration);
        }

        private void Initialize(Vector3 requesterPosition, Vector3 targetPosition, Color color, float effectDuration)
        {
            duration = Mathf.Max(0.05f, effectDuration);

            CreateLink(requesterPosition, targetPosition, color);
            CreateSprite("ImpactSlashA", new Vector3(0f, 0.08f, 0f), new Vector3(1.5f, 0.09f, 1f), 38f, color);
            CreateSprite("ImpactSlashB", new Vector3(0f, 0.08f, 0f), new Vector3(1.5f, 0.09f, 1f), -38f, Color.white);
            CreateSprite("ImpactCore", Vector3.zero, new Vector3(0.35f, 0.35f, 1f), 45f, new Color(1f, 0.95f, 0.35f, 0.95f));
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = 1f - t;
            float scale = Mathf.Lerp(0.75f, 1.35f, t);
            transform.localScale = new Vector3(scale, scale, 1f);

            for (int i = 0; i < renderers.Count; i++)
            {
                SpriteRenderer spriteRenderer = renderers[i];
                if (spriteRenderer == null)
                {
                    continue;
                }

                Color color = baseColors[i];
                color.a *= alpha;
                spriteRenderer.color = color;
            }

            if (elapsed >= duration)
            {
                Destroy(gameObject);
            }
        }

        private void CreateLink(Vector3 requesterPosition, Vector3 targetPosition, Color color)
        {
            Vector3 start = requesterPosition + Vector3.up * 0.45f;
            Vector3 end = targetPosition + Vector3.up * 0.18f;
            Vector3 delta = end - start;
            float length = delta.magnitude;
            if (length <= 0.01f)
            {
                return;
            }

            Vector3 midpoint = (start + end) * 0.5f;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            Color linkColor = color;
            linkColor.a = Mathf.Min(linkColor.a, 0.72f);
            CreateSprite("QTELink", midpoint - transform.position, new Vector3(length, 0.055f, 1f), angle, linkColor);
        }

        private void CreateSprite(string childName, Vector3 localPosition, Vector3 localScale, float angle, Color color)
        {
            GameObject child = new GameObject(childName);
            child.transform.SetParent(transform, false);
            child.transform.localPosition = localPosition;
            child.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            child.transform.localScale = localScale;

            SpriteRenderer spriteRenderer = child.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = GetPixelSprite();
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = SortingOrder;

            renderers.Add(spriteRenderer);
            baseColors.Add(color);
        }

        private static Sprite GetPixelSprite()
        {
            if (pixelSprite != null)
            {
                return pixelSprite;
            }

            Texture2D texture = new Texture2D(1, 1)
            {
                filterMode = FilterMode.Point
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            pixelSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            return pixelSprite;
        }
    }
}
