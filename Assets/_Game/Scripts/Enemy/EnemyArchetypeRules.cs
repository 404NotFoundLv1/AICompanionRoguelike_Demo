using UnityEngine;

namespace AICompanionRoguelike.Enemy
{
    public static class EnemyArchetypeRules
    {
        public static string GetDisplayName(EnemyArchetypeType archetypeType)
        {
            switch (archetypeType)
            {
                case EnemyArchetypeType.Ranged:
                    return "Ranged";
                case EnemyArchetypeType.Guard:
                    return "Guard";
                default:
                    return "Melee";
            }
        }

        public static string GetReadableRoleHint(EnemyArchetypeType archetypeType)
        {
            switch (archetypeType)
            {
                case EnemyArchetypeType.Ranged:
                    return "range pressure: longer warning lane, lower health";
                case EnemyArchetypeType.Guard:
                    return "slow guard: high health, slower movement, longer windup";
                default:
                    return "close pressure: standard chase and short warning";
            }
        }

        public static Color GetRoleColor(EnemyArchetypeType archetypeType)
        {
            switch (archetypeType)
            {
                case EnemyArchetypeType.Ranged:
                    return new Color(0.35f, 0.78f, 1f, 1f);
                case EnemyArchetypeType.Guard:
                    return new Color(0.92f, 0.84f, 0.36f, 1f);
                default:
                    return new Color(1f, 0.42f, 0.28f, 1f);
            }
        }

        public static float GetHealthMultiplier(EnemyArchetypeType archetypeType)
        {
            switch (archetypeType)
            {
                case EnemyArchetypeType.Ranged:
                    return 0.85f;
                case EnemyArchetypeType.Guard:
                    return 1.75f;
                default:
                    return 1f;
            }
        }

        public static float GetDamageMultiplier(EnemyArchetypeType archetypeType)
        {
            switch (archetypeType)
            {
                case EnemyArchetypeType.Ranged:
                    return 0.75f;
                case EnemyArchetypeType.Guard:
                    return 1.2f;
                default:
                    return 1f;
            }
        }

        public static float GetScaleMultiplier(EnemyArchetypeType archetypeType)
        {
            switch (archetypeType)
            {
                case EnemyArchetypeType.Ranged:
                    return 0.95f;
                case EnemyArchetypeType.Guard:
                    return 1.18f;
                default:
                    return 1f;
            }
        }

        public static float GetAttackRange(EnemyArchetypeType archetypeType)
        {
            switch (archetypeType)
            {
                case EnemyArchetypeType.Ranged:
                    return 3.25f;
                case EnemyArchetypeType.Guard:
                    return 1.35f;
                default:
                    return 1.2f;
            }
        }

        public static float GetCooldown(EnemyArchetypeType archetypeType)
        {
            switch (archetypeType)
            {
                case EnemyArchetypeType.Ranged:
                    return 1.35f;
                case EnemyArchetypeType.Guard:
                    return 1.25f;
                default:
                    return 1f;
            }
        }

        public static float GetWarningDuration(EnemyArchetypeType archetypeType)
        {
            switch (archetypeType)
            {
                case EnemyArchetypeType.Ranged:
                    return 0.48f;
                case EnemyArchetypeType.Guard:
                    return 0.68f;
                default:
                    return 0.35f;
            }
        }

        public static Vector2 GetWarningSize(EnemyArchetypeType archetypeType)
        {
            switch (archetypeType)
            {
                case EnemyArchetypeType.Ranged:
                    return new Vector2(2.8f, 0.65f);
                case EnemyArchetypeType.Guard:
                    return new Vector2(1.55f, 1f);
                default:
                    return new Vector2(1.35f, 0.8f);
            }
        }

        public static float GetDetectionRange(EnemyArchetypeType archetypeType)
        {
            return archetypeType == EnemyArchetypeType.Ranged ? 7.5f : 6f;
        }

        public static float GetMoveSpeed(EnemyArchetypeType archetypeType)
        {
            switch (archetypeType)
            {
                case EnemyArchetypeType.Ranged:
                    return 1.8f;
                case EnemyArchetypeType.Guard:
                    return 1.35f;
                default:
                    return 2.2f;
            }
        }

        public static float GetStopDistance(EnemyArchetypeType archetypeType)
        {
            return archetypeType == EnemyArchetypeType.Ranged ? 2.65f : 0.9f;
        }
    }
}
