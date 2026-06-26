using UnityEngine;

namespace AICompanionRoguelike.Roguelike
{
    public sealed class RoomModifierVisualMarker2D : MonoBehaviour
    {
        [SerializeField] private RoomModifierType modifierType = RoomModifierType.None;
        [SerializeField] private string label;
        [SerializeField] private string readableVisualHint;
        [SerializeField] private Color visualColor = Color.white;
        [SerializeField] private Vector3 localOffset = new Vector3(0f, 1.08f, 0f);
        [SerializeField] private Vector3 markerScale = new Vector3(0.2f, 0.2f, 1f);

        private const string MarkerObjectName = "RoomModifierMarker";

        private static Sprite markerSprite;
        private SpriteRenderer markerRenderer;

        public RoomModifierType ModifierType => modifierType;
        public string Label => label;
        public string ReadableVisualHint => readableVisualHint;
        public Color VisualColor => visualColor;
        public bool HasMarkerVisual => markerRenderer != null;

        private void Awake()
        {
            if (modifierType != RoomModifierType.None)
            {
                EnsureMarkerVisual();
            }
        }

        public void Configure(
            RoomModifierType modifier,
            string markerLabel,
            Color markerColor,
            string visualHint)
        {
            modifierType = modifier;
            label = markerLabel ?? string.Empty;
            visualColor = markerColor;
            readableVisualHint = visualHint ?? string.Empty;

            if (modifierType == RoomModifierType.None)
            {
                RemoveMarkerVisual();
                return;
            }

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
            markerRenderer.color = visualColor;
            markerRenderer.sortingOrder = 50;
        }

        private void RemoveMarkerVisual()
        {
            markerRenderer = null;
            Transform markerTransform = transform.Find(MarkerObjectName);
            if (markerTransform != null)
            {
                Destroy(markerTransform.gameObject);
            }
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
