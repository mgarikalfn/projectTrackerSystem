using projectTracker.Application.Dto;
using projectTracker.Domain.Enums;
using projectTracker.Domain.ValueObjects;
using projectTracker.Infrastructure.Risk.Evaluators;

public class CurrentProjectRiskCalculator
{
    public ProjectHealth Calculate(ProgressMetricsDto metrics)
    {
        var completionRisk = new CompletionRiskEvaluator()
            .Evaluate(metrics.CompletedTasks, metrics.TotalTasks);

        var burnRateRisk = new BurnRateEvaluator()
            .Evaluate(metrics.CompletedStoryPoints, metrics.TotalStoryPoints);

        var blockerRisk = new BlockerEvaluator()
            .Evaluate(metrics.ActiveBlockers);

        var activityRisk = new ActivityEvaluator()
            .Evaluate(metrics.RecentUpdates);

        var compositeScore = (completionRisk * 0.4) +
                             (burnRateRisk * 0.3) +
                             (blockerRisk * 0.2) +
                             (activityRisk * 0.1);

        var level = compositeScore switch
        {
            <= 0.4 => HealthLevel.OnTrack,        // Projects with composite score up to 0.4 are On Track
            <= 0.7 => HealthLevel.NeedAttension, // Projects with composite score between 0.4 and 0.7 need attention
            _ => HealthLevel.Critical             // Projects with composite score above 0.7 are Critical
        };

        return new ProjectHealth
        {
            Level = level,
            Reason = GetPrimaryRiskIndicator(completionRisk, burnRateRisk, blockerRisk),
            Score = compositeScore,
            Confidence = CalculateConfidence(metrics)
        };
    }

    private string GetPrimaryRiskIndicator(double completion, double burn, double blocker)
    {
        if (blocker >= completion && blocker >= burn)
            return "Blockers present";
        if (burn >= completion)
            return "Low story point progress";
        return "Low task completion";
    }

    private string CalculateConfidence(ProgressMetricsDto metrics)
    {
        if (metrics.TotalTasks < 5 || metrics.TotalStoryPoints < 5)
            return "Low";
        if (metrics.RecentUpdates < 2)
            return "Medium";
        return "High";
    }
}
