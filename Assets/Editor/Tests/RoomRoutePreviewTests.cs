using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Roguelike;
using NUnit.Framework;
using UnityEngine;

namespace AICompanionRoguelike.Tests
{
    public sealed class RoomRoutePreviewTests
    {
        [Test]
        public void PreparedRoomChoicesExposeReadablePreviewForEachChoice()
        {
            GameObject runObject = new GameObject("RunManagerRoomPreviewTest");

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
                        RoomType.EliteRoom,
                        RoomType.SafeRoom,
                        RoomType.ShopRoom,
                        RoomType.BranchEventRoom
                    });

                Invoke(runManager, "PrepareNextRoomChoices");

                IReadOnlyList<object> previews = ReadPreviewList(runManager);

                Assert.AreEqual(runManager.CurrentRoomChoices.Count, previews.Count);
                Assert.AreEqual(4, previews.Count);
                Assert.True(ContainsPreviewForRoom(previews, RoomType.BattleRoom));
                Assert.True(ContainsPreviewForRoom(previews, RoomType.EliteRoom));
                Assert.True(ContainsPreviewForRoom(previews, RoomType.SafeRoom));
                Assert.True(ContainsPreviewForRoom(previews, RoomType.ShopRoom));

                for (int i = 0; i < previews.Count; i++)
                {
                    object preview = previews[i];
                    Assert.AreEqual(runManager.CurrentRoomChoices[i], ReadProperty(preview, "RoomType"));
                    AssertReadableString(preview, "Title");
                    AssertReadableString(preview, "ThreatPreview");
                    AssertReadableString(preview, "RewardPreview");
                    AssertReadableString(preview, "RouteNote");
                    Assert.AreNotEqual(RoomType.BranchEventRoom, ReadProperty(preview, "RoomType"));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void EliteRoomOffersMoreRewardChoicesThanBattleRoom()
        {
            GameObject runObject = new GameObject("RunManagerEliteRewardPreviewTest");

            try
            {
                runObject.AddComponent<RoomManager>();
                RunManager runManager = runObject.AddComponent<RunManager>();

                Invoke(runManager, "PrepareRewardChoices", RoomType.BattleRoom);
                int battleRewardCount = runManager.CurrentRewardChoices.Count;

                Invoke(runManager, "ClearRewardChoices");
                Invoke(runManager, "PrepareRewardChoices", RoomType.EliteRoom);
                int eliteRewardCount = runManager.CurrentRewardChoices.Count;

                Assert.Greater(eliteRewardCount, battleRewardCount);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void SafeRoomPreviewEffectHealsPlayerOnEntry()
        {
            GameObject runObject = new GameObject("RunManagerSafeRoomHealTest");
            GameObject playerObject = new GameObject("Player");

            try
            {
                runObject.AddComponent<RoomManager>();
                RunManager runManager = runObject.AddComponent<RunManager>();
                HealthComponent health = playerObject.AddComponent<HealthComponent>();
                health.SetMaxHealth(100f, true);
                health.TakeDamage(new DamageInfo(60f, DamageSourceType.Environment, null));

                Invoke(runManager, "AdvanceToRoom", RoomType.SafeRoom);

                Assert.Greater(health.CurrentHealth, 40f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerObject);
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        private static IReadOnlyList<object> ReadPreviewList(RunManager runManager)
        {
            object value = ReadProperty(runManager, "CurrentRoomChoicePreviews");
            IEnumerable enumerable = value as IEnumerable;
            Assert.NotNull(enumerable, "CurrentRoomChoicePreviews should be enumerable.");

            List<object> previews = new List<object>();
            foreach (object preview in enumerable)
            {
                previews.Add(preview);
            }

            return previews;
        }

        private static bool ContainsPreviewForRoom(IReadOnlyList<object> previews, RoomType roomType)
        {
            for (int i = 0; i < previews.Count; i++)
            {
                if ((RoomType)ReadProperty(previews[i], "RoomType") == roomType)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AssertReadableString(object target, string propertyName)
        {
            string value = ReadProperty(target, propertyName) as string;
            Assert.False(string.IsNullOrWhiteSpace(value), $"{propertyName} should be readable.");
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
