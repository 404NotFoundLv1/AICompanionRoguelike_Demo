using System;
using System.Reflection;
using AICompanionRoguelike.Home;
using AICompanionRoguelike.Memory;
using AICompanionRoguelike.Roguelike;
using NUnit.Framework;
using UnityEngine;

namespace AICompanionRoguelike.Tests
{
    public sealed class CompanionRelationshipStateTests
    {
        [SetUp]
        public void SetUp()
        {
            ClearRelationshipStateIfAvailable();
        }

        [TearDown]
        public void TearDown()
        {
            ClearRelationshipStateIfAvailable();
        }

        [Test]
        public void HomeDialogueChoiceCarriesRelationshipIntoNextBattleRelationship()
        {
            GameObject homeCompanion = new GameObject("HomeCompanionUnderTest");

            try
            {
                CompanionRelationship homeRelationship = homeCompanion.AddComponent<CompanionRelationship>();
                HomeCompanionDialogue dialogue = homeCompanion.AddComponent<HomeCompanionDialogue>();
                dialogue.Configure(homeRelationship);

                RunSessionState.StartRunFromHome("Assets/Scenes/SampleScene.unity");
                RunSessionState.EndRun(RunEndReason.Victory, finalTrust: 64, finalAffection: 58);

                dialogue.ApplyDialogueChoice(HomeCompanionDialogueChoice.DiscussTactics);

                Assert.AreEqual(68, homeRelationship.Trust);
                Assert.AreEqual(59, homeRelationship.Affection);
                Assert.AreEqual(1, homeRelationship.GetMemoryTagScore(RelationshipMemoryTag.Reliable));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(homeCompanion);
            }

            GameObject battleCompanion = new GameObject("BattleCompanionUnderTest");

            try
            {
                CompanionRelationship battleRelationship = battleCompanion.AddComponent<CompanionRelationship>();
                InvokeLifecycle(battleRelationship, "Awake");

                Assert.AreEqual(68, battleRelationship.Trust);
                Assert.AreEqual(59, battleRelationship.Affection);
                Assert.AreEqual(1, battleRelationship.GetMemoryTagScore(RelationshipMemoryTag.Reliable));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(battleCompanion);
            }
        }

        [Test]
        public void BattleRelationshipChangeCarriesBackIntoNextHomeRelationship()
        {
            GameObject firstHomeCompanion = new GameObject("FirstHomeCompanionUnderTest");

            try
            {
                firstHomeCompanion.AddComponent<CompanionRelationship>();
                HomeCompanionDialogue dialogue = firstHomeCompanion.AddComponent<HomeCompanionDialogue>();
                dialogue.Configure(firstHomeCompanion.GetComponent<CompanionRelationship>());

                RunSessionState.StartRunFromHome("Assets/Scenes/SampleScene.unity");
                RunSessionState.EndRun(RunEndReason.Victory, finalTrust: 64, finalAffection: 58);

                dialogue.ApplyDialogueChoice(HomeCompanionDialogueChoice.ThankSupport);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(firstHomeCompanion);
            }

            GameObject battleCompanion = new GameObject("BattleCompanionUnderTest");

            try
            {
                CompanionRelationship battleRelationship = battleCompanion.AddComponent<CompanionRelationship>();
                InvokeLifecycle(battleRelationship, "Awake");
                battleRelationship.ApplyMemoryEvent("Boss Follow-up", 2, 1, RelationshipMemoryTag.Brave);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(battleCompanion);
            }

            GameObject nextHomeCompanion = new GameObject("NextHomeCompanionUnderTest");

            try
            {
                CompanionRelationship nextHomeRelationship = nextHomeCompanion.AddComponent<CompanionRelationship>();
                InvokeLifecycle(nextHomeRelationship, "Awake");

                Assert.AreEqual(67, nextHomeRelationship.Trust);
                Assert.AreEqual(63, nextHomeRelationship.Affection);
                Assert.AreEqual(1, nextHomeRelationship.GetMemoryTagScore(RelationshipMemoryTag.Protected));
                Assert.AreEqual(1, nextHomeRelationship.GetMemoryTagScore(RelationshipMemoryTag.Brave));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(nextHomeCompanion);
            }
        }

        private static void ClearRelationshipStateIfAvailable()
        {
            Type stateType = Type.GetType("AICompanionRoguelike.Memory.CompanionRelationshipState, Assembly-CSharp");
            MethodInfo clearMethod = stateType?.GetMethod("Clear", BindingFlags.Public | BindingFlags.Static);
            clearMethod?.Invoke(null, Array.Empty<object>());
        }

        private static void InvokeLifecycle(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method, $"{target.GetType().Name} should expose lifecycle method {methodName}.");
            method.Invoke(target, Array.Empty<object>());
        }
    }
}
