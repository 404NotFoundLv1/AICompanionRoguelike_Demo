using System;
using System.Reflection;
using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Enemy;
using AICompanionRoguelike.Roguelike;
using NUnit.Framework;
using UnityEngine;

namespace AICompanionRoguelike.Tests
{
    public sealed class RoomObjectiveFeedbackTests
    {
        [TearDown]
        public void TearDown()
        {
            if (RunSessionState.IsRunActive)
            {
                RunSessionState.EndRun(RunEndReason.ManualReturnHome);
            }
        }

        [Test]
        public void BattleRoomExposesEnemyObjectiveAndProgress()
        {
            GameObject runObject = new GameObject("BattleObjectiveRunManager");
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

                Assert.AreEqual(2, ReadProperty<int>(roomManager, "InitialEnemyCount"));
                Assert.That(ReadProperty<string>(runManager, "CurrentRoomObjectiveLabel"), Does.Contain("Defeat enemies 0/2"));
                Assert.That(ReadProperty<string>(runManager, "CurrentRoomProgressLabel"), Does.Contain("Enemies remaining: 2/2"));
                Assert.That(ReadProperty<string>(runManager, "CurrentNextStepLabel"), Does.Contain("Clear the room"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemyPrefab);
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void ClearingCombatRoomExposesRewardAndRouteFeedback()
        {
            GameObject runObject = new GameObject("ClearObjectiveRunManager");
            GameObject enemyPrefab = CreateEnemyPrefab();

            try
            {
                RoomManager roomManager = runObject.AddComponent<RoomManager>();
                WritePrivateField(roomManager, "enemyPrefab", enemyPrefab);
                WritePrivateField(roomManager, "battleEnemyCount", 1);
                WritePrivateField(roomManager, "logRoomMessages", false);
                RunManager runManager = runObject.AddComponent<RunManager>();
                WritePrivateField(runManager, "logRunMessages", false);

                Invoke(runManager, "AdvanceToRoom", RoomType.BattleRoom);
                roomManager.ForceClearCurrentRoom();

                Assert.That(ReadProperty<string>(runManager, "CurrentRoomObjectiveLabel"), Does.Contain("Room Cleared"));
                Assert.That(ReadProperty<string>(runManager, "CurrentNextStepLabel"), Does.Contain("Select a reward"));
                Assert.That(runManager.LastRoomFeedbackMessage, Does.Contain("Reward Available"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemyPrefab);
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void SafeRoomExposesRestOrSkipObjective()
        {
            GameObject runObject = CreateRunManagerObject("SafeObjectiveRunManager");
            GameObject playerObject = CreatePlayer("Player");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();

                Invoke(runManager, "AdvanceToRoom", RoomType.SafeRoom);

                Assert.That(ReadProperty<string>(runManager, "CurrentRoomObjectiveLabel"), Does.Contain("Use rest point or skip"));
                Assert.That(ReadProperty<string>(runManager, "CurrentRoomProgressLabel"), Does.Contain("Safe room"));
                Assert.That(ReadProperty<string>(runManager, "CurrentNextStepLabel"), Does.Contain("Interact with rest point"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerObject);
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void ShopRoomExposesShopOrSkipObjective()
        {
            GameObject runObject = CreateRunManagerObject("ShopObjectiveRunManager");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();

                Invoke(runManager, "AdvanceToRoom", RoomType.ShopRoom);

                Assert.That(ReadProperty<string>(runManager, "CurrentRoomObjectiveLabel"), Does.Contain("Interact with shop or skip"));
                Assert.That(ReadProperty<string>(runManager, "CurrentRoomProgressLabel"), Does.Contain("Supply room"));
                Assert.That(ReadProperty<string>(runManager, "CurrentNextStepLabel"), Does.Contain("Interact with shop"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void RouteChoiceStateExposesChooseRouteNextStep()
        {
            GameObject runObject = CreateRunManagerObject("RouteObjectiveRunManager");
            GameObject playerObject = CreatePlayer("Player");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();

                Invoke(runManager, "AdvanceToRoom", RoomType.SafeRoom);
                Assert.True((bool)Invoke(runManager, "OpenSafeRestDraft"));
                Invoke(runManager, "CloseSafeRestDraft");

                Assert.True(runManager.IsWaitingForNextRoom);
                Assert.That(ReadProperty<string>(runManager, "CurrentRoomObjectiveLabel"), Does.Contain("Route opened"));
                Assert.That(ReadProperty<string>(runManager, "CurrentNextStepLabel"), Does.Contain("Choose a route"));
                Assert.That(runManager.LastRoomFeedbackMessage, Does.Contain("Route Opened"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerObject);
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        private static GameObject CreateRunManagerObject(string objectName)
        {
            GameObject runObject = new GameObject(objectName);
            runObject.AddComponent<RoomManager>();
            runObject.AddComponent<RunManager>();
            return runObject;
        }

        private static GameObject CreatePlayer(string objectName)
        {
            GameObject player = new GameObject(objectName);
            HealthComponent health = player.AddComponent<HealthComponent>();
            health.SetMaxHealth(100f, true);
            return player;
        }

        private static GameObject CreateEnemyPrefab()
        {
            GameObject enemy = new GameObject("ObjectiveEnemyPrefab");
            enemy.SetActive(false);
            enemy.AddComponent<Rigidbody2D>();
            enemy.AddComponent<HealthComponent>();
            enemy.AddComponent<EnemyAttack2D>();
            enemy.AddComponent<EnemyController2D>();
            enemy.AddComponent<SpriteRenderer>();
            return enemy;
        }

        private static object Invoke(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(method, $"{target.GetType().Name}.{methodName} should exist.");
            return method.Invoke(target, args);
        }

        private static T ReadProperty<T>(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(property, $"{target.GetType().Name}.{propertyName} should exist.");
            object value = property.GetValue(target);
            return value is T typed ? typed : default;
        }

        private static void WritePrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"{target.GetType().Name}.{fieldName} should exist.");
            field.SetValue(target, value);
        }
    }
}
