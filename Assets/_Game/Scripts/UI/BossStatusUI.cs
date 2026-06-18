using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Enemy;
using AICompanionRoguelike.Roguelike;
using UnityEngine;

namespace AICompanionRoguelike.UI
{
    public sealed class BossStatusUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RoomManager roomManager;

        [Header("Layout")]
        [SerializeField] private Rect barRect = new Rect(0f, 18f, 560f, 42f);
        [SerializeField] private Rect messageRect = new Rect(0f, 66f, 500f, 44f);
        [SerializeField] private float messageDuration = 2.8f;
        [SerializeField] private bool showBossBar = true;

        [Header("Colors")]
        [SerializeField] private Color backgroundColor = new Color(0.05f, 0.05f, 0.05f, 0.88f);
        [SerializeField] private Color phaseOneColor = new Color(0.9f, 0.18f, 0.28f, 1f);
        [SerializeField] private Color phaseTwoColor = new Color(1f, 0.65f, 0.08f, 1f);

        private HealthComponent bossHealth;
        private BossPhaseController bossPhaseController;
        private string bossName;
        private string messageText;
        private float messageTimer;

        private void OnEnable()
        {
            ResolveReferences();
            SubscribeToRoomManager();
        }

        private void Start()
        {
            ResolveReferences();
            SubscribeToRoomManager();
            RefreshBossTarget();
        }

        private void Update()
        {
            if (messageTimer > 0f)
            {
                messageTimer = Mathf.Max(0f, messageTimer - Time.deltaTime);
            }

            if (roomManager == null)
            {
                ResolveReferences();
                SubscribeToRoomManager();
            }

            if (bossHealth != null && bossHealth.IsDead)
            {
                ClearBossTarget();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromBossPhase();

            if (roomManager != null)
            {
                roomManager.RoomStarted -= HandleRoomStarted;
                roomManager.RoomCleared -= HandleRoomCleared;
            }
        }

        private void ResolveReferences()
        {
            roomManager = roomManager != null ? roomManager : FindAnyObjectByType<RoomManager>();
        }

        private void SubscribeToRoomManager()
        {
            if (roomManager == null)
            {
                return;
            }

            roomManager.RoomStarted -= HandleRoomStarted;
            roomManager.RoomCleared -= HandleRoomCleared;
            roomManager.RoomStarted += HandleRoomStarted;
            roomManager.RoomCleared += HandleRoomCleared;
        }

        private void HandleRoomStarted(RoomManager manager, RoomType roomType, int roomNumber)
        {
            RefreshBossTarget();

            if (roomType == RoomType.BossRoom && bossHealth != null)
            {
                ShowMessage("BOSS APPEARED");
            }
        }

        private void HandleRoomCleared(RoomManager manager, RoomType roomType, int roomNumber)
        {
            if (roomType == RoomType.BossRoom)
            {
                ShowMessage("BOSS DEFEATED");
            }

            ClearBossTarget();
        }

        private void RefreshBossTarget()
        {
            ClearBossTarget();

            if (roomManager == null || roomManager.CurrentRoomType != RoomType.BossRoom)
            {
                return;
            }

            for (int i = 0; i < roomManager.ActiveEnemies.Count; i++)
            {
                EnemyController2D enemy = roomManager.ActiveEnemies[i];
                if (enemy == null)
                {
                    continue;
                }

                bossHealth = enemy.GetComponent<HealthComponent>();
                bossPhaseController = enemy.GetComponent<BossPhaseController>();
                bossName = enemy.name;
                SubscribeToBossPhase();
                return;
            }
        }

        private void ClearBossTarget()
        {
            UnsubscribeFromBossPhase();
            bossHealth = null;
            bossPhaseController = null;
            bossName = null;
        }

        private void SubscribeToBossPhase()
        {
            if (bossPhaseController == null)
            {
                return;
            }

            bossPhaseController.PhaseTwoStarted -= HandlePhaseTwoStarted;
            bossPhaseController.PhaseTwoStarted += HandlePhaseTwoStarted;
        }

        private void UnsubscribeFromBossPhase()
        {
            if (bossPhaseController == null)
            {
                return;
            }

            bossPhaseController.PhaseTwoStarted -= HandlePhaseTwoStarted;
        }

        private void HandlePhaseTwoStarted(BossPhaseController phaseController)
        {
            ShowMessage("BOSS PHASE TWO");
        }

        private void ShowMessage(string text)
        {
            messageText = text;
            messageTimer = messageDuration;
        }

        private void OnGUI()
        {
            if (showBossBar && bossHealth != null && !bossHealth.IsDead)
            {
                DrawBossBar();
            }

            if (messageTimer > 0f && !string.IsNullOrEmpty(messageText))
            {
                DrawMessage();
            }
        }

        private void DrawBossBar()
        {
            Rect rect = GetCenteredRect(barRect);
            float ratio = Mathf.Clamp01(bossHealth.CurrentHealth / bossHealth.MaxHealth);
            Color fillColor = bossPhaseController != null && bossPhaseController.IsInPhaseTwo
                ? phaseTwoColor
                : phaseOneColor;

            GUI.Box(rect, GUIContent.none);
            DrawSolidRect(new Rect(rect.x + 8f, rect.y + 22f, rect.width - 16f, 12f), backgroundColor);
            DrawSolidRect(new Rect(rect.x + 8f, rect.y + 22f, (rect.width - 16f) * ratio, 12f), fillColor);

            GUI.Label(
                new Rect(rect.x + 10f, rect.y + 4f, rect.width - 20f, 18f),
                $"{bossName}  HP {bossHealth.CurrentHealth:0}/{bossHealth.MaxHealth:0}");
        }

        private void DrawMessage()
        {
            Rect rect = GetCenteredRect(messageRect);
            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Label(messageText);
            GUILayout.EndArea();
        }

        private static Rect GetCenteredRect(Rect sourceRect)
        {
            float width = Mathf.Min(sourceRect.width, Mathf.Max(180f, Screen.width - 16f));
            float x = sourceRect.x <= 0f ? (Screen.width - width) * 0.5f : sourceRect.x;
            return new Rect(Mathf.Max(8f, x), sourceRect.y, width, sourceRect.height);
        }

        private static void DrawSolidRect(Rect rect, Color color)
        {
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previousColor;
        }
    }
}
