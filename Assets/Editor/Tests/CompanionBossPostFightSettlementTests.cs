using System;
using System.Reflection;
using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Memory;
using AICompanionRoguelike.Roguelike;
using NUnit.Framework;
using UnityEngine;

namespace AICompanionRoguelike.Tests
{
    public sealed class CompanionBossPostFightSettlementTests
    {
        [Test]
        public void BossVictoryAppliesSupportDodgeAndLowHealthRelationshipMemory()
        {
            GameObject player = new GameObject("PlayerUnderTest");
            GameObject relationshipObject = new GameObject("RelationshipUnderTest");
            GameObject settlementObject = new GameObject("SettlementUnderTest");

            try
            {
                HealthComponent playerHealth = player.AddComponent<HealthComponent>();
                playerHealth.SetMaxHealth(100f, true);
                playerHealth.TakeDamage(new DamageInfo(80f, DamageSourceType.Enemy, settlementObject));

                CompanionRelationship relationship = relationshipObject.AddComponent<CompanionRelationship>();
                int previousTrust = relationship.Trust;
                int previousAffection = relationship.Affection;

                Component settlement = CreateSettlement(settlementObject);
                Invoke(settlement, "Configure", playerHealth, relationship);
                Invoke(settlement, "RecordSupportActivated");
                Invoke(settlement, "RecordBossWarningResolved", false);

                object report = Invoke(settlement, "SettleBossVictory");

                Assert.AreEqual(Mathf.Clamp(previousTrust + 3, 0, 100), relationship.Trust);
                Assert.AreEqual(Mathf.Clamp(previousAffection + 3, 0, 100), relationship.Affection);
                Assert.AreEqual(1, relationship.GetMemoryTagScore(RelationshipMemoryTag.Protected));
                Assert.AreEqual(1, relationship.GetMemoryTagScore(RelationshipMemoryTag.Reliable));
                Assert.AreEqual(1, relationship.GetMemoryTagScore(RelationshipMemoryTag.Brave));

                Assert.AreEqual(1, ReadProperty<int>(report, "SupportActivations"));
                Assert.AreEqual(0, ReadProperty<int>(report, "WarningHits"));
                Assert.AreEqual(1, ReadProperty<int>(report, "WarningDodges"));
                Assert.AreEqual(3, ReadProperty<int>(report, "TrustDelta"));
                Assert.AreEqual(3, ReadProperty<int>(report, "AffectionDelta"));
                Assert.That(ReadProperty<string>(report, "FeedbackLine"), Does.Contain("shielded"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settlementObject);
                UnityEngine.Object.DestroyImmediate(relationshipObject);
                UnityEngine.Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void BossWarningHitIsRecordedInRunSummary()
        {
            GameObject player = new GameObject("PlayerUnderTest");
            GameObject relationshipObject = new GameObject("RelationshipUnderTest");
            GameObject settlementObject = new GameObject("SettlementUnderTest");

            try
            {
                RunSessionState.StartRunFromHome("Assets/Scenes/SampleScene.unity");

                HealthComponent playerHealth = player.AddComponent<HealthComponent>();
                playerHealth.SetMaxHealth(100f, true);

                CompanionRelationship relationship = relationshipObject.AddComponent<CompanionRelationship>();
                relationship.ApplyMemoryEvent("Seed Trust", 50, 0, RelationshipMemoryTag.Reliable);
                int previousTrust = relationship.Trust;

                Component settlement = CreateSettlement(settlementObject);
                Invoke(settlement, "Configure", playerHealth, relationship);
                Invoke(settlement, "RecordBossWarningResolved", true);
                Invoke(settlement, "SettleBossVictory");

                RunSessionState.EndRun(RunEndReason.Victory, relationship.Trust, relationship.Affection);
                RunSessionSummary summary = RunSessionState.LastSummary;

                Assert.AreEqual(Mathf.Clamp(previousTrust - 1, 0, 100), relationship.Trust);
                Assert.AreEqual(1, relationship.GetMemoryTagScore(RelationshipMemoryTag.Stubborn));
                Assert.That(ReadProperty<string>(summary, "CompanionFeedbackLine"), Does.Contain("warning hit"));
                Assert.AreEqual(-1, ReadProperty<int>(summary, "CompanionTrustDelta"));
                Assert.AreEqual(0, ReadProperty<int>(summary, "CompanionAffectionDelta"));
                Assert.AreEqual(0, ReadProperty<int>(summary, "BossSupportActivations"));
                Assert.AreEqual(1, ReadProperty<int>(summary, "BossWarningHits"));
                Assert.AreEqual(0, ReadProperty<int>(summary, "BossWarningDodges"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settlementObject);
                UnityEngine.Object.DestroyImmediate(relationshipObject);
                UnityEngine.Object.DestroyImmediate(player);
            }
        }

        private static Component CreateSettlement(GameObject owner)
        {
            Type settlementType = Type.GetType(
                "AICompanionRoguelike.Companion.CompanionBossPostFightSettlement, Assembly-CSharp");
            Assert.NotNull(settlementType, "CompanionBossPostFightSettlement should exist.");
            return owner.AddComponent(settlementType);
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
