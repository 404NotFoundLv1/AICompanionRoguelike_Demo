using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using AICompanionRoguelike.Roguelike;
using NUnit.Framework;
using UnityEngine;

namespace AICompanionRoguelike.Tests
{
    public sealed class RunRouteMapTests
    {
        [Test]
        public void RouteMapSnapshotListsCurrentNodeNextChoicesAndBossEndpoint()
        {
            GameObject runObject = new GameObject("RunManagerRouteMapTest");

            try
            {
                runObject.AddComponent<RoomManager>();
                RunManager runManager = runObject.AddComponent<RunManager>();
                WritePrivateField(runManager, "roomChoiceCount", 3);
                WritePrivateField(runManager, "roomsToCompleteRun", 4);
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

                Invoke(runManager, "AdvanceToRoom", RoomType.BattleRoom);
                Invoke(runManager, "PrepareNextRoomChoices");

                IReadOnlyList<object> nodes = ReadRouteMapNodes(runManager, "CurrentRouteMapNodes");

                AssertHasNode(nodes, RoomType.BattleRoom, "IsCurrent", true);
                AssertHasNode(nodes, RoomType.SafeRoom, "IsNextChoice", true);
                AssertHasNode(nodes, RoomType.ShopRoom, "IsNextChoice", true);
                AssertHasNode(nodes, RoomType.BossRoom, "IsBossEndpoint", true);
                AssertHasNoNode(nodes, RoomType.BranchEventRoom);
                AssertNodeStep(nodes, RoomType.BossRoom, "IsBossEndpoint", true, 4);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void RouteMapLabelNamesHistoryChoicesAndBoss()
        {
            GameObject runObject = new GameObject("RunManagerRouteMapLabelTest");

            try
            {
                runObject.AddComponent<RoomManager>();
                RunManager runManager = runObject.AddComponent<RunManager>();
                WritePrivateField(runManager, "roomChoiceCount", 3);
                WritePrivateField(runManager, "roomsToCompleteRun", 4);

                Invoke(runManager, "AdvanceToRoom", RoomType.BattleRoom);
                Invoke(runManager, "PrepareNextRoomChoices");

                string label = ReadProperty(runManager, "CurrentRouteMapLabel") as string;

                Assert.That(label, Does.Contain("Map"));
                Assert.That(label, Does.Contain("Battle"));
                Assert.That(label, Does.Contain("Next"));
                Assert.That(label, Does.Contain("Boss"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void NextRoomChoicePortalCachesRouteMapSnapshotWhenChoicesArePrepared()
        {
            GameObject runObject = new GameObject("RunManagerRouteMapPortalTest");
            GameObject portalObject = new GameObject("NextRoomChoicePortalRouteMapTest");

            try
            {
                runObject.AddComponent<RoomManager>();
                RunManager runManager = runObject.AddComponent<RunManager>();
                WritePrivateField(runManager, "roomChoiceCount", 3);
                WritePrivateField(runManager, "roomsToCompleteRun", 4);

                portalObject.AddComponent<BoxCollider2D>();
                NextRoomChoicePortal portal = portalObject.AddComponent<NextRoomChoicePortal>();
                WritePrivateField(portal, "runManager", runManager);

                Invoke(runManager, "AdvanceToRoom", RoomType.BattleRoom);
                Invoke(runManager, "PrepareNextRoomChoices");
                Invoke(portal, "HandleRoomChoicesPrepared", runManager, runManager.CurrentRoomChoices);

                IReadOnlyList<object> nodes = ReadRouteMapNodes(portal, "OfferedRouteMapNodes");

                AssertHasNode(nodes, RoomType.BattleRoom, "IsCurrent", true);
                AssertHasNode(nodes, RoomType.BossRoom, "IsBossEndpoint", true);
                AssertHasNode(nodes, RoomType.SafeRoom, "IsNextChoice", true);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(portalObject);
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        private static IReadOnlyList<object> ReadRouteMapNodes(object target, string propertyName)
        {
            object value = ReadProperty(target, propertyName);
            IEnumerable enumerable = value as IEnumerable;
            Assert.NotNull(enumerable, $"{propertyName} should be enumerable.");

            List<object> nodes = new List<object>();
            foreach (object node in enumerable)
            {
                nodes.Add(node);
            }

            Assert.Greater(nodes.Count, 0, $"{propertyName} should contain route map nodes.");
            return nodes;
        }

        private static void AssertHasNode(
            IReadOnlyList<object> nodes,
            RoomType roomType,
            string flagPropertyName,
            bool flagValue)
        {
            Assert.True(
                TryFindNode(nodes, roomType, flagPropertyName, flagValue, out _),
                $"Expected a {roomType} route map node with {flagPropertyName}={flagValue}.");
        }

        private static void AssertHasNoNode(IReadOnlyList<object> nodes, RoomType roomType)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                Assert.AreNotEqual(roomType, ReadProperty(nodes[i], "RoomType"));
            }
        }

        private static void AssertNodeStep(
            IReadOnlyList<object> nodes,
            RoomType roomType,
            string flagPropertyName,
            bool flagValue,
            int expectedStep)
        {
            Assert.True(
                TryFindNode(nodes, roomType, flagPropertyName, flagValue, out object node),
                $"Expected a {roomType} route map node with {flagPropertyName}={flagValue}.");
            Assert.AreEqual(expectedStep, ReadProperty(node, "StepNumber"));
        }

        private static bool TryFindNode(
            IReadOnlyList<object> nodes,
            RoomType roomType,
            string flagPropertyName,
            bool flagValue,
            out object foundNode)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                object node = nodes[i];
                if ((RoomType)ReadProperty(node, "RoomType") != roomType)
                {
                    continue;
                }

                if ((bool)ReadProperty(node, flagPropertyName) != flagValue)
                {
                    continue;
                }

                string label = ReadProperty(node, "Label") as string;
                Assert.False(string.IsNullOrWhiteSpace(label), "Route map node label should be readable.");
                foundNode = node;
                return true;
            }

            foundNode = null;
            return false;
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
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(method, $"{target.GetType().Name} should expose {methodName}.");
            return method.Invoke(target, parameters);
        }
    }
}
