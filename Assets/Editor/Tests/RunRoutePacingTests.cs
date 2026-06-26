using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using AICompanionRoguelike.Roguelike;
using NUnit.Framework;
using UnityEngine;

namespace AICompanionRoguelike.Tests
{
    public sealed class RunRoutePacingTests
    {
        [Test]
        public void RunManagerTracksRouteProgressAndPathLabels()
        {
            GameObject runObject = new GameObject("RunManagerRouteProgressTest");

            try
            {
                runObject.AddComponent<RoomManager>();
                RunManager runManager = runObject.AddComponent<RunManager>();
                WritePrivateField(runManager, "roomsToCompleteRun", 4);

                Invoke(runManager, "AdvanceToRoom", RoomType.BattleRoom);
                Invoke(runManager, "AdvanceToRoom", RoomType.EliteRoom);

                IReadOnlyList<RoomType> route = ReadRouteHistory(runManager);
                string progress = ReadProperty(runManager, "CurrentRouteProgressLabel") as string;
                string path = ReadProperty(runManager, "CurrentRoutePathLabel") as string;

                Assert.AreEqual(2, route.Count);
                Assert.AreEqual(RoomType.BattleRoom, route[0]);
                Assert.AreEqual(RoomType.EliteRoom, route[1]);
                Assert.That(progress, Does.Contain("Room 2/4"));
                Assert.That(progress, Does.Contain("Boss"));
                Assert.That(path, Does.Contain("Battle"));
                Assert.That(path, Does.Contain("Elite"));
                Assert.That(path, Does.Contain("->"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void RouteChoicesAvoidBackToBackSupportRooms()
        {
            GameObject runObject = new GameObject("RunManagerRoutePacingTest");

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

                Invoke(runManager, "AdvanceToRoom", RoomType.SafeRoom);

                Assert.True(runManager.IsWaitingForNextRoom);
                Assert.Greater(runManager.CurrentRoomChoices.Count, 0);

                for (int i = 0; i < runManager.CurrentRoomChoices.Count; i++)
                {
                    Assert.False(
                        IsSupportRoom(runManager.CurrentRoomChoices[i]),
                        $"Support room {runManager.CurrentRoomChoices[i]} should not be offered immediately after a support room.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void RunSessionSummaryRecordsRoutePathForCompletion()
        {
            if (RunSessionState.IsRunActive)
            {
                RunSessionState.EndRun(RunEndReason.ManualReturnHome);
            }

            RunSessionState.EnsureRunStartedFromBattleScene("Assets/Scenes/TestBattleScene.unity");

            MethodInfo recordRoomEntered = typeof(RunSessionState).GetMethod(
                "RecordRoomEntered",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(RoomType), typeof(int) },
                null);
            Assert.NotNull(recordRoomEntered, "RunSessionState should record rooms when they are entered.");

            recordRoomEntered.Invoke(null, new object[] { RoomType.BattleRoom, 1 });
            recordRoomEntered.Invoke(null, new object[] { RoomType.EliteRoom, 2 });
            recordRoomEntered.Invoke(null, new object[] { RoomType.BossRoom, 3 });
            RunSessionState.RecordRoomCleared(RoomType.BossRoom, 3);
            RunSessionState.EndRun(RunEndReason.Victory, 30, 40);

            RunSessionSummary summary = RunSessionState.LastSummary;
            RoomType[] routePath = ReadSummaryRoutePath(summary);
            string routePathLabel = ReadProperty(summary, "RoutePathLabel") as string;

            Assert.AreEqual(3, routePath.Length);
            Assert.AreEqual(RoomType.BattleRoom, routePath[0]);
            Assert.AreEqual(RoomType.EliteRoom, routePath[1]);
            Assert.AreEqual(RoomType.BossRoom, routePath[2]);
            Assert.That(routePathLabel, Does.Contain("Battle"));
            Assert.That(routePathLabel, Does.Contain("Elite"));
            Assert.That(routePathLabel, Does.Contain("Boss"));
            Assert.That(routePathLabel, Does.Contain("->"));
        }

        private static IReadOnlyList<RoomType> ReadRouteHistory(RunManager runManager)
        {
            object value = ReadProperty(runManager, "CurrentRouteHistory");
            IEnumerable enumerable = value as IEnumerable;
            Assert.NotNull(enumerable, "CurrentRouteHistory should be enumerable.");

            List<RoomType> route = new List<RoomType>();
            foreach (object entry in enumerable)
            {
                route.Add((RoomType)entry);
            }

            return route;
        }

        private static RoomType[] ReadSummaryRoutePath(RunSessionSummary summary)
        {
            object value = ReadProperty(summary, "RoutePath");
            RoomType[] routePath = value as RoomType[];
            Assert.NotNull(routePath, "RunSessionSummary.RoutePath should be a RoomType array.");
            return routePath;
        }

        private static bool IsSupportRoom(RoomType roomType)
        {
            return roomType == RoomType.SafeRoom || roomType == RoomType.ShopRoom;
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
