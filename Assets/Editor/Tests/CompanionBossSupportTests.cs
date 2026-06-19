using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Companion;
using AICompanionRoguelike.Enemy;
using AICompanionRoguelike.Memory;
using NUnit.Framework;
using UnityEngine;

namespace AICompanionRoguelike.Tests
{
    public sealed class CompanionBossSupportTests
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
        public void BossSupportShieldShowsVisualOnlyWhileActive()
        {
            GameObject player = new GameObject("PlayerUnderTest");

            try
            {
                PlayerBossSupportShield shield = player.AddComponent<PlayerBossSupportShield>();

                shield.Activate(1f, 0.5f);

                Transform visual = player.transform.Find("BossSupportShieldVisual");
                Assert.NotNull(visual);
                Assert.True(visual.gameObject.activeSelf);
                Assert.NotNull(visual.GetComponent<SpriteRenderer>());

                shield.Tick(1f);

                Assert.False(visual.gameObject.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void SupportFeedbackExplainsTrustAndCooldownBlockedSupport()
        {
            GameObject player = new GameObject("PlayerUnderTest");
            GameObject companion = new GameObject("CompanionUnderTest");
            GameObject relationshipObject = new GameObject("RelationshipUnderTest");
            GameObject boss = new GameObject("BossUnderTest");
            GameObject secondBoss = new GameObject("SecondBossUnderTest");

            try
            {
                player.AddComponent<HealthComponent>().SetMaxHealth(100f, true);
                PlayerBossSupportShield shield = player.AddComponent<PlayerBossSupportShield>();
                CompanionRelationship relationship = relationshipObject.AddComponent<CompanionRelationship>();

                CompanionBossSupport support = companion.AddComponent<CompanionBossSupport>();
                support.Configure(
                    player.transform,
                    relationship,
                    shield,
                    requiredTrust: 90,
                    shieldDuration: 2f,
                    incomingDamageMultiplier: 0.5f,
                    cooldown: 5f);

                BossTelegraphedAttack2D bossAttack = boss.AddComponent<BossTelegraphedAttack2D>();
                bossAttack.Configure(
                    player.transform,
                    damage: 18f,
                    triggerRange: 5f,
                    warningDuration: 0.5f,
                    cooldown: 1f,
                    attackSize: new Vector2(2f, 1f));
                support.SetBossAttack(bossAttack);

                Assert.True(bossAttack.TryStartWarning());
                Assert.AreEqual("TrustTooLow", ReadPublicProperty(support, "LastFeedbackState").ToString());
                Assert.That((string)ReadPublicProperty(support, "LastFeedbackMessage"), Does.Contain("Trust"));

                relationship.ApplyMemoryEvent("Test Trust", 50, 0, RelationshipMemoryTag.Reliable);
                support.Configure(
                    player.transform,
                    relationship,
                    shield,
                    requiredTrust: 30,
                    shieldDuration: 2f,
                    incomingDamageMultiplier: 0.5f,
                    cooldown: 5f);

                BossTelegraphedAttack2D secondBossAttack = secondBoss.AddComponent<BossTelegraphedAttack2D>();
                secondBossAttack.Configure(
                    player.transform,
                    damage: 18f,
                    triggerRange: 5f,
                    warningDuration: 0.5f,
                    cooldown: 1f,
                    attackSize: new Vector2(2f, 1f));
                support.SetBossAttack(secondBossAttack);

                Assert.True(secondBossAttack.TryStartWarning());
                Assert.AreEqual("Activated", ReadPublicProperty(support, "LastFeedbackState").ToString());

                GameObject cooldownBoss = new GameObject("CooldownBossUnderTest");
                try
                {
                    BossTelegraphedAttack2D cooldownAttack = cooldownBoss.AddComponent<BossTelegraphedAttack2D>();
                    cooldownAttack.Configure(
                        player.transform,
                        damage: 18f,
                        triggerRange: 5f,
                        warningDuration: 0.5f,
                        cooldown: 1f,
                        attackSize: new Vector2(2f, 1f));
                    support.SetBossAttack(cooldownAttack);

                    Assert.True(cooldownAttack.TryStartWarning());
                    Assert.AreEqual("Cooldown", ReadPublicProperty(support, "LastFeedbackState").ToString());
                    Assert.That((string)ReadPublicProperty(support, "LastFeedbackMessage"), Does.Contain("cooldown"));
                }
                finally
                {
                    Object.DestroyImmediate(cooldownBoss);
                }
            }
            finally
            {
                Object.DestroyImmediate(secondBoss);
                Object.DestroyImmediate(boss);
                Object.DestroyImmediate(relationshipObject);
                Object.DestroyImmediate(companion);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void BossWarningActivatesShieldWhenTrustIsHighEnough()
        {
            GameObject player = new GameObject("PlayerUnderTest");
            GameObject companion = new GameObject("CompanionUnderTest");
            GameObject relationshipObject = new GameObject("RelationshipUnderTest");
            GameObject boss = new GameObject("BossUnderTest");

            try
            {
                HealthComponent playerHealth = player.AddComponent<HealthComponent>();
                playerHealth.SetMaxHealth(100f, true);
                PlayerBossSupportShield shield = player.AddComponent<PlayerBossSupportShield>();

                CompanionRelationship relationship = relationshipObject.AddComponent<CompanionRelationship>();
                relationship.ApplyMemoryEvent("Test Trust", 50, 0, RelationshipMemoryTag.Reliable);

                CompanionBossSupport support = companion.AddComponent<CompanionBossSupport>();
                support.Configure(
                    player.transform,
                    relationship,
                    shield,
                    requiredTrust: 30,
                    shieldDuration: 2f,
                    incomingDamageMultiplier: 0.5f,
                    cooldown: 5f);

                BossTelegraphedAttack2D bossAttack = boss.AddComponent<BossTelegraphedAttack2D>();
                bossAttack.Configure(
                    player.transform,
                    damage: 18f,
                    triggerRange: 5f,
                    warningDuration: 0.5f,
                    cooldown: 1f,
                    attackSize: new Vector2(2f, 1f));
                support.SetBossAttack(bossAttack);

                int promptCount = 0;
                int activationCount = 0;
                support.SupportPrompted += _ => promptCount++;
                support.SupportActivated += _ => activationCount++;

                Assert.True(bossAttack.TryStartWarning());

                Assert.AreEqual(1, promptCount);
                Assert.AreEqual(1, activationCount);
                Assert.True(shield.IsActive);

                playerHealth.TakeDamage(new DamageInfo(20f, DamageSourceType.Enemy, boss));

                Assert.AreEqual(90f, playerHealth.CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(boss);
                Object.DestroyImmediate(relationshipObject);
                Object.DestroyImmediate(companion);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void LowTrustStillWarnsButDoesNotActivateShield()
        {
            GameObject player = new GameObject("PlayerUnderTest");
            GameObject companion = new GameObject("CompanionUnderTest");
            GameObject relationshipObject = new GameObject("RelationshipUnderTest");
            GameObject boss = new GameObject("BossUnderTest");

            try
            {
                HealthComponent playerHealth = player.AddComponent<HealthComponent>();
                playerHealth.SetMaxHealth(100f, true);
                PlayerBossSupportShield shield = player.AddComponent<PlayerBossSupportShield>();
                CompanionRelationship relationship = relationshipObject.AddComponent<CompanionRelationship>();

                CompanionBossSupport support = companion.AddComponent<CompanionBossSupport>();
                support.Configure(
                    player.transform,
                    relationship,
                    shield,
                    requiredTrust: 90,
                    shieldDuration: 2f,
                    incomingDamageMultiplier: 0.5f,
                    cooldown: 5f);

                BossTelegraphedAttack2D bossAttack = boss.AddComponent<BossTelegraphedAttack2D>();
                bossAttack.Configure(
                    player.transform,
                    damage: 18f,
                    triggerRange: 5f,
                    warningDuration: 0.5f,
                    cooldown: 1f,
                    attackSize: new Vector2(2f, 1f));
                support.SetBossAttack(bossAttack);

                int promptCount = 0;
                int activationCount = 0;
                support.SupportPrompted += _ => promptCount++;
                support.SupportActivated += _ => activationCount++;

                Assert.True(bossAttack.TryStartWarning());

                Assert.AreEqual(1, promptCount);
                Assert.AreEqual(0, activationCount);
                Assert.False(shield.IsActive);

                playerHealth.TakeDamage(new DamageInfo(20f, DamageSourceType.Enemy, boss));

                Assert.AreEqual(80f, playerHealth.CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(boss);
                Object.DestroyImmediate(relationshipObject);
                Object.DestroyImmediate(companion);
                Object.DestroyImmediate(player);
            }
        }

        private static object ReadPublicProperty(object target, string propertyName)
        {
            System.Reflection.PropertyInfo property = target.GetType().GetProperty(propertyName);
            Assert.NotNull(property, $"{target.GetType().Name} should expose {propertyName}.");
            return property.GetValue(target);
        }
    }
}
