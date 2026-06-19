using System;
using System.Reflection;
using AICompanionRoguelike.Character;
using AICompanionRoguelike.Memory;
using AICompanionRoguelike.Roguelike;
using NUnit.Framework;
using UnityEngine;

namespace AICompanionRoguelike.Tests
{
    public sealed class HomeCompanionDialogueTests
    {
        [SetUp]
        public void SetUp()
        {
            CompanionRelationshipState.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            CompanionRelationshipState.Clear();
        }

        [Test]
        public void DialogueTextUsesLastRunCompanionFeedbackAndBond()
        {
            GameObject owner = new GameObject("HomeCompanionDialogueUnderTest");

            try
            {
                RunSessionState.StartRunFromHome("Assets/Scenes/SampleScene.unity");
                RunSessionState.RecordCompanionBossFeedback(
                    "AI: I shielded you, and you trusted my warning. Good fight.",
                    trustDelta: 3,
                    affectionDelta: 2,
                    supportActivations: 1,
                    warningHits: 0,
                    warningDodges: 2);
                RunSessionState.EndRun(RunEndReason.Victory, finalTrust: 64, finalAffection: 58);

                Component dialogue = CreateDialogue(owner);
                string text = (string)Invoke(dialogue, "BuildDialogueText");

                Assert.That(text, Does.Contain("I shielded you"));
                Assert.That(text, Does.Contain("Trust 64"));
                Assert.That(text, Does.Contain("Affection 58"));
                Assert.That(text, Does.Contain("shield 1"));
                Assert.That(text, Does.Contain("dodge 2"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void DialogueOnlyTogglesWhilePlayerIsInRange()
        {
            GameObject owner = new GameObject("HomeCompanionDialogueUnderTest");
            GameObject player = new GameObject("PlayerUnderTest");

            try
            {
                owner.AddComponent<BoxCollider2D>().isTrigger = true;
                Component dialogue = CreateDialogue(owner);

                player.AddComponent<Rigidbody2D>();
                player.AddComponent<PlayerInputReader>();
                player.AddComponent<PlayerMovement2D>();
                Collider2D playerCollider = player.AddComponent<BoxCollider2D>();

                bool toggledBeforeRange = (bool)Invoke(dialogue, "TryToggleDialogue");
                Assert.False(toggledBeforeRange);
                Assert.False(ReadProperty<bool>(dialogue, "IsDialogueOpen"));

                Invoke(dialogue, "OnTriggerEnter2D", playerCollider);

                Assert.True(ReadProperty<bool>(dialogue, "IsPlayerInRange"));
                Assert.True((bool)Invoke(dialogue, "TryToggleDialogue"));
                Assert.True(ReadProperty<bool>(dialogue, "IsDialogueOpen"));

                Invoke(dialogue, "OnTriggerExit2D", playerCollider);

                Assert.False(ReadProperty<bool>(dialogue, "IsPlayerInRange"));
                Assert.False(ReadProperty<bool>(dialogue, "IsDialogueOpen"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(player);
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void ThankSupportChoiceUsesLastRunBondAndWritesMemory()
        {
            GameObject owner = new GameObject("HomeCompanionDialogueUnderTest");

            try
            {
                CompanionRelationship relationship = owner.AddComponent<CompanionRelationship>();
                Component dialogue = CreateDialogue(owner);

                RunSessionState.StartRunFromHome("Assets/Scenes/SampleScene.unity");
                RunSessionState.RecordCompanionBossFeedback(
                    "AI: I kept the shield ready when the fight got rough.",
                    trustDelta: 2,
                    affectionDelta: 1,
                    supportActivations: 1,
                    warningHits: 0,
                    warningDodges: 1);
                RunSessionState.EndRun(RunEndReason.Victory, finalTrust: 64, finalAffection: 58);

                object thankSupport = CreateDialogueChoice("ThankSupport");
                Invoke(dialogue, "ApplyDialogueChoice", thankSupport);

                Assert.AreEqual(65, relationship.Trust);
                Assert.AreEqual(62, relationship.Affection);
                Assert.AreEqual(1, relationship.GetMemoryTagScore(RelationshipMemoryTag.Protected));

                string text = (string)Invoke(dialogue, "BuildDialogueText");
                Assert.That(text, Does.Contain("Thank"));
                Assert.That(text, Does.Contain("Trust 65"));
                Assert.That(text, Does.Contain("Affection 62"));
                Assert.That(text, Does.Contain("Protected 1"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        private static Component CreateDialogue(GameObject owner)
        {
            Type type = Type.GetType("AICompanionRoguelike.Home.HomeCompanionDialogue, Assembly-CSharp");
            Assert.NotNull(type, "HomeCompanionDialogue should exist.");
            return owner.AddComponent(type);
        }

        private static object CreateDialogueChoice(string choiceName)
        {
            Type type = Type.GetType("AICompanionRoguelike.Home.HomeCompanionDialogueChoice, Assembly-CSharp");
            Assert.NotNull(type, "HomeCompanionDialogueChoice should exist.");
            return Enum.Parse(type, choiceName);
        }

        private static object Invoke(object target, string methodName, params object[] parameters)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(method, $"{target.GetType().Name} should expose {methodName}.");
            return method.Invoke(target, parameters);
        }

        private static T ReadProperty<T>(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName);
            Assert.NotNull(property, $"{target.GetType().Name} should expose {propertyName}.");
            return (T)property.GetValue(target);
        }
    }
}
