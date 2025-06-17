
namespace projectTracker.Infrastructure.Risk.Evaluators
{
    public class OverdueTasksEvaluator
    {
       
        public double Evaluate(int overdueTasksCount)
        {
            return overdueTasksCount switch
            {
                0 => 0.1,  // No overdue tasks, very low risk
                >= 1 and <= 2 => 0.4, // A few overdue tasks, medium risk
                >= 3 and <= 5 => 0.7, // Several overdue tasks, high risk
                _ => 1.0   // Many overdue tasks, critical risk
            };
        }
    }
}