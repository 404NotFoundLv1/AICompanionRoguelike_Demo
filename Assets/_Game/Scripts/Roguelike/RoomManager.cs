using System;
using System.Collections.Generic;
using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Enemy;
using UnityEngine;

namespace AICompanionRoguelike.Roguelike
{
    public sealed class RoomManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private Transform playerTarget;
        [SerializeField] private Transform enemyContainer;

        [Header("Battle Rooms")]
        [SerializeField, Min(0)] private int battleEnemyCount = 1;
        [SerializeField, Min(0)] private int eliteEnemyCount = 2;
        [SerializeField] private Vector2[] spawnPositions =
        {
            new Vector2(3.2f, -1.15f),
            new Vector2(5.2f, -1.15f),
            new Vector2(1.6f, -1.15f)
        };

        [Header("Boss Room")]
        [SerializeField, Min(1)] private int bossEnemyCount = 1;
        [SerializeField] private Vector2 bossSpawnPosition = new Vector2(4.35f, -1.15f);
        [SerializeField, Min(1f)] private float bossHealthMultiplier = 3f;
        [SerializeField, Min(0f)] private float bossDamageMultiplier = 1.6f;
        [SerializeField, Min(0.1f)] private float bossScaleMultiplier = 1.35f;
        [SerializeField] private Color bossTint = new Color(0.95f, 0.22f, 0.35f, 1f);

        [Header("Boss Phase Two")]
        [SerializeField, Range(0.05f, 0.95f)] private float bossPhaseTwoHealthRatio = 0.5f;
        [SerializeField, Min(1f)] private float bossPhaseTwoDamageMultiplier = 1.4f;
        [SerializeField, Min(1f)] private float bossPhaseTwoScaleMultiplier = 1.1f;
        [SerializeField] private Color bossPhaseTwoTint = new Color(1f, 0.08f, 0.08f, 1f);

        [Header("Debug")]
        [SerializeField] private bool logRoomMessages = true;

        private readonly List<EnemyController2D> activeEnemies = new List<EnemyController2D>(8);

        public event Action<RoomManager, RoomType, int> RoomStarted;
        public event Action<RoomManager, RoomType, int> RoomCleared;

        public RoomType CurrentRoomType { get; private set; }
        public int CurrentRoomNumber { get; private set; }
        public bool IsRoomActive { get; private set; }
        public bool IsRoomCleared { get; private set; }
        public int RemainingEnemyCount => activeEnemies.Count;
        public IReadOnlyList<EnemyController2D> ActiveEnemies => activeEnemies;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            EnemyController2D.OnEnemyDeath += HandleEnemyDeath;
        }

        private void OnDisable()
        {
            EnemyController2D.OnEnemyDeath -= HandleEnemyDeath;
        }

        public void EnterRoom(RoomType roomType, int roomNumber)
        {
            ResolveReferences();
            ClearSpawnedEnemies();

            CurrentRoomType = roomType;
            CurrentRoomNumber = Mathf.Max(1, roomNumber);
            IsRoomActive = true;
            IsRoomCleared = false;

            int enemyCount = GetEnemyCount(roomType);
            for (int i = 0; i < enemyCount; i++)
            {
                SpawnEnemy(i, enemyCount);
            }

            if (logRoomMessages)
            {
                Debug.Log($"Entered {CurrentRoomType} #{CurrentRoomNumber}. Enemies: {activeEnemies.Count}", this);
            }

            RoomStarted?.Invoke(this, CurrentRoomType, CurrentRoomNumber);

            if (activeEnemies.Count == 0 && CurrentRoomType != RoomType.BranchEventRoom)
            {
                ClearRoom();
            }
        }

        public void ClearRoom()
        {
            if (IsRoomCleared)
            {
                return;
            }

            IsRoomActive = false;
            IsRoomCleared = true;

            if (logRoomMessages)
            {
                Debug.Log($"Room cleared: {CurrentRoomType} #{CurrentRoomNumber}", this);
            }

            RoomCleared?.Invoke(this, CurrentRoomType, CurrentRoomNumber);
        }

        public void ForceClearCurrentRoom()
        {
            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                EnemyController2D enemy = activeEnemies[i];
                if (enemy != null)
                {
                    enemy.gameObject.SetActive(false);
                    Destroy(enemy.gameObject);
                }
            }

            activeEnemies.Clear();
            ClearRoom();
        }

        private void ResolveReferences()
        {
            if (playerTarget == null)
            {
                GameObject player = GameObject.Find("Player");
                playerTarget = player != null ? player.transform : null;
            }

            if (enemyContainer == null)
            {
                Transform existingContainer = transform.Find("Enemies");
                if (existingContainer != null)
                {
                    enemyContainer = existingContainer;
                }
                else
                {
                    GameObject container = new GameObject("Enemies");
                    container.transform.SetParent(transform);
                    container.transform.localPosition = Vector3.zero;
                    enemyContainer = container.transform;
                }
            }
        }

        private int GetEnemyCount(RoomType roomType)
        {
            switch (roomType)
            {
                case RoomType.BattleRoom:
                    return battleEnemyCount;
                case RoomType.EliteRoom:
                    return eliteEnemyCount;
                case RoomType.BossRoom:
                    return bossEnemyCount;
                default:
                    return 0;
            }
        }

        private void SpawnEnemy(int index, int enemyCount)
        {
            if (enemyPrefab == null)
            {
                Debug.LogWarning("RoomManager cannot spawn enemies because enemyPrefab is not assigned.", this);
                return;
            }

            Vector3 spawnPosition = GetSpawnPosition(index);
            GameObject enemyObject = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity, enemyContainer);
            enemyObject.name = CurrentRoomType == RoomType.BossRoom
                ? $"Boss_Room{CurrentRoomNumber}_{index + 1}"
                : $"{enemyPrefab.name}_Room{CurrentRoomNumber}_{index + 1}";
            enemyObject.SetActive(true);

            EnemyController2D enemy = enemyObject.GetComponent<EnemyController2D>();
            if (enemy == null)
            {
                Debug.LogWarning($"{enemyObject.name} does not have EnemyController2D and cannot be tracked by RoomManager.", enemyObject);
                Destroy(enemyObject);
                return;
            }

            enemy.SetTarget(playerTarget);
            ConfigureSpawnedEnemy(enemyObject);
            activeEnemies.Add(enemy);

            if (logRoomMessages)
            {
                Debug.Log($"Spawned enemy {index + 1}/{enemyCount}: {enemyObject.name}", enemyObject);
            }
        }

        private Vector3 GetSpawnPosition(int index)
        {
            if (CurrentRoomType == RoomType.BossRoom)
            {
                return new Vector3(bossSpawnPosition.x, bossSpawnPosition.y, 0f);
            }

            if (spawnPositions != null && spawnPositions.Length > 0)
            {
                Vector2 position = spawnPositions[index % spawnPositions.Length];
                return new Vector3(position.x, position.y, 0f);
            }

            return transform.position + Vector3.right * (3f + index * 1.5f);
        }

        private void ConfigureSpawnedEnemy(GameObject enemyObject)
        {
            if (CurrentRoomType != RoomType.BossRoom || enemyObject == null)
            {
                return;
            }

            enemyObject.transform.localScale *= bossScaleMultiplier;

            HealthComponent health = enemyObject.GetComponent<HealthComponent>();
            if (health != null)
            {
                health.SetMaxHealth(health.MaxHealth * bossHealthMultiplier, true);
            }

            EnemyAttack2D attack = enemyObject.GetComponent<EnemyAttack2D>();
            if (attack != null)
            {
                attack.MultiplyDamage(bossDamageMultiplier);
            }

            SpriteRenderer spriteRenderer = enemyObject.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = bossTint;
            }

            BossPhaseController phaseController = enemyObject.GetComponent<BossPhaseController>();
            if (phaseController == null)
            {
                phaseController = enemyObject.AddComponent<BossPhaseController>();
            }

            phaseController.SetPhaseTwoTint(bossPhaseTwoTint);
            phaseController.Configure(
                health,
                attack,
                bossPhaseTwoHealthRatio,
                bossPhaseTwoDamageMultiplier,
                bossPhaseTwoScaleMultiplier);
        }

        private void HandleEnemyDeath(EnemyController2D enemy)
        {
            if (enemy == null || !activeEnemies.Remove(enemy))
            {
                return;
            }

            if (logRoomMessages)
            {
                Debug.Log($"Room enemy defeated. Remaining: {activeEnemies.Count}", this);
            }

            if (activeEnemies.Count == 0)
            {
                ClearRoom();
            }
        }

        private void ClearSpawnedEnemies()
        {
            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                EnemyController2D enemy = activeEnemies[i];
                if (enemy != null)
                {
                    enemy.gameObject.SetActive(false);
                    Destroy(enemy.gameObject);
                }
            }

            activeEnemies.Clear();

            if (enemyContainer == null)
            {
                return;
            }

            for (int i = enemyContainer.childCount - 1; i >= 0; i--)
            {
                GameObject child = enemyContainer.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }
    }
}
