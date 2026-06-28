using System;
using System.Reflection;
using AICompanionRoguelike.Companion;
using AICompanionRoguelike.Home;
using AICompanionRoguelike.Memory;
using AICompanionRoguelike.Progression;
using NUnit.Framework;
using UnityEngine;

namespace AICompanionRoguelike.Tests
{
    public sealed class HomeExpeditionPreparationTests
    {
        [SetUp]
        public void SetUp()
        {
            MetaProgressionState.Clear();
            CompanionRelationshipState.Clear();
            CompanionRunBuildState.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            MetaProgressionState.Clear();
            CompanionRelationshipState.Clear();
            CompanionRunBuildState.Reset();
        }

        [Test]
        public void PreparationLinesShowPermanentUpgradeLevels()
        {
            MetaProgressionState.RestoreSnapshot(12, 2, 1, 3);

            string[] lines = (string[])InvokeStatic(
                typeof(HomeExitPortal),
                "BuildPreparationLines");
            string joined = string.Join("\n", lines);

            Assert.That(joined, Does.Contain("Expedition Preparation"));
            Assert.That(joined, Does.Contain("HP Lv2"));
            Assert.That(joined, Does.Contain("Damage Lv1"));
            Assert.That(joined, Does.Contain("AI Cooldown Lv3"));
        }

        [Test]
        public void CompanionReadinessLineShowsRelationshipMemoryAndBuild()
        {
            CompanionRelationshipState.RestoreSnapshot(
                74,
                72,
                new[]
                {
                    new RelationshipMemoryTagScore
                    {
                        tag = RelationshipMemoryTag.Brave,
                        score = 4
                    }
                });
            CompanionRunBuildState.SetTendency(CompanionSkillTendency.Link);

            string line = (string)InvokeStatic(
                typeof(HomeExitPortal),
                "BuildCompanionReadinessLine");

            Assert.That(line, Does.Contain("Synchronized"));
            Assert.That(line, Does.Contain("Trust 74"));
            Assert.That(line, Does.Contain("Affection 72"));
            Assert.That(line, Does.Contain("Brave x4"));
            Assert.That(line, Does.Contain("Link"));
        }

        [Test]
        public void PortalCanOpenAndCancelPreparationWithoutTransitioning()
        {
            GameObject portalObject = new GameObject("ExpeditionPrepPortal");
            portalObject.AddComponent<BoxCollider2D>();
            HomeExitPortal portal = portalObject.AddComponent<HomeExitPortal>();

            try
            {
                portal.OpenPreparationPanel();
                Assert.True(portal.IsPreparationOpen);
                Assert.False(portal.IsTransitioning);

                portal.CancelPreparation();
                Assert.False(portal.IsPreparationOpen);
                Assert.False(portal.IsTransitioning);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(portalObject);
            }
        }

        [Test]
        public void OpeningPreparationStartsFreshGuardianTactic()
        {
            CompanionRunBuildState.SetTendency(CompanionSkillTendency.Link);
            CompanionRunBuildState.AddUpgrade(CompanionSkillTendency.Link);

            GameObject portalObject = new GameObject("ExpeditionPrepPortal");
            portalObject.AddComponent<BoxCollider2D>();
            HomeExitPortal portal = portalObject.AddComponent<HomeExitPortal>();

            try
            {
                portal.OpenPreparationPanel();

                Assert.True(portal.IsPreparationOpen);
                Assert.AreEqual(CompanionSkillTendency.Guardian, CompanionRunBuildState.CurrentTendency);
                Assert.AreEqual(0, CompanionRunBuildState.GetUpgradeLevel(CompanionSkillTendency.Link));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(portalObject);
            }
        }

        [Test]
        public void PreparationTacticSelectionUpdatesRunBuildStateAndPanelText()
        {
            GameObject portalObject = new GameObject("ExpeditionPrepPortal");
            portalObject.AddComponent<BoxCollider2D>();
            HomeExitPortal portal = portalObject.AddComponent<HomeExitPortal>();

            try
            {
                portal.OpenPreparationPanel();
                InvokeInstance(portal, "SelectPreparationTendency", CompanionSkillTendency.Link);

                Assert.AreEqual(CompanionSkillTendency.Link, CompanionRunBuildState.CurrentTendency);

                string[] lines = (string[])InvokeStatic(
                    typeof(HomeExitPortal),
                    "BuildTacticalChoiceLines");
                string joined = string.Join("\n", lines);

                Assert.That(joined, Does.Contain("Current AI Tactic: Link"));
                Assert.That(joined, Does.Contain("[1] Guardian"));
                Assert.That(joined, Does.Contain("[2] Suppressor"));
                Assert.That(joined, Does.Contain("[3] Link"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(portalObject);
            }
        }

        [Test]
        public void PreparationLinesExposeTacticalChoices()
        {
            CompanionRunBuildState.SetTendency(CompanionSkillTendency.Suppressor);

            string[] lines = (string[])InvokeStatic(
                typeof(HomeExitPortal),
                "BuildPreparationLines");
            string joined = string.Join("\n", lines);

            Assert.That(joined, Does.Contain("AI Tactic Choices"));
            Assert.That(joined, Does.Contain("Current AI Tactic: Suppressor"));
            Assert.That(joined, Does.Contain("[1] Guardian"));
            Assert.That(joined, Does.Contain("[2] Suppressor"));
            Assert.That(joined, Does.Contain("[3] Link"));
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

        private static object InvokeInstance(object target, string methodName, params object[] parameters)
        {
            MethodInfo[] methods = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name == methodName && methods[i].GetParameters().Length == parameters.Length)
                {
                    return methods[i].Invoke(target, parameters);
                }
            }

            Assert.Fail($"{target.GetType().Name} should expose instance {methodName} with {parameters.Length} parameters.");
            return null;
        }
    }
}
