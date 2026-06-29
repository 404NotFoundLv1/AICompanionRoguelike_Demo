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
    public sealed class RunRelicSystemTests
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
        public void RelicDefinitionsExposeReadableEffects()
        {
            Type relicType = RequireRuntimeType("AICompanionRoguelike.Roguelike.RunRelicType");
            Type rulesType = RequireRuntimeType("AICompanionRoguelike.Roguelike.RunRelicRules");

            object firstAid = Enum.Parse(relicType, "FirstAidCharm");
            object syncMark = Enum.Parse(relicType, "SyncMark");
            object fieldBackpack = Enum.Parse(relicType, "FieldBackpack");

            AssertReadableRelic(rulesType, firstAid, "First Aid");
            AssertReadableRelic(rulesType, syncMark, "Sync Mark");
            AssertReadableRelic(rulesType, fieldBackpack, "Field Backpack");
        }

        [Test]
        public void AcquiringRelicStoresItOnceAndBuildsReadableSummary()
        {
            Type relicType = RequireRuntimeType("AICompanionRoguelike.Roguelike.RunRelicType");
            object firstAid = Enum.Parse(relicType, "FirstAidCharm");
            GameObject runObject = CreateRunManagerObject("RelicInventoryRunManager");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();

                Assert.True((bool)Invoke(runManager, "AcquireRelic", firstAid));
                Assert.False((bool)Invoke(runManager, "AcquireRelic", firstAid));

                Assert.True((bool)Invoke(runManager, "HasRelic", firstAid));
                Assert.AreEqual(1, CountEnumerable(ReadProperty(runManager, "CurrentRelics") as IEnumerable));
                Assert.That(ReadProperty<string>(runManager, "CurrentRelicSummaryLabel"), Does.Contain("First Aid"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void BattleAndShopRewardDraftsExposeRelicChoices()
        {
            GameObject runObject = CreateRunManagerObject("RelicRewardDraftRunManager");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();

                Invoke(runManager, "PrepareRewardChoices", RoomType.BattleRoom);
                Assert.True(HasRewardCategory(runManager.CurrentRewardChoices, "Relic"));

                Invoke(runManager, "ClearRewardChoices");
                Invoke(runManager, "PrepareRewardChoices", RoomType.ShopRoom);
                Assert.True(HasRewardCategory(runManager.CurrentRewardChoices, "Relic"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void FirstAidCharmHealsPlayerWhenEnteringCombatRoom()
        {
            Type relicType = RequireRuntimeType("AICompanionRoguelike.Roguelike.RunRelicType");
            object firstAid = Enum.Parse(relicType, "FirstAidCharm");
            GameObject runObject = CreateRunManagerObject("FirstAidRelicRunManager");
            GameObject playerObject = CreatePlayer("Player", 40f);

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();
                Assert.True((bool)Invoke(runManager, "AcquireRelic", firstAid));

                Invoke(runManager, "AdvanceToRoom", RoomType.BattleRoom);

                HealthComponent health = playerObject.GetComponent<HealthComponent>();
                Assert.Greater(health.CurrentHealth, 40f);
                Assert.That(ReadProperty<string>(runManager, "LastRelicFeedbackMessage"), Does.Contain("First Aid"));
                Assert.That(ReadProperty<string>(runManager, "LastRoomFeedbackMessage"), Does.Contain("First Aid"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerObject);
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void FieldBackpackAddsExtraSupplyToSafeRoomPrepare()
        {
            Type relicType = RequireRuntimeType("AICompanionRoguelike.Roguelike.RunRelicType");
            object fieldBackpack = Enum.Parse(relicType, "FieldBackpack");
            GameObject runObject = CreateRunManagerObject("FieldBackpackRelicRunManager");
            GameObject playerObject = CreatePlayer("Player", 80f);

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();
                Assert.True((bool)Invoke(runManager, "AcquireRelic", fieldBackpack));

                Invoke(runManager, "AdvanceToRoom", RoomType.SafeRoom);
                Assert.True(runManager.OpenSafeRestDraft());
                runManager.SelectSafeRestChoice(2);

                Assert.AreEqual(2, runManager.CurrentSupplies);
                Assert.That(ReadProperty<string>(runManager, "LastSafeRestFeedbackMessage"), Does.Contain("Field Backpack"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerObject);
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void SyncMarkRelicOnlyBoostsPlayerDamageAgainstMarkedTargets()
        {
            Type rulesType = RequireRuntimeType("AICompanionRoguelike.Roguelike.RunRelicRules");
            Type markerType = RequireRuntimeType("AICompanionRoguelike.Roguelike.RelicSyncMarkTarget");
            GameObject targetObject = new GameObject("SyncMarkedEnemy");

            try
            {
                HealthComponent targetHealth = targetObject.AddComponent<HealthComponent>();
                targetHealth.SetMaxHealth(100f, true);
                DamageInfo damageInfo = new DamageInfo(10f, DamageSourceType.Player, null);

                DamageInfo unmarked = InvokeStatic<DamageInfo>(
                    rulesType,
                    "ModifyPlayerOutgoingDamage",
                    targetHealth,
                    damageInfo,
                    true,
                    1.25f);
                Assert.AreEqual(10f, unmarked.damage, 0.01f);

                object marker = targetObject.AddComponent(markerType);
                Invoke(marker, "MarkByCompanion");

                DamageInfo marked = InvokeStatic<DamageInfo>(
                    rulesType,
                    "ModifyPlayerOutgoingDamage",
                    targetHealth,
                    damageInfo,
                    true,
                    1.25f);
                Assert.AreEqual(12.5f, marked.damage, 0.01f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(targetObject);
            }
        }

        [Test]
        public void CompletedRunSummaryRecordsCollectedRelics()
        {
            Type relicType = RequireRuntimeType("AICompanionRoguelike.Roguelike.RunRelicType");
            object syncMark = Enum.Parse(relicType, "SyncMark");
            GameObject runObject = CreateRunManagerObject("RelicSummaryRunManager");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();
                RunSessionState.EnsureRunStartedFromBattleScene("Assets/Scenes/SampleScene.unity");
                Assert.True((bool)Invoke(runManager, "AcquireRelic", syncMark));

                RunSessionState.EndRun(RunEndReason.Victory);

                RunSessionSummary summary = RunSessionState.LastSummary;
                string[] relicTitles = ReadProperty<string[]>(summary, "RelicTitles");
                Assert.NotNull(relicTitles);
                Assert.AreEqual(1, relicTitles.Length);
                Assert.That(relicTitles[0], Does.Contain("Sync Mark"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        private static void AssertReadableRelic(Type rulesType, object relic, string expectedTitlePart)
        {
            string title = InvokeStatic<string>(rulesType, "GetTitle", relic);
            string description = InvokeStatic<string>(rulesType, "GetDescription", relic);
            string hudLabel = InvokeStatic<string>(rulesType, "GetHudLabel", relic);

            Assert.That(title, Does.Contain(expectedTitlePart));
            Assert.False(string.IsNullOrWhiteSpace(description));
            Assert.False(string.IsNullOrWhiteSpace(hudLabel));
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

        private static bool HasRewardCategory(IEnumerable<RunRewardChoice> rewards, string expectedCategoryLabel)
        {
            foreach (RunRewardChoice reward in rewards)
            {
                if (reward.CategoryLabel == expectedCategoryLabel)
                {
                    return true;
                }
            }

            return false;
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

        private static T InvokeStatic<T>(Type type, string methodName, params object[] args)
        {
            MethodInfo method = FindMethod(type, methodName, BindingFlags.Public | BindingFlags.Static, args.Length);
            Assert.NotNull(method, $"{type.Name}.{methodName} should exist.");
            return (T)method.Invoke(null, args);
        }

        private static object Invoke(object target, string methodName, params object[] args)
        {
            MethodInfo method = FindMethod(target.GetType(), methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, args.Length);
            Assert.NotNull(method, $"{target.GetType().Name}.{methodName} should exist.");
            return method.Invoke(target, args);
        }

        private static MethodInfo FindMethod(Type type, string methodName, BindingFlags flags, int argumentCount)
        {
            MethodInfo[] methods = type.GetMethods(flags);
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
