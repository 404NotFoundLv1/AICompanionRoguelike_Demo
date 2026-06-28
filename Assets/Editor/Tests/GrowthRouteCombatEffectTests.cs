using System;
using System.Reflection;
using AICompanionRoguelike.Character;
using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Companion;
using AICompanionRoguelike.Memory;
using AICompanionRoguelike.Roguelike;
using NUnit.Framework;
using UnityEngine;

namespace AICompanionRoguelike.Tests
{
    public sealed class GrowthRouteCombatEffectTests
    {
        [Test]
        public void PlayerRouteIncreasesEffectivePlayerDamage()
        {
            GameObject player = CreatePlayer("Player");
            GameObject runObject = CreateRunManagerObject("PlayerRouteEffectRunManager");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();
                PlayerCombat2D combat = player.GetComponent<PlayerCombat2D>();

                Invoke(runManager, "ApplyReward", ParseRewardType("PlayerDamage"));
                Invoke(runManager, "ApplyReward", ParseRewardType("MoveSpeed"));

                Assert.That(ReadFloatProperty(runManager, "PlayerRouteDamageMultiplier"), Is.GreaterThan(1f));
                Assert.That(ReadFloatProperty(combat, "EffectiveDamage"), Is.GreaterThan(combat.Damage));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
                UnityEngine.Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void CounterplayRouteCreatesDodgeCounterDamageWithoutDodgeReward()
        {
            GameObject player = CreatePlayer("Player");
            GameObject enemy = CreateEnemy("CounterplayRouteTarget");
            GameObject runObject = CreateRunManagerObject("CounterplayRouteEffectRunManager");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();
                PlayerCounterplayFeedback counterplay = player.GetComponent<PlayerCounterplayFeedback>();
                HealthComponent enemyHealth = enemy.GetComponent<HealthComponent>();
                DamageInfo baseDamage = new DamageInfo(40f, DamageSourceType.Player, player);

                Invoke(runManager, "ApplyReward", ParseRewardType("DashCooldown"));
                Invoke(runManager, "ApplyReward", ParseRewardType("RecoveryWindow"));
                counterplay.ReportProjectileDodge();
                DamageInfo modifiedDamage = counterplay.ModifyOutgoingDamage(enemyHealth, baseDamage);

                Assert.That(ReadFloatProperty(runManager, "CounterplayRouteDodgeBoostDurationBonus"), Is.GreaterThan(0f));
                Assert.That(ReadFloatProperty(counterplay, "EffectiveDodgeDamageMultiplier"), Is.GreaterThan(1f));
                Assert.That(modifiedDamage.damage, Is.GreaterThan(baseDamage.damage));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
                UnityEngine.Object.DestroyImmediate(enemy);
                UnityEngine.Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void CompanionRouteLowersEffectiveCompanionCooldown()
        {
            GameObject companion = CreateCompanion("CompanionRouteEffectCompanion");
            GameObject runObject = CreateRunManagerObject("CompanionRouteEffectRunManager");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();
                CompanionCombat combat = companion.GetComponent<CompanionCombat>();

                Invoke(runManager, "ApplyReward", ParseRewardType("CompanionCooldown"));
                Invoke(runManager, "ApplyReward", ParseRewardType("CompanionCooldown"));

                Assert.That(ReadFloatProperty(runManager, "CompanionRouteCooldownMultiplier"), Is.LessThan(1f));
                Assert.That(ReadFloatProperty(combat, "EffectiveCooldown"), Is.LessThan(combat.Cooldown));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
                UnityEngine.Object.DestroyImmediate(companion);
            }
        }

        [Test]
        public void SurvivalRouteRaisesEffectiveRescueHealth()
        {
            GameObject player = CreatePlayer("Player");
            GameObject runObject = CreateRunManagerObject("SurvivalRouteEffectRunManager");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();
                BondRescueSystem rescue = player.GetComponent<BondRescueSystem>();

                Invoke(runManager, "ApplyReward", ParseRewardType("MaxHealth"));
                Invoke(runManager, "ApplyReward", ParseRewardType("BondRescueHealth"));

                Assert.That(ReadFloatProperty(runManager, "SurvivalRouteRescueHealthBonus"), Is.GreaterThan(0f));
                Assert.That(ReadFloatProperty(rescue, "EffectiveRescueHealth"), Is.GreaterThan(rescue.RescueHealth));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
                UnityEngine.Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void BuildRouteGrantsTemporaryTacticalSupportBonusLevel()
        {
            GameObject runObject = CreateRunManagerObject("BuildRouteEffectRunManager");

            try
            {
                RunManager runManager = runObject.GetComponent<RunManager>();
                CompanionRunBuildState.SetTendency(CompanionSkillTendency.Guardian);

                Invoke(runManager, "ApplyReward", ParseRewardType("GuardianBuildUpgrade"));
                Invoke(runManager, "ApplyReward", ParseRewardType("GuardianBuildUpgrade"));

                int bonusLevel = ReadIntProperty(runManager, "BuildRouteBonusLevel");
                CompanionRelationshipProfileSnapshot profile = CompanionRelationshipProfile.Evaluate(
                    50,
                    50,
                    Array.Empty<RelationshipMemoryTagScore>());
                CompanionTacticalSupportTuning baseTuning = EvaluateTacticalSupportWithRouteBonus(
                    profile,
                    CompanionSkillTendency.Guardian,
                    0);
                CompanionTacticalSupportTuning routeTuning = EvaluateTacticalSupportWithRouteBonus(
                    profile,
                    CompanionSkillTendency.Guardian,
                    bonusLevel);

                Assert.That(bonusLevel, Is.GreaterThan(0));
                Assert.That(routeTuning.GuardDamageMultiplier, Is.LessThan(baseTuning.GuardDamageMultiplier));
            }
            finally
            {
                CompanionRunBuildState.Reset();
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        private static GameObject CreateRunManagerObject(string objectName)
        {
            GameObject runObject = new GameObject(objectName);
            runObject.AddComponent<RoomManager>();
            runObject.AddComponent<RunManager>();
            return runObject;
        }

        private static GameObject CreatePlayer(string objectName)
        {
            GameObject player = new GameObject(objectName);
            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            player.AddComponent<BoxCollider2D>().size = new Vector2(0.8f, 1.6f);
            player.AddComponent<PlayerInputReader>();
            player.AddComponent<HealthComponent>().SetMaxHealth(100f, true);
            player.AddComponent<PlayerMovement2D>();
            player.AddComponent<PlayerCombat2D>();
            player.AddComponent<PlayerCounterplayFeedback>();
            player.AddComponent<BondRescueSystem>();
            return player;
        }

        private static GameObject CreateEnemy(string objectName)
        {
            GameObject enemy = new GameObject(objectName);
            Rigidbody2D body = enemy.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            enemy.AddComponent<BoxCollider2D>().size = new Vector2(0.8f, 1.6f);
            enemy.AddComponent<HealthComponent>().SetMaxHealth(100f, true);
            return enemy;
        }

        private static GameObject CreateCompanion(string objectName)
        {
            GameObject companion = new GameObject(objectName);
            companion.AddComponent<HealthComponent>().SetMaxHealth(80f, true);
            companion.AddComponent<CompanionSensor>();
            companion.AddComponent<CompanionCombat>();
            return companion;
        }

        private static object ParseRewardType(string rewardName)
        {
            Type rewardType = RequireRuntimeType("AICompanionRoguelike.Roguelike.RunRewardType");
            return Enum.Parse(rewardType, rewardName);
        }

        private static Type RequireRuntimeType(string fullName)
        {
            Type type = Type.GetType($"{fullName}, Assembly-CSharp");
            Assert.NotNull(type, $"Runtime type {fullName} should exist.");
            return type;
        }

        private static float ReadFloatProperty(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(property, $"{target.GetType().Name} should expose {propertyName}.");
            return (float)property.GetValue(target);
        }

        private static int ReadIntProperty(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(property, $"{target.GetType().Name} should expose {propertyName}.");
            return (int)property.GetValue(target);
        }

        private static CompanionTacticalSupportTuning EvaluateTacticalSupportWithRouteBonus(
            CompanionRelationshipProfileSnapshot profile,
            CompanionSkillTendency tendency,
            int routeBonusLevel)
        {
            MethodInfo method = typeof(CompanionTacticalSupportRules).GetMethod(
                "Evaluate",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(CompanionRelationshipProfileSnapshot), typeof(CompanionSkillTendency), typeof(int) },
                null);
            Assert.NotNull(method, "CompanionTacticalSupportRules should expose Evaluate(profile, tendency, routeBonusLevel).");
            return (CompanionTacticalSupportTuning)method.Invoke(null, new object[] { profile, tendency, routeBonusLevel });
        }

        private static object Invoke(object target, string methodName, params object[] parameters)
        {
            MethodInfo[] methods = target.GetType().GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name == methodName
                    && methods[i].GetParameters().Length == parameters.Length)
                {
                    return methods[i].Invoke(target, parameters);
                }
            }

            Assert.Fail($"{target.GetType().Name} should expose {methodName} with {parameters.Length} parameters.");
            return null;
        }
    }
}
