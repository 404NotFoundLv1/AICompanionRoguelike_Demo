using System;
using System.Reflection;
using AICompanionRoguelike.Roguelike;
using AICompanionRoguelike.UI;
using NUnit.Framework;
using UnityEngine;

namespace AICompanionRoguelike.Tests
{
    public sealed class DemoRunFlowStabilizationTests
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
        public void DemoRouteCanReachBossVictoryWithActionableStops()
        {
            GameObject runObject = CreateRunManagerObject("DemoFlowRunManager");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();
                RunSessionState.EnsureRunStartedFromBattleScene("Assets/Scenes/SampleScene.unity");

                Invoke(runManager, "AdvanceToRoom", RoomType.BattleRoom);
                AssertActionable(runManager, "Reward");

                runManager.SelectReward(0);
                AssertActionable(runManager, "Choose route");

                runManager.AdvanceToSelectedRoom(RoomType.SafeRoom);
                AssertActionable(runManager, "Rest point");

                Assert.True((bool)Invoke(runManager, "OpenSafeRestDraft"));
                AssertActionable(runManager, "Rest choice");

                Invoke(runManager, "CloseSafeRestDraft");
                AssertActionable(runManager, "Choose route");

                runManager.AdvanceToSelectedRoom(RoomType.BattleRoom);
                AssertActionable(runManager, "Reward");

                runManager.SelectReward(0);
                AssertActionable(runManager, "Boss");
                Assert.AreEqual(1, runManager.CurrentRoomChoices.Count);
                Assert.AreEqual(RoomType.BossRoom, runManager.CurrentRoomChoices[0]);

                runManager.AdvanceToSelectedRoom(RoomType.BossRoom);

                Assert.True(runManager.IsRunCompleted);
                AssertActionable(runManager, "Return home");
                Assert.AreEqual(RunEndReason.Victory, RunSessionState.LastEndReason);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void DemoFlowKeepsBlockedShopPurchaseActionableUntilClosed()
        {
            GameObject runObject = CreateRunManagerObject("DemoFlowShopRunManager");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();

                Invoke(runManager, "AdvanceToRoom", RoomType.ShopRoom);
                AssertActionable(runManager, "Shop");

                Assert.True((bool)Invoke(runManager, "OpenShopRewardDraft"));
                runManager.SelectReward(0);

                Assert.True(runManager.IsWaitingForReward);
                Assert.False(runManager.IsWaitingForNextRoom);
                AssertActionable(runManager, "Close shop");

                Invoke(runManager, "CloseShopRewardDraft");

                Assert.False(runManager.IsWaitingForReward);
                Assert.True(runManager.IsWaitingForNextRoom);
                AssertActionable(runManager, "Choose route");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void DemoFlowFlagsMissingRouteExitAsBlocked()
        {
            GameObject runObject = CreateRunManagerObject("DemoFlowBlockedRouteRunManager");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();
                WritePrivateField(runManager, "useRoomChoicePortal", false);
                WritePrivateField(runManager, "allowDebugNextRoomKey", false);

                Invoke(runManager, "AdvanceToRoom", RoomType.SafeRoom);

                Assert.False(ReadProperty<bool>(runManager, "HasActionableDemoNextStep"));
                Assert.That(
                    ReadProperty<string>(runManager, "CurrentDemoFlowLabel"),
                    Does.Contain("Blocked"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void StatusFeedbackUIExposesDemoFlowLine()
        {
            GameObject runObject = CreateRunManagerObject("DemoFlowStatusRunManager");
            GameObject uiObject = new GameObject("DemoFlowStatusUI");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();
                Invoke(runManager, "AdvanceToRoom", RoomType.SafeRoom);

                StatusFeedbackUI ui = uiObject.AddComponent<StatusFeedbackUI>();
                WritePrivateField(ui, "runManager", runManager);

                string line = Invoke<string>(ui, "BuildDemoFlowLine");

                Assert.That(line, Does.Contain("Demo Flow"));
                Assert.That(line, Does.Contain("Rest point"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(uiObject);
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        private static GameObject CreateRunManagerObject(string objectName)
        {
            GameObject runObject = new GameObject(objectName);
            RoomManager roomManager = runObject.AddComponent<RoomManager>();
            WritePrivateField(roomManager, "battleEnemyCount", 0);
            WritePrivateField(roomManager, "eliteEnemyCount", 0);
            WritePrivateField(roomManager, "bossEnemyCount", 0);
            WritePrivateField(roomManager, "logRoomMessages", false);

            RunManager runManager = runObject.AddComponent<RunManager>();
            WritePrivateField(runManager, "roomsToCompleteRun", 4);
            WritePrivateField(runManager, "roomChoiceCount", 4);
            WritePrivateField(runManager, "logRunMessages", false);
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
            return runObject;
        }

        private static void AssertActionable(RunManager runManager, string expectedLabelFragment)
        {
            Assert.True(ReadProperty<bool>(runManager, "HasActionableDemoNextStep"));
            Assert.That(
                ReadProperty<string>(runManager, "CurrentDemoFlowLabel"),
                Does.Contain(expectedLabelFragment));
        }

        private static T Invoke<T>(object target, string methodName, params object[] args)
        {
            object value = Invoke(target, methodName, args);
            return value is T typed ? typed : default;
        }

        private static object Invoke(object target, string methodName, params object[] args)
        {
            MethodInfo method = FindMethod(target.GetType(), methodName, args.Length);
            Assert.NotNull(method, $"{target.GetType().Name}.{methodName} should exist.");
            return method.Invoke(target, args);
        }

        private static MethodInfo FindMethod(Type type, string methodName, int argumentCount)
        {
            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name == methodName && methods[i].GetParameters().Length == argumentCount)
                {
                    return methods[i];
                }
            }

            return null;
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
