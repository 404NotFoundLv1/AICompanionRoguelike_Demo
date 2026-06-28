using System;
using System.Reflection;
using AICompanionRoguelike.Character;
using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Enemy;
using NUnit.Framework;
using UnityEngine;

namespace AICompanionRoguelike.Tests
{
    public sealed class PlayerCounterplayFeedbackTests
    {
        [Test]
        public void DashInvincibilityCancelsEnemyDamageAndReportsDodge()
        {
            GameObject player = CreatePlayer("CounterplayDashPlayer");

            try
            {
                HealthComponent health = player.GetComponent<HealthComponent>();
                PlayerMovement2D movement = player.GetComponent<PlayerMovement2D>();
                Component counterplay = AddCounterplay(player);
                WriteAutoProperty(movement, "IsInvincible", true);

                health.TakeDamage(new DamageInfo(25f, DamageSourceType.Enemy, new GameObject("DashEnemy")));

                Assert.AreEqual(100f, health.CurrentHealth);
                Assert.AreEqual("DashDodge", ReadProperty(counterplay, "LastFeedbackKind").ToString());
                Assert.That((string)ReadProperty(counterplay, "LastFeedbackMessage"), Does.Contain("dodge"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(player);
                DestroyNamed("DashEnemy");
            }
        }

        [Test]
        public void PostHitInvulnerabilityBlocksRapidFollowUpDamage()
        {
            GameObject player = CreatePlayer("CounterplayRecoveryPlayer");
            GameObject enemy = new GameObject("RecoveryEnemy");

            try
            {
                HealthComponent health = player.GetComponent<HealthComponent>();
                Component counterplay = AddCounterplay(player);

                health.TakeDamage(new DamageInfo(20f, DamageSourceType.Enemy, enemy));
                Assert.AreEqual(80f, health.CurrentHealth);
                Assert.True((bool)ReadProperty(counterplay, "IsRecovering"));

                health.TakeDamage(new DamageInfo(20f, DamageSourceType.Enemy, enemy));

                Assert.AreEqual(80f, health.CurrentHealth);
                Assert.AreEqual("RecoveryBlock", ReadProperty(counterplay, "LastFeedbackKind").ToString());
                Assert.That((string)ReadProperty(counterplay, "LastFeedbackMessage"), Does.Contain("Recovering"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemy);
                UnityEngine.Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void RangedProjectileResolvingIntoDashingPlayerCountsAsDodge()
        {
            GameObject player = CreatePlayer("CounterplayProjectilePlayer");
            GameObject enemy = new GameObject("ProjectileEnemy");

            try
            {
                HealthComponent health = player.GetComponent<HealthComponent>();
                PlayerMovement2D movement = player.GetComponent<PlayerMovement2D>();
                Component counterplay = AddCounterplay(player);
                WriteAutoProperty(movement, "IsInvincible", true);
                bool? resolvedHit = null;

                EnemyProjectile2D projectile = EnemyProjectile2D.Create(
                    new Vector2(-1f, 0f),
                    Vector2.right,
                    health,
                    20f,
                    enemy,
                    6f,
                    Color.cyan,
                    hit => resolvedHit = hit);

                projectile.Tick(0.25f);

                Assert.AreEqual(100f, health.CurrentHealth);
                Assert.True(projectile.IsResolved);
                Assert.AreEqual(false, resolvedHit);
                Assert.AreEqual("ProjectileDodge", ReadProperty(counterplay, "LastFeedbackKind").ToString());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemy);
                UnityEngine.Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void HittingGuardOpeningReportsCounterFeedback()
        {
            GameObject player = CreatePlayer("CounterplayGuardPlayer");
            GameObject guard = CreateGuard("CounterplayGuardEnemy", new Vector3(0.9f, 0f, 0f));

            try
            {
                PlayerCombat2D combat = player.GetComponent<PlayerCombat2D>();
                Component counterplay = AddCounterplay(player);
                EnemyAttackPattern2D pattern = guard.GetComponent<EnemyAttackPattern2D>();
                WritePrivateField(pattern, "guardVulnerabilityTimer", 1f);

                Invoke(combat, "Attack");

                Assert.AreEqual("GuardOpening", ReadProperty(counterplay, "LastFeedbackKind").ToString());
                Assert.That((string)ReadProperty(counterplay, "LastFeedbackMessage"), Does.Contain("Guard opening"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(guard);
                UnityEngine.Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void CounterplayComponentExposesDeterministicFeedbackApi()
        {
            GameObject player = CreatePlayer("CounterplayApiPlayer");

            try
            {
                Component counterplay = AddCounterplay(player);
                Assert.NotNull(counterplay.GetType().GetMethod("Tick", new[] { typeof(float) }));
                Assert.NotNull(counterplay.GetType().GetMethod("ClearFeedback", Type.EmptyTypes));
                Assert.NotNull(counterplay.GetType().GetEvent("FeedbackIssued"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(player);
            }
        }

        private static GameObject CreatePlayer(string objectName)
        {
            GameObject player = new GameObject(objectName);
            player.transform.position = Vector3.zero;
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

        private static GameObject CreateGuard(string objectName, Vector3 position)
        {
            GameObject guard = new GameObject(objectName);
            guard.transform.position = position;
            Rigidbody2D body = guard.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            guard.AddComponent<BoxCollider2D>().size = new Vector2(0.8f, 1.6f);
            guard.AddComponent<HealthComponent>().SetMaxHealth(100f, true);
            guard.AddComponent<EnemyAttack2D>();
            guard.AddComponent<EnemyController2D>();
            EnemyAttackPattern2D pattern = guard.AddComponent<EnemyAttackPattern2D>();
            pattern.Configure(EnemyArchetypeType.Guard);
            return guard;
        }

        private static Component AddCounterplay(GameObject player)
        {
            Type type = Type.GetType("AICompanionRoguelike.Combat.PlayerCounterplayFeedback, Assembly-CSharp");
            Assert.NotNull(type, "PlayerCounterplayFeedback should exist.");
            return player.AddComponent(type);
        }

        private static object ReadProperty(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(property, $"{target.GetType().Name} should expose {propertyName}.");
            return property.GetValue(target);
        }

        private static void WriteAutoProperty(object target, string propertyName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                $"<{propertyName}>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"{target.GetType().Name} should have an auto-property backing field for {propertyName}.");
            field.SetValue(target, value);
        }

        private static void WritePrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
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

        private static void DestroyNamed(string objectName)
        {
            GameObject target = GameObject.Find(objectName);
            if (target != null)
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}
