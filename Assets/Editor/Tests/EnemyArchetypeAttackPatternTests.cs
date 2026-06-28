using System;
using System.Reflection;
using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Enemy;
using AICompanionRoguelike.Roguelike;
using NUnit.Framework;
using UnityEngine;

namespace AICompanionRoguelike.Tests
{
    public sealed class EnemyArchetypeAttackPatternTests
    {
        [Test]
        public void MeleePatternLungesTowardTargetDuringWarning()
        {
            GameObject enemy = CreateEnemy("MeleePatternEnemy", Vector3.zero);
            GameObject player = CreateTarget("MeleePatternPlayer", new Vector3(1f, 0f, 0f));

            try
            {
                EnemyController2D controller = enemy.GetComponent<EnemyController2D>();
                EnemyAttack2D attack = enemy.GetComponent<EnemyAttack2D>();
                controller.SetTarget(player.transform);
                ConfigureAttack(attack, EnemyArchetypeType.Melee, 2f, 0.3f);
                Component pattern = AddAndConfigurePattern(enemy, EnemyArchetypeType.Melee);
                float startX = enemy.transform.position.x;

                attack.TryAttack(player.transform);
                Invoke(pattern, "Tick", 0.1f);

                Assert.True((bool)ReadProperty(pattern, "IsLunging"));
                Assert.True((bool)ReadProperty(pattern, "HasBehaviorVisual"));
                Assert.Greater(enemy.transform.position.x, startX);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(player);
                UnityEngine.Object.DestroyImmediate(enemy);
            }
        }

        [Test]
        public void RangedAttackLaunchesVisibleProjectileBeforeApplyingDamage()
        {
            GameObject enemy = CreateEnemy("RangedPatternEnemy", Vector3.zero);
            GameObject player = CreateTarget("RangedPatternPlayer", new Vector3(3f, 0f, 0f));

            try
            {
                EnemyAttack2D attack = enemy.GetComponent<EnemyAttack2D>();
                HealthComponent playerHealth = player.GetComponent<HealthComponent>();
                WritePrivateField(attack, "damage", 10f);
                ConfigureAttack(attack, EnemyArchetypeType.Ranged, 4f, 0.1f);

                attack.TryAttack(player.transform);
                attack.Tick(0.1f);

                Assert.AreEqual(100f, playerHealth.CurrentHealth);
                Component projectile = (Component)ReadProperty(attack, "LastSpawnedProjectile");
                Assert.NotNull(projectile);
                Assert.True((bool)ReadProperty(projectile, "HasVisual"));

                Invoke(projectile, "Tick", 0.5f);

                Assert.AreEqual(90f, playerHealth.CurrentHealth, 0.001f);
            }
            finally
            {
                DestroyLastProjectile(enemy.GetComponent<EnemyAttack2D>());
                UnityEngine.Object.DestroyImmediate(player);
                UnityEngine.Object.DestroyImmediate(enemy);
            }
        }

        [Test]
        public void RangedProjectileMissesWhenPlayerLeavesItsLine()
        {
            GameObject enemy = CreateEnemy("RangedDodgeEnemy", Vector3.zero);
            GameObject player = CreateTarget("RangedDodgePlayer", new Vector3(3f, 0f, 0f));

            try
            {
                EnemyAttack2D attack = enemy.GetComponent<EnemyAttack2D>();
                HealthComponent playerHealth = player.GetComponent<HealthComponent>();
                WritePrivateField(attack, "damage", 10f);
                ConfigureAttack(attack, EnemyArchetypeType.Ranged, 4f, 0.1f);
                attack.TryAttack(player.transform);
                attack.Tick(0.1f);
                Component projectile = (Component)ReadProperty(attack, "LastSpawnedProjectile");
                Assert.NotNull(projectile);

                player.transform.position = new Vector3(3f, 2f, 0f);
                Invoke(projectile, "Tick", 0.5f);

                Assert.AreEqual(100f, playerHealth.CurrentHealth);
            }
            finally
            {
                DestroyLastProjectile(enemy.GetComponent<EnemyAttack2D>());
                UnityEngine.Object.DestroyImmediate(player);
                UnityEngine.Object.DestroyImmediate(enemy);
            }
        }

        [Test]
        public void GuardBlocksFrontDamageThenBecomesVulnerableAfterAttack()
        {
            GameObject guard = CreateEnemy("GuardPatternEnemy", Vector3.zero);
            GameObject player = CreateTarget("GuardPatternPlayer", new Vector3(-0.8f, 0f, 0f));

            try
            {
                EnemyController2D controller = guard.GetComponent<EnemyController2D>();
                EnemyAttack2D attack = guard.GetComponent<EnemyAttack2D>();
                HealthComponent guardHealth = guard.GetComponent<HealthComponent>();
                controller.SetTarget(player.transform);
                ConfigureAttack(attack, EnemyArchetypeType.Guard, 2f, 0.1f);
                Component pattern = AddAndConfigurePattern(guard, EnemyArchetypeType.Guard);

                guardHealth.TakeDamage(new DamageInfo(20f, DamageSourceType.Player, player));

                Assert.AreEqual(93f, guardHealth.CurrentHealth, 0.001f);
                Assert.True((bool)ReadProperty(pattern, "LastDamageWasBlocked"));
                Assert.True((bool)ReadProperty(pattern, "HasBehaviorVisual"));

                guardHealth.SetMaxHealth(100f, true);
                attack.TryAttack(player.transform);
                Invoke(pattern, "Tick", 0.1f);

                Assert.True((bool)ReadProperty(pattern, "IsGuardVulnerable"));
                guardHealth.TakeDamage(new DamageInfo(20f, DamageSourceType.Player, player));
                Assert.AreEqual(73f, guardHealth.CurrentHealth, 0.001f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(player);
                UnityEngine.Object.DestroyImmediate(guard);
            }
        }

        [Test]
        public void SpawnedRoomsWireArchetypeAttackPatternsAndEffectiveRanges()
        {
            GameObject roomObject = new GameObject("AttackPatternRoomTest");
            GameObject enemyPrefab = CreateEnemy("AttackPatternPrefab", Vector3.zero);
            enemyPrefab.SetActive(false);

            try
            {
                RoomManager roomManager = roomObject.AddComponent<RoomManager>();
                WritePrivateField(roomManager, "enemyPrefab", enemyPrefab);
                WritePrivateField(roomManager, "battleEnemyCount", 2);
                WritePrivateField(roomManager, "eliteEnemyCount", 2);
                WritePrivateField(roomManager, "logRoomMessages", false);
                roomManager.EnterRoom(RoomType.BattleRoom, 1);

                EnemyController2D melee = roomManager.ActiveEnemies[0];
                EnemyController2D ranged = roomManager.ActiveEnemies[1];
                Type patternType = RequireRuntimeType("AICompanionRoguelike.Enemy.EnemyAttackPattern2D");
                Assert.NotNull(melee.GetComponent(patternType));
                Assert.NotNull(ranged.GetComponent(patternType));
                Assert.AreEqual(
                    "Projectile",
                    ReadProperty(ranged.GetComponent<EnemyAttack2D>(), "DeliveryMode").ToString());
                Assert.AreEqual(
                    ranged.GetComponent<EnemyAttack2D>().AttackRange,
                    (float)ReadProperty(ranged, "EffectiveAttackRange"),
                    0.001f);

                roomManager.EnterRoom(RoomType.EliteRoom, 2);
                Component guardPattern = roomManager.ActiveEnemies[0].GetComponent(patternType);
                Assert.NotNull(guardPattern);
                Assert.True((bool)ReadProperty(guardPattern, "HasBehaviorVisual"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemyPrefab);
                UnityEngine.Object.DestroyImmediate(roomObject);
            }
        }

        [Test]
        public void RoomFeedbackExplainsProjectileDodgeAndGuardOpening()
        {
            GameObject runObject = new GameObject("AttackPatternFeedbackTest");

            try
            {
                runObject.AddComponent<RoomManager>();
                RunManager runManager = runObject.AddComponent<RunManager>();
                string battle = (string)Invoke(
                    runManager,
                    "BuildRoomFeedbackMessage",
                    RoomType.BattleRoom,
                    0f,
                    RoomModifierType.None);
                string elite = (string)Invoke(
                    runManager,
                    "BuildRoomFeedbackMessage",
                    RoomType.EliteRoom,
                    0f,
                    RoomModifierType.None);

                Assert.That(battle.ToLowerInvariant(), Does.Contain("projectile"));
                Assert.That(elite.ToLowerInvariant(), Does.Contain("opening"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        private static GameObject CreateEnemy(string objectName, Vector3 position)
        {
            GameObject enemy = new GameObject(objectName);
            enemy.transform.position = position;
            Rigidbody2D body = enemy.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            enemy.AddComponent<HealthComponent>().SetMaxHealth(100f, true);
            enemy.AddComponent<EnemyAttack2D>();
            enemy.AddComponent<EnemyController2D>();
            enemy.AddComponent<SpriteRenderer>();
            return enemy;
        }

        private static GameObject CreateTarget(string objectName, Vector3 position)
        {
            GameObject target = new GameObject(objectName);
            target.transform.position = position;
            target.AddComponent<HealthComponent>().SetMaxHealth(100f, true);
            return target;
        }

        private static void ConfigureAttack(
            EnemyAttack2D attack,
            EnemyArchetypeType archetype,
            float range,
            float warningDuration)
        {
            attack.ConfigureAttackProfile(
                range,
                1f,
                warningDuration,
                new Vector2(range, 0.65f),
                EnemyArchetypeRules.GetRoleColor(archetype));
            Invoke(attack, "ConfigureArchetypeBehavior", archetype);
        }

        private static Component AddAndConfigurePattern(GameObject enemy, EnemyArchetypeType archetype)
        {
            Type patternType = RequireRuntimeType("AICompanionRoguelike.Enemy.EnemyAttackPattern2D");
            Component pattern = enemy.AddComponent(patternType);
            Invoke(pattern, "Configure", archetype);
            return pattern;
        }

        private static void DestroyLastProjectile(EnemyAttack2D attack)
        {
            if (attack == null)
            {
                return;
            }

            Component projectile = ReadPropertyOrNull(attack, "LastSpawnedProjectile") as Component;
            if (projectile != null)
            {
                UnityEngine.Object.DestroyImmediate(projectile.gameObject);
            }
        }

        private static Type RequireRuntimeType(string fullName)
        {
            Type type = Type.GetType($"{fullName}, Assembly-CSharp");
            Assert.NotNull(type, $"Runtime type {fullName} should exist.");
            return type;
        }

        private static object ReadProperty(object target, string propertyName)
        {
            object value = ReadPropertyOrNull(target, propertyName, true);
            return value;
        }

        private static object ReadPropertyOrNull(
            object target,
            string propertyName,
            bool requireProperty = false)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public);
            if (requireProperty)
            {
                Assert.NotNull(property, $"{target.GetType().Name} should expose {propertyName}.");
            }

            return property != null ? property.GetValue(target) : null;
        }

        private static void WritePrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"{target.GetType().Name} should define {fieldName}.");
            field.SetValue(target, value);
        }

        private static object Invoke(object target, string methodName, params object[] parameters)
        {
            MethodInfo[] methods = target.GetType().GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name == methodName
                    && methods[i].GetParameters().Length == parameters.Length)
                {
                    return methods[i].Invoke(target, parameters);
                }
            }

            Assert.Fail($"{target.GetType().Name} should expose {methodName} with {parameters.Length} parameters.");
            return null;
        }
    }
}
