using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Memory;
using AICompanionRoguelike.Roguelike;
using NUnit.Framework;
using UnityEngine;

namespace AICompanionRoguelike.Tests
{
    public sealed class SafeRoomRestLoopTests
    {
        [SetUp]
        public void SetUp()
        {
            CompanionRelationshipState.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            CompanionRelationshipState.Clear();
            if (RunSessionState.IsRunActive)
            {
                RunSessionState.EndRun(RunEndReason.ManualReturnHome);
            }
        }

        [Test]
        public void SafeRoomEntryDoesNotAutoHealAndKeepsRouteChoiceAvailable()
        {
            GameObject runObject = CreateRunManagerObject("SafeRestEntryRunManager");
            GameObject playerObject = CreatePlayer("Player", 40f);

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();

                Invoke(runManager, "AdvanceToRoom", RoomType.SafeRoom);

                HealthComponent health = playerObject.GetComponent<HealthComponent>();
                Assert.AreEqual(40f, health.CurrentHealth, 0.01f);
                Assert.False(runManager.IsWaitingForReward);
                Assert.True(runManager.IsWaitingForNextRoom);
                Assert.Greater(runManager.CurrentRoomChoices.Count, 0);
                Assert.That(ReadProperty<string>(runManager, "LastRoomFeedbackMessage"), Does.Contain("rest point"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerObject);
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void RestChoiceHealsOnceAndReturnsToRouteChoice()
        {
            GameObject runObject = CreateRunManagerObject("SafeRestHealRunManager");
            GameObject playerObject = CreatePlayer("Player", 40f);

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();
                Invoke(runManager, "AdvanceToRoom", RoomType.SafeRoom);

                Assert.True((bool)Invoke(runManager, "OpenSafeRestDraft"));
                Assert.True(ReadProperty<bool>(runManager, "IsWaitingForRest"));
                Assert.GreaterOrEqual(CountEnumerable(ReadProperty(runManager, "CurrentRestChoices") as IEnumerable), 3);

                Invoke(runManager, "SelectSafeRestChoice", 0);

                HealthComponent health = playerObject.GetComponent<HealthComponent>();
                Assert.Greater(health.CurrentHealth, 40f);
                Assert.False(ReadProperty<bool>(runManager, "IsWaitingForRest"));
                Assert.True(runManager.IsWaitingForNextRoom);
                Assert.True(ReadProperty<bool>(runManager, "HasUsedCurrentSafeRest"));
                Assert.False((bool)Invoke(runManager, "OpenSafeRestDraft"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerObject);
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void ClosingRestDraftSkipsWithoutSpendingRestPoint()
        {
            GameObject runObject = CreateRunManagerObject("SafeRestSkipRunManager");
            GameObject playerObject = CreatePlayer("Player", 35f);

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();
                Invoke(runManager, "AdvanceToRoom", RoomType.SafeRoom);

                Assert.True((bool)Invoke(runManager, "OpenSafeRestDraft"));
                Invoke(runManager, "CloseSafeRestDraft");

                HealthComponent health = playerObject.GetComponent<HealthComponent>();
                Assert.AreEqual(35f, health.CurrentHealth, 0.01f);
                Assert.False(ReadProperty<bool>(runManager, "IsWaitingForRest"));
                Assert.True(runManager.IsWaitingForNextRoom);
                Assert.Greater(runManager.CurrentRoomChoices.Count, 0);
                Assert.False(ReadProperty<bool>(runManager, "HasUsedCurrentSafeRest"));
                Assert.That(ReadProperty<string>(runManager, "LastSafeRestFeedbackMessage"), Does.Contain("Skipped"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerObject);
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void TalkRestChoiceWritesRelationshipMemory()
        {
            GameObject runObject = CreateRunManagerObject("SafeRestTalkRunManager");
            GameObject playerObject = CreatePlayer("Player", 80f);
            GameObject companionObject = new GameObject("CompanionRelationship");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();
                CompanionRelationship relationship = companionObject.AddComponent<CompanionRelationship>();

                int trustBefore = relationship.Trust;
                int affectionBefore = relationship.Affection;

                Invoke(runManager, "AdvanceToRoom", RoomType.SafeRoom);
                Assert.True((bool)Invoke(runManager, "OpenSafeRestDraft"));
                Invoke(runManager, "SelectSafeRestChoice", 1);

                Assert.Greater(relationship.Trust, trustBefore);
                Assert.Greater(relationship.Affection, affectionBefore);
                Assert.AreEqual(1, relationship.GetMemoryTagScore(RelationshipMemoryTag.Reliable));
                Assert.That(ReadProperty<string>(runManager, "LastSafeRestFeedbackMessage"), Does.Contain("Talked"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(companionObject);
                UnityEngine.Object.DestroyImmediate(playerObject);
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void PrepareRestChoiceGrantsSupplyForNextRoute()
        {
            GameObject runObject = CreateRunManagerObject("SafeRestPrepareRunManager");
            GameObject playerObject = CreatePlayer("Player", 80f);

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();
                Invoke(runManager, "AdvanceToRoom", RoomType.SafeRoom);

                Assert.True((bool)Invoke(runManager, "OpenSafeRestDraft"));
                Invoke(runManager, "SelectSafeRestChoice", 2);

                Assert.AreEqual(1, runManager.CurrentSupplies);
                Assert.True(runManager.IsWaitingForNextRoom);
                Assert.That(ReadProperty<string>(runManager, "LastSafeRestFeedbackMessage"), Does.Contain("Prepared"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerObject);
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void SafeRestInteractableOpensRestDraftOnlyInSafeRoom()
        {
            Type restType = RequireRuntimeType("AICompanionRoguelike.Roguelike.SafeRestInteractable");
            GameObject runObject = CreateRunManagerObject("SafeRestInteractableRunManager");
            GameObject restObject = new GameObject("SafeRestInteractableTest");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();
                object restPoint = restObject.AddComponent(restType);
                Invoke(restPoint, "Configure", runManager);

                Assert.False((bool)Invoke(restPoint, "Interact"));

                Invoke(runManager, "AdvanceToRoom", RoomType.SafeRoom);

                Assert.True((bool)Invoke(restPoint, "Interact"));
                Assert.True(ReadProperty<bool>(runManager, "IsWaitingForRest"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(restObject);
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

        private static GameObject CreatePlayer(string objectName, float currentHealth)
        {
            GameObject player = new GameObject(objectName);
            HealthComponent health = player.AddComponent<HealthComponent>();
            health.SetMaxHealth(100f, true);
            health.TakeDamage(new DamageInfo(100f - currentHealth, DamageSourceType.Environment, null));
            return player;
        }

        private static Type RequireRuntimeType(string fullName)
        {
            Type type = Type.GetType($"{fullName}, Assembly-CSharp");
            Assert.NotNull(type, $"{fullName} should exist.");
            return type;
        }

        private static int CountEnumerable(IEnumerable enumerable)
        {
            Assert.NotNull(enumerable);
            int count = 0;
            foreach (object _ in enumerable)
            {
                count++;
            }

            return count;
        }

        private static object Invoke(object target, string methodName, params object[] args)
        {
            MethodInfo method = null;
            MethodInfo[] methods = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name == methodName && methods[i].GetParameters().Length == args.Length)
                {
                    method = methods[i];
                    break;
                }
            }

            Assert.NotNull(method, $"{target.GetType().Name}.{methodName} should exist.");
            return method.Invoke(target, args);
        }

        private static T ReadProperty<T>(object target, string propertyName)
        {
            object value = ReadProperty(target, propertyName);
            return value is T typed ? typed : default;
        }

        private static object ReadProperty(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(property, $"{target.GetType().Name}.{propertyName} should exist.");
            return property.GetValue(target);
        }
    }
}
