using System;
using System.Collections.Generic;
using AICompanionRoguelike.Memory;

namespace AICompanionRoguelike.Save
{
    [Serializable]
    public sealed class GameSaveData
    {
        public const int CurrentVersion = 1;

        public int saveVersion = CurrentVersion;
        public string savedAtUtc = string.Empty;
        public RelationshipSaveData relationship = new RelationshipSaveData();
        public MetaProgressionSaveData metaProgression = new MetaProgressionSaveData();

        public void Normalize()
        {
            if (relationship == null)
            {
                relationship = new RelationshipSaveData();
            }

            if (metaProgression == null)
            {
                metaProgression = new MetaProgressionSaveData();
            }

            relationship.Normalize();
            metaProgression.Normalize();
        }
    }

    [Serializable]
    public sealed class RelationshipSaveData
    {
        public bool hasData;
        public int trust;
        public int affection;
        public List<RelationshipMemoryTagScore> memoryTags = new List<RelationshipMemoryTagScore>();

        public void Normalize()
        {
            if (memoryTags == null)
            {
                memoryTags = new List<RelationshipMemoryTagScore>();
            }
        }
    }

    [Serializable]
    public sealed class MetaProgressionSaveData
    {
        public bool hasData;
        public int coreFragments;
        public int playerMaxHealthLevel;
        public int playerDamageLevel;
        public int companionCooldownLevel;

        public void Normalize()
        {
            coreFragments = Math.Max(0, coreFragments);
            playerMaxHealthLevel = Math.Max(0, playerMaxHealthLevel);
            playerDamageLevel = Math.Max(0, playerDamageLevel);
            companionCooldownLevel = Math.Max(0, companionCooldownLevel);
        }
    }
}
