using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace projectTracker.Infrastructure.Risk.Evaluators
{
    public class ActivityEvaluator
    {
        public double Evaluate(int recentUpdates)
        {
            return recentUpdates switch
            {
                >= 10 => 0.1,
                >= 5 => 0.4,
                >= 1 => 0.7,
                _ => 1.0
            };
        }
    }

}
