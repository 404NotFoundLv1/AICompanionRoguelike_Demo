using UnityEngine;

namespace AICompanionRoguelike.Enemy
{
    public sealed class EnemyArchetype2D : MonoBehaviour
    {
        [SerializeField] private EnemyArchetypeType archetypeType = EnemyArchetypeType.Melee;
        [SerializeField] private string displayName = "Melee";
        [SerializeField] private string readableRoleHint = "close pressure";
        [SerializeField] private Color roleColor = Color.white;
        [SerializeField] private Vector3 localOffset = new Vector3(0f, 1.38f, 0f);
        [SerializeField] private Vector3 markerScale = new Vector3(0.16f, 0.16f, 1f);

        private const string MarkerObjectName = "EnemyRoleMarker";

        private static Sprite markerSprite;
        private SpriteRenderer markerRenderer;

        public EnemyArchetypeType ArchetypeType => archetypeType;
        public string DisplayName => displayName;
        public string ReadableRoleHint => readableRoleHint;
        public Color RoleColor => roleColor;
        public bool HasMarkerVisual => markerRenderer != null;

        private void Awake()
        {
            EnsureMarkerVisual();
        }

        public void Configure(
            EnemyArchetypeType newArchetypeType,
            string newDisplayName,
            string newReadableRoleHint,
            Color newRoleColor)
        {
            archetypeType = newArchetypeType;
            displayName = string.IsNullOrWhiteSpace(newDisplayName)
                ? EnemyArchetypeRules.GetDisplayName(newArchetypeType)
                : newDisplayName;
            readableRoleHint = newReadableRoleHint ?? string.Empty;
            roleColor = newRoleColor;
            EnsureMarkerVisual();
        }

        private void EnsureMarkerVisual()
        {
            Transform markerTransform = transform.Find(MarkerObjectName);
            if (markerTransform == null)
            {
                GameObject markerObject = new GameObject(MarkerObjectName);
                markerTransform = markerObject.transform;
                markerTransform.SetParent(transform, false);
            }

            markerTransform.localPosition = localOffset;
            markerTransform.localRotation = Quaternion.identity;
            markerTransform.localScale = markerScale;

            markerRenderer = markerTransform.GetComponent<SpriteRenderer>();
            if (markerRenderer == null)
            {
                markerRenderer = markerTransform.gameObject.AddComponent<SpriteRenderer>();
            }

            markerRenderer.sprite = GetMarkerSprite();
            markerRenderer.color = roleColor;
            markerRenderer.sortingOrder = 54;
        }

        private static Sprite GetMarkerSprite()
        {
            if (markerSprite != null)
            {
                return markerSprite;
            }

            Texture2D texture = new Texture2D(8, 8, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point
            };

            Color[] pixels = new Color[64];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            texture.SetPixels(pixels);
            texture.Apply();

            markerSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                8f);
            markerSprite.hideFlags = HideFlags.HideAndDontSave;
            return markerSprite;
        }
    }
}
