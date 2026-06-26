using System;
using System.Reflection;
using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Enemy;
using AICompanionRoguelike.Memory;
using NUnit.Framework;
using UnityEngine;

namespace AICompanionRoguelike.Tests
{
    public sealed class CompanionTacticalSupportTests
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
        public void TacticalRulesMakeProtectedSynchronizedGuardStrongerThanDistantGuard()
        {
            CompanionRelationshipProfileSnapshot distantProfile = CompanionRelationshipProfile.Evaluate(
                25,
                35,
                Array.Empty<RelationshipMemoryTagScore>());
            CompanionRelationshipProfileSnapshot protectedProfile = CompanionRelationshipProfile.Evaluate(
                82,
                78,
                new[]
                {
                    new RelationshipMemoryTagScore
                    {
                        tag = RelationshipMemoryTag.Protected,
                        score = 3
                    }
                });

            object weakTuning = EvaluateTacticalSupport(distantProfile);
            object strongTuning = EvaluateTacticalSupport(protectedProfile);

            Assert.Greater(
                ReadFloatProperty(strongTuning, "GuardDuration"),
                ReadFloatProperty(weakTuning, "GuardDuration"));
            Assert.Less(
                ReadFloatProperty(strongTuning, "GuardDamageMultiplier"),
                ReadFloatProperty(weakTuning, "GuardDamageMultiplier"));
            Assert.Less(
                ReadFloatProperty(strongTuning, "GuardCooldown"),
                ReadFloatProperty(weakTuning, "GuardCooldown"));
            Assert.Less(
                ReadFloatProperty(strongTuning, "SuppressionDamageMultiplier"),
                ReadFloatProperty(weakTuning, "SuppressionDamageMultiplier"));
        }

        [Test]
        public void TacticalSuppressionReducesEnemyAttackDamageForActiveDuration()
        {
            GameObject enemyObject = new GameObject("EnemyUnderSuppression");
            GameObject playerObject = new GameObject("PlayerUnderSuppressionTest");

            try
            {
                EnemyAttack2D attack = enemyObject.AddComponent<EnemyAttack2D>();
                HealthComponent playerHealth = playerObject.AddComponent<HealthComponent>();
                playerHealth.SetMaxHealth(100f, true);

                MethodInfo applySuppression = typeof(EnemyAttack2D).GetMethod(
                    "ApplyTacticalSuppression",
                    BindingFlags.Instance | BindingFlags.Public);
                Assert.NotNull(applySuppression, "EnemyAttack2D should expose ApplyTacticalSuppression.");
                applySuppression.Invoke(attack, new object[] { 1f, 0.4f });

                attack.TryAttack(playerObject.transform);
                attack.Tick(0.35f);

                Assert.AreEqual(96f, playerHealth.CurrentHealth, 0.001f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerObject);
                UnityEngine.Object.DestroyImmediate(enemyObject);
            }
        }

        [Test]
        public void TacticalGuardActivatesShieldAndWritesProtectedMemory()
        {
            GameObject playerObject = new GameObject("PlayerTacticalGuardTest");
            GameObject companionObject = new GameObject("CompanionTacticalGuardTest");

            try
            {
                HealthComponent playerHealth = playerObject.AddComponent<HealthComponent>();
                playerHealth.SetMaxHealth(100f, true);
                PlayerBossSupportShield shield = playerObject.AddComponent<PlayerBossSupportShield>();

                CompanionRelationship relationship = companionObject.AddComponent<CompanionRelationship>();
                relationship.SetRelationshipSnapshot(
                    80,
                    80,
                    Array.Empty<RelationshipMemoryTagScore>(),
                    updateSessionState: false);

                Component support = companionObject.AddComponent(
                    RequireRuntimeType("AICompanionRoguelike.Companion.CompanionTacticalSupport"));
                Invoke(
                    support,
                    "Configure",
                    playerHealth,
                    relationship,
                    shield,
                    null,
                    null);

                bool activated = (bool)Invoke(support, "TryActivateGuard", "Test Guard");

                Assert.True(activated);
                Assert.True(shield.IsActive);
                Assert.Less(shield.IncomingDamageMultiplier, 0.65f);
                Assert.Greater(relationship.GetMemoryTagScore(RelationshipMemoryTag.Protected), 0);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(companionObject);
                UnityEngine.Object.DestroyImmediate(playerObject);
            }
        }

        private static object EvaluateTacticalSupport(CompanionRelationshipProfileSnapshot profile)
        {
            Type rulesType = RequireRuntimeType("AICompanionRoguelike.Companion.CompanionTacticalSupportRules");
            MethodInfo evaluateMethod = rulesType.GetMethod(
                "Evaluate",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(CompanionRelationshipProfileSnapshot) },
                null);
            Assert.NotNull(evaluateMethod, "CompanionTacticalSupportRules should expose Evaluate.");
            return evaluateMethod.Invoke(null, new object[] { profile });
        }

        private static Type RequireRuntimeType(string fullName)
        {
            Type type = Type.GetType($"{fullName}, Assembly-CSharp");
            Assert.NotNull(type, $"Runtime type {fullName} should exist.");
            return type;
        }

        private static float ReadFloatProperty(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(property, $"{target.GetType().Name} should expose property {propertyName}.");
            return (float)property.GetValue(target);
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
