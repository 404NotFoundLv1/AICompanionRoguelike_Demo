using System;
using System.Reflection;
using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Companion;
using AICompanionRoguelike.Enemy;
using AICompanionRoguelike.Memory;
using AICompanionRoguelike.UI;
using NUnit.Framework;
using UnityEngine;

namespace AICompanionRoguelike.Tests
{
    public sealed class CompanionEnemyTargetPriorityTests
    {
        [SetUp]
        public void SetUp()
        {
            CompanionRunBuildState.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            CompanionRunBuildState.Reset();
        }

        [Test]
        public void BalancedRulesPreferRangedThreatOverCloserMelee()
        {
            float meleeScore = EvaluateScore(
                EnemyArchetypeType.Melee,
                CompanionSkillTendency.Balanced,
                1f,
                4f,
                false,
                false);
            float rangedScore = EvaluateScore(
                EnemyArchetypeType.Ranged,
                CompanionSkillTendency.Balanced,
                3f,
                4f,
                false,
                false);

            Assert.Greater(rangedScore, meleeScore);
        }

        [Test]
        public void GuardianRulesInterceptImmediatePlayerThreatBeforeDistantRangedEnemy()
        {
            float rangedScore = EvaluateScore(
                EnemyArchetypeType.Ranged,
                CompanionSkillTendency.Guardian,
                3.5f,
                5f,
                false,
                false);
            float interceptScore = EvaluateScore(
                EnemyArchetypeType.Melee,
                CompanionSkillTendency.Guardian,
                1.5f,
                1f,
                true,
                false);

            Assert.Greater(interceptScore, rangedScore);
        }

        [Test]
        public void BuildTendenciesStrengthenTheirMatchingTargetChoices()
        {
            float balancedRanged = EvaluateScore(
                EnemyArchetypeType.Ranged,
                CompanionSkillTendency.Balanced,
                3f,
                4f,
                false,
                false);
            float suppressorRanged = EvaluateScore(
                EnemyArchetypeType.Ranged,
                CompanionSkillTendency.Suppressor,
                3f,
                4f,
                false,
                false);
            float balancedGuard = EvaluateScore(
                EnemyArchetypeType.Guard,
                CompanionSkillTendency.Balanced,
                2f,
                3f,
                false,
                false);
            float linkGuard = EvaluateScore(
                EnemyArchetypeType.Guard,
                CompanionSkillTendency.Link,
                2f,
                3f,
                false,
                false);

            Assert.Greater(suppressorRanged, balancedRanged);
            Assert.Greater(linkGuard, balancedGuard);
        }

        [Test]
        public void RangedDecisionLineExplainsSuppressorIntent()
        {
            Type reasonType = RequireRuntimeType(
                "AICompanionRoguelike.Companion.CompanionTargetDecisionReason");
            object reason = Enum.Parse(reasonType, "RangedThreat");
            Type rulesType = RequireRuntimeType(
                "AICompanionRoguelike.Companion.CompanionTargetPriorityRules");
            MethodInfo method = rulesType.GetMethod(
                "BuildDecisionLine",
                BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);

            string line = (string)method.Invoke(
                null,
                new[]
                {
                    (object)EnemyArchetypeType.Ranged,
                    CompanionSkillTendency.Suppressor,
                    reason
                });

            Assert.That(line, Does.Contain("Ranged"));
            Assert.That(line.ToLowerInvariant(), Does.Contain("suppress"));
        }

        [Test]
        public void SensorSelectsRangedEnemyAndExposesDecisionReason()
        {
            const int enemyLayer = 8;
            GameObject companion = new GameObject("TargetPriorityCompanion");
            GameObject player = new GameObject("TargetPriorityPlayer");
            GameObject melee = CreateEnemy("TargetPriorityMelee", EnemyArchetypeType.Melee, enemyLayer, 1f);
            GameObject ranged = CreateEnemy("TargetPriorityRanged", EnemyArchetypeType.Ranged, enemyLayer, 3f);

            try
            {
                companion.AddComponent<HealthComponent>();
                CompanionSensor sensor = companion.AddComponent<CompanionSensor>();
                WritePrivateField(sensor, "detectionRadius", 5f);
                WritePrivateField(sensor, "enemyLayerMask", (LayerMask)(1 << enemyLayer));
                WritePrivateField(sensor, "protectedPlayer", player.transform);
                InvokePrivate(sensor, "ConfigureEnemyFilter");
                Physics2D.SyncTransforms();

                HealthComponent selected = sensor.ScanNow();

                Assert.AreSame(ranged.GetComponent<HealthComponent>(), selected);
                PropertyInfo reasonProperty = typeof(CompanionSensor).GetProperty(
                    "CurrentTargetDecisionReason",
                    BindingFlags.Instance | BindingFlags.Public);
                Assert.NotNull(reasonProperty);
                Assert.AreEqual("RangedThreat", reasonProperty.GetValue(sensor).ToString());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(ranged);
                UnityEngine.Object.DestroyImmediate(melee);
                UnityEngine.Object.DestroyImmediate(player);
                UnityEngine.Object.DestroyImmediate(companion);
            }
        }

        [Test]
        public void SuppressorTargetChangeAppearsInExistingSpeechBubble()
        {
            const int enemyLayer = 8;
            GameObject companion = new GameObject("TargetPriorityDialogueCompanion");
            GameObject player = new GameObject("TargetPriorityDialoguePlayer");
            GameObject ranged = CreateEnemy(
                "TargetPriorityDialogueRanged",
                EnemyArchetypeType.Ranged,
                enemyLayer,
                3f);

            try
            {
                player.AddComponent<HealthComponent>();
                companion.AddComponent<HealthComponent>();
                companion.AddComponent<CompanionRelationship>();
                CompanionSpeechBubbleUI bubble = companion.AddComponent<CompanionSpeechBubbleUI>();
                CompanionSensor sensor = companion.AddComponent<CompanionSensor>();
                WritePrivateField(sensor, "detectionRadius", 5f);
                WritePrivateField(sensor, "enemyLayerMask", (LayerMask)(1 << enemyLayer));
                WritePrivateField(sensor, "protectedPlayer", player.transform);
                InvokePrivate(sensor, "ConfigureEnemyFilter");
                CompanionCombatDialogueController dialogue =
                    companion.AddComponent<CompanionCombatDialogueController>();
                InvokePrivate(dialogue, "OnEnable");
                CompanionRunBuildState.SetTendency(CompanionSkillTendency.Suppressor);
                Physics2D.SyncTransforms();

                sensor.ScanNow();

                Assert.True(bubble.IsVisible);
                Assert.That(bubble.CurrentMessage, Does.Contain("Ranged"));
                Assert.That(bubble.CurrentMessage.ToLowerInvariant(), Does.Contain("suppress"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(ranged);
                UnityEngine.Object.DestroyImmediate(player);
                UnityEngine.Object.DestroyImmediate(companion);
            }
        }

        [Test]
        public void RangedThreatFeedbackOverridesRecentMeleeTargetCallout()
        {
            const int enemyLayer = 8;
            GameObject companion = new GameObject("TargetPriorityOverrideCompanion");
            GameObject player = new GameObject("TargetPriorityOverridePlayer");
            GameObject melee = CreateEnemy(
                "TargetPriorityOverrideMelee",
                EnemyArchetypeType.Melee,
                enemyLayer,
                1f);
            GameObject ranged = null;

            try
            {
                player.AddComponent<HealthComponent>();
                companion.AddComponent<HealthComponent>();
                companion.AddComponent<CompanionRelationship>();
                CompanionSpeechBubbleUI bubble = companion.AddComponent<CompanionSpeechBubbleUI>();
                CompanionSensor sensor = companion.AddComponent<CompanionSensor>();
                WritePrivateField(sensor, "detectionRadius", 5f);
                WritePrivateField(sensor, "enemyLayerMask", (LayerMask)(1 << enemyLayer));
                WritePrivateField(sensor, "protectedPlayer", player.transform);
                InvokePrivate(sensor, "ConfigureEnemyFilter");
                CompanionCombatDialogueController dialogue =
                    companion.AddComponent<CompanionCombatDialogueController>();
                InvokePrivate(dialogue, "OnEnable");
                CompanionRunBuildState.SetTendency(CompanionSkillTendency.Suppressor);
                Physics2D.SyncTransforms();

                sensor.ScanNow();
                Assert.That(bubble.CurrentMessage, Does.Contain("Melee"));

                ranged = CreateEnemy(
                    "TargetPriorityOverrideRanged",
                    EnemyArchetypeType.Ranged,
                    enemyLayer,
                    3f);
                Physics2D.SyncTransforms();
                sensor.ScanNow();

                Assert.That(bubble.CurrentMessage, Does.Contain("Ranged"));
            }
            finally
            {
                if (ranged != null)
                {
                    UnityEngine.Object.DestroyImmediate(ranged);
                }

                UnityEngine.Object.DestroyImmediate(melee);
                UnityEngine.Object.DestroyImmediate(player);
                UnityEngine.Object.DestroyImmediate(companion);
            }
        }

        private static float EvaluateScore(
            EnemyArchetypeType archetype,
            CompanionSkillTendency tendency,
            float companionDistance,
            float playerDistance,
            bool warningActive,
            bool isCurrentTarget)
        {
            Type rulesType = RequireRuntimeType(
                "AICompanionRoguelike.Companion.CompanionTargetPriorityRules");
            MethodInfo method = rulesType.GetMethod(
                "EvaluateScore",
                BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);

            return (float)method.Invoke(
                null,
                new object[]
                {
                    archetype,
                    tendency,
                    companionDistance,
                    playerDistance,
                    warningActive,
                    isCurrentTarget
                });
        }

        private static GameObject CreateEnemy(
            string objectName,
            EnemyArchetypeType archetype,
            int layer,
            float positionX)
        {
            GameObject enemy = new GameObject(objectName)
            {
                layer = layer
            };
            enemy.transform.position = new Vector3(positionX, 0f, 0f);
            enemy.AddComponent<BoxCollider2D>();
            enemy.AddComponent<HealthComponent>();
            EnemyArchetype2D role = enemy.AddComponent<EnemyArchetype2D>();
            role.Configure(
                archetype,
                EnemyArchetypeRules.GetDisplayName(archetype),
                EnemyArchetypeRules.GetReadableRoleHint(archetype),
                EnemyArchetypeRules.GetRoleColor(archetype));
            return enemy;
        }

        private static Type RequireRuntimeType(string fullName)
        {
            Type type = Type.GetType($"{fullName}, Assembly-CSharp");
            Assert.NotNull(type, $"Runtime type {fullName} should exist.");
            return type;
        }

        private static void WritePrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"{target.GetType().Name} should define {fieldName}.");
            field.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method, $"{target.GetType().Name} should define {methodName}.");
            method.Invoke(target, null);
        }
    }
}
