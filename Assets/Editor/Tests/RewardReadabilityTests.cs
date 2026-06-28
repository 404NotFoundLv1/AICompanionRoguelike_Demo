using System;
using System.Collections.Generic;
using System.Reflection;
using AICompanionRoguelike.Character;
using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Memory;
using AICompanionRoguelike.Roguelike;
using NUnit.Framework;
using UnityEngine;

namespace AICompanionRoguelike.Tests
{
    public sealed class RewardReadabilityTests
    {
        [Test]
        public void RewardChoicesExposeCategoryLabelsAndValuePreviews()
        {
            GameObject player = CreatePlayer("Player");
            GameObject runObject = CreateRunManagerObject("RewardReadabilityChoiceRunManager");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();

                AssertChoiceHasReadableData(runManager, "PlayerDamage", "Player", "Damage", "->");
                AssertChoiceHasReadableData(runManager, "DashCooldown", "Counterplay", "Dash CD", "->");
                AssertChoiceHasReadableData(runManager, "RecoveryWindow", "Counterplay", "Recovery", "->");
                AssertChoiceHasReadableData(runManager, "BondRescueHealth", "Survival", "Rescue HP", "->");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
                UnityEngine.Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void PreparedBattleRewardsContainCategoryMix()
        {
            GameObject player = CreatePlayer("Player");
            GameObject companion = CreateCompanion("RewardReadabilityCompanion");
            GameObject runObject = CreateRunManagerObject("RewardReadabilityPreparedRunManager");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();

                Invoke(runManager, "PrepareRewardChoices", RoomType.BattleRoom);

                HashSet<string> categories = new HashSet<string>();
                bool hasCounterplay = false;
                foreach (RunRewardChoice reward in runManager.CurrentRewardChoices)
                {
                    string category = ReadStringProperty(reward, "CategoryLabel");
                    string preview = ReadStringProperty(reward, "PreviewLine");
                    categories.Add(category);
                    hasCounterplay |= category == "Counterplay";
                    Assert.That(preview, Is.Not.Empty, $"{reward.RewardType} should show a preview line.");
                }

                Assert.That(runManager.CurrentRewardChoices.Count, Is.GreaterThanOrEqualTo(3));
                Assert.That(hasCounterplay, Is.True, "Battle rewards should always include at least one Counterplay option.");
                Assert.That(categories.Count, Is.GreaterThanOrEqualTo(2), "Battle rewards should not all come from one category.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
                UnityEngine.Object.DestroyImmediate(companion);
                UnityEngine.Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void ApplyingRewardsUpdatesRunGrowthSummary()
        {
            GameObject player = CreatePlayer("Player");
            GameObject runObject = CreateRunManagerObject("RewardReadabilityGrowthRunManager");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();

                Invoke(runManager, "ApplyReward", ParseRewardType("PlayerDamage"));
                Invoke(runManager, "ApplyReward", ParseRewardType("DashCooldown"));
                string summary = ReadStringProperty(runManager, "CurrentGrowthSummaryLabel");

                Assert.That(summary, Does.Contain("Player 1"));
                Assert.That(summary, Does.Contain("Counterplay 1"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
                UnityEngine.Object.DestroyImmediate(player);
            }
        }

        private static void AssertChoiceHasReadableData(
            RunManager runManager,
            string rewardName,
            string expectedCategory,
            string expectedPreviewPrefix,
            string expectedPreviewMarker)
        {
            RunRewardChoice choice = (RunRewardChoice)Invoke(runManager, "CreateRewardChoice", ParseRewardType(rewardName));

            Assert.AreEqual(expectedCategory, ReadStringProperty(choice, "CategoryLabel"));
            Assert.That(ReadStringProperty(choice, "PreviewLine"), Does.Contain(expectedPreviewPrefix));
            Assert.That(ReadStringProperty(choice, "PreviewLine"), Does.Contain(expectedPreviewMarker));
            Assert.That(ReadStringProperty(choice, "GrowthTag"), Is.Not.Empty);
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
