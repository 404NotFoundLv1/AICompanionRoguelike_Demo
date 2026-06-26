using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Enemy;
using AICompanionRoguelike.Roguelike;
using NUnit.Framework;
using UnityEngine;

namespace AICompanionRoguelike.Tests
{
    public sealed class RoomModifierRiskRewardTests
    {
        [Test]
        public void PreparedRouteChoicesExposeModifierRiskAndRewardPreviews()
        {
            GameObject runObject = new GameObject("RunManagerModifierPreviewTest");

            try
            {
                runObject.AddComponent<RoomManager>();
                RunManager runManager = runObject.AddComponent<RunManager>();
                WritePrivateField(runManager, "roomChoiceCount", 4);
                WritePrivateField(
                    runManager,
                    "selectableRoomTypes",
                    new[]
                    {
                        RoomType.BattleRoom,
                        RoomType.SafeRoom,
                        RoomType.ShopRoom,
                        RoomType.EliteRoom
                    });

                Invoke(runManager, "PrepareNextRoomChoices");

                IReadOnlyList<object> modifiers = ReadEnumerableProperty(runManager, "CurrentRoomChoiceModifiers");
                IReadOnlyList<object> previews = ReadEnumerableProperty(runManager, "CurrentRoomChoicePreviews");

                Assert.AreEqual(runManager.CurrentRoomChoices.Count, modifiers.Count);
                Assert.AreEqual(runManager.CurrentRoomChoices.Count, previews.Count);
                AssertModifierNamed(modifiers[0], "Reinforced");
                AssertPreviewContainsModifier(previews[0], "Reinforced");
                AssertPreviewContainsModifier(previews[1], "Recovery");

                string modifierRisk = ReadProperty(previews[0], "ModifierRiskPreview") as string;
                string modifierReward = ReadProperty(previews[0], "ModifierRewardPreview") as string;
                Assert.That(modifierRisk, Does.Contain("Risk"));
                Assert.That(modifierReward, Does.Contain("Reward"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void SelectingRecoverySafeRoomAppliesStrongerHealAndRecordsModifier()
        {
            GameObject runObject = new GameObject("RunManagerRecoveryModifierTest");
            GameObject playerObject = new GameObject("Player");

            try
            {
                runObject.AddComponent<RoomManager>();
                RunManager runManager = runObject.AddComponent<RunManager>();
                WritePrivateField(runManager, "roomChoiceCount", 4);
                WritePrivateField(
                    runManager,
                    "selectableRoomTypes",
                    new[]
                    {
                        RoomType.BattleRoom,
                        RoomType.SafeRoom,
                        RoomType.ShopRoom,
                        RoomType.EliteRoom
                    });
                HealthComponent health = playerObject.AddComponent<HealthComponent>();
                health.SetMaxHealth(100f, true);
                health.TakeDamage(new DamageInfo(60f, DamageSourceType.Environment, null));

                Invoke(runManager, "PrepareNextRoomChoices");
                WritePrivateField(runManager, "waitingForNextRoom", true);
                int safeIndex = FindChoiceIndex(runManager, RoomType.SafeRoom);
                Invoke(runManager, "AdvanceToSelectedRoom", safeIndex);

                Assert.GreaterOrEqual(health.CurrentHealth, 77f);
                AssertModifierNamed(ReadProperty(runManager, "CurrentRoomModifier"), "Recovery");
                Assert.That(ReadProperty(runManager, "LastRoomFeedbackMessage") as string, Does.Contain("Recovery"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerObject);
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void AmbushAddsEnemyAndReinforcedTunesEnemyStats()
        {
            GameObject ambushObject = new GameObject("AmbushRoomManagerTest");
            GameObject normalObject = new GameObject("NormalRoomManagerTest");
            GameObject reinforcedObject = new GameObject("ReinforcedRoomManagerTest");
            GameObject enemyPrefab = CreateEnemyPrefab();

            try
            {
                RoomManager ambushManager = ambushObject.AddComponent<RoomManager>();
                RoomManager normalManager = normalObject.AddComponent<RoomManager>();
                RoomManager reinforcedManager = reinforcedObject.AddComponent<RoomManager>();
                WritePrivateField(ambushManager, "enemyPrefab", enemyPrefab);
                WritePrivateField(normalManager, "enemyPrefab", enemyPrefab);
                WritePrivateField(reinforcedManager, "enemyPrefab", enemyPrefab);
                WritePrivateField(ambushManager, "logRoomMessages", false);
                WritePrivateField(normalManager, "logRoomMessages", false);
                WritePrivateField(reinforcedManager, "logRoomMessages", false);

                InvokeEnterRoomWithModifier(ambushManager, RoomType.BattleRoom, 1, "Ambush");
                normalManager.EnterRoom(RoomType.BattleRoom, 1);
                InvokeEnterRoomWithModifier(reinforcedManager, RoomType.BattleRoom, 1, "Reinforced");

                GameObject normalEnemy = normalManager.ActiveEnemies[0].gameObject;
                GameObject reinforcedEnemy = reinforcedManager.ActiveEnemies[0].gameObject;

                Assert.AreEqual(2, ambushManager.ActiveEnemies.Count);
                Assert.Greater(
                    reinforcedEnemy.GetComponent<HealthComponent>().MaxHealth,
                    normalEnemy.GetComponent<HealthComponent>().MaxHealth);
                Assert.Greater(
                    reinforcedEnemy.GetComponent<EnemyAttack2D>().Damage,
                    normalEnemy.GetComponent<EnemyAttack2D>().Damage);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemyPrefab);
                UnityEngine.Object.DestroyImmediate(reinforcedObject);
                UnityEngine.Object.DestroyImmediate(normalObject);
                UnityEngine.Object.DestroyImmediate(ambushObject);
            }
        }

        [Test]
        public void BattleModifierBonusIncreasesRewardChoiceCount()
        {
            GameObject runObject = new GameObject("RunManagerModifierRewardTest");

            try
            {
                runObject.AddComponent<RoomManager>();
                RunManager runManager = runObject.AddComponent<RunManager>();
                WritePrivateField(runManager, "roomChoiceCount", 4);
                WritePrivateField(runManager, "rewardChoiceCount", 3);
                WritePrivateField(
                    runManager,
                    "selectableRoomTypes",
                    new[]
                    {
                        RoomType.BattleRoom,
                        RoomType.SafeRoom,
                        RoomType.ShopRoom,
                        RoomType.EliteRoom
                    });

                Invoke(runManager, "PrepareNextRoomChoices");
                WritePrivateField(runManager, "waitingForNextRoom", true);
                int battleIndex = FindChoiceIndex(runManager, RoomType.BattleRoom);
                Invoke(runManager, "AdvanceToSelectedRoom", battleIndex);

                AssertModifierNamed(ReadProperty(runManager, "CurrentRoomModifier"), "Reinforced");
                Assert.True(runManager.IsWaitingForReward);
                Assert.AreEqual(4, runManager.CurrentRewardChoices.Count);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        private static IReadOnlyList<object> ReadEnumerableProperty(object target, string propertyName)
        {
            object value = ReadProperty(target, propertyName);
            IEnumerable enumerable = value as IEnumerable;
            Assert.NotNull(enumerable, $"{propertyName} should be enumerable.");

            List<object> entries = new List<object>();
            foreach (object entry in enumerable)
            {
                entries.Add(entry);
            }

            return entries;
        }

        private static void AssertPreviewContainsModifier(object preview, string expectedModifierName)
        {
            AssertModifierNamed(ReadProperty(preview, "ModifierType"), expectedModifierName);
            string title = ReadProperty(preview, "ModifierTitle") as string;
            Assert.That(title, Does.Contain(expectedModifierName));
        }

        private static void AssertModifierNamed(object modifierValue, string expectedName)
        {
            Assert.NotNull(modifierValue, "Modifier value should exist.");
            Assert.AreEqual(expectedName, modifierValue.ToString());
        }

        private static void InvokeEnterRoomWithModifier(
            RoomManager roomManager,
            RoomType roomType,
            int roomNumber,
            string modifierName)
        {
            Type modifierType = Type.GetType("AICompanionRoguelike.Roguelike.RoomModifierType, Assembly-CSharp");
            Assert.NotNull(modifierType, "RoomModifierType should exist.");
            object modifierValue = Enum.Parse(modifierType, modifierName);
            MethodInfo method = typeof(RoomManager).GetMethod(
                "EnterRoom",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { typeof(RoomType), typeof(int), modifierType },
                null);
            Assert.NotNull(method, "RoomManager should expose EnterRoom(RoomType, int, RoomModifierType).");
            method.Invoke(roomManager, new[] { roomType, roomNumber, modifierValue });
        }

        private static int FindChoiceIndex(RunManager runManager, RoomType roomType)
        {
            for (int i = 0; i < runManager.CurrentRoomChoices.Count; i++)
            {
                if (runManager.CurrentRoomChoices[i] == roomType)
                {
                    return i;
                }
            }

            Assert.Fail($"Expected {roomType} in current room choices.");
            return -1;
        }

        private static GameObject CreateEnemyPrefab()
        {
            GameObject enemy = new GameObject("ModifierEnemyPrefab");
            enemy.SetActive(false);
            enemy.AddComponent<Rigidbody2D>();
            enemy.AddComponent<HealthComponent>();
            enemy.AddComponent<EnemyAttack2D>();
            enemy.AddComponent<EnemyController2D>();
            enemy.AddComponent<SpriteRenderer>();
            return enemy;
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
