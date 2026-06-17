using System;
using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Roguelike;
using UnityEngine;

namespace AICompanionRoguelike.Memory
{
    [RequireComponent(typeof(HealthComponent))]
    public sealed class BondRescueSystem : MonoBehaviour, IDamageModifier
    {
        [Header("References")]
        [SerializeField] private CompanionRelationship companionRelationship;
        [SerializeField] private RunManager runManager;

        [Header("Rules")]
        [SerializeField, Range(0, 100)] private int requiredTrust = 0;
        [SerializeField, Min(1f)] private float rescueHealth = 1f;
        [SerializeField] private bool enterBranchEventRoomOnRescue = true;
        [SerializeField] private bool logRescue = true;

        private bool rescueUsedThisRun;

        public static event Action<BondRescueSystem, HealthComponent, DamageInfo> RescueTriggered;
        public event Action<BondRescueSystem, HealthComponent, DamageInfo> LocalRescueTriggered;

        public bool RescueUsedThisRun => rescueUsedThisRun;
        public int RequiredTrust => requiredTrust;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            RunManager.AnyRunStarted += HandleRunStarted;
        }

        private void Start()
        {
            ResolveReferences();
        }

        private void OnDisable()
        {
            RunManager.AnyRunStarted -= HandleRunStarted;
        }

        private void OnValidate()
        {
            rescueHealth = Mathf.Max(1f, rescueHealth);
        }

        public void ResetRescueAvailability()
        {
            rescueUsedThisRun = false;
        }

        public DamageInfo ModifyIncomingDamage(HealthComponent target, DamageInfo damageInfo)
        {
            if (target == null || damageInfo.damage <= 0f || !WouldBeFatal(target, damageInfo.damage))
            {
                return damageInfo;
            }

            ResolveReferences();

            if (!CanRescue())
            {
                return damageInfo;
            }

            rescueUsedThisRun = true;
            float appliedDamage = Mathf.Max(0f, target.CurrentHealth - rescueHealth);
            damageInfo.damage = appliedDamage;

            LocalRescueTriggered?.Invoke(this, target, damageInfo);
            RescueTriggered?.Invoke(this, target, damageInfo);

            if (logRescue)
            {
                Debug.Log($"Bond rescue triggered. Trust={companionRelationship.Trust}, player HP will be held at {rescueHealth}.", this);
            }

            if (enterBranchEventRoomOnRescue && runManager != null)
            {
                runManager.EnterBranchEventRoom();
            }

            return damageInfo;
        }

        private void ResolveReferences()
        {
            if (companionRelationship == null)
            {
                companionRelationship = FindAnyObjectByType<CompanionRelationship>();
            }

            if (runManager == null)
            {
                runManager = FindAnyObjectByType<RunManager>();
            }
        }

        private bool CanRescue()
        {
            return !rescueUsedThisRun
                && companionRelationship != null
                && companionRelationship.Trust >= requiredTrust;
        }

        private static bool WouldBeFatal(HealthComponent target, float damage)
        {
            return target.CurrentHealth - damage <= 0f;
        }

        private void HandleRunStarted(RunManager startedRunManager)
        {
            if (runManager == null || startedRunManager == runManager)
            {
                runManager = startedRunManager;
                ResetRescueAvailability();
            }
        }
    }
}
