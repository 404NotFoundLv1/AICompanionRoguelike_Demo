using UnityEngine;

namespace AICompanionRoguelike.Roguelike
{
    public sealed class RoomInteractionVisualCue2D : MonoBehaviour
    {
        private const string MarkerObjectName = "InteractionCueMarker";

        private static Sprite sharedMarkerSprite;

        [SerializeField] private string roleLabel = "Interact";
        [SerializeField] private Color availableColor = new Color(0.35f, 0.9f, 1f, 0.95f);
        [SerializeField] private Color highlightedColor = new Color(0.85f, 1f, 0.35f, 1f);
        [SerializeField] private Color unavailableColor = new Color(0.45f, 0.45f, 0.45f, 0.65f);
        [SerializeField] private Vector3 markerLocalPosition = new Vector3(0f, 0.72f, 0f);
        [SerializeField] private Vector3 markerLocalScale = new Vector3(0.42f, 0.1f, 1f);

        private SpriteRenderer markerRenderer;

        public string RoleLabel => roleLabel;
        public bool IsVisible { get; private set; }
        public bool IsAvailable { get; private set; }
        public bool IsHighlighted { get; private set; }
        public Color CurrentColor { get; private set; }
        public bool HasMarkerVisual => markerRenderer != null && markerRenderer.sprite != null;

        private void Awake()
        {
            CurrentColor = unavailableColor;
            EnsureMarkerVisual();
            ApplyState(false, false, false);
        }

        public void Configure(string newRoleLabel, Color newAvailableColor, Color newHighlightedColor, Color newUnavailableColor)
        {
            roleLabel = string.IsNullOrWhiteSpace(newRoleLabel) ? roleLabel : newRoleLabel;
            availableColor = newAvailableColor;
            highlightedColor = newHighlightedColor;
            unavailableColor = newUnavailableColor;
            CurrentColor = IsHighlighted ? highlightedColor : IsAvailable ? availableColor : unavailableColor;
            EnsureMarkerVisual();
            RefreshMarker();
        }

        public void ApplyState(bool visible, bool available, bool highlighted)
        {
            IsVisible = visible;
            IsAvailable = available;
            IsHighlighted = visible && available && highlighted;
            CurrentColor = IsHighlighted ? highlightedColor : available ? availableColor : unavailableColor;
            EnsureMarkerVisual();
            RefreshMarker();
        }

        private void EnsureMarkerVisual()
        {
            if (markerRenderer != null)
            {
                return;
            }

            Transform markerTransform = transform.Find(MarkerObjectName);
            GameObject markerObject = markerTransform != null
                ? markerTransform.gameObject
                : new GameObject(MarkerObjectName);
            markerObject.transform.SetParent(transform, false);
            markerObject.transform.localPosition = markerLocalPosition;
            markerObject.transform.localScale = markerLocalScale;

            markerRenderer = markerObject.GetComponent<SpriteRenderer>();
            if (markerRenderer == null)
            {
                markerRenderer = markerObject.AddComponent<SpriteRenderer>();
            }

            markerRenderer.sprite = GetMarkerSprite();
            markerRenderer.sortingOrder = 80;
        }

        private void RefreshMarker()
        {
            if (markerRenderer == null)
            {
                return;
            }

            markerRenderer.color = CurrentColor;
            markerRenderer.gameObject.SetActive(IsVisible);
        }

        private static Sprite GetMarkerSprite()
        {
            if (sharedMarkerSprite != null)
            {
                return sharedMarkerSprite;
            }

            Texture2D texture = Texture2D.whiteTexture;
            sharedMarkerSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                texture.width);
            sharedMarkerSprite.hideFlags = HideFlags.HideAndDontSave;
            return sharedMarkerSprite;
        }
    }
}
