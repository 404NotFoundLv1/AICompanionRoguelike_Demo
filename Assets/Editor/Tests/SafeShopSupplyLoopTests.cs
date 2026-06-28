using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Roguelike;
using AICompanionRoguelike.UI;
using NUnit.Framework;
using UnityEngine;

namespace AICompanionRoguelike.Tests
{
    public sealed class SafeShopSupplyLoopTests
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
        public void SupplyRulesDefineCombatGainsShopCostAndAffordability()
        {
            Type rulesType = RequireRuntimeType("AICompanionRoguelike.Roguelike.RunSupplyRules");

            Assert.AreEqual(1, InvokeStatic<int>(rulesType, "GetSupplyGain", RoomType.BattleRoom));
            Assert.AreEqual(2, InvokeStatic<int>(rulesType, "GetSupplyGain", RoomType.EliteRoom));
            Assert.AreEqual(0, InvokeStatic<int>(rulesType, "GetSupplyGain", RoomType.SafeRoom));
            Assert.AreEqual(2, InvokeStatic<int>(rulesType, "ShopRewardCost"));
            Assert.True(InvokeStatic<bool>(rulesType, "CanAffordShopReward", 2));
            Assert.False(InvokeStatic<bool>(rulesType, "CanAffordShopReward", 1));

            string affordLabel = InvokeStatic<string>(rulesType, "BuildShopAffordabilityLabel", 3);
            string shortLabel = InvokeStatic<string>(rulesType, "BuildShopAffordabilityLabel", 1);

            Assert.That(affordLabel, Does.Contain("Supplies 3"));
            Assert.That(affordLabel, Does.Contain("Cost 2"));
            Assert.That(shortLabel, Does.Contain("Need 1 more"));
        }

        [Test]
        public void CombatRoomClearsGrantSupplies()
        {
            GameObject runObject = CreateRunManagerObject("SupplyGainRunManager");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();

                Invoke(runManager, "HandleRoomCleared", runObject.GetComponent<RoomManager>(), RoomType.BattleRoom, 1);
                Assert.AreEqual(1, ReadProperty<int>(runManager, "CurrentSupplies"));

                Invoke(runManager, "HandleRoomCleared", runObject.GetComponent<RoomManager>(), RoomType.EliteRoom, 2);
                Assert.AreEqual(3, ReadProperty<int>(runManager, "CurrentSupplies"));
                Assert.That(ReadProperty<string>(runManager, "LastRoomFeedbackMessage"), Does.Contain("Supplies +2"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void ShopRoomDoesNotAutomaticallyOpenPurchaseDraft()
        {
            GameObject runObject = CreateRunManagerObject("ShopNoAutoPopupRunManager");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();

                Invoke(runManager, "AdvanceToRoom", RoomType.ShopRoom);

                Assert.False(runManager.IsWaitingForReward, "Entering ShopRoom should not open purchases automatically.");
                Assert.True(runManager.IsWaitingForNextRoom, "ShopRoom should allow skipping directly to the next route.");
                Assert.Greater(runManager.CurrentRoomChoices.Count, 0, "Skipping shopping should still leave route choices available.");
                Assert.That(ReadProperty<string>(runManager, "LastRoomFeedbackMessage"), Does.Contain("interact"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void ShopRewardSpendsSuppliesAndReturnsToRouteChoice()
        {
            GameObject runObject = CreateRunManagerObject("ShopSpendRunManager");
            GameObject playerObject = CreatePlayer("Player");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();
                WritePrivateField(runManager, "currentSupplies", 2);

                Invoke(runManager, "AdvanceToRoom", RoomType.ShopRoom);
                Assert.True((bool)Invoke(runManager, "OpenShopRewardDraft"));
                Assert.True(runManager.IsWaitingForReward);

                runManager.SelectReward(0);

                Assert.AreEqual(0, ReadProperty<int>(runManager, "CurrentSupplies"));
                Assert.False(runManager.IsWaitingForReward);
                Assert.True(runManager.IsWaitingForNextRoom);
                Assert.That(ReadProperty<string>(runManager, "LastShopFeedbackMessage"), Does.Contain("Spent 2"));
                Assert.That(ReadProperty<string>(runManager, "LastRoomFeedbackMessage"), Does.Contain("Supplies 0"));

                Assert.False((bool)Invoke(runManager, "OpenShopRewardDraft"), "One shop room should not sell a second reward after purchase.");
                Assert.That(ReadProperty<string>(runManager, "LastShopFeedbackMessage"), Does.Contain("already used"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerObject);
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void ShopRewardWithoutSuppliesCanBeClosedAndReturnsToRouteChoice()
        {
            GameObject runObject = CreateRunManagerObject("ShopBlockedRunManager");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();

                Invoke(runManager, "AdvanceToRoom", RoomType.ShopRoom);
                Assert.True((bool)Invoke(runManager, "OpenShopRewardDraft"));
                runManager.SelectReward(0);

                Assert.AreEqual(0, ReadProperty<int>(runManager, "CurrentSupplies"));
                Assert.True(runManager.IsWaitingForReward);
                Assert.False(runManager.IsWaitingForNextRoom);
                Assert.That(ReadProperty<string>(runManager, "LastShopFeedbackMessage"), Does.Contain("Not enough supplies"));

                Invoke(runManager, "CloseShopRewardDraft");

                Assert.False(runManager.IsWaitingForReward);
                Assert.True(runManager.IsWaitingForNextRoom);
                Assert.Greater(runManager.CurrentRoomChoices.Count, 0);
                Assert.That(ReadProperty<string>(runManager, "LastShopFeedbackMessage"), Does.Contain("Skipped"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void SafeAndShopPreviewsExplainRecoverySuppliesAndCost()
        {
            GameObject runObject = CreateRunManagerObject("SupplyPreviewRunManager");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();
                WritePrivateField(runManager, "roomChoiceCount", 4);
                WritePrivateField(
                    runManager,
                    "selectableRoomTypes",
                    new[]
                    {
                        RoomType.BattleRoom,
                        RoomType.EliteRoom,
                        RoomType.SafeRoom,
                        RoomType.ShopRoom
                    });
                WritePrivateField(runManager, "currentSupplies", 1);

                Invoke(runManager, "PrepareNextRoomChoices");

                object safePreview = FindPreview(runManager.CurrentRoomChoicePreviews, RoomType.SafeRoom);
                object shopPreview = FindPreview(runManager.CurrentRoomChoicePreviews, RoomType.ShopRoom);

                Assert.That(ReadProperty<string>(safePreview, "RewardPreview"), Does.Contain("restore"));
                Assert.That(ReadProperty<string>(safePreview, "RouteNote"), Does.Contain("Supplies"));
                Assert.That(ReadProperty<string>(shopPreview, "RewardPreview"), Does.Contain("Cost 2"));
                Assert.That(ReadProperty<string>(shopPreview, "RewardPreview"), Does.Contain("Supplies 1"));
                Assert.That(ReadProperty<string>(shopPreview, "RewardPreview"), Does.Contain("Need 1 more"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void SupplyShopInteractableOpensShopDraftOnlyInShopRoom()
        {
            Type shopType = RequireRuntimeType("AICompanionRoguelike.Roguelike.SupplyShopInteractable");
            GameObject runObject = CreateRunManagerObject("SupplyShopInteractableRunManager");
            GameObject shopObject = new GameObject("SupplyShopInteractableTest");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();
                object shop = shopObject.AddComponent(shopType);
                Invoke(shop, "Configure", runManager);

                Assert.False((bool)Invoke(shop, "Interact"), "Shop interaction should do nothing before a ShopRoom.");

                WritePrivateField(runManager, "currentSupplies", 2);
                Invoke(runManager, "AdvanceToRoom", RoomType.ShopRoom);

                Assert.True((bool)Invoke(shop, "Interact"));
                Assert.True(runManager.IsWaitingForReward);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(shopObject);
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void SupportRoomsTriggerCompanionSupplyFeedback()
        {
            GameObject runObject = CreateRunManagerObject("SupplyCompanionFeedbackRunManager");
            GameObject playerObject = CreatePlayer("Player");
            GameObject companionObject = new GameObject("CompanionBubble");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();
                CompanionSpeechBubbleUI bubble = companionObject.AddComponent<CompanionSpeechBubbleUI>();

                Invoke(runManager, "AdvanceToRoom", RoomType.SafeRoom);
                Assert.That(bubble.CurrentMessage ?? string.Empty, Does.Contain("recover"));

                WritePrivateField(runManager, "waitingForNextRoom", true);
                runManager.AdvanceToSelectedRoom(RoomType.ShopRoom);
                Assert.That(bubble.CurrentMessage ?? string.Empty, Does.Contain("supplies"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(companionObject);
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
            health.TakeDamage(new DamageInfo(35f, DamageSourceType.Environment, null));
            return player;
        }

        private static object FindPreview(IEnumerable previews, RoomType roomType)
        {
            foreach (object preview in previews)
            {
                if (ReadProperty<RoomType>(preview, "RoomType") == roomType)
                {
                    return preview;
                }
            }

            Assert.Fail($"Missing preview for {roomType}.");
            return null;
        }

        private static Type RequireRuntimeType(string fullName)
        {
            Type type = Type.GetType($"{fullName}, Assembly-CSharp");
            Assert.NotNull(type, $"{fullName} should exist.");
            return type;
        }

        private static T InvokeStatic<T>(Type type, string methodName, params object[] args)
        {
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, $"{methodName} should be a public static method.");
            return (T)method.Invoke(null, args);
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

        private static void WritePrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"{target.GetType().Name}.{fieldName} should exist.");
            field.SetValue(target, value);
        }
    }
}
