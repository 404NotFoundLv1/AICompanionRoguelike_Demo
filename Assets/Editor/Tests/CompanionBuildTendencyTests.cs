using System;
using System.Reflection;
using AICompanionRoguelike.Companion;
using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Memory;
using AICompanionRoguelike.Roguelike;
using AICompanionRoguelike.UI;
using NUnit.Framework;
using UnityEngine;

namespace AICompanionRoguelike.Tests
{
    public sealed class CompanionBuildTendencyTests
    {
        [SetUp]
        public void SetUp()
        {
            CompanionRelationshipState.Clear();
            ResetBuildStateIfPresent();
        }

        [TearDown]
        public void TearDown()
        {
            ResetBuildStateIfPresent();
            CompanionRelationshipState.Clear();
        }

        [Test]
        public void GuardianTendencyStrengthensGuardComparedWithBalanced()
        {
            CompanionRelationshipProfileSnapshot profile = CompanionRelationshipProfile.Evaluate(
                50,
                50,
                Array.Empty<RelationshipMemoryTagScore>());

            object balanced = EvaluateTacticalSupport(profile, "Balanced");
            object guardian = EvaluateTacticalSupport(profile, "Guardian");

            Assert.Less(
                ReadFloatProperty(guardian, "GuardDamageMultiplier"),
                ReadFloatProperty(balanced, "GuardDamageMultiplier"));
            Assert.Less(
                ReadFloatProperty(guardian, "GuardCooldown"),
                ReadFloatProperty(balanced, "GuardCooldown"));
            Assert.GreaterOrEqual(
                ReadFloatProperty(guardian, "GuardTriggerHealthRatio"),
                ReadFloatProperty(balanced, "GuardTriggerHealthRatio"));
        }

        [Test]
        public void SuppressorTendencyStrengthensSuppressionComparedWithBalanced()
        {
            CompanionRelationshipProfileSnapshot profile = CompanionRelationshipProfile.Evaluate(
                50,
                50,
                Array.Empty<RelationshipMemoryTagScore>());

            object balanced = EvaluateTacticalSupport(profile, "Balanced");
            object suppressor = EvaluateTacticalSupport(profile, "Suppressor");

            Assert.Less(
                ReadFloatProperty(suppressor, "SuppressionDamageMultiplier"),
                ReadFloatProperty(balanced, "SuppressionDamageMultiplier"));
            Assert.Less(
                ReadFloatProperty(suppressor, "SuppressionCooldown"),
                ReadFloatProperty(balanced, "SuppressionCooldown"));
            Assert.GreaterOrEqual(
                ReadFloatProperty(suppressor, "SuppressionTriggerHealthRatio"),
                ReadFloatProperty(balanced, "SuppressionTriggerHealthRatio"));
        }

        [Test]
        public void LinkTendencyLowersQteRequestCooldown()
        {
            GameObject companionObject = new GameObject("CompanionLinkBuildTest");

            try
            {
                SetCurrentTendency("Link");
                CompanionRelationship relationship = companionObject.AddComponent<CompanionRelationship>();
                relationship.SetRelationshipSnapshot(
                    50,
                    50,
                    Array.Empty<RelationshipMemoryTagScore>(),
                    updateSessionState: false);

                Component requester = companionObject.AddComponent(
                    RequireRuntimeType("AICompanionRoguelike.Companion.CompanionQTERequester"));
                WritePrivateField(requester, "relationship", relationship);
                WritePrivateField(requester, "requestCooldown", 4f);

                float cooldown = ReadFloatProperty(requester, "EffectiveRequestCooldown");

                Assert.Less(cooldown, 4f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(companionObject);
            }
        }

        [Test]
        public void BuildChoiceSelectionWritesCurrentRunTendency()
        {
            GameObject uiObject = new GameObject("CompanionBuildChoiceUITest");

            try
            {
                Component ui = uiObject.AddComponent(
                    RequireRuntimeType("AICompanionRoguelike.UI.CompanionBuildChoiceUI"));
                object suppressor = ParseTendency("Suppressor");

                Invoke(ui, "SelectTendency", suppressor);

                Assert.AreEqual("Suppressor", ReadCurrentTendencyName());
                Assert.False((bool)ReadProperty(ui, "IsOpen"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(uiObject);
            }
        }

        [Test]
        public void HudSummaryNamesSelectedBuildAndReadableEffect()
        {
            string guardianSummary = BuildHudSummary("Guardian");
            string linkSummary = BuildHudSummary("Link");

            Assert.That(guardianSummary, Does.Contain("AI Build"));
            Assert.That(guardianSummary, Does.Contain("Guard"));
            Assert.That(linkSummary, Does.Contain("AI Build"));
            Assert.That(linkSummary, Does.Contain("QTE"));
        }

        [Test]
        public void BuildChoiceSelectionSpeaksReadableTendencyLine()
        {
            GameObject companionObject = new GameObject("CompanionBuildChoiceSpeechTest");

            try
            {
                CompanionRelationship relationship = companionObject.AddComponent<CompanionRelationship>();
                relationship.SetRelationshipSnapshot(
                    50,
                    50,
                    Array.Empty<RelationshipMemoryTagScore>(),
                    updateSessionState: false);
                CompanionSpeechBubbleUI speechBubble = companionObject.AddComponent<CompanionSpeechBubbleUI>();
                companionObject.AddComponent<CompanionCombatDialogueController>();
                CompanionBuildChoiceUI ui = companionObject.AddComponent<CompanionBuildChoiceUI>();

                ui.SelectTendency(CompanionSkillTendency.Link);

                Assert.That(speechBubble.CurrentMessage, Does.Contain("Link"));
                Assert.That(speechBubble.CurrentMessage, Does.Contain("QTE"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(companionObject);
            }
        }

        [Test]
        public void GuardianGuardActivationSpeaksBuildSpecificFeedback()
        {
            GameObject playerObject = new GameObject("PlayerGuardianBuildFeedbackTest");
            GameObject companionObject = new GameObject("CompanionGuardianBuildFeedbackTest");

            try
            {
                SetCurrentTendency("Guardian");

                HealthComponent playerHealth = playerObject.AddComponent<HealthComponent>();
                playerHealth.SetMaxHealth(100f, true);
                PlayerBossSupportShield shield = playerObject.AddComponent<PlayerBossSupportShield>();

                CompanionRelationship relationship = companionObject.AddComponent<CompanionRelationship>();
                relationship.SetRelationshipSnapshot(
                    50,
                    50,
                    Array.Empty<RelationshipMemoryTagScore>(),
                    updateSessionState: false);
                CompanionSpeechBubbleUI speechBubble = companionObject.AddComponent<CompanionSpeechBubbleUI>();
                CompanionCombatDialogueController dialogue = companionObject.AddComponent<CompanionCombatDialogueController>();
                CompanionTacticalSupport support = companionObject.AddComponent<CompanionTacticalSupport>();
                support.Configure(playerHealth, relationship, shield, null, dialogue);

                bool activated = support.TryActivateGuard("Guardian Build Test");

                Assert.True(activated);
                Assert.That(speechBubble.CurrentMessage, Does.Contain("Guardian"));
                Assert.That(speechBubble.CurrentMessage, Does.Contain("Guard"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(companionObject);
                UnityEngine.Object.DestroyImmediate(playerObject);
            }
        }

        [TestCase("Guardian", "GuardianBuildUpgrade")]
        [TestCase("Suppressor", "SuppressorBuildUpgrade")]
        [TestCase("Link", "LinkBuildUpgrade")]
        public void RewardChoicesIncludeCurrentBuildUpgrade(string tendencyName, string rewardName)
        {
            GameObject runObject = new GameObject("RunManagerBuildRewardChoiceTest");

            try
            {
                SetCurrentTendency(tendencyName);
                runObject.AddComponent<RoomManager>();
                RunManager runManager = runObject.AddComponent<RunManager>();

                Invoke(runManager, "PrepareRewardChoices");

                Assert.That(
                    ContainsReward(runManager.CurrentRewardChoices, rewardName),
                    Is.True,
                    $"Reward choices should include the selected {tendencyName} build upgrade.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void GuardianBuildRewardStrengthensGuardTuningAndShowsLevel()
        {
            GameObject runObject = new GameObject("GuardianBuildRewardApplyTest");

            try
            {
                SetCurrentTendency("Guardian");
                runObject.AddComponent<RoomManager>();
                RunManager runManager = runObject.AddComponent<RunManager>();
                CompanionRelationshipProfileSnapshot profile = CompanionRelationshipProfile.Evaluate(
                    50,
                    50,
                    Array.Empty<RelationshipMemoryTagScore>());

                object before = EvaluateTacticalSupport(profile, "Guardian");
                Invoke(runManager, "ApplyReward", ParseRewardType("GuardianBuildUpgrade"));
                object after = EvaluateTacticalSupport(profile, "Guardian");

                Assert.Less(
                    ReadFloatProperty(after, "GuardDamageMultiplier"),
                    ReadFloatProperty(before, "GuardDamageMultiplier"));
                Assert.AreEqual(1, ReadBuildUpgradeLevel("Guardian"));
                Assert.That(BuildHudSummary("Guardian"), Does.Contain("Lv1"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void LinkBuildRewardFurtherLowersQteCooldownAndShowsLevel()
        {
            GameObject runObject = new GameObject("LinkBuildRewardApplyTest");
            GameObject companionObject = new GameObject("CompanionLinkRewardTest");

            try
            {
                SetCurrentTendency("Link");
                runObject.AddComponent<RoomManager>();
                RunManager runManager = runObject.AddComponent<RunManager>();
                CompanionRelationship relationship = companionObject.AddComponent<CompanionRelationship>();
                relationship.SetRelationshipSnapshot(
                    50,
                    50,
                    Array.Empty<RelationshipMemoryTagScore>(),
                    updateSessionState: false);
                Component requester = companionObject.AddComponent(
                    RequireRuntimeType("AICompanionRoguelike.Companion.CompanionQTERequester"));
                WritePrivateField(requester, "relationship", relationship);
                WritePrivateField(requester, "requestCooldown", 4f);

                float before = ReadFloatProperty(requester, "EffectiveRequestCooldown");
                Invoke(runManager, "ApplyReward", ParseRewardType("LinkBuildUpgrade"));
                float after = ReadFloatProperty(requester, "EffectiveRequestCooldown");

                Assert.Less(after, before);
                Assert.AreEqual(1, ReadBuildUpgradeLevel("Link"));
                Assert.That(BuildHudSummary("Link"), Does.Contain("Lv1"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(companionObject);
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        private static object EvaluateTacticalSupport(CompanionRelationshipProfileSnapshot profile, string tendencyName)
        {
            Type rulesType = RequireRuntimeType("AICompanionRoguelike.Companion.CompanionTacticalSupportRules");
            Type tendencyType = RequireRuntimeType("AICompanionRoguelike.Companion.CompanionSkillTendency");
            MethodInfo evaluateMethod = rulesType.GetMethod(
                "Evaluate",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(CompanionRelationshipProfileSnapshot), tendencyType },
                null);
            Assert.NotNull(evaluateMethod, "CompanionTacticalSupportRules should expose Evaluate(profile, tendency).");
            return evaluateMethod.Invoke(null, new[] { profile, ParseTendency(tendencyName) });
        }

        private static void SetCurrentTendency(string tendencyName)
        {
            Type stateType = RequireRuntimeType("AICompanionRoguelike.Companion.CompanionRunBuildState");
            MethodInfo setTendencyMethod = stateType.GetMethod("SetTendency", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(setTendencyMethod, "CompanionRunBuildState should expose SetTendency.");
            setTendencyMethod.Invoke(null, new[] { ParseTendency(tendencyName) });
        }

        private static void ResetBuildStateIfPresent()
        {
            Type stateType = Type.GetType("AICompanionRoguelike.Companion.CompanionRunBuildState, Assembly-CSharp");
            MethodInfo resetMethod = stateType?.GetMethod("Reset", BindingFlags.Public | BindingFlags.Static);
            resetMethod?.Invoke(null, Array.Empty<object>());
        }

        private static string ReadCurrentTendencyName()
        {
            Type stateType = RequireRuntimeType("AICompanionRoguelike.Companion.CompanionRunBuildState");
            return ReadProperty(null, "CurrentTendency", stateType).ToString();
        }

        private static object ParseTendency(string tendencyName)
        {
            Type tendencyType = RequireRuntimeType("AICompanionRoguelike.Companion.CompanionSkillTendency");
            return Enum.Parse(tendencyType, tendencyName);
        }

        private static string BuildHudSummary(string tendencyName)
        {
            Type rulesType = RequireRuntimeType("AICompanionRoguelike.Companion.CompanionSkillTendencyRules");
            MethodInfo method = rulesType.GetMethod("GetHudSummaryLine", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "CompanionSkillTendencyRules should expose GetHudSummaryLine.");
            return (string)method.Invoke(null, new[] { ParseTendency(tendencyName) });
        }

        private static object ParseRewardType(string rewardName)
        {
            Type rewardType = RequireRuntimeType("AICompanionRoguelike.Roguelike.RunRewardType");
            return Enum.Parse(rewardType, rewardName);
        }

        private static int ReadBuildUpgradeLevel(string tendencyName)
        {
            Type stateType = RequireRuntimeType("AICompanionRoguelike.Companion.CompanionRunBuildState");
            MethodInfo method = stateType.GetMethod("GetUpgradeLevel", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "CompanionRunBuildState should expose GetUpgradeLevel.");
            return (int)method.Invoke(null, new[] { ParseTendency(tendencyName) });
        }

        private static bool ContainsReward(
            System.Collections.Generic.IEnumerable<RunRewardChoice> choices,
            string rewardName)
        {
            foreach (RunRewardChoice choice in choices)
            {
                if (choice.RewardType.ToString() == rewardName)
                {
                    return true;
                }
            }

            return false;
        }

        private static Type RequireRuntimeType(string fullName)
        {
            Type type = Type.GetType($"{fullName}, Assembly-CSharp");
            Assert.NotNull(type, $"Runtime type {fullName} should exist.");
            return type;
        }

        private static float ReadFloatProperty(object target, string propertyName)
        {
            return (float)ReadProperty(target, propertyName);
        }

        private static object ReadProperty(object target, string propertyName)
        {
            return ReadProperty(target, propertyName, target.GetType());
        }

        private static object ReadProperty(object target, string propertyName, Type type)
        {
            PropertyInfo property = type.GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
            Assert.NotNull(property, $"{type.Name} should expose property {propertyName}.");
            return property.GetValue(target);
        }

        private static void WritePrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"{target.GetType().Name} should define field {fieldName}.");
            field.SetValue(target, value);
        }

        private static object Invoke(object target, string methodName, params object[] parameters)
        {
            Type[] parameterTypes = new Type[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                parameterTypes[i] = parameters[i].GetType();
            }

            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                parameterTypes,
                null);
            Assert.NotNull(method, $"{target.GetType().Name} should expose {methodName}.");
            return method.Invoke(target, parameters);
        }
    }
}
