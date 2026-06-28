using System;
using System.Reflection;
using AICompanionRoguelike.Character;
using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Memory;
using AICompanionRoguelike.Roguelike;
using NUnit.Framework;
using UnityEngine;

namespace AICompanionRoguelike.Tests
{
    public sealed class RouteExclusiveRewardTests
    {
        [Test]
        public void ActiveRouteOffersExclusiveRewardBeforeNormalRouteReward()
        {
            GameObject player = CreatePlayer("Player");
            GameObject runObject = CreateRunManagerObject("RouteExclusiveRewardDraftRunManager");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();

                Invoke(runManager, "ApplyReward", ParseRewardType("PlayerDamage"));
                Invoke(runManager, "ApplyReward", ParseRewardType("MoveSpeed"));
                Invoke(runManager, "PrepareRewardChoices", RoomType.BattleRoom);

                Assert.That(runManager.CurrentRewardChoices.Count, Is.GreaterThan(0));
                RunRewardChoice firstChoice = runManager.CurrentRewardChoices[0];
                Assert.AreEqual("GrowthRouteSpecialization", firstChoice.RewardType.ToString());
                Assert.AreEqual("Player", firstChoice.CategoryLabel);
                Assert.That(firstChoice.Title, Does.Contain("Player"));
                Assert.That(firstChoice.PreviewLine, Does.Contain("Damage"));
                Assert.That(firstChoice.GrowthTag, Does.Contain("route-special"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
                UnityEngine.Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void SelectingExclusiveRewardDeepensCurrentRouteCombatEffect()
        {
            GameObject player = CreatePlayer("Player");
            GameObject runObject = CreateRunManagerObject("RouteExclusiveRewardEffectRunManager");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();

                Invoke(runManager, "ApplyReward", ParseRewardType("PlayerDamage"));
                Invoke(runManager, "ApplyReward", ParseRewardType("MoveSpeed"));
                float beforeMultiplier = ReadFloatProperty(runManager, "PlayerRouteDamageMultiplier");

                Invoke(runManager, "ApplyReward", ParseRewardType("GrowthRouteSpecialization"));

                Assert.That(ReadIntProperty(runManager, "CurrentGrowthRouteSpecializationCount"), Is.EqualTo(1));
                Assert.That(ReadFloatProperty(runManager, "PlayerRouteDamageMultiplier"), Is.GreaterThan(beforeMultiplier));
                Assert.That(runManager.CurrentGrowthRouteLabel, Does.Contain("Special x1"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
                UnityEngine.Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void VictorySummaryRecordsRouteIdentityEffectAndExclusiveCount()
        {
            if (RunSessionState.IsRunActive)
            {
                RunSessionState.EndRun(RunEndReason.ManualReturnHome);
            }

            GameObject player = CreatePlayer("Player");
            GameObject runObject = CreateRunManagerObject("RouteExclusiveSummaryRunManager");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();
                RunSessionState.EnsureRunStartedFromBattleScene("Assets/Scenes/TestBattleScene.unity");

                Invoke(runManager, "ApplyReward", ParseRewardType("PlayerDamage"));
                Invoke(runManager, "ApplyReward", ParseRewardType("MoveSpeed"));
                Invoke(runManager, "ApplyReward", ParseRewardType("GrowthRouteSpecialization"));
                Invoke(runManager, "CompleteRun", RoomType.BossRoom, 4);

                RunSessionSummary summary = RunSessionState.LastSummary;
                Assert.That(ReadBoolProperty(summary, "HasGrowthRouteSummary"), Is.True);
                Assert.That(ReadStringProperty(summary, "GrowthRouteLabel"), Does.Contain("Player"));
                Assert.That(ReadStringProperty(summary, "GrowthRouteEffectLabel"), Does.Contain("Damage"));
                Assert.That(ReadIntProperty(summary, "GrowthRouteSpecializationCount"), Is.EqualTo(1));
                Assert.That(ReadStringProperty(summary, "GrowthRouteSummaryLine"), Does.Contain("Special x1"));
            }
            finally
            {
                if (RunSessionState.IsRunActive)
                {
                    RunSessionState.EndRun(RunEndReason.ManualReturnHome);
                }

                UnityEngine.Object.DestroyImmediate(runObject);
                UnityEngine.Object.DestroyImmediate(player);
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
            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            player.AddComponent<BoxCollider2D>().size = new Vector2(0.8f, 1.6f);
            player.AddComponent<PlayerInputReader>();
            player.AddComponent<HealthComponent>().SetMaxHealth(100f, true);
            player.AddComponent<PlayerMovement2D>();
            player.AddComponent<PlayerCombat2D>();
            player.AddComponent<PlayerCounterplayFeedback>();
            player.AddComponent<BondRescueSystem>();
            return player;
        }

        private static object ParseRewardType(string rewardName)
        {
            Type rewardType = RequireRuntimeType("AICompanionRoguelike.Roguelike.RunRewardType");
            return Enum.Parse(rewardType, rewardName);
        }

        private static Type RequireRuntimeType(string fullName)
        {
            Type type = Type.GetType($"{fullName}, Assembly-CSharp");
            Assert.NotNull(type, $"Runtime type {fullName} should exist.");
            return type;
        }

        private static bool ReadBoolProperty(object target, string propertyName)
        {
            PropertyInfo property = GetPublicProperty(target, propertyName);
            return (bool)property.GetValue(target);
        }

        private static float ReadFloatProperty(object target, string propertyName)
        {
            PropertyInfo property = GetPublicProperty(target, propertyName);
            return (float)property.GetValue(target);
        }

        private static int ReadIntProperty(object target, string propertyName)
        {
            PropertyInfo property = GetPublicProperty(target, propertyName);
            return (int)property.GetValue(target);
        }

        private static string ReadStringProperty(object target, string propertyName)
        {
            PropertyInfo property = GetPublicProperty(target, propertyName);
            return (string)property.GetValue(target);
        }

        private static PropertyInfo GetPublicProperty(object target, string propertyName)
        {
            Type type = target.GetType();
            PropertyInfo property = type.GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(property, $"{type.Name} should expose {propertyName}.");
            return property;
        }

        private static object Invoke(object target, string methodName, params object[] parameters)
        {
            MethodInfo[] methods = target.GetType().GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name == methodName
                    && methods[i].GetParameters().Length == parameters.Length)
                {
                    return methods[i].Invoke(target, parameters);
                }
            }

            Assert.Fail($"{target.GetType().Name} should expose {methodName} with {parameters.Length} parameters.");
            return null;
        }
    }
}
