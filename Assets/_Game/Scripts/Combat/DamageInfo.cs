using System;
using UnityEngine;

namespace AICompanionRoguelike.Combat
{
    [Serializable]
    public struct DamageInfo
    {
        public float damage;
        public DamageSourceType sourceType;
        public GameObject sourceObject;

        public DamageInfo(float damage, DamageSourceType sourceType, GameObject sourceObject)
        {
            this.damage = Mathf.Max(0f, damage);
            this.sourceType = sourceType;
            this.sourceObject = sourceObject;
        }
    }
}
