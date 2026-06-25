using System;
using System.Reflection;
using AICompanionRoguelike.Memory;
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
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(method, $"{target.GetType().Name} should expose {methodName}.");
            return method.Invoke(target, parameters);
        }
    }
}
