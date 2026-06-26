using System;
using System.Reflection;
using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Enemy;
using AICompanionRoguelike.Roguelike;
using NUnit.Framework;
using UnityEngine;

namespace AICompanionRoguelike.Tests
{
    public sealed class CombatFeelPolishTests
    {
        [Test]
        public void EnemyAttackWarnsBeforeDamageAndShowsReadableVisual()
        {
            GameObject enemyObject = new GameObject("WarningEnemy");
            GameObject playerObject = new GameObject("WarningPlayer");

            try
            {
                enemyObject.transform.position = Vector3.zero;
                playerObject.transform.position = new Vector3(0.8f, 0f, 0f);
                EnemyAttack2D attack = enemyObject.AddComponent<EnemyAttack2D>();
                HealthComponent playerHealth = playerObject.AddComponent<HealthComponent>();
                playerHealth.SetMaxHealth(100f, true);
                WritePrivateField(attack, "damage", 10f);
                WritePrivateField(attack, "cooldown", 1f);
                WritePrivateField(attack, "attackRange", 1.5f);
                WritePrivateField(attack, "warningDuration", 0.25f);

                attack.TryAttack(playerObject.transform);

                Assert.True((bool)ReadProperty(attack, "IsWarningActive"));
                Assert.True((bool)ReadProperty(attack, "HasWarningVisual"));
                Assert.AreEqual(100f, playerHealth.CurrentHealth);

                Invoke(attack, "Tick", 0.25f);

                Assert.False((bool)ReadProperty(attack, "IsWarningActive"));
                Assert.AreEqual(90f, playerHealth.CurrentHealth);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerObject);
                UnityEngine.Object.DestroyImmediate(enemyObject);
            }
        }

        [Test]
        public void DamageFlashFeedbackColorsSpriteAndRestoresOriginalColor()
        {
            GameObject targetObject = new GameObject("DamageFlashTarget");

            try
            {
                SpriteRenderer spriteRenderer = targetObject.AddComponent<SpriteRenderer>();
                spriteRenderer.color = Color.white;
                HealthComponent health = targetObject.AddComponent<HealthComponent>();
                health.SetMaxHealth(100f, true);

                Type feedbackType = RequireRuntimeType("AICompanionRoguelike.Combat.DamageFlashFeedback2D");
                Component feedback = targetObject.AddComponent(feedbackType);
                WritePrivateField(feedback, "flashDuration", 0.2f);

                health.TakeDamage(new DamageInfo(10f, DamageSourceType.Enemy, null));

                Assert.True((bool)ReadProperty(feedback, "IsFlashing"));
                Assert.AreNotEqual(Color.white, spriteRenderer.color);

                Invoke(feedback, "Tick", 0.2f);

                Assert.False((bool)ReadProperty(feedback, "IsFlashing"));
                Assert.AreEqual(Color.white, spriteRenderer.color);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(targetObject);
            }
        }

        [Test]
        public void BattleRoomEntryAndClearExposeReadablePaceFeedback()
        {
            GameObject runObject = new GameObject("CombatPaceRunManagerTest");
            GameObject enemyPrefab = CreateEnemyPrefab();

            try
            {
                RoomManager roomManager = runObject.AddComponent<RoomManager>();
                WritePrivateField(roomManager, "enemyPrefab", enemyPrefab);
                WritePrivateField(roomManager, "logRoomMessages", false);
                RunManager runManager = runObject.AddComponent<RunManager>();
                WritePrivateField(runManager, "logRunMessages", false);

                Invoke(runManager, "AdvanceToRoom", RoomType.BattleRoom);

                Assert.That(runManager.LastRoomFeedbackMessage, Does.Contain("Combat Started"));
                Assert.That(runManager.LastRoomFeedbackMessage, Does.Contain("enemy warning"));
                Assert.NotNull(roomManager.ActiveEnemies[0].GetComponent(RequireRuntimeType("AICompanionRoguelike.Combat.DamageFlashFeedback2D")));

                roomManager.ForceClearCurrentRoom();

                Assert.That(runManager.LastRoomFeedbackMessage, Does.Contain("Room Clear"));
                Assert.That(runManager.LastRoomFeedbackMessage, Does.Contain("reward"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemyPrefab);
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        private static GameObject CreateEnemyPrefab()
        {
            GameObject enemy = new GameObject("CombatFeelEnemyPrefab");
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
