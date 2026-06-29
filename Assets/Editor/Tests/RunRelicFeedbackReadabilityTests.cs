using System;
using System.Reflection;
using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Roguelike;
using NUnit.Framework;
using UnityEngine;

namespace AICompanionRoguelike.Tests
{
    public sealed class RunRelicFeedbackReadabilityTests
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
        public void RelicRewardChoicesUseStrongReadableDisplayLabel()
        {
            RunRewardChoice relicChoice = new RunRewardChoice(
                RunRewardType.RelicFirstAidCharm,
                "First Aid Charm",
                "Entering combat rooms restores HP.",
                RunRewardCategory.Relic,
                "Relic preview",
                "relic-firstaid");

            Assert.AreEqual("Relic", relicChoice.CategoryLabel);
            Assert.AreEqual("RELIC", ReadProperty<string>(relicChoice, "DisplayCategoryLabel"));

            Type rulesType = typeof(RunRelicRules);
            string prefix = InvokeStatic<string>(rulesType, "GetChoicePrefix", RunRelicType.FirstAidCharm);
            Assert.AreEqual("RELIC", prefix);
        }

        [Test]
        public void AcquiringRelicCreatesShortPickupBanner()
        {
            GameObject runObject = CreateRunManagerObject("RelicPickupFeedbackRunManager");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();

                Assert.True(runManager.AcquireRelic(RunRelicType.FirstAidCharm));

                string banner = ReadProperty<string>(runManager, "LastRelicBannerMessage");
                Assert.That(banner, Does.Contain("Relic acquired"));
                Assert.That(banner, Does.Contain("First Aid"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void FirstAidCharmEntryEffectCreatesShortEffectBanner()
        {
            GameObject runObject = CreateRunManagerObject("FirstAidReadableFeedbackRunManager");
            GameObject playerObject = CreatePlayer("Player", 40f);

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();
                Assert.True(runManager.AcquireRelic(RunRelicType.FirstAidCharm));

                Invoke(runManager, "AdvanceToRoom", RoomType.BattleRoom);

                string banner = ReadProperty<string>(runManager, "LastRelicBannerMessage");
                Assert.That(banner, Does.Contain("First Aid"));
                Assert.That(banner, Does.Contain("+"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerObject);
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void FieldBackpackPrepareEffectCreatesShortEffectBanner()
        {
            GameObject runObject = CreateRunManagerObject("FieldBackpackReadableFeedbackRunManager");
            GameObject playerObject = CreatePlayer("Player", 80f);

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();
                Assert.True(runManager.AcquireRelic(RunRelicType.FieldBackpack));

                Invoke(runManager, "AdvanceToRoom", RoomType.SafeRoom);
                Assert.True(runManager.OpenSafeRestDraft());
                runManager.SelectSafeRestChoice(2);

                string banner = ReadProperty<string>(runManager, "LastRelicBannerMessage");
                Assert.That(banner, Does.Contain("Field Backpack"));
                Assert.That(banner, Does.Contain("+1"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerObject);
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void SyncMarkCreatesVisibleChildMarkerWhenMarked()
        {
            GameObject targetObject = new GameObject("SyncMarkVisualEnemy");

            try
            {
                RelicSyncMarkTarget marker = targetObject.AddComponent<RelicSyncMarkTarget>();

                marker.MarkByCompanion();

                Transform visual = targetObject.transform.Find("SyncMarkVisual");
                Assert.NotNull(visual);
                Assert.True(visual.gameObject.activeSelf);
                Assert.NotNull(visual.GetComponent<SpriteRenderer>());

                marker.ClearMark();
                Assert.False(visual.gameObject.activeSelf);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(targetObject);
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
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(property, $"{target.GetType().Name}.{propertyName} should exist.");
            object value = property.GetValue(target);
            return value is T typed ? typed : default;
        }
    }
}
