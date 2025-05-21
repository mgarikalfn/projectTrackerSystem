using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace projectTracker.Infrastructure.Risk.Evaluators
{
    public class CompletionRiskEvaluator
    {
        public double Evaluate(int completed, int total)
        {
            if (total == 0) return 1.0; // unknown = high risk
            var ratio = completed / (double)total;

            return ratio switch
            {
                >= 0.8 => 0.1,  // Low risk
                >= 0.5 => 0.4,
                >= 0.3 => 0.7,
                _ => 1.0        // Critical
            };
        }
    }

}
