using System.Collections.Generic;
using UnityEngine;

namespace AICompanionRoguelike.Memory
{
    public enum CompanionBondTier
    {
        Distant,
        Trusted,
        Synchronized
    }

    public enum CompanionDialogueTone
    {
        Guarded,
        Neutral,
        Warm
    }

    public readonly struct CompanionRelationshipProfileSnapshot
    {
        public CompanionRelationshipProfileSnapshot(
            CompanionBondTier tier,
            CompanionDialogueTone tone,
            float qteCooldownMultiplier,
            bool hasDominantMemory,
            RelationshipMemoryTag dominantMemoryTag,
            int dominantMemoryScore)
        {
            Tier = tier;
            Tone = tone;
            QteCooldownMultiplier = qteCooldownMultiplier;
            HasDominantMemory = hasDominantMemory;
            DominantMemoryTag = dominantMemoryTag;
            DominantMemoryScore = dominantMemoryScore;
        }

        public CompanionBondTier Tier { get; }
        public CompanionDialogueTone Tone { get; }
        public float QteCooldownMultiplier { get; }
        public bool HasDominantMemory { get; }
        public RelationshipMemoryTag DominantMemoryTag { get; }
        public int DominantMemoryScore { get; }
    }

    public static class CompanionRelationshipProfile
    {
        private const int DistantThreshold = 40;
        private const int SynchronizedThreshold = 70;
        private const float LowTrustQteCooldownMultiplier = 1.25f;
        private const float NormalQteCooldownMultiplier = 1f;
        private const float HighTrustQteCooldownMultiplier = 0.75f;

        public static CompanionRelationshipProfileSnapshot Evaluate(
            int trust,
            int affection,
            IReadOnlyList<RelationshipMemoryTagScore> memoryTags)
        {
            int clampedTrust = Mathf.Clamp(trust, 0, 100);
            int clampedAffection = Mathf.Clamp(affection, 0, 100);
            CompanionBondTier tier = GetTier(clampedTrust, clampedAffection);
            CompanionDialogueTone tone = GetTone(clampedAffection);
            FindDominantMemory(
                memoryTags,
                out bool hasDominantMemory,
                out RelationshipMemoryTag dominantMemoryTag,
                out int dominantMemoryScore);

            return new CompanionRelationshipProfileSnapshot(
                tier,
                tone,
                GetQteCooldownMultiplier(clampedTrust),
                hasDominantMemory,
                dominantMemoryTag,
                dominantMemoryScore);
        }

        public static float GetQteCooldownMultiplier(int trust)
        {
            int clampedTrust = Mathf.Clamp(trust, 0, 100);
            if (clampedTrust < DistantThreshold)
            {
                return LowTrustQteCooldownMultiplier;
            }

            return clampedTrust >= SynchronizedThreshold
                ? HighTrustQteCooldownMultiplier
                : NormalQteCooldownMultiplier;
        }

        private static CompanionBondTier GetTier(int trust, int affection)
        {
            if (trust < DistantThreshold || affection < DistantThreshold)
            {
                return CompanionBondTier.Distant;
            }

            return trust >= SynchronizedThreshold && affection >= SynchronizedThreshold
                ? CompanionBondTier.Synchronized
                : CompanionBondTier.Trusted;
        }

        private static CompanionDialogueTone GetTone(int affection)
        {
            if (affection < DistantThreshold)
            {
                return CompanionDialogueTone.Guarded;
            }

            return affection >= SynchronizedThreshold
                ? CompanionDialogueTone.Warm
                : CompanionDialogueTone.Neutral;
        }

        private static void FindDominantMemory(
            IReadOnlyList<RelationshipMemoryTagScore> memoryTags,
            out bool hasDominantMemory,
            out RelationshipMemoryTag dominantMemoryTag,
            out int dominantMemoryScore)
        {
            hasDominantMemory = false;
            dominantMemoryTag = default;
            dominantMemoryScore = 0;

            if (memoryTags == null)
            {
                return;
            }

            for (int i = 0; i < memoryTags.Count; i++)
            {
                RelationshipMemoryTagScore entry = memoryTags[i];
                if (entry.score <= 0)
                {
                    continue;
                }

                bool isHigherScore = !hasDominantMemory || entry.score > dominantMemoryScore;
                bool winsTie = hasDominantMemory
                    && entry.score == dominantMemoryScore
                    && entry.tag < dominantMemoryTag;
                if (!isHigherScore && !winsTie)
                {
                    continue;
                }

                hasDominantMemory = true;
                dominantMemoryTag = entry.tag;
                dominantMemoryScore = entry.score;
            }
        }
    }
}
