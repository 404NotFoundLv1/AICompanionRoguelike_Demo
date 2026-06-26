using System.Text;
using AICompanionRoguelike.Roguelike;
using UnityEngine;

namespace AICompanionRoguelike.UI
{
    public sealed class HomeRunSummaryUI : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private Rect panelRect = new Rect(16f, 16f, 540f, 260f);
        [SerializeField] private bool showPanel = true;

        private readonly StringBuilder rewardBuilder = new StringBuilder(128);

        private void OnGUI()
        {
            if (!showPanel || !RunSessionState.LastSummary.HasSummary)
            {
                return;
            }

            RunSessionSummary summary = RunSessionState.LastSummary;
            GUILayout.BeginArea(panelRect, GUI.skin.box);
            GUILayout.Label("上一局结算");
            GUILayout.Space(4f);
            GUILayout.Label($"结果：{GetEndReasonLabel(summary.EndReason)}");
            GUILayout.Label($"清理房间：{summary.RoomsCleared}    最后房间：#{summary.LastRoomNumber} {summary.LastRoomType}");
            GUILayout.Label(BuildRouteLine(summary));
            GUILayout.Label(BuildRewardLine(summary));
            GUILayout.Label(BuildRelationshipLine(summary));
            GUILayout.Label(BuildBossLine(summary));
            GUILayout.Label(BuildCompanionFeedbackLine(summary));
            GUILayout.EndArea();
        }

        private string BuildRewardLine(RunSessionSummary summary)
        {
            if (summary.RewardTitles == null || summary.RewardTitles.Length == 0)
            {
                return "选择奖励：无";
            }

            rewardBuilder.Clear();
            rewardBuilder.Append("选择奖励：");

            for (int i = 0; i < summary.RewardTitles.Length; i++)
            {
                if (i > 0)
                {
                    rewardBuilder.Append(" / ");
                }

                rewardBuilder.Append(summary.RewardTitles[i]);
            }

            return rewardBuilder.ToString();
        }

        private static string BuildRouteLine(RunSessionSummary summary)
        {
            return summary.HasRoutePath ? summary.RoutePathLabel : "Route: none";
        }

        private static string BuildRelationshipLine(RunSessionSummary summary)
        {
            if (!summary.HasRelationship)
            {
                return "AI 关系：未记录";
            }

            return $"AI 关系：信赖 {summary.FinalTrust}    好感 {summary.FinalAffection}";
        }

        private static string BuildBossLine(RunSessionSummary summary)
        {
            return $"Boss AI Stats: shield {summary.BossSupportActivations}, warning hit {summary.BossWarningHits}, dodge {summary.BossWarningDodges}";
        }

        private static string BuildCompanionFeedbackLine(RunSessionSummary summary)
        {
            if (!summary.HasCompanionFeedback)
            {
                return "AI Feedback: none";
            }

            return $"{summary.CompanionFeedbackLine}  Bond {summary.CompanionTrustDelta:+#;-#;0}/{summary.CompanionAffectionDelta:+#;-#;0}";
        }

        private static string GetEndReasonLabel(RunEndReason reason)
        {
            switch (reason)
            {
                case RunEndReason.Victory:
                    return "通关";
                case RunEndReason.PlayerDeath:
                    return "玩家死亡";
                case RunEndReason.BranchLeave:
                    return "分歧房离开";
                case RunEndReason.ManualReturnHome:
                    return "返回家园";
                default:
                    return reason.ToString();
            }
        }
    }
}
