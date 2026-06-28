using System;
using System.Collections;
using System.Reflection;
using AICompanionRoguelike.Character;
using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Enemy;
using AICompanionRoguelike.Roguelike;
using NUnit.Framework;
using UnityEngine;

namespace AICompanionRoguelike.Tests
{
    public sealed class CounterplayRewardLinkTests
    {
        private static readonly string[] CounterplayRewardNames =
        {
            "DashCooldown",
            "RecoveryWindow",
            "DodgeDamageBoost",
            "GuardOpeningDamage"
        };

        [Test]
        public void CounterplayRewardsAreReadableAndAvailable()
        {
            GameObject runObject = new GameObject("CounterplayRewardChoiceRunManager");

            try
            {
                runObject.AddComponent<RoomManager>();
                RunManager runManager = runObject.AddComponent<RunManager>();
                IEnumerable candidates = (IEnumerable)Invoke(runManager, "BuildRewardCandidateList");

                foreach (string rewardName in CounterplayRewardNames)
                {
                    object rewardType = ParseRewardType(rewardName);
                    Assert.That(ContainsReward(candidates, rewardName), Is.True, $"{rewardName} should be in the runtime reward pool.");

                    RunRewardChoice choice = (RunRewardChoice)Invoke(runManager, "CreateRewardChoice", rewardType);
                    Assert.That(choice.Title, Does.Contain("Counterplay"));
                    Assert.That(choice.Description, Does.Not.Empty);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
            }
        }

        [Test]
        public void DashCooldownRewardReducesPlayerDashCooldown()
        {
            GameObject player = CreatePlayer("Player");
            GameObject runObject = new GameObject("CounterplayDashRewardRunManager");

            try
            {
                runObject.AddComponent<RoomManager>();
                RunManager runManager = runObject.AddComponent<RunManager>();
                PlayerMovement2D movement = player.GetComponent<PlayerMovement2D>();
                float before = ReadFloatProperty(movement, "DashCooldown");

                Invoke(runManager, "ApplyReward", ParseRewardType("DashCooldown"));

                Assert.Less(ReadFloatProperty(movement, "DashCooldown"), before);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
                UnityEngine.Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void RecoveryWindowRewardExtendsPostHitProtection()
        {
            GameObject player = CreatePlayer("Player");
            GameObject runObject = new GameObject("CounterplayRecoveryRewardRunManager");

            try
            {
                runObject.AddComponent<RoomManager>();
                RunManager runManager = runObject.AddComponent<RunManager>();
                Component counterplay = player.AddComponent(RequireRuntimeType("AICompanionRoguelike.Combat.PlayerCounterplayFeedback"));
                float before = ReadFloatProperty(counterplay, "PostHitInvulnerabilityDuration");

                Invoke(runManager, "ApplyReward", ParseRewardType("RecoveryWindow"));

                Assert.Greater(ReadFloatProperty(counterplay, "PostHitInvulnerabilityDuration"), before);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
                UnityEngine.Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void DodgeDamageBoostRewardMultipliesPlayerDamageAfterDodge()
        {
            GameObject player = CreatePlayer("Player");
            GameObject enemy = CreateEnemy("CounterplayDodgeRewardEnemy");
            GameObject runObject = new GameObject("CounterplayDodgeRewardRunManager");

            try
            {
                runObject.AddComponent<RoomManager>();
                RunManager runManager = runObject.AddComponent<RunManager>();
                Component counterplay = player.AddComponent(RequireRuntimeType("AICompanionRoguelike.Combat.PlayerCounterplayFeedback"));
                HealthComponent enemyHealth = enemy.GetComponent<HealthComponent>();
                DamageInfo baseDamage = new DamageInfo(40f, DamageSourceType.Player, player);

                Invoke(runManager, "ApplyReward", ParseRewardType("DodgeDamageBoost"));
                Invoke(counterplay, "ReportProjectileDodge");
                DamageInfo modifiedDamage = (DamageInfo)Invoke(counterplay, "ModifyOutgoingDamage", enemyHealth, baseDamage);

                Assert.Greater(modifiedDamage.damage, baseDamage.damage);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
                UnityEngine.Object.DestroyImmediate(enemy);
                UnityEngine.Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void GuardOpeningRewardAddsDamageAgainstVulnerableGuard()
        {
            GameObject player = CreatePlayer("Player");
            GameObject guard = CreateGuard("CounterplayGuardRewardEnemy");
            GameObject runObject = new GameObject("CounterplayGuardRewardRunManager");

            try
            {
                runObject.AddComponent<RoomManager>();
                RunManager runManager = runObject.AddComponent<RunManager>();
                Component counterplay = player.AddComponent(RequireRuntimeType("AICompanionRoguelike.Combat.PlayerCounterplayFeedback"));
                HealthComponent guardHealth = guard.GetComponent<HealthComponent>();
                EnemyAttackPattern2D pattern = guard.GetComponent<EnemyAttackPattern2D>();
                WritePrivateField(pattern, "guardVulnerabilityTimer", 1f);
                DamageInfo baseDamage = new DamageInfo(40f, DamageSourceType.Player, player);

                Invoke(runManager, "ApplyReward", ParseRewardType("GuardOpeningDamage"));
                DamageInfo modifiedDamage = (DamageInfo)Invoke(counterplay, "ModifyOutgoingDamage", guardHealth, baseDamage);

                Assert.Greater(modifiedDamage.damage, baseDamage.damage);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
                UnityEngine.Object.DestroyImmediate(guard);
                UnityEngine.Object.DestroyImmediate(player);
            }
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

        private static GameObject CreateGuard(string objectName)
        {
            GameObject guard = CreateEnemy(objectName);
            guard.AddComponent<EnemyAttack2D>();
            guard.AddComponent<EnemyController2D>();
            EnemyAttackPattern2D pattern = guard.AddComponent<EnemyAttackPattern2D>();
            pattern.Configure(EnemyArchetypeType.Guard);
            return guard;
        }

        private static object ParseRewardType(string rewardName)
        {
            Type rewardType = RequireRuntimeType("AICompanionRoguelike.Roguelike.RunRewardType");
            return Enum.Parse(rewardType, rewardName);
        }

        private static bool ContainsReward(IEnumerable candidates, string rewardName)
        {
            foreach (object candidate in candidates)
            {
                if (candidate != null && candidate.ToString() == rewardName)
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
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(property, $"{target.GetType().Name} should expose {propertyName}.");
            return (float)property.GetValue(target);
        }

        private static void WritePrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"{target.GetType().Name} should define {fieldName}.");
            field.SetValue(target, value);
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
