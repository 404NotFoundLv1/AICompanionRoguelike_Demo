using System;
using System.Reflection;
using AICompanionRoguelike.Companion;
using AICompanionRoguelike.Memory;
using AICompanionRoguelike.UI;
using NUnit.Framework;
using UnityEngine;

namespace AICompanionRoguelike.Tests
{
    public sealed class CompanionRelationshipProfileTests
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

        [TestCase(30, 30, "Distant", "Guarded", 1.25f)]
        [TestCase(55, 55, "Trusted", "Neutral", 1f)]
        [TestCase(80, 80, "Synchronized", "Warm", 0.75f)]
        public void EvaluateDerivesRelationshipProfile(
            int trust,
            int affection,
            string expectedTier,
            string expectedTone,
            float expectedCooldownMultiplier)
        {
            object profile = EvaluateProfile(
                trust,
                affection,
                Array.Empty<RelationshipMemoryTagScore>());

            Assert.AreEqual(expectedTier, ReadProperty(profile, "Tier").ToString());
            Assert.AreEqual(expectedTone, ReadProperty(profile, "Tone").ToString());
            Assert.AreEqual(
                expectedCooldownMultiplier,
                (float)ReadProperty(profile, "QteCooldownMultiplier"),
                0.001f);
            Assert.False((bool)ReadProperty(profile, "HasDominantMemory"));
        }

        [Test]
        public void EvaluateChoosesHighestScoringMemoryTag()
        {
            RelationshipMemoryTagScore[] tags =
            {
                new RelationshipMemoryTagScore
                {
                    tag = RelationshipMemoryTag.Reliable,
                    score = 2
                },
                new RelationshipMemoryTagScore
                {
                    tag = RelationshipMemoryTag.Brave,
                    score = 5
                }
            };

            object profile = EvaluateProfile(55, 55, tags);

            Assert.True((bool)ReadProperty(profile, "HasDominantMemory"));
            Assert.AreEqual(
                RelationshipMemoryTag.Brave,
                (RelationshipMemoryTag)ReadProperty(profile, "DominantMemoryTag"));
            Assert.AreEqual(5, (int)ReadProperty(profile, "DominantMemoryScore"));
        }

        [Test]
        public void OpeningFeedbackUsesWarmToneAndBraveMemory()
        {
            GameObject companionObject = new GameObject("CompanionFeedbackUnderTest");

            try
            {
                CompanionRelationship relationship = companionObject.AddComponent<CompanionRelationship>();
                relationship.SetRelationshipSnapshot(
                    80,
                    80,
                    new[]
                    {
                        new RelationshipMemoryTagScore
                        {
                            tag = RelationshipMemoryTag.Brave,
                            score = 4
                        }
                    },
                    updateSessionState: false);

                Type feedbackType = RequireRuntimeType("AICompanionRoguelike.Companion.CompanionRunFeedback");
                Component feedback = companionObject.AddComponent(feedbackType);
                string line = (string)Invoke(feedback, "BuildOpeningFeedback");

                Assert.That(line, Does.Contain("know your pace"));
                Assert.That(line, Does.Contain("hard fight"));
                Assert.That(line, Does.Not.Contain("0.75"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(companionObject);
            }
        }

        [TestCase(30, 5f)]
        [TestCase(55, 4f)]
        [TestCase(80, 3f)]
        public void QteRequesterUsesTrustDrivenCooldown(int trust, float expectedCooldown)
        {
            GameObject companionObject = new GameObject("CompanionQTEUnderTest");

            try
            {
                CompanionRelationship relationship = companionObject.AddComponent<CompanionRelationship>();
                relationship.SetRelationshipSnapshot(
                    trust,
                    55,
                    Array.Empty<RelationshipMemoryTagScore>(),
                    updateSessionState: false);
                CompanionQTERequester requester = companionObject.AddComponent<CompanionQTERequester>();
                WriteField(requester, "relationship", relationship);
                WriteField(requester, "requestCooldown", 4f);

                PropertyInfo property = typeof(CompanionQTERequester).GetProperty("EffectiveRequestCooldown");
                Assert.NotNull(property, "CompanionQTERequester should expose EffectiveRequestCooldown.");
                Assert.AreEqual(expectedCooldown, (float)property.GetValue(requester), 0.001f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(companionObject);
            }
        }

        [Test]
        public void StatusPanelShowsTierAndDominantMemory()
        {
            GameObject relationshipObject = new GameObject("RelationshipUnderTest");
            GameObject uiObject = new GameObject("StatusFeedbackUnderTest");

            try
            {
                CompanionRelationship relationship = relationshipObject.AddComponent<CompanionRelationship>();
                relationship.SetRelationshipSnapshot(
                    80,
                    80,
                    new[]
                    {
                        new RelationshipMemoryTagScore
                        {
                            tag = RelationshipMemoryTag.Brave,
                            score = 4
                        }
                    },
                    updateSessionState: false);
                StatusFeedbackUI statusUI = uiObject.AddComponent<StatusFeedbackUI>();
                WriteField(statusUI, "companionRelationship", relationship);

                string relationshipLine = (string)Invoke(statusUI, "BuildRelationshipLine");
                string memoryLine = (string)Invoke(statusUI, "BuildMemoryLine");

                Assert.That(relationshipLine, Does.Contain("Synchronized"));
                Assert.That(memoryLine, Does.Contain("Brave 4"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(uiObject);
                UnityEngine.Object.DestroyImmediate(relationshipObject);
            }
        }

        [Test]
        public void ToastRectAvoidsStatusPanelAtDesktopWidth()
        {
            GameObject uiObject = new GameObject("StatusFeedbackLayoutUnderTest");

            try
            {
                StatusFeedbackUI statusUI = uiObject.AddComponent<StatusFeedbackUI>();
                WriteField(statusUI, "panelRect", new Rect(16f, 16f, 330f, 210f));

                Rect toastRect = (Rect)Invoke(statusUI, "CalculateToastRect", 960f, 394f);
                Rect statusRect = new Rect(16f, 16f, 330f, 210f);

                Assert.False(toastRect.Overlaps(statusRect));
                Assert.GreaterOrEqual(toastRect.xMin, 8f);
                Assert.LessOrEqual(toastRect.xMax, 952f);
                Assert.LessOrEqual(toastRect.yMax, 386f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(uiObject);
            }
        }

        private static object EvaluateProfile(
            int trust,
            int affection,
            RelationshipMemoryTagScore[] tags)
        {
            Type profileType = RequireRuntimeType("AICompanionRoguelike.Memory.CompanionRelationshipProfile");
            MethodInfo evaluateMethod = profileType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(evaluateMethod, "CompanionRelationshipProfile should expose Evaluate.");
            return evaluateMethod.Invoke(null, new object[] { trust, affection, tags });
        }

        private static Type RequireRuntimeType(string fullName)
        {
            Type type = Type.GetType($"{fullName}, Assembly-CSharp");
            Assert.NotNull(type, $"Runtime type {fullName} should exist.");
            return type;
        }

        private static object Invoke(object target, string methodName, params object[] parameters)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(method, $"{target.GetType().Name} should expose {methodName}.");
            return method.Invoke(target, parameters);
        }

        private static object ReadProperty(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(property, $"{target.GetType().Name} should expose property {propertyName}.");
            return property.GetValue(target);
        }

        private static void WriteField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"{target.GetType().Name} should contain field {fieldName}.");
            field.SetValue(target, value);
        }
    }
}
