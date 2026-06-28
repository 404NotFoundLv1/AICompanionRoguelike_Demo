using System;

namespace AICompanionRoguelike.Roguelike
{
    public readonly struct RunSessionSummary
    {
        public RunSessionSummary(
            int runId,
            RunEndReason endReason,
            int roomsCleared,
            int lastRoomNumber,
            RoomType lastRoomType,
            string[] rewardTitles,
            int finalTrust,
            int finalAffection)
            : this(
                runId,
                endReason,
                roomsCleared,
                lastRoomNumber,
                lastRoomType,
                rewardTitles,
                finalTrust,
                finalAffection,
                string.Empty,
                0,
                0,
                0,
                0,
                0,
                Array.Empty<RoomType>(),
                Array.Empty<RoomModifierType>())
        {
        }

        public RunSessionSummary(
            int runId,
            RunEndReason endReason,
            int roomsCleared,
            int lastRoomNumber,
            RoomType lastRoomType,
            string[] rewardTitles,
            int finalTrust,
            int finalAffection,
            string companionFeedbackLine,
            int companionTrustDelta,
            int companionAffectionDelta,
            int bossSupportActivations,
            int bossWarningHits,
            int bossWarningDodges)
            : this(
                runId,
                endReason,
                roomsCleared,
                lastRoomNumber,
                lastRoomType,
                rewardTitles,
                finalTrust,
                finalAffection,
                companionFeedbackLine,
                companionTrustDelta,
                companionAffectionDelta,
                bossSupportActivations,
                bossWarningHits,
                bossWarningDodges,
                Array.Empty<RoomType>(),
                Array.Empty<RoomModifierType>())
        {
        }

        public RunSessionSummary(
            int runId,
            RunEndReason endReason,
            int roomsCleared,
            int lastRoomNumber,
            RoomType lastRoomType,
            string[] rewardTitles,
            int finalTrust,
            int finalAffection,
            string companionFeedbackLine,
            int companionTrustDelta,
            int companionAffectionDelta,
            int bossSupportActivations,
            int bossWarningHits,
            int bossWarningDodges,
            RoomType[] routePath)
            : this(
                runId,
                endReason,
                roomsCleared,
                lastRoomNumber,
                lastRoomType,
                rewardTitles,
                finalTrust,
                finalAffection,
                companionFeedbackLine,
                companionTrustDelta,
                companionAffectionDelta,
                bossSupportActivations,
                bossWarningHits,
                bossWarningDodges,
                routePath,
                Array.Empty<RoomModifierType>())
        {
        }

        public RunSessionSummary(
            int runId,
            RunEndReason endReason,
            int roomsCleared,
            int lastRoomNumber,
            RoomType lastRoomType,
            string[] rewardTitles,
            int finalTrust,
            int finalAffection,
            string companionFeedbackLine,
            int companionTrustDelta,
            int companionAffectionDelta,
            int bossSupportActivations,
            int bossWarningHits,
            int bossWarningDodges,
            RoomType[] routePath,
            RoomModifierType[] routeModifiers)
            : this(
                runId,
                endReason,
                roomsCleared,
                lastRoomNumber,
                lastRoomType,
                rewardTitles,
                finalTrust,
                finalAffection,
                companionFeedbackLine,
                companionTrustDelta,
                companionAffectionDelta,
                bossSupportActivations,
                bossWarningHits,
                bossWarningDodges,
                routePath,
                routeModifiers,
                string.Empty,
                string.Empty,
                0)
        {
        }

        public RunSessionSummary(
            int runId,
            RunEndReason endReason,
            int roomsCleared,
            int lastRoomNumber,
            RoomType lastRoomType,
            string[] rewardTitles,
            int finalTrust,
            int finalAffection,
            string companionFeedbackLine,
            int companionTrustDelta,
            int companionAffectionDelta,
            int bossSupportActivations,
            int bossWarningHits,
            int bossWarningDodges,
            RoomType[] routePath,
            RoomModifierType[] routeModifiers,
            string growthRouteLabel,
            string growthRouteEffectLabel,
            int growthRouteSpecializationCount)
            : this(
                runId,
                endReason,
                roomsCleared,
                lastRoomNumber,
                lastRoomType,
                rewardTitles,
                finalTrust,
                finalAffection,
                companionFeedbackLine,
                companionTrustDelta,
                companionAffectionDelta,
                bossSupportActivations,
                bossWarningHits,
                bossWarningDodges,
                routePath,
                routeModifiers,
                growthRouteLabel,
                growthRouteEffectLabel,
                growthRouteSpecializationCount,
                0,
                0,
                0)
        {
        }

        public RunSessionSummary(
            int runId,
            RunEndReason endReason,
            int roomsCleared,
            int lastRoomNumber,
            RoomType lastRoomType,
            string[] rewardTitles,
            int finalTrust,
            int finalAffection,
            string companionFeedbackLine,
            int companionTrustDelta,
            int companionAffectionDelta,
            int bossSupportActivations,
            int bossWarningHits,
            int bossWarningDodges,
            RoomType[] routePath,
            RoomModifierType[] routeModifiers,
            string growthRouteLabel,
            string growthRouteEffectLabel,
            int growthRouteSpecializationCount,
            int growthRouteLevel,
            int metaFragmentsEarned,
            int metaFragmentsTotal)
        {
            RunId = runId;
            EndReason = endReason;
            RoomsCleared = roomsCleared;
            LastRoomNumber = lastRoomNumber;
            LastRoomType = lastRoomType;
            RoutePath = routePath ?? Array.Empty<RoomType>();
            RouteModifiers = routeModifiers ?? Array.Empty<RoomModifierType>();
            RewardTitles = rewardTitles ?? Array.Empty<string>();
            FinalTrust = finalTrust;
            FinalAffection = finalAffection;
            CompanionFeedbackLine = companionFeedbackLine ?? string.Empty;
            CompanionTrustDelta = companionTrustDelta;
            CompanionAffectionDelta = companionAffectionDelta;
            BossSupportActivations = Math.Max(0, bossSupportActivations);
            BossWarningHits = Math.Max(0, bossWarningHits);
            BossWarningDodges = Math.Max(0, bossWarningDodges);
            GrowthRouteLabel = growthRouteLabel ?? string.Empty;
            GrowthRouteEffectLabel = growthRouteEffectLabel ?? string.Empty;
            GrowthRouteSpecializationCount = Math.Max(0, growthRouteSpecializationCount);
            GrowthRouteLevel = Math.Max(0, growthRouteLevel);
            MetaFragmentsEarned = Math.Max(0, metaFragmentsEarned);
            MetaFragmentsTotal = Math.Max(0, metaFragmentsTotal);
        }

        public static RunSessionSummary Empty =>
            new RunSessionSummary(0, RunEndReason.None, 0, 0, RoomType.BattleRoom, Array.Empty<string>(), -1, -1);

        public int RunId { get; }
        public RunEndReason EndReason { get; }
        public int RoomsCleared { get; }
        public int LastRoomNumber { get; }
        public RoomType LastRoomType { get; }
        public RoomType[] RoutePath { get; }
        public RoomModifierType[] RouteModifiers { get; }
        public string[] RewardTitles { get; }
        public int FinalTrust { get; }
        public int FinalAffection { get; }
        public string CompanionFeedbackLine { get; }
        public int CompanionTrustDelta { get; }
        public int CompanionAffectionDelta { get; }
        public int BossSupportActivations { get; }
        public int BossWarningHits { get; }
        public int BossWarningDodges { get; }
        public string GrowthRouteLabel { get; }
        public string GrowthRouteEffectLabel { get; }
        public int GrowthRouteSpecializationCount { get; }
        public int GrowthRouteLevel { get; }
        public int MetaFragmentsEarned { get; }
        public int MetaFragmentsTotal { get; }
        public bool HasSummary => RunId > 0 && EndReason != RunEndReason.None;
        public bool HasRelationship => FinalTrust >= 0 && FinalAffection >= 0;
        public bool HasCompanionFeedback => !string.IsNullOrEmpty(CompanionFeedbackLine);
        public bool HasRoutePath => RoutePath.Length > 0;
        public bool HasRouteModifiers => RouteModifiers.Length > 0;
        public bool HasGrowthRouteSummary => !string.IsNullOrEmpty(GrowthRouteLabel);
        public string GrowthRouteSummaryLine => HasGrowthRouteSummary
            ? $"Growth Route: {GrowthRouteLabel} | {GrowthRouteEffectLabel} | Special x{GrowthRouteSpecializationCount}"
            : "Growth Route: none";
        public string MetaProgressionSummaryLine =>
            $"Core Fragments: +{MetaFragmentsEarned} | Total {MetaFragmentsTotal}";

        public string RoutePathLabel
        {
            get
            {
                if (RoutePath.Length == 0)
                {
                    return "Route: none";
                }

                string[] labels = new string[RoutePath.Length];
                for (int i = 0; i < RoutePath.Length; i++)
                {
                    RoomModifierType modifier = i < RouteModifiers.Length
                        ? RouteModifiers[i]
                        : RoomModifierType.None;
                    labels[i] = RoomModifierRules.FormatRoomWithModifier(GetRoomLabel(RoutePath[i]), modifier);
                }

                return $"Route: {string.Join(" -> ", labels)}";
            }
        }

        private static string GetRoomLabel(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.BattleRoom => "Battle",
                RoomType.EliteRoom => "Elite",
                RoomType.SafeRoom => "Safe",
                RoomType.ShopRoom => "Supply",
                RoomType.BossRoom => "Boss",
                RoomType.BranchEventRoom => "Branch",
                _ => roomType.ToString()
            };
        }
    }
}
