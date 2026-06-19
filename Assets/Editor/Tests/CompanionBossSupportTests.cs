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
    }
}
