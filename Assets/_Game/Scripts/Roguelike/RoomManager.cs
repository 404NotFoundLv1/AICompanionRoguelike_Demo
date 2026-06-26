using System;
using System.Collections.Generic;
using AICompanionRoguelike.Companion;
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
        [SerializeField, Min(1f)] private float eliteHealthMultiplier = 1.5f;
        [SerializeField, Min(1f)] private float eliteDamageMultiplier = 1.25f;
        [SerializeField, Min(0.1f)] private float eliteScaleMultiplier = 1.18f;
        [SerializeField] private Color eliteTint = new Color(1f, 0.72f, 0.22f, 1f);
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

        [Header("Boss Telegraph Attack")]
        [SerializeField] private bool useBossTelegraphedAttack = true;
        [SerializeField, Min(0f)] private float bossTelegraphedAttackDamage = 18f;
        [SerializeField, Min(0f)] private float bossTelegraphedAttackTriggerRange = 4.5f;
        [SerializeField, Min(0.05f)] private float bossTelegraphedAttackWarningDuration = 0.9f;
        [SerializeField, Min(0.05f)] private float bossTelegraphedAttackCooldown = 4f;
        [SerializeField] private Vector2 bossTelegraphedAttackSize = new Vector2(2.8f, 1.35f);
        [SerializeField, Min(1f)] private float bossTelegraphedAttackPhaseTwoDamageMultiplier = 1.25f;
        [SerializeField, Range(0.1f, 1f)] private float bossTelegraphedAttackPhaseTwoCooldownMultiplier = 0.75f;

        [Header("Debug")]
        [SerializeField] private bool logRoomMessages = true;

        private readonly List<EnemyController2D> activeEnemies = new List<EnemyController2D>(8);

        public event Action<RoomManager, RoomType, int> RoomStarted;
        public event Action<RoomManager, RoomType, int> RoomCleared;

        public RoomType CurrentRoomType { get; private set; }
        public RoomModifierType CurrentRoomModifier { get; private set; }
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
            EnterRoom(roomType, roomNumber, RoomModifierType.None);
        }

        public void EnterRoom(RoomType roomType, int roomNumber, RoomModifierType roomModifier)
        {
            ResolveReferences();
            ClearSpawnedEnemies();

            CurrentRoomType = roomType;
            CurrentRoomModifier = ShouldAllowModifier(roomType) ? roomModifier : RoomModifierType.None;
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
                    DestroyRoomObject(enemy.gameObject);
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
            int baseCount;
            switch (roomType)
            {
                case RoomType.BattleRoom:
                    baseCount = battleEnemyCount;
                    break;
                case RoomType.EliteRoom:
                    baseCount = eliteEnemyCount;
                    break;
                case RoomType.BossRoom:
                    baseCount = bossEnemyCount;
                    break;
                default:
                    return 0;
            }

            return Mathf.Max(0, baseCount + RoomModifierRules.GetExtraEnemyCount(CurrentRoomModifier));
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
            EnemyArchetypeType archetypeType = GetEnemyArchetype(index, enemyCount);
            enemyObject.name = GetEnemyName(index, archetypeType);
            enemyObject.SetActive(true);

            EnemyController2D enemy = enemyObject.GetComponent<EnemyController2D>();
            if (enemy == null)
            {
                Debug.LogWarning($"{enemyObject.name} does not have EnemyController2D and cannot be tracked by RoomManager.", enemyObject);
                DestroyRoomObject(enemyObject);
                return;
            }

            enemy.SetTarget(playerTarget);
            ConfigureSpawnedEnemy(enemyObject, archetypeType);
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

        private void ConfigureSpawnedEnemy(GameObject enemyObject, EnemyArchetypeType archetypeType)
        {
            if (enemyObject == null)
            {
                return;
            }

            EnsureCombatFeelFeedback(enemyObject);

            if (CurrentRoomType != RoomType.BossRoom)
            {
                ApplyEnemyArchetype(enemyObject, archetypeType);
            }

            if (CurrentRoomType == RoomType.EliteRoom)
            {
                ApplyEnemyTuning(enemyObject, eliteHealthMultiplier, eliteDamageMultiplier, eliteScaleMultiplier, eliteTint);
            }

            if (CurrentRoomType == RoomType.BossRoom)
            {
                ConfigureBossEnemy(enemyObject);
                return;
            }

            ApplyRoomModifierEnemyTuning(enemyObject);
        }

        private EnemyArchetypeType GetEnemyArchetype(int index, int enemyCount)
        {
            if (CurrentRoomType == RoomType.EliteRoom)
            {
                return index == 0 ? EnemyArchetypeType.Guard : EnemyArchetypeType.Ranged;
            }

            if (CurrentRoomType == RoomType.BattleRoom)
            {
                if (CurrentRoomModifier == RoomModifierType.Ambush && index == enemyCount - 1 && enemyCount > 1)
                {
                    return EnemyArchetypeType.Melee;
                }

                return enemyCount > 1 && index % 3 == 1
                    ? EnemyArchetypeType.Ranged
                    : EnemyArchetypeType.Melee;
            }

            return EnemyArchetypeType.Melee;
        }

        private static void ApplyEnemyArchetype(GameObject enemyObject, EnemyArchetypeType archetypeType)
        {
            EnemyArchetype2D marker = enemyObject.GetComponent<EnemyArchetype2D>();
            if (marker == null)
            {
                marker = enemyObject.AddComponent<EnemyArchetype2D>();
            }

            marker.Configure(
                archetypeType,
                EnemyArchetypeRules.GetDisplayName(archetypeType),
                EnemyArchetypeRules.GetReadableRoleHint(archetypeType),
                EnemyArchetypeRules.GetRoleColor(archetypeType));

            enemyObject.transform.localScale *= Mathf.Max(0.1f, EnemyArchetypeRules.GetScaleMultiplier(archetypeType));

            HealthComponent health = enemyObject.GetComponent<HealthComponent>();
            if (health != null)
            {
                health.SetMaxHealth(health.MaxHealth * Mathf.Max(0.1f, EnemyArchetypeRules.GetHealthMultiplier(archetypeType)), true);
            }

            EnemyAttack2D attack = enemyObject.GetComponent<EnemyAttack2D>();
            if (attack != null)
            {
                attack.MultiplyDamage(EnemyArchetypeRules.GetDamageMultiplier(archetypeType));
                attack.ConfigureAttackProfile(
                    EnemyArchetypeRules.GetAttackRange(archetypeType),
                    EnemyArchetypeRules.GetCooldown(archetypeType),
                    EnemyArchetypeRules.GetWarningDuration(archetypeType),
                    EnemyArchetypeRules.GetWarningSize(archetypeType),
                    EnemyArchetypeRules.GetRoleColor(archetypeType));
            }

            EnemyController2D controller = enemyObject.GetComponent<EnemyController2D>();
            if (controller != null)
            {
                controller.ConfigureMovement(
                    EnemyArchetypeRules.GetDetectionRange(archetypeType),
                    EnemyArchetypeRules.GetMoveSpeed(archetypeType),
                    EnemyArchetypeRules.GetStopDistance(archetypeType));
            }

            SpriteRenderer spriteRenderer = enemyObject.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = EnemyArchetypeRules.GetRoleColor(archetypeType);
            }
        }

        private static void EnsureCombatFeelFeedback(GameObject enemyObject)
        {
            if (enemyObject.GetComponent<HealthComponent>() == null)
            {
                return;
            }

            if (enemyObject.GetComponent<DamageFlashFeedback2D>() == null)
            {
                enemyObject.AddComponent<DamageFlashFeedback2D>();
            }
        }

        private void ApplyRoomModifierEnemyTuning(GameObject enemyObject)
        {
            if (!ShouldAllowModifier(CurrentRoomType) || CurrentRoomModifier == RoomModifierType.None)
            {
                return;
            }

            ApplyEnemyTuning(
                enemyObject,
                RoomModifierRules.GetEnemyHealthMultiplier(CurrentRoomModifier),
                RoomModifierRules.GetEnemyDamageMultiplier(CurrentRoomModifier),
                RoomModifierRules.GetEnemyScaleMultiplier(CurrentRoomModifier),
                RoomModifierRules.GetEnemyTint(CurrentRoomModifier));
            ApplyRoomModifierVisualMarker(enemyObject);
        }

        private void ApplyRoomModifierVisualMarker(GameObject enemyObject)
        {
            if (CurrentRoomModifier != RoomModifierType.Reinforced && CurrentRoomModifier != RoomModifierType.Ambush)
            {
                return;
            }

            RoomModifierVisualMarker2D marker = enemyObject.GetComponent<RoomModifierVisualMarker2D>();
            if (marker == null)
            {
                marker = enemyObject.AddComponent<RoomModifierVisualMarker2D>();
            }

            marker.Configure(
                CurrentRoomModifier,
                RoomModifierRules.GetFeedbackTitle(CurrentRoomModifier),
                RoomModifierRules.GetFeedbackColor(CurrentRoomModifier),
                RoomModifierRules.GetReadableVisualHint(CurrentRoomModifier));
        }

        private static bool ShouldAllowModifier(RoomType roomType)
        {
            switch (roomType)
            {
                case RoomType.BattleRoom:
                case RoomType.EliteRoom:
                case RoomType.SafeRoom:
                case RoomType.ShopRoom:
                    return true;
                default:
                    return false;
            }
        }

        private string GetEnemyName(int index, EnemyArchetypeType archetypeType)
        {
            string modifierPrefix = string.IsNullOrWhiteSpace(RoomModifierRules.GetShortLabel(CurrentRoomModifier))
                ? string.Empty
                : $"{RoomModifierRules.GetShortLabel(CurrentRoomModifier)}_";
            string archetypePrefix = CurrentRoomType == RoomType.BossRoom
                ? string.Empty
                : $"{EnemyArchetypeRules.GetDisplayName(archetypeType)}_";
            switch (CurrentRoomType)
            {
                case RoomType.BossRoom:
                    return $"Boss_Room{CurrentRoomNumber}_{index + 1}";
                case RoomType.EliteRoom:
                    return $"{modifierPrefix}{archetypePrefix}Elite_{enemyPrefab.name}_Room{CurrentRoomNumber}_{index + 1}";
                default:
                    return $"{modifierPrefix}{archetypePrefix}{enemyPrefab.name}_Room{CurrentRoomNumber}_{index + 1}";
            }
        }

        private void ConfigureBossEnemy(GameObject enemyObject)
        {
            ApplyEnemyTuning(enemyObject, bossHealthMultiplier, bossDamageMultiplier, bossScaleMultiplier, bossTint);
            HealthComponent health = enemyObject.GetComponent<HealthComponent>();
            EnemyAttack2D attack = enemyObject.GetComponent<EnemyAttack2D>();

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

            ConfigureBossTelegraphedAttack(enemyObject);
        }

        private static void ApplyEnemyTuning(
            GameObject enemyObject,
            float healthMultiplier,
            float damageMultiplier,
            float scaleMultiplier,
            Color tint)
        {
            enemyObject.transform.localScale *= Mathf.Max(0.1f, scaleMultiplier);

            HealthComponent health = enemyObject.GetComponent<HealthComponent>();
            if (health != null)
            {
                health.SetMaxHealth(health.MaxHealth * Mathf.Max(1f, healthMultiplier), true);
            }

            EnemyAttack2D attack = enemyObject.GetComponent<EnemyAttack2D>();
            if (attack != null)
            {
                attack.MultiplyDamage(Mathf.Max(1f, damageMultiplier));
            }

            SpriteRenderer spriteRenderer = enemyObject.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = tint;
            }
        }

        private void ConfigureBossTelegraphedAttack(GameObject enemyObject)
        {
            BossTelegraphedAttack2D telegraphedAttack = enemyObject.GetComponent<BossTelegraphedAttack2D>();
            if (!useBossTelegraphedAttack)
            {
                if (telegraphedAttack != null)
                {
                    telegraphedAttack.enabled = false;
                }

                return;
            }

            if (telegraphedAttack == null)
            {
                telegraphedAttack = enemyObject.AddComponent<BossTelegraphedAttack2D>();
            }

            telegraphedAttack.enabled = true;
            telegraphedAttack.Configure(
                playerTarget,
                bossTelegraphedAttackDamage,
                bossTelegraphedAttackTriggerRange,
                bossTelegraphedAttackWarningDuration,
                bossTelegraphedAttackCooldown,
                bossTelegraphedAttackSize);
            telegraphedAttack.SetPhaseTwoTuning(
                bossTelegraphedAttackPhaseTwoDamageMultiplier,
                bossTelegraphedAttackPhaseTwoCooldownMultiplier);

            CompanionBossSupport companionBossSupport = FindAnyObjectByType<CompanionBossSupport>();
            if (companionBossSupport != null)
            {
                companionBossSupport.SetBossAttack(telegraphedAttack);
            }
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
                    DestroyRoomObject(enemy.gameObject);
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
                DestroyRoomObject(child);
            }
        }

        private static void DestroyRoomObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
