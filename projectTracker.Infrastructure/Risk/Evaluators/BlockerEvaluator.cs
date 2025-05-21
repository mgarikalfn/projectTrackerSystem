using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace projectTracker.Infrastructure.Risk.Evaluators
{
    public class BlockerEvaluator
    {
        public double Evaluate(int blockers)
        {
            return blockers switch
            {
                0 => 0.1,
                <= 2 => 0.4,
                <= 5 => 0.7,
                _ => 1.0
            };
        }
    }

}
