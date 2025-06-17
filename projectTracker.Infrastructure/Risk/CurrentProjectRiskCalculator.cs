// projectTracker.Infrastructure/Risk/CurrentProjectRiskCalculator.cs

using projectTracker.Application.Dto;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Enums;
using projectTracker.Domain.ValueObjects;
using projectTracker.Infrastructure.Risk.Evaluators;
using System;

namespace projectTracker.Infrastructure.Risk
{
    public class CurrentProjectRiskCalculator : IRiskCalculatorService // Implement the interface
    {
        // Evaluator instances
        private readonly CompletionRiskEvaluator _completionEvaluator;
        private readonly BurnRateEvaluator _burnRateEvaluator;
        private readonly BlockerEvaluator _blockerEvaluator;
        private readonly ActivityEvaluator _activityEvaluator;
        private readonly OverdueTasksEvaluator _overdueTasksEvaluator; // New evaluator

        public CurrentProjectRiskCalculator()
        {
            _completionEvaluator = new CompletionRiskEvaluator();
            _burnRateEvaluator = new BurnRateEvaluator();
            _blockerEvaluator = new BlockerEvaluator();
            _activityEvaluator = new ActivityEvaluator();
            _overdueTasksEvaluator = new OverdueTasksEvaluator(); // Initialize new evaluator
        }

        public ProjectHealth Calculate(ProgressMetricsDto metrics)
        {
            // Calculate individual risk scores
            var completionRisk = _completionEvaluator
                .Evaluate(metrics.CompletedTasks, metrics.TotalTasks);

            var burnRateRisk = _burnRateEvaluator
                .Evaluate(metrics.CompletedStoryPoints, metrics.TotalStoryPoints);

            var blockerRisk = _blockerEvaluator
                .Evaluate(metrics.ActiveBlockers);

            var activityRisk = _activityEvaluator
                .Evaluate(metrics.RecentUpdates);

            var overdueRisk = _overdueTasksEvaluator // Use new evaluator
                .Evaluate(metrics.OverdueTasks); // ProgressMetricsDto needs OverdueTasks


            // Composite score with adjusted weights to include overdue risk
            // Weights should sum to 1.0 (or be scaled later)
            var compositeScore = (completionRisk * 0.3) +    // Reduced from 0.4
                                 (burnRateRisk * 0.25) +   // Reduced from 0.3
                                 (blockerRisk * 0.2) +     // Same
                                 (activityRisk * 0.15) +   // Increased from 0.1
                                 (overdueRisk * 0.1);      // New weight


            var level = compositeScore switch
            {
                <= 0.3 => HealthLevel.OnTrack,       // Adjust thresholds
                <= 0.6 => HealthLevel.NeedAttension,
                _ => HealthLevel.Critical
            };

            return new ProjectHealth // Assuming ProjectHealth is now settable via public setters
            {
                Level = level,
                Reason = GetPrimaryRiskIndicator(completionRisk, burnRateRisk, blockerRisk, overdueRisk), // Include overdue risk
                Score = compositeScore,
                Confidence = CalculateConfidence(metrics)
            };
        }

        private string GetPrimaryRiskIndicator(double completion, double burn, double blocker, double overdue)
        {
            // Prioritize higher risks for the primary indicator
            if (overdue >= 0.7) return "Numerous overdue tasks";
            if (blocker >= 0.7) return "Significant active blockers";
            if (burn >= 0.7) return "Lagging story point progress";
            if (completion >= 0.7) return "Low task completion rate";
            if (overdue >= 0.4) return "Some overdue tasks";
            if (blocker >= 0.4) return "Active blockers present";
            if (burn >= 0.4) return "Moderate story point progress issues";
            return "Good progress"; // Default if no major risks
        }

        private string CalculateConfidence(ProgressMetricsDto metrics)
        {
            // Confidence should reflect data completeness and recency
            // Consider more data points like number of issues with estimates, etc.
            if (metrics.TotalTasks < 5 || metrics.TotalStoryPoints < 5 || metrics.RecentUpdates == 0)
                return "Low";
            if (metrics.RecentUpdates < 2 && metrics.TotalTasks < 10)
                return "Medium";
            return "High";
        }
    }
}