using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Enemy;
using NUnit.Framework;
using UnityEngine;

namespace AICompanionRoguelike.Tests
{
    public sealed class BossTelegraphedAttack2DTests
    {
        [Test]
        public void WarningWindowDelaysDamageUntilImpact()
        {
            GameObject boss = new GameObject("BossUnderTest");
            GameObject player = new GameObject("PlayerUnderTest");

            try
            {
                HealthComponent playerHealth = player.AddComponent<HealthComponent>();
                playerHealth.SetMaxHealth(100f, true);
                player.transform.position = new Vector3(1f, 0f, 0f);

                BossTelegraphedAttack2D attack = boss.AddComponent<BossTelegraphedAttack2D>();
                attack.Configure(
                    player.transform,
                    damage: 18f,
                    triggerRange: 5f,
                    warningDuration: 0.5f,
                    cooldown: 1f,
                    attackSize: new Vector2(2.5f, 1.5f));

                Assert.True(attack.TryStartWarning());

                attack.Tick(0.49f);

                Assert.True(attack.IsWarningActive);
                Assert.AreEqual(100f, playerHealth.CurrentHealth);

                attack.Tick(0.02f);

                Assert.False(attack.IsWarningActive);
                Assert.AreEqual(82f, playerHealth.CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(boss);
            }
        }

        [Test]
        public void PlayerCanAvoidImpactByLeavingWarnedArea()
        {
            GameObject boss = new GameObject("BossUnderTest");
            GameObject player = new GameObject("PlayerUnderTest");

            try
            {
                HealthComponent playerHealth = player.AddComponent<HealthComponent>();
                playerHealth.SetMaxHealth(100f, true);
                player.transform.position = new Vector3(0.5f, 0f, 0f);

                BossTelegraphedAttack2D attack = boss.AddComponent<BossTelegraphedAttack2D>();
                attack.Configure(
                    player.transform,
                    damage: 18f,
                    triggerRange: 5f,
                    warningDuration: 0.5f,
                    cooldown: 1f,
                    attackSize: new Vector2(2f, 1.5f));

                Assert.True(attack.TryStartWarning());

                player.transform.position = new Vector3(4f, 0f, 0f);
                attack.Tick(0.5f);

                Assert.AreEqual(100f, playerHealth.CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(boss);
            }
        }
    }
}
