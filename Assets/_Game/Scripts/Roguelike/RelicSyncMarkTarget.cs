using UnityEngine;

namespace AICompanionRoguelike.Roguelike
{
    public sealed class RelicSyncMarkTarget : MonoBehaviour
    {
        private const string VisualName = "SyncMarkVisual";
        private static Sprite markerSprite;

        [SerializeField] private bool isMarked;
        [SerializeField] private Vector3 markerLocalPosition = new Vector3(0f, 1.15f, 0f);
        [SerializeField] private Vector3 markerLocalScale = new Vector3(0.22f, 0.08f, 1f);
        [SerializeField] private Color markerColor = new Color(0.2f, 1f, 1f, 0.95f);

        private GameObject markerVisual;

        public bool IsMarked => isMarked;

        public void MarkByCompanion()
        {
            isMarked = true;
            SetMarkerVisualVisible(true);
        }

        public void ClearMark()
        {
            isMarked = false;
            SetMarkerVisualVisible(false);
        }

        private void OnDisable()
        {
            SetMarkerVisualVisible(false);
        }

        private void SetMarkerVisualVisible(bool visible)
        {
            if (visible)
            {
                EnsureMarkerVisual();
            }

            if (markerVisual != null)
            {
                markerVisual.SetActive(visible);
            }
        }

        private void EnsureMarkerVisual()
        {
            if (markerVisual != null)
            {
                return;
            }

            Transform existing = transform.Find(VisualName);
            markerVisual = existing != null
                ? existing.gameObject
                : new GameObject(VisualName);
            markerVisual.transform.SetParent(transform, false);
            markerVisual.transform.localPosition = markerLocalPosition;
            markerVisual.transform.localScale = markerLocalScale;

            SpriteRenderer spriteRenderer = markerVisual.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = markerVisual.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = GetMarkerSprite();
            spriteRenderer.color = markerColor;
            spriteRenderer.sortingOrder = 50;
        }

        private static Sprite GetMarkerSprite()
        {
            if (markerSprite == null)
            {
                Texture2D texture = Texture2D.whiteTexture;
                markerSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    texture.width);
            }

            return markerSprite;
        }
    }
}
