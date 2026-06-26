using System;
using System.Reflection;
using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Enemy;
using AICompanionRoguelike.Roguelike;
using NUnit.Framework;
using UnityEngine;

namespace AICompanionRoguelike.Tests
{
    public sealed class EnemyArchetypeDiversityTests
    {
        [Test]
        public void BattleRoomWithMultipleEnemiesSpawnsMeleeAndRangedRoles()
        {
            GameObject roomObject = new GameObject("BattleArchetypeRoomManagerTest");
            GameObject enemyPrefab = CreateEnemyPrefab();

            try
            {
                RoomManager roomManager = roomObject.AddComponent<RoomManager>();
                WritePrivateField(roomManager, "enemyPrefab", enemyPrefab);
                WritePrivateField(roomManager, "battleEnemyCount", 2);
                WritePrivateField(roomManager, "logRoomMessages", false);

                roomManager.EnterRoom(RoomType.BattleRoom, 1);

                Assert.AreEqual(2, roomManager.ActiveEnemies.Count);
                GameObject meleeEnemy = roomManager.ActiveEnemies[0].gameObject;
                GameObject rangedEnemy = roomManager.ActiveEnemies[1].gameObject;

                AssertEnemyRole(meleeEnemy, "Melee", "close");
                AssertEnemyRole(rangedEnemy, "Ranged", "range");
                Assert.That(meleeEnemy.name, Does.Contain("Melee"));
                Assert.That(rangedEnemy.name, Does.Contain("Ranged"));

                EnemyAttack2D meleeAttack = meleeEnemy.GetComponent<EnemyAttack2D>();
                EnemyAttack2D rangedAttack = rangedEnemy.GetComponent<EnemyAttack2D>();
                Assert.Greater(rangedAttack.AttackRange, meleeAttack.AttackRange);
                Assert.Greater(rangedAttack.WarningSize.x, meleeAttack.WarningSize.x);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemyPrefab);
                UnityEngine.Object.DestroyImmediate(roomObject);
            }
        }

        [Test]
        public void EliteRoomSpawnsGuardAndRangedRolesWithDifferentTuning()
        {
            GameObject roomObject = new GameObject("EliteArchetypeRoomManagerTest");
            GameObject enemyPrefab = CreateEnemyPrefab();

            try
            {
                RoomManager roomManager = roomObject.AddComponent<RoomManager>();
                WritePrivateField(roomManager, "enemyPrefab", enemyPrefab);
                WritePrivateField(roomManager, "eliteEnemyCount", 2);
                WritePrivateField(roomManager, "logRoomMessages", false);

                roomManager.EnterRoom(RoomType.EliteRoom, 2);

                Assert.AreEqual(2, roomManager.ActiveEnemies.Count);
                GameObject guardEnemy = roomManager.ActiveEnemies[0].gameObject;
                GameObject rangedEnemy = roomManager.ActiveEnemies[1].gameObject;

                AssertEnemyRole(guardEnemy, "Guard", "slow");
                AssertEnemyRole(rangedEnemy, "Ranged", "range");

                Assert.Greater(
                    guardEnemy.GetComponent<HealthComponent>().MaxHealth,
                    rangedEnemy.GetComponent<HealthComponent>().MaxHealth);
                Assert.Less(
                    guardEnemy.GetComponent<EnemyController2D>().MoveSpeed,
                    rangedEnemy.GetComponent<EnemyController2D>().MoveSpeed);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemyPrefab);
                UnityEngine.Object.DestroyImmediate(roomObject);
            }
        }

        [Test]
        public void CombatRoomFeedbackDescribesExpectedEnemyTypes()
        {
            GameObject runObject = new GameObject("EnemyArchetypeFeedbackRunManagerTest");
            GameObject enemyPrefab = CreateEnemyPrefab();

            try
            {
                RoomManager roomManager = runObject.AddComponent<RoomManager>();
                WritePrivateField(roomManager, "enemyPrefab", enemyPrefab);
                WritePrivateField(roomManager, "battleEnemyCount", 2);
                WritePrivateField(roomManager, "logRoomMessages", false);
                RunManager runManager = runObject.AddComponent<RunManager>();
                WritePrivateField(runManager, "logRunMessages", false);

                Invoke(runManager, "AdvanceToRoom", RoomType.BattleRoom);

                Assert.That(runManager.LastRoomFeedbackMessage, Does.Contain("Enemy Types"));
                Assert.That(runManager.LastRoomFeedbackMessage, Does.Contain("Melee"));
                Assert.That(runManager.LastRoomFeedbackMessage, Does.Contain("Ranged"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemyPrefab);
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        private static void AssertEnemyRole(GameObject enemyObject, string expectedRoleName, string expectedHintWord)
        {
            Type roleType = RequireRuntimeType("AICompanionRoguelike.Enemy.EnemyArchetype2D");
            Component role = enemyObject.GetComponent(roleType);
            Assert.NotNull(role, $"{enemyObject.name} should expose an enemy archetype marker.");
            Assert.That(ReadProperty(role, "ArchetypeType").ToString(), Is.EqualTo(expectedRoleName));
            Assert.That(ReadProperty(role, "DisplayName") as string, Does.Contain(expectedRoleName));
            Assert.That(ReadProperty(role, "ReadableRoleHint") as string, Does.Contain(expectedHintWord));
            Assert.True((bool)ReadProperty(role, "HasMarkerVisual"));
        }

        private static GameObject CreateEnemyPrefab()
        {
            GameObject enemy = new GameObject("EnemyArchetypePrefab");
            enemy.SetActive(false);
            enemy.AddComponent<Rigidbody2D>();
            enemy.AddComponent<HealthComponent>();
            enemy.AddComponent<EnemyAttack2D>();
            enemy.AddComponent<EnemyController2D>();
            enemy.AddComponent<SpriteRenderer>();
            return enemy;
        }

        private static Type RequireRuntimeType(string fullTypeName)
        {
            Type type = Type.GetType($"{fullTypeName}, Assembly-CSharp");
            Assert.NotNull(type, $"{fullTypeName} should exist.");
            return type;
        }

        private static object ReadProperty(object target, string propertyName)
        {
            Type type = target.GetType();
            PropertyInfo property = type.GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
            Assert.NotNull(property, $"{type.Name} should expose property {propertyName}.");
            return property.GetValue(target);
        }

        private static void WritePrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"{target.GetType().Name} should define field {fieldName}.");
            field.SetValue(target, value);
        }

        private static object Invoke(object target, string methodName, params object[] parameters)
        {
            Type[] parameterTypes = new Type[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                parameterTypes[i] = parameters[i].GetType();
            }

            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                parameterTypes,
                null);
            Assert.NotNull(method, $"{target.GetType().Name} should expose {methodName}.");
            return method.Invoke(target, parameters);
        }
    }
}
