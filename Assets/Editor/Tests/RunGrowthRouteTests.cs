using System;
using System.Reflection;
using AICompanionRoguelike.Character;
using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Memory;
using AICompanionRoguelike.Roguelike;
using AICompanionRoguelike.UI;
using NUnit.Framework;
using UnityEngine;

namespace AICompanionRoguelike.Tests
{
    public sealed class RunGrowthRouteTests
    {
        [Test]
        public void TwoRewardsInOneCategoryActivateGrowthRouteAndCompanionFeedback()
        {
            GameObject player = CreatePlayer("Player");
            GameObject runObject = CreateRunManagerObject("GrowthRouteActivationRunManager");
            GameObject companionObject = new GameObject("GrowthRouteCompanionBubble");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();
                CompanionSpeechBubbleUI bubble = companionObject.AddComponent<CompanionSpeechBubbleUI>();

                Invoke(runManager, "ApplyReward", ParseRewardType("DashCooldown"));
                Invoke(runManager, "ApplyReward", ParseRewardType("RecoveryWindow"));

                string routeLabel = ReadStringProperty(runManager, "CurrentGrowthRouteLabel");
                Assert.That(routeLabel, Does.Contain("Counterplay"));
                Assert.That(routeLabel, Does.Contain("Lv2"));
                Assert.That(bubble.CurrentMessage, Does.Contain("Counterplay"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(companionObject);
                UnityEngine.Object.DestroyImmediate(runObject);
                UnityEngine.Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void ActiveRouteBiasesNextRewardChoicesTowardSameCategory()
        {
            GameObject player = CreatePlayer("Player");
            GameObject companion = CreateCompanion("GrowthRouteCompanion");
            GameObject runObject = CreateRunManagerObject("GrowthRouteRewardBiasRunManager");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();

                Invoke(runManager, "ApplyReward", ParseRewardType("DashCooldown"));
                Invoke(runManager, "ApplyReward", ParseRewardType("RecoveryWindow"));
                Invoke(runManager, "PrepareRewardChoices", RoomType.BattleRoom);

                Assert.That(runManager.CurrentRewardChoices.Count, Is.GreaterThan(0));
                Assert.AreEqual("Counterplay", runManager.CurrentRewardChoices[0].CategoryLabel);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
                UnityEngine.Object.DestroyImmediate(companion);
                UnityEngine.Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void StrongerCategoryCanReplaceActiveGrowthRoute()
        {
            GameObject player = CreatePlayer("Player");
            GameObject runObject = CreateRunManagerObject("GrowthRouteSwitchRunManager");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();

                Invoke(runManager, "ApplyReward", ParseRewardType("DashCooldown"));
                Invoke(runManager, "ApplyReward", ParseRewardType("RecoveryWindow"));
                Assert.That(ReadStringProperty(runManager, "CurrentGrowthRouteLabel"), Does.Contain("Counterplay"));

                Invoke(runManager, "ApplyReward", ParseRewardType("PlayerDamage"));
                Invoke(runManager, "ApplyReward", ParseRewardType("MoveSpeed"));
                Invoke(runManager, "ApplyReward", ParseRewardType("PlayerDamage"));

                string routeLabel = ReadStringProperty(runManager, "CurrentGrowthRouteLabel");
                Assert.That(routeLabel, Does.Contain("Player"));
                Assert.That(routeLabel, Does.Contain("Lv3"));
            }
            finally
            {
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

        private static GameObject CreateCompanion(string objectName)
        {
            GameObject companion = new GameObject(objectName);
            companion.AddComponent<HealthComponent>().SetMaxHealth(80f, true);
            companion.AddComponent<AICompanionRoguelike.Companion.CompanionSensor>();
            companion.AddComponent<AICompanionRoguelike.Companion.CompanionCombat>();
            return companion;
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

        private static string ReadStringProperty(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(property, $"{target.GetType().Name} should expose {propertyName}.");
            return (string)property.GetValue(target);
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
