using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Enemy;
using NUnit.Framework;
using UnityEngine;

namespace AICompanionRoguelike.Tests
{
    public sealed class BossPhaseControllerTests
    {
        [Test]
        public void EntersPhaseTwoOnceWhenHealthFallsToThreshold()
        {
            GameObject boss = new GameObject("BossUnderTest");

            try
            {
                HealthComponent health = boss.AddComponent<HealthComponent>();
                EnemyAttack2D attack = boss.AddComponent<EnemyAttack2D>();
                BossPhaseController phaseController = boss.AddComponent<BossPhaseController>();
                phaseController.Configure(
                    health,
                    attack,
                    phaseTwoHealthRatio: 0.5f,
                    phaseTwoDamageMultiplier: 1.4f,
                    phaseTwoScaleMultiplier: 1.1f);

                health.SetMaxHealth(100f, true);
                attack.MultiplyDamage(2f);

                int phaseTwoTriggeredCount = 0;
                phaseController.PhaseTwoStarted += _ => phaseTwoTriggeredCount++;

                health.TakeDamage(new DamageInfo(49f, DamageSourceType.Player, null));

                Assert.False(phaseController.IsInPhaseTwo);
                Assert.AreEqual(20f, attack.Damage);

                health.TakeDamage(new DamageInfo(1f, DamageSourceType.Player, null));
                health.TakeDamage(new DamageInfo(10f, DamageSourceType.Player, null));

                Assert.True(phaseController.IsInPhaseTwo);
                Assert.AreEqual(1, phaseTwoTriggeredCount);
                Assert.AreEqual(28f, attack.Damage);
                Assert.AreEqual(new Vector3(1.1f, 1.1f, 1.1f), boss.transform.localScale);
            }
            finally
            {
                Object.DestroyImmediate(boss);
            }
        }
    }
}
