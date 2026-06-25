using System;
using System.Reflection;
using AICompanionRoguelike.Memory;
using NUnit.Framework;
using UnityEngine;

namespace AICompanionRoguelike.Tests
{
    public sealed class CompanionCombatDialogueTests
    {
        [Test]
        public void QteSuccessLineUsesDominantReliableMemory()
        {
            object eventType = ParseDialogueEventType("QTESuccess");
            CompanionRelationshipProfileSnapshot profile = CompanionRelationshipProfile.Evaluate(
                80,
                80,
                new[]
                {
                    new RelationshipMemoryTagScore
                    {
                        tag = RelationshipMemoryTag.Reliable,
                        score = 5
                    },
                    new RelationshipMemoryTagScore
                    {
                        tag = RelationshipMemoryTag.Brave,
                        score = 2
                    }
                });

            string line = BuildLine(eventType, profile);

            Assert.That(line, Does.Contain("Good sync"));
            Assert.That(line, Does.Contain("dependable"));
        }

        [Test]
        public void LowHealthLineUsesProtectedMemory()
        {
            object eventType = ParseDialogueEventType("PlayerLowHealth");
            CompanionRelationshipProfileSnapshot profile = CompanionRelationshipProfile.Evaluate(
                75,
                75,
                new[]
                {
                    new RelationshipMemoryTagScore
                    {
                        tag = RelationshipMemoryTag.Protected,
                        score = 3
                    }
                });

            string line = BuildLine(eventType, profile);

            Assert.That(line, Does.Contain("Stay behind me"));
            Assert.That(line, Does.Contain("protected"));
        }

        [Test]
        public void DialogueGateSuppressesLowerPriorityAndCooldownNoise()
        {
            Type gateType = RequireRuntimeType("AICompanionRoguelike.Companion.CompanionDialoguePriorityGate");
            MethodInfo shouldShowMethod = gateType.GetMethod(
                "ShouldShow",
                BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(shouldShowMethod);

            Assert.False((bool)shouldShowMethod.Invoke(null, new object[] { 1, 3, true, 10f, 0f }));
            Assert.True((bool)shouldShowMethod.Invoke(null, new object[] { 4, 3, true, 10f, 20f }));
            Assert.True((bool)shouldShowMethod.Invoke(null, new object[] { 4, 0, false, 10f, 20f }));
            Assert.False((bool)shouldShowMethod.Invoke(null, new object[] { 2, 0, false, 10f, 20f }));
            Assert.True((bool)shouldShowMethod.Invoke(null, new object[] { 2, 0, false, 21f, 20f }));
        }

        [Test]
        public void SpeechBubbleRectStaysInsideScreen()
        {
            Type bubbleType = RequireRuntimeType("AICompanionRoguelike.UI.CompanionSpeechBubbleUI");
            MethodInfo method = bubbleType.GetMethod(
                "CalculateScreenRect",
                BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);

            Rect rect = (Rect)method.Invoke(
                null,
                new object[]
                {
                    new Vector2(8f, 16f),
                    new Vector2(340f, 72f),
                    320f,
                    180f,
                    10f
                });

            Assert.GreaterOrEqual(rect.xMin, 10f);
            Assert.GreaterOrEqual(rect.yMin, 10f);
            Assert.LessOrEqual(rect.xMax, 310f);
            Assert.LessOrEqual(rect.yMax, 170f);
        }

        private static object ParseDialogueEventType(string value)
        {
            Type eventType = RequireRuntimeType("AICompanionRoguelike.Companion.CompanionDialogueEventType");
            return Enum.Parse(eventType, value);
        }

        private static string BuildLine(object eventType, CompanionRelationshipProfileSnapshot profile)
        {
            Type lineType = RequireRuntimeType("AICompanionRoguelike.Companion.CompanionCombatDialogueLines");
            MethodInfo method = lineType.GetMethod(
                "BuildLine",
                BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);
            return (string)method.Invoke(null, new object[] { eventType, profile });
        }

        private static Type RequireRuntimeType(string fullName)
        {
            Type type = Type.GetType($"{fullName}, Assembly-CSharp");
            Assert.NotNull(type, $"Runtime type {fullName} should exist.");
            return type;
        }
    }
}
