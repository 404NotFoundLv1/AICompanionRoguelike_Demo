using System;
using System.Reflection;
using AICompanionRoguelike.Progression;
using AICompanionRoguelike.Roguelike;
using NUnit.Framework;
using UnityEngine;

namespace AICompanionRoguelike.Tests
{
    public sealed class MetaProgressionReadabilityTests
    {
        [SetUp]
        public void SetUp()
        {
            MetaProgressionState.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            MetaProgressionState.Clear();
        }

        [Test]
        public void HomeHudStatusLinesExposeFragmentsAndUpgradeLevels()
        {
            MetaProgressionState.RestoreSnapshot(18, 2, 1, 3);
            Type hudType = RequireRuntimeType("AICompanionRoguelike.Home.HomeMetaProgressionHUD");
            string[] lines = (string[])InvokeStatic(hudType, "BuildStatusLines");
            string joined = string.Join("\n", lines);

            Assert.That(joined, Does.Contain("Core Fragments: 18"));
            Assert.That(joined, Does.Contain("Player Max HP Lv2"));
            Assert.That(joined, Does.Contain("Player Damage Lv1"));
            Assert.That(joined, Does.Contain("AI Support Cooldown Lv3"));
        }

        [Test]
        public void UpgradeStationReportsPurchaseSuccessAndFailure()
        {
            Type stationType = RequireRuntimeType("AICompanionRoguelike.Home.HomeMetaUpgradeStation");
            MetaProgressionState.RestoreSnapshot(6, 0, 0, 0);
            Assert.True(MetaProgressionState.TryPurchaseUpgrade(MetaUpgradeType.PlayerMaxHealth));
            string success = (string)InvokeStatic(
                stationType,
                "BuildPurchaseFeedback",
                MetaUpgradeType.PlayerMaxHealth,
                true);

            MetaProgressionState.RestoreSnapshot(0, 0, 0, 0);
            string failure = (string)InvokeStatic(
                stationType,
                "BuildPurchaseFeedback",
                MetaUpgradeType.PlayerMaxHealth,
                false);

            Assert.That(success, Does.Contain("Purchased"));
            Assert.That(success, Does.Contain("Player Max HP"));
            Assert.That(success, Does.Contain("Lv1"));
            Assert.That(success, Does.Contain("Core Fragments"));
            Assert.That(failure, Does.Contain("Need"));
            Assert.That(failure, Does.Contain("Core Fragments"));
        }

        [Test]
        public void RunStartFeedbackLineListsAppliedMetaLevels()
        {
            MethodInfo method = typeof(RunManager).GetMethod(
                "BuildMetaProgressionAppliedLine",
                BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "RunManager should expose a readable applied meta progression line.");

            string line = (string)method.Invoke(null, new object[] { 2, 1, 3 });

            Assert.That(line, Does.Contain("Permanent Upgrades Applied"));
            Assert.That(line, Does.Contain("HP Lv2"));
            Assert.That(line, Does.Contain("Damage Lv1"));
            Assert.That(line, Does.Contain("AI Cooldown Lv3"));
        }

        private static Type RequireRuntimeType(string fullName)
        {
            Type type = Type.GetType($"{fullName}, Assembly-CSharp");
            Assert.NotNull(type, $"Runtime type {fullName} should exist.");
            return type;
        }

        private static object InvokeStatic(Type type, string methodName, params object[] parameters)
        {
            MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name == methodName && methods[i].GetParameters().Length == parameters.Length)
                {
                    return methods[i].Invoke(null, parameters);
                }
            }

            Assert.Fail($"{type.Name} should expose static {methodName} with {parameters.Length} parameters.");
            return null;
        }
    }
}
